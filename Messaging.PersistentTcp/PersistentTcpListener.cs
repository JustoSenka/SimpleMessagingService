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
    public class PersistentTcpListener : IDisposable
    {
        public event Action<long> Connected;
        public event Action<ClientInfo> Disconnected;
        public event Action<long, string> InvalidMessageReceived;
        public event Action<long, Message> MessageReceived;
        
        public event Action<Exception> ErrorWhileConnecting;

        public bool IsDisposing { get; private set; }

        private TcpListener m_TcpListener;

        private readonly int m_Port;

        private Thread m_ListeningThread;

        public ConcurrentDictionary<long, ClientInfo> ConnectedClients => m_ConnectedClients;
        private readonly ConcurrentDictionary<long, ClientInfo> m_ConnectedClients = new ConcurrentDictionary<long, ClientInfo>();

        private long m_IdCount = 1;

        public PersistentTcpListener(int port)
        {
            m_Port = port;
        }

        public void Start()
        {
            m_TcpListener = new TcpListener(IPAddress.Loopback, m_Port);
            m_TcpListener.Start();

            m_ListeningThread = new Thread(async () =>
            {
                while (!IsDisposing)
                {
                    try
                    {
                        var tcp = await m_TcpListener.AcceptTcpClientAsync();
                        var tcpClient = new PersistentTcpClient(tcp);
                        var ip = tcpClient.Client.Client.RemoteEndPoint.ToString();
                        var id = m_IdCount;

                        var client = new ClientInfo()
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
                    client.client.Stream.Dispose();
                    client.client.Dispose();
                }
                catch (Exception) { }

                Disconnected?.Invoke(client);
            }

            // Do nothing if it's already closed.
        }

        public async Task Send(long clientID, Message message)
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
