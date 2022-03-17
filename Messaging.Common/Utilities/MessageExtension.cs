namespace Messaging.Common.Utilities
{
    public static class MessageExtension
    {
        public static Message ToMessage(this byte[] bytes) => BinaryObjectIO.DeserializeFromBytes<Message>(bytes);
        public static byte[] ToBytes(this Message m) => BinaryObjectIO.SerializeToBytes(m);
    }
}
