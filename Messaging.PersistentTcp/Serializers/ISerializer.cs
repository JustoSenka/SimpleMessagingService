namespace Messaging.PersistentTcp.Serializers
{
    public interface ISerializer<T>
    {
        public byte[] Serialize(T obj);
        public T Deserialize(byte[] bytes);
    }
}
