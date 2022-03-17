using Messaging.Common;
using Messaging.Server.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging.Server
{
    public class MessagingServer : IDisposable
    {
        public const int DefaultPort = 13000;


        public event Action<long> Disconnected;
        public bool IsDisposing { get; private set; }


        private TcpListener m_TcpListener;

        private readonly int m_Port;
        private readonly IPAddress m_LocalAddr = IPAddress.Parse("127.0.0.1");

        private Thread m_ListeningThread;
        private readonly Dictionary<long, ConnectedClient<Message>> m_Clients = new Dictionary<long, ConnectedClient<Message>>();

        private long m_IdCount = 1;

        public MessagingServer(int port = DefaultPort)
        {
            m_Port = port;
        }

        public void Start()
        {
            m_TcpListener = new TcpListener(m_LocalAddr, m_Port);
            m_TcpListener.Start();
            Logger.LogServer("Starting TCP Server: {0}:{1}", m_LocalAddr, m_Port);

            m_ListeningThread = new Thread(async () =>
            {
                while (!IsDisposing)
                {
                    try
                    {
                        var tcp = await m_TcpListener.AcceptTcpClientAsync();
                        var tcpClient = new PersistentTcpClient<Message>(tcp);
                        var ip = tcpClient.Client.Client.RemoteEndPoint.ToString();

                        var client = new ConnectedClient<Message>()
                        {
                            id = m_IdCount,
                            client = tcpClient,
                            endpoint = ip,
                        };

                        tcpClient.MessageReceived += msg => OnMessageReceived(client, msg);
                        tcpClient.InvalidMessageReceived += msg => OnInvalidMessageReceived(client, msg);
                        tcpClient.Disconnected += () => OnDisconnected(client);
                        tcpClient.Connected += () => OnConnected(client);

                        Logger.LogClient(client, "Client connected.");

                        m_Clients[m_IdCount] = client;
                        m_IdCount++;

                    }
                    catch (InvalidOperationException ioe)
                    {
                        Logger.LogServer("Cannnot establish connection: {0}", ioe);
                    }
                    catch (SocketException se)
                    {
                        Logger.LogServer("Cannnot establish connection: {0}", se);
                    }
                }
            });

            m_ListeningThread.Start();
        }

        private void OnDisconnected(ConnectedClient<Message> client)
        {
            CloseClient(client.id);
        }

        private void OnConnected(ConnectedClient<Message> client)
        {
            Logger.LogClient(client, "Client connected.");
        }

        private void OnInvalidMessageReceived(ConnectedClient<Message> client, string str)
        {
            Logger.LogClient(client, str);
        }

        private void OnMessageReceived(ConnectedClient<Message> client, Message msg)
        {
            Logger.LogClient(client, msg.ToString());
        }

        public void CloseClient(long id)
        {
            var client = m_Clients[id];

            try
            {
                client.client.Dispose();
                client.client.Stream.Dispose();
            }
            catch (Exception) { }

            m_Clients.Remove(id);

            Logger.LogClient(client, "Client disconnected.");
            Disconnected?.Invoke(id);
        }

        public async Task Send(long clientID, Message message)
        {
            if (!IsConnected(clientID))
            {
                Logger.LogServer("Client not connected: {0}", clientID);
                return;
            }

            var client = m_Clients[clientID];

            try
            {
                await client.client.Send(message);
            }
            catch (InvalidOperationException e)
            {
                Logger.LogServer("Failed to send a message: {0}", e);
            }
            catch (IOException e)
            {
                Logger.LogServer("Failed to send a message: {0}", e);
            }
        }

        private void CloseAllClientConnetcions()
        {
            var ids = m_Clients.Keys.ToArray();
            foreach (var id in ids)
                CloseClient(id);
        }

        public bool IsConnected(long id) => m_Clients.ContainsKey(id) && m_Clients[id].client.IsConnected();

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
            Logger.LogServer("Stopping TCP Server.");
        }
    }
}
