namespace Messaging.PersistentTcp
{
    public struct ClientInfo<TMessage> where TMessage : Message
    {
        public long id;
        public string endpoint;
        public PersistentTcpClient<TMessage> client;
    }
}
