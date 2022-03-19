using Messaging.PersistentTcp;
using Messaging.PersistentTcp.Utilities;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace Messaging.Tests
{
    public class ConnectionTests
    {
        public const int DefaultPort = 13000;
        public const string DefaultAddress = "127.0.0.1";

        private PersistentTcpListener s;
        private PersistentTcpClient c1;
        private PersistentTcpClient c2;
        private PersistentTcpClient c3;

        private int m_Timeout = 2000;

        [SetUp]
        public void Setup()
        {
            s = new PersistentTcpListener(DefaultPort);
            c1 = new PersistentTcpClient(DefaultAddress, DefaultPort, autoConnect: true);
            c2 = new PersistentTcpClient(DefaultAddress, DefaultPort, autoConnect: true);
            c3 = new PersistentTcpClient(DefaultAddress, DefaultPort, autoConnect: false);
        }

        [TearDown]
        public void Cleanup()
        {
            s.Dispose();
            c1.Dispose();
            c2.Dispose();
            c3.Dispose();
        }

        [Test]
        public async Task Client_CanConnectTo_Server()
        {
            s.Start();
            await c1.AutoConnectAsync();

            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);
            Assert.IsTrue(s.ConnectedClients.Count == 1, "Client did not connect to server");
        }

        [Test]
        public async Task MultipleClients_CanConnectTo_Server()
        {
            s.Start();
            await c1.AutoConnectAsync();
            await c2.AutoConnectAsync();
            await c3.AutoConnectAsync();

            Sync.WaitFor(() => s.ConnectedClients.Count == 3, m_Timeout);
            Assert.IsTrue(s.ConnectedClients.Count == 3, "Clients did not connect to server");
        }

        [Test]
        public async Task Client_CanDisconnectFrom_Server()
        {
            s.Start();
            await c3.AutoConnectAsync();

            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);
            c3.Disconnect();

            Sync.WaitFor(() => s.ConnectedClients.Count == 0, m_Timeout);
            Assert.IsTrue(s.ConnectedClients.Count == 0, "Client did not disconnect from server");
        }

        [Test]
        public async Task Server_CanDisconnect_Client()
        {
            s.Start();
            await c1.AutoConnectAsync();
            await c3.AutoConnectAsync();

            Sync.WaitFor(() => s.ConnectedClients.Count == 2, m_Timeout);
            s.CloseClientConnection(2);

            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);
            Sync.WaitFor(() => c3.IsConnected() == false, m_Timeout);

            Assert.IsTrue(s.ConnectedClients.Count == 1, "Client did not disconnect from server");
            Assert.IsTrue(c1.IsConnected(), "C1 connected");
            Assert.IsFalse(c3.IsConnected(), "C3 connected");
        }

        [Test]
        public async Task Clients_AutomaticallyReconnect_IfSetToTrue()
        {
            s.Start();
            await c1.AutoConnectAsync();
            await c2.AutoConnectAsync();
            await c3.AutoConnectAsync();

            Sync.WaitFor(() => s.ConnectedClients.Count == 3, m_Timeout);
            s.CloseAllClientConnetcions();


            Sync.WaitFor(() => s.ConnectedClients.Count == 1, m_Timeout);
            Assert.IsTrue(s.ConnectedClients.Count == 2, "2 Clients should have reconnected");
            Assert.IsTrue(s.ConnectedClients.Keys.First() > 3, "Client id's have pushed forward when reconnecting");
        }
    }
}