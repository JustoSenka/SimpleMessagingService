using Messaging.Common.Utilities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging.Common
{
    public class PersistentTcpClient<T> : IDisposable
    {
        public event Action Disconnected;
        public event Action Connected;
        public event Action<string> InvalidMessageReceived;
        public event Action CannotConnect;
        public event Action Connecting;
        public event Action<T> MessageReceived;

        public bool AutoReconnect { get; set; }
        public bool IsDisposed { get; private set; }
        public bool IsDisconnecting { get; private set; }
        public bool IsConnecting { get; private set; }
        public TcpClient Client { get; private set; }
        public NetworkStream Stream { get; private set; }

        private Thread m_Thread;
        private readonly object DisconnectingLock = new object();
        private readonly object ConnectingLock = new object();

        private const int m_DefaultReconnectDelay = 500;
        private const int m_MaxReconnectDelay = 300000; // 5 minutes
        private int m_ReconnectDelay = m_DefaultReconnectDelay;

        private string m_Host;
        private int m_Port;

        public PersistentTcpClient()
        {
            Client = new TcpClient();
        }

        public PersistentTcpClient(TcpClient tcpClient)
        {
            Client = tcpClient;
            Stream = tcpClient.GetStream();
            ListenForMessages();
        }

        public async Task AutoConnectAsync(string hostname, int port)
        {
            lock (ConnectingLock)
            {
                if (IsConnecting)
                    return;

                IsConnecting = true;
            }

            m_Host = hostname;
            m_Port = port;

            while (!IsDisposed && !Client.Connected)
            {
                try
                {
                    Connecting?.Invoke();

                    await Client.ConnectAsync(m_Host, m_Port);
                    Stream = Client.GetStream();
                }
                catch (SocketException)
                {
                    CannotConnect?.Invoke();

                    if (AutoReconnect)
                        await DelayUntilNextConnectionAttempt();
                    else
                        break;
                }
            }

            IsConnecting = false;
            m_ReconnectDelay = m_DefaultReconnectDelay;
            Connected?.Invoke();
            ListenForMessages();
        }

        public void ListenForMessages()
        {
            m_Thread = new Thread(async () =>
            {
                while (!IsDisposed)
                {
                    try
                    {
                        var msg = await ReadNextMessage();
                        MessageReceived?.Invoke(msg);
                    }
                    catch (SocketException)
                    {
                        await OnDisconnected();
                        return;
                    }
                    catch (InvalidOperationException)
                    {
                        await OnDisconnected();
                        return;
                    }
                    catch (IOException)
                    {
                        await OnDisconnected();
                        return;
                    }
                    catch (InvalidMessageException e)
                    {
                        // TODO: clean the stream somehow
                        InvalidMessageReceived?.Invoke(e.Message);

                        // As i'm not fixing the stream with possibly leftover data, just disconnect and reconnect
                        await OnDisconnected();
                        return;
                    }
                }
            });

            m_Thread.Start();
        }

        private async Task<T> ReadNextMessage()
        {
            int msgLength = GetMessageLength();

            var array = new byte[msgLength];
            var bytesRead = await Stream.ReadAsync(array.AsMemory());

            if (bytesRead == 0)
                throw new SocketException();

            if (bytesRead != msgLength)
            {
                // var diff = msgLength - bytesRead;
                // await Stream.ReadAsync(new byte[diff], 0, diff);
                throw new InvalidMessageException($"Invalid message length, expected {msgLength} bytes, got {bytesRead}");
            }

            return JsonConvert.DeserializeObject<T>(array.ToStringUTF8());
        }

        public async Task Send(T message)
        {
            var bytes = JsonConvert.SerializeObject(message).ToBytesUTF8();
            var msgLength = bytes.Length;

            var byte1 = (byte)(msgLength >> 8);
            var byte2 = (byte)(msgLength % 0xff);

            // Writing bytes in big-endien, bigger byte first
            Stream.WriteByte(byte1);
            Stream.WriteByte(byte2);

            await Stream.WriteAsync(bytes.AsMemory());
        }

        private int GetMessageLength()
        {
            var byte1 = Stream.ReadByte();
            var byte2 = Stream.ReadByte();

            // Assuming the messages is encoded in big-endian
            return byte1 * 256 + byte2;
        }

        private async Task OnDisconnected()
        {
            // Try diconnect, make sure to do it only once
            lock (DisconnectingLock)
            {
                if (IsDisconnecting)
                    return;

                IsDisconnecting = true;
            }

            Disconnected?.Invoke();
            Dispose();
            // m_Thread.Join();

            IsDisconnecting = false;

            // continue to reconnect
            if (AutoReconnect)
            {
                Client = new TcpClient();
                IsDisposed = false;
                await AutoConnectAsync(m_Host, m_Port);
            }
        }

        private async Task DelayUntilNextConnectionAttempt()
        {
            await Task.Delay(m_ReconnectDelay);
            m_ReconnectDelay = Math.Min(m_ReconnectDelay * 2, m_MaxReconnectDelay);
        }

        public void Dispose()
        {
            IsDisposed = true;

            try
            {
                Stream.Dispose();
            }
            catch (Exception) { }

            try
            {
                Client.Dispose();
            }
            catch (Exception) { }
        }

        public bool IsConnected() => Client.Connected;
    }
}
