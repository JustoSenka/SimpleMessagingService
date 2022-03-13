using System.Net.Sockets;

namespace Messaging.Server
{
    public struct ConnectedClient
    {
        public long id;
        public string endpoint;
        public TcpClient client;
        public NetworkStream stream;
    }
}
