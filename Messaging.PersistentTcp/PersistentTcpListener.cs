using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging.PersistentTcp
{
    public class PersistentTcpListener<TMessage> : IDisposable where TMessage : Message
    {
        public event Action<long> Connected;
        public event Action<ClientInfo<TMessage>> Disconnected;
        public event Action<long, string> InvalidMessageReceived;
        public event Action<long, TMessage> MessageReceived;
        
        public event Action<Exception> ErrorWhileConnecting;

        public const int DefaultPort = 13000;

        public bool IsDisposing { get; private set; }

        private TcpListener m_TcpListener;

        private readonly int m_Port;
        private readonly IPAddress m_LocalAddr = IPAddress.Parse("127.0.0.1");

        private Thread m_ListeningThread;

        public ConcurrentDictionary<long, ClientInfo<TMessage>> ConnectedClients => m_ConnectedClients;
        public readonly ConcurrentDictionary<long, ClientInfo<TMessage>> m_ConnectedClients = new ConcurrentDictionary<long, ClientInfo<TMessage>>();

        private long m_IdCount = 1;

        public PersistentTcpListener(int port = DefaultPort)
        {
            m_Port = port;
        }

        public void Start()
        {
            m_TcpListener = new TcpListener(m_LocalAddr, m_Port);
            m_TcpListener.Start();

            m_ListeningThread = new Thread(async () =>
            {
                while (!IsDisposing)
                {
                    try
                    {
                        var tcp = await m_TcpListener.AcceptTcpClientAsync();
                        var tcpClient = new PersistentTcpClient<TMessage>(tcp);
                        var ip = tcpClient.Client.Client.RemoteEndPoint.ToString();
                        var id = m_IdCount;

                        var client = new ClientInfo<TMessage>()
                        {
                            id = id,
                            client = tcpClient,
                            endpoint = ip,
                        };

                        tcpClient.MessageReceived += msg => MessageReceived?.Invoke(id, msg);
                        tcpClient.InvalidMessageReceived += msg => InvalidMessageReceived?.Invoke(id, msg);
                        
                        tcpClient.Disconnected += () => CloseClientConnection(id);
                        
                        ConnectedClients[id] = client;
                        Connected?.Invoke(id);

                        m_IdCount++;
                    }
                    catch (InvalidOperationException ioe)
                    {
                        ErrorWhileConnecting?.Invoke(ioe);
                    }
                    catch (SocketException se)
                    {
                        ErrorWhileConnecting?.Invoke(se);
                    }
                }
            });

            m_ListeningThread.Start();
        }

        public void CloseClientConnection(long id)
        {
            if (ConnectedClients.TryRemove(id, out var client))
            {
                try
                {
                    client.client.Dispose();
                    client.client.Stream.Dispose();
                }
                catch (Exception) { }

                Disconnected?.Invoke(client);
            }

            // Do nothing if it's already closed.
        }

        public async Task Send(long clientID, TMessage message)
        {
            if (!IsConnected(clientID))
                throw new InvalidOperationException($"Client not connected: {clientID}");

            var client = ConnectedClients[clientID];
            await client.client.Send(message);
        }

        public void CloseAllClientConnetcions()
        {
            var ids = ConnectedClients.Keys.ToArray();
            foreach (var id in ids)
                CloseClientConnection(id);
        }

        public bool IsConnected(long id) => ConnectedClients.ContainsKey(id) && ConnectedClients[id].client.IsConnected();

        public void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;

            try
            {
                m_TcpListener.Stop();
                m_ListeningThread.Join();
            }
            catch (Exception) { }

            CloseAllClientConnetcions();

            IsDisposing = false;
        }
    }
}
