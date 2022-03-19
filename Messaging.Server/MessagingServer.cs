using Messaging.Common;
using Messaging.PersistentTcp;
using Messaging.Server.Utilities;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Messaging.Server
{
    public class MessagingServer : IDisposable
    {
        public const int DefaultPort = 13000;

        private readonly int m_Port;
        private readonly IPAddress m_LocalAddr = IPAddress.Parse("127.0.0.1");

        private readonly PersistentTcpListener m_PersistentTcpListener;

        public MessagingServer(int port = DefaultPort)
        {
            m_PersistentTcpListener = new PersistentTcpListener();

            m_PersistentTcpListener.MessageReceived += OnMessageReceived;
            m_PersistentTcpListener.InvalidMessageReceived += OnInvalidMessageReceived;
            m_PersistentTcpListener.Connected += OnConnected;
            m_PersistentTcpListener.Disconnected += OnDisconnected;
            m_PersistentTcpListener.ErrorWhileConnecting += OnErrorWhileConnecting;

            m_Port = port;
        }

        private void OnErrorWhileConnecting(Exception e)
        {
            Logger.LogServer("Cannnot establish connection: {0}", e);
        }

        private void OnDisconnected(ClientInfo clientInfo)
        {
            Logger.LogClient(clientInfo, "Client disconnected.");
        }

        private void OnConnected(long id)
        {
            var client = m_PersistentTcpListener.ConnectedClients[id];
            Logger.LogClient(client, "Client connected.");
        }

        private void OnInvalidMessageReceived(long id, string msg)
        {
            var client = m_PersistentTcpListener.ConnectedClients[id];
            Logger.LogClient(client, msg);
        }

        private void OnMessageReceived(long id, Message msg)
        {
            var client = m_PersistentTcpListener.ConnectedClients[id];
            Logger.LogClient(client, msg.ToString());
        }

        public void Start()
        {
            Logger.LogServer("Starting TCP Server: {0}:{1}", m_LocalAddr, m_Port);
            m_PersistentTcpListener.Start();
        }

        public void CloseClient(long id)
        {
            m_PersistentTcpListener.CloseClientConnection(id);
        }

        public async Task Send(long clientID, MessageCommand message)
        {
            try
            {
                await m_PersistentTcpListener.Send(clientID, message);
            }
            catch (InvalidOperationException e)
            {
                Logger.LogServer("Client not connected: {0}", clientID);
            }
            catch (IOException e)
            {
                Logger.LogServer("Failed to send a message: {0}", e);
            }
        }

        public void Dispose()
        {
            m_PersistentTcpListener.Dispose();
        }
    }
}
