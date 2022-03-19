namespace Messaging.Common.Utilities
{
    public static class MessageExtension
    {
        public static MessageCommand ToMessage(this byte[] bytes) => BinaryObjectIO.DeserializeFromBytes<MessageCommand>(bytes);
        public static byte[] ToBytes(this MessageCommand m) => BinaryObjectIO.SerializeToBytes(m);
    }
}
