using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging.Client
{
    public class MClient
    {
        public const int DefaultPort = 13000;
        public const string DefaultAddress = "127.0.0.1";

        private readonly string m_Address;
        private readonly int m_Port;

        private TcpClient m_TcpClient;
        private NetworkStream m_Stream;

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

                Console.WriteLine("Connected to: {0}", endPoint);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Unable to connect to host {0}:{1}: {2}", m_Address, m_Port, e);
            }
        }

        public async Task Send(string message)
        {
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                var data = bytes.AsMemory();
                await m_Stream.WriteAsync(data);

                Console.WriteLine("Sent: {0}", message);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Failed to send a message: {0}", e);
            }
            catch (IOException e)
            {
                Console.WriteLine("Failed to send a message: {0}", e);
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

                    while ((bytesRead = await client.stream.ReadAsync(buffer)) != 0)
                    {
                        var data = System.Text.Encoding.UTF8.GetString(array, 0, bytesRead);
                        Logger.LogClient(client, data);
                    }
                }
                catch (InvalidOperationException)
                {
                    Logger.LogClient(client, "Connection closed. {0}");
                }
                catch (IOException)
                {
                    Logger.LogClient(client, "Connection closed. {0}");
                }

                CloseClient(client.id);
            });

            thread.Start();
            m_Threads[client.id] = thread;
        }

        public void Stop()
        {
            try
            {
                m_Stream.Close();
                m_TcpClient.Close();
            }
            catch (Exception) { }
        }
    }
}
