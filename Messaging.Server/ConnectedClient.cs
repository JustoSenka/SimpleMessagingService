using Messaging.Common;
using System.Net.Sockets;

namespace Messaging.Server
{
    public struct ConnectedClient<T>
    {
        public long id;
        public string endpoint;
        public PersistentTcpClient<T> client;
    }
}
