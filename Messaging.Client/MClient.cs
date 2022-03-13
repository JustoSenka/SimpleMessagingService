using Messaging.Client.Utilities;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging.Client
{
    public class MClient : IDisposable
    {
        public const int DefaultPort = 13000;
        public const string DefaultAddress = "127.0.0.1";

        private readonly string m_Address;
        private readonly int m_Port;

        private TcpClient m_TcpClient;
        private NetworkStream m_Stream;

        public event Action Disconnected;

        public bool IsDisposing { get; private set; }

        public MClient(string address = DefaultAddress, int port = DefaultPort)
        {
            m_Address = address;
            m_Port = port;
            m_TcpClient = new TcpClient();
        }

        public async Task Connect()
        {
            try
            {
                await m_TcpClient.ConnectAsync(m_Address, m_Port);
                var endPoint = m_TcpClient.Client.RemoteEndPoint;
                m_Stream = m_TcpClient.GetStream();

                Logger.LogClient("Connected to: {0}", endPoint);

                ListenForMessages();
            }
            catch (SocketException e)
            {
                Logger.LogClient("Unable to connect to host {0}:{1}: {2}", m_Address, m_Port, e);
            }
        }

        public async Task Send(string message)
        {
            if (!IsConnected())
            {
                Logger.LogClient("Client not connected");
                return;
            }

            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                var data = bytes.AsMemory();
                await m_Stream.WriteAsync(data);
            }
            catch (InvalidOperationException e)
            {
                Logger.LogClient("Failed to send a message: {0}", e);
            }
            catch (IOException e)
            {
                Logger.LogClient("Failed to send a message: {0}", e);
            }
        }

        private void ListenForMessages()
        {
            var thread = new Thread(async () =>
            {
                try
                {
                    var array = new byte[256];
                    var buffer = array.AsMemory();
                    int bytesRead;

                    while ((bytesRead = await m_Stream.ReadAsync(buffer)) != 0)
                    {
                        var data = System.Text.Encoding.UTF8.GetString(array, 0, bytesRead);
                        Logger.LogServer(data);
                    }
                }
                catch (Exception) { } // Do nothing, will close connection below

                Stop();
            });

            thread.Start();
        }

        public void Stop()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;

            try
            {
                m_Stream.Close();
                m_Stream.Dispose();
            }
            catch (Exception) { }

            try
            {
                m_TcpClient.Close();
                m_TcpClient.Dispose();
            }
            catch (Exception) { }

            Disconnected?.Invoke();
            Logger.LogClient("Connection closed.");
        }

        public bool IsConnected() => m_TcpClient.Connected;

        public void Dispose()
        {
            Stop();
        }
    }
}
