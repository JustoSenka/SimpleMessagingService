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
        public event Action<long> Disconnected;
        public bool IsDisposing { get; private set; }


        private TcpListener m_TcpListener;

        private Int32 m_Port = 13000;
        private IPAddress m_LocalAddr = IPAddress.Parse("127.0.0.1");

        private Thread m_ListeningThread;

        private Dictionary<long, ConnectedClient> m_Clients = new Dictionary<long, ConnectedClient>();
        private Dictionary<long, Thread> m_Threads = new Dictionary<long, Thread>();

        private long m_IdCount = 1;
        
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
                        var tcpClient = await m_TcpListener.AcceptTcpClientAsync();
                        var ip = tcpClient.Client.RemoteEndPoint.ToString();

                        var client = new ConnectedClient()
                        {
                            id = m_IdCount,
                            client = tcpClient,
                            stream = tcpClient.GetStream(),
                            endpoint = ip,
                        };

                        Logger.LogClient(client, "Client connected.");

                        m_Clients[m_IdCount] = client;
                        m_IdCount++;

                        ListenForMessages(client);
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

        private void ListenForMessages(ConnectedClient client)
        {
            var thread = new Thread(async () =>
            {
                try
                {
                    var array = new byte[256];
                    var buffer = array.AsMemory();
                    int bytesRead;

                    while ((bytesRead = await client.stream.ReadAsync(buffer)) != 0)
                    {
                        var data = System.Text.Encoding.UTF8.GetString(array, 0, bytesRead);
                        Logger.LogClient(client, data);
                    }
                }
                catch (Exception) { } // Do nothing, below make sure everything is disconnected

                CloseClient(client.id);
            });

            thread.Start();
            m_Threads[client.id] = thread;
        }

        public void CloseClient(long id)
        {
            var client = m_Clients[id];

            try
            {
                client.client.Close();
                client.client.Dispose();
            }
            catch (Exception) { }

            try
            {
                client.stream.Close();
                client.stream.Dispose();
            }
            catch (Exception) { }

            m_Clients.Remove(id);
            m_Threads.Remove(id);

            Disconnected?.Invoke(id);
            Logger.LogClient(client, "Connection closed. {0}", id);
        }

        public async Task Send(long clientID, string message)
        {
            if (!IsConnected(clientID))
            {
                Logger.LogServer("Client not connected: {0}", clientID);
                return;
            }

            var client = m_Clients[clientID];

            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                var data = bytes.AsMemory();
                await client.stream.WriteAsync(data);
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

        public void Stop()
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

        private void CloseAllClientConnetcions()
        {
            var ids = m_Clients.Keys.ToArray();
            for (int i = 0; i < ids.Length; i++)
            {
                CloseClient(ids[i]);
            }
        }

        public bool IsConnected(long id) => m_Clients.ContainsKey(id) && m_Clients[id].client.Connected;

        public void Dispose()
        {
            Stop();
        }
    }
}
