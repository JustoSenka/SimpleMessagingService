using Messaging.Client.Utilities;
using Messaging.Common;
using Messaging.PersistentTcp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Messaging.Client
{
    public class MessagingClient : IDisposable
    {
        public const int DefaultPort = 13000;
        public const string DefaultAddress = "127.0.0.1";

        private readonly string m_Host;
        private readonly int m_Port;

        private readonly PersistentTcpClient m_Client;

        public MessagingClient(string address = DefaultAddress, int port = DefaultPort, bool autoConnect = false)
        {
            m_Host = address;
            m_Port = port;
            m_Client = new PersistentTcpClient(m_Host, m_Port, autoConnect: autoConnect);

            m_Client.CannotConnect += OnCannotConnect;
            m_Client.MessageReceived += OnMessageReceived;
            m_Client.Disconnected += OnDisconnected;
            m_Client.Connected += OnConnected;
            m_Client.InvalidMessageReceived += OnInvalidMessageReceived;
            m_Client.Connecting += OnConnectingToServer;
        }

        private void OnConnectingToServer()
        {
            Logger.LogClient("Trying to connect to host {0}:{1}.", m_Host, m_Port);
        }

        private void OnConnected()
        {
            Logger.LogClient("Connected to server.");
        }

        private void OnDisconnected()
        {
            Logger.LogClient("Lost connection to server.");
        }

        private void OnInvalidMessageReceived(string str)
        {
            Logger.LogClient(str);
        }

        private void OnMessageReceived(Message msg)
        {
            Logger.LogServer(msg.ToString());
        }

        private void OnCannotConnect()
        {
            Logger.LogClient("Unable to connect to host {0}:{1}.", m_Host, m_Port);
        }

        public async Task Connect()
        {
            await m_Client.AutoConnectAsync();
        }

        public async Task Send(MessageCommand message)
        {
            if (!m_Client.IsConnected())
            {
                Logger.LogClient("Client not connected.");
                return;
            }

            try
            {
                await m_Client.Send(message);
            }
            catch (InvalidOperationException)
            {
                Logger.LogClient("Failed to send a message.");
            }
            catch (IOException)
            {
                Logger.LogClient("Failed to send a message.");
            }
        }

        public void Dispose()
        {
            m_Client.Dispose();
        }
    }
}
