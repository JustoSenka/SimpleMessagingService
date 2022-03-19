using Messaging.PersistentTcp.Serializers;
using Messaging.PersistentTcp.Utilities;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging.PersistentTcp
{
    public class PersistentTcpClient : IDisposable
    {
        public event Action Connected;
        public event Action Connecting;
        public event Action CannotConnect;
        public event Action Disconnected;

        public event Action<string> InvalidMessageReceived;
        public event Action<Message> MessageReceived;

        public bool AutoReconnect { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool IsConnecting { get; private set; }
        public TcpClient Client { get; private set; }
        public NetworkStream Stream { get; private set; }

        private Thread m_Thread;
        private readonly object ConnectingLock = new object();

        private const int m_DefaultReconnectDelay = 500;
        private const int m_MaxReconnectDelay = 300000; // 5 minutes
        private int m_ReconnectDelay = m_DefaultReconnectDelay;

        private bool m_IsConnected;

        private readonly string m_Host;
        private readonly int m_Port;

        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> m_Responses = new ConcurrentDictionary<Guid, TaskCompletionSource<Message>>();

        private readonly ISerializer<Message> m_Serializer;

        public PersistentTcpClient(string hostname, int port, bool autoConnect = false)
        {
            Client = new TcpClient();
            m_Serializer = new JsonSerializer<Message>();

            m_Host = hostname;
            m_Port = port;
            AutoReconnect = autoConnect;
        }

        public PersistentTcpClient(TcpClient tcpClient)
        {
            Client = tcpClient;
            Stream = tcpClient.GetStream();
            ListenForMessages();

            m_IsConnected = true;
            m_Serializer = new JsonSerializer<Message>();
        }

        public async Task AutoConnectAsync()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(PersistentTcpClient));

            lock (ConnectingLock)
            {
                if (IsConnecting)
                    return;

                IsConnecting = true;
            }


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
            m_IsConnected = true;

            m_ReconnectDelay = m_DefaultReconnectDelay;
            Connected?.Invoke();
            ListenForMessages();
        }

        private void ListenForMessages()
        {
            m_Thread = new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        var msg = await ReadNextMessage();
                        MessageReceived?.Invoke(msg);
                        CheckMessageIsResponse(msg);
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }
                    catch (IOException)
                    {
                        break;
                    }
                    catch (InvalidMessageException e)
                    {
                        // TODO: clean the stream somehow
                        InvalidMessageReceived?.Invoke(e.Message);

                        // As i'm not fixing the stream with possibly leftover data, just disconnect and reconnect
                        break;
                    }
                }

                Disconnect();
            });

            m_Thread.Start();
        }

        private async Task<Message> ReadNextMessage()
        {
            int msgLength = GetMessageLength();
            if (msgLength == -257)
                throw new SocketException(); // Connection closed from other side

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

            return m_Serializer.Deserialize(array);
        }

        public async Task Send(Message message)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(PersistentTcpClient));

            var bytes = m_Serializer.Serialize(message);
            var msgLength = bytes.Length;

            var byte1 = (byte)(msgLength >> 8);
            var byte2 = (byte)(msgLength % 0xff);

            // Writing bytes in big-endien, bigger byte first
            Stream.WriteByte(byte1);
            Stream.WriteByte(byte2);

            await Stream.WriteAsync(bytes.AsMemory());
        }

        public async Task<Message> SendAndWaitForResponse(Message message, int timeout = 10000, CancellationToken cancellationToken = default)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(PersistentTcpClient));

            var tcs = new TaskCompletionSource<Message>();

            m_Responses[message.Guid] = tcs;

            var timeoutTask = Task.Run(async () =>
            {
                await Task.Delay(timeout);
                return (Message) new Message("Timeout".ToBytesUTF8());
            }, cancellationToken);

            await Send(message);

            var finishedTask = await Task.WhenAny(timeoutTask, tcs.Task);
            return await finishedTask;
        }

        private void CheckMessageIsResponse(Message msg)
        {
            // Removes message guid from response collection
            // Sets task result for anyone waiting for it
            if (m_Responses.TryRemove(msg.Guid, out var taskSource))
            {
                taskSource.SetResult(msg);
            }
        }

        private int GetMessageLength()
        {
            var byte1 = Stream.ReadByte();
            var byte2 = Stream.ReadByte();

            // Assuming the messages is encoded in big-endian
            return byte1 * 256 + byte2;
        }

        private async Task DelayUntilNextConnectionAttempt()
        {
            await Task.Delay(m_ReconnectDelay);
            m_ReconnectDelay = Math.Min(m_ReconnectDelay * 2, m_MaxReconnectDelay);
        }

        public void Disconnect()
        {
            if (!m_IsConnected)
                return;

            m_IsConnected = false;

            try
            {
                Client.Dispose();
                Stream.Dispose();
            }
            catch (Exception) { }

            Disconnected?.Invoke();

            // continue to reconnect
            if (AutoReconnect && !IsDisposed)
            {
                Client = new TcpClient();
                Task.Run(AutoConnectAsync);
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
            Disconnect();
        }

        public bool IsConnected() => m_IsConnected && Client.Connected;
    }
}
