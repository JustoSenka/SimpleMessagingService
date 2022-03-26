using Messaging.Client;
using Messaging.Common;
using Messaging.PersistentTcp;
using Messaging.PersistentTcp.Utilities;
using Messaging.Server;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging.Tests
{
    public class MessagingTests
    {
        public const int DefaultPort = 13000;
        public const string DefaultAddress = "127.0.0.1";

        private PersistentTcpListener s;
        private PersistentTcpClient c;

        private int m_Timeout = 2000;

        private TaskCompletionSource<(long id, Message msg)> m_LatestMessages;

        private string LatestMessage => m_LatestMessages.Task.IsCompleted ? m_LatestMessages.Task.Result.msg.bytes.ToStringUTF8() : "Task Timeout";
        private long LatestMessageSenderID => m_LatestMessages.Task.IsCompleted ? m_LatestMessages.Task.Result.id : -1;

        private ConcurrentQueue<Message> m_Messages = new ConcurrentQueue<Message>();

        private int m_MessagesSent = 0;

        [SetUp]
        public void Setup()
        {
            s = new PersistentTcpListener(DefaultPort);
            c = new PersistentTcpClient(DefaultAddress, DefaultPort, autoConnect: true);

            s.MessageReceived += S_MessageReceived;
            c.MessageReceived += C_MessageReceived;

            ClearLatestResult();
            m_MessagesSent = 0;
            m_Messages.Clear();
        }

        private async Task WaitForMessageToArrive() => await Task.WhenAny(m_LatestMessages.Task, Task.Delay(m_Timeout));

        private void ClearLatestResult()
        {
            m_LatestMessages = new TaskCompletionSource<(long id, Message msg)>();
        }

        private void S_MessageReceived(long id, Message msg)
        {
            m_MessagesSent++;
            m_Messages.Enqueue(msg);

            ClearLatestResult();
            m_LatestMessages.SetResult((id, msg));
        }

        private void C_MessageReceived(Message msg)
        {
            m_MessagesSent++;
            m_Messages.Enqueue(msg);

            ClearLatestResult();
            m_LatestMessages.SetResult((0, msg));
        }

        [TearDown]
        public void Cleanup()
        {
            s.Dispose();
            c.Dispose();
        }

        [Test]
        public async Task Client_SendTextMessageTo_Server_GetsCorrectMessage()
        {
            s.Start();
            await c.AutoConnectAsync();
            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);

            var msgSent = new Message("test".ToBytesUTF8());

            ClearLatestResult();
            await c.Send(msgSent);
            await WaitForMessageToArrive();

            m_Messages.TryDequeue(out Message msgReceived);

            Assert.AreEqual(msgSent.bytes.ToStringUTF8(), msgReceived.bytes.ToStringUTF8(), "Message text did not match");
            Assert.AreEqual(msgSent.Guid, msgReceived.Guid, "Message guid did not match");
        }

        [Test]
        public async Task Client_SendTextMessageTo_Server()
        {
            s.Start();
            await c.AutoConnectAsync();
            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);

            ClearLatestResult();
            await c.Send(new Message("test".ToBytesUTF8()));
            await WaitForMessageToArrive();

            Assert.AreEqual(LatestMessage, "test", "Server did not receive correct message");
            Assert.AreEqual(LatestMessageSenderID, 1, "Sender id incorrect");
        }

        [Test]
        public async Task Server_SendTextMessageTo_Client()
        {
            s.Start();
            await c.AutoConnectAsync();
            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);

            ClearLatestResult();
            await s.Send(1, new Message("test2".ToBytesUTF8()));
            await WaitForMessageToArrive();

            Assert.AreEqual(LatestMessage, "test2", "Client did not receive correct message");
            Assert.AreEqual(LatestMessageSenderID, 0, "Client id incorrect");
        }

        [Test]
        public async Task Server_SendingMultipleMessages_WorksFine()
        {
            s.Start();
            await c.AutoConnectAsync();
            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);
            
            await s.Send(1, new Message("1".ToBytesUTF8()));
            await s.Send(1, new Message("2".ToBytesUTF8()));
            await s.Send(1, new Message("3".ToBytesUTF8()));
            await s.Send(1, new Message("4".ToBytesUTF8()));
            await s.Send(1, new Message("5".ToBytesUTF8()));

            Sync.WaitFor(() => m_MessagesSent == 5, m_Timeout);

            Assert.AreEqual(5, m_MessagesSent, "Not all messages arrived");
        }

        [Test]
        public async Task Server_SendTextMessageTo_Client_WaitForResponse()
        {
            s.Start();
            await c.AutoConnectAsync();
            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);

            ClearLatestResult();
            var response = await s.SendAndWaitForResponse(1, new Message("test2".ToBytesUTF8()));

            Assert.AreEqual("R", response.bytes.ToStringUTF8(), "Server did not receive correct response");
        }

        [Test]
        public async Task Client_SendTextMessageTo_Server_WaitForResponse()
        {
            s.Start();
            await c.AutoConnectAsync();
            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);

            ClearLatestResult();
            var response = await c.SendAndWaitForResponse(new Message("test2".ToBytesUTF8()));

            Assert.AreEqual("R", response.bytes.ToStringUTF8(), "Server did not receive correct response");
        }
    }
}