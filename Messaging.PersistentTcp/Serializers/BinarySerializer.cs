using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Messaging.PersistentTcp.Serializers
{
    public class BinarySerializer<T> : ISerializer<T>
    {
        public byte[] Serialize(T obj)
        {
            if (obj == null)
                return null;

            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            stream.Flush();
            return stream.ToArray();
        }

        public T Deserialize(byte[] bytes)
        {
            if (bytes == null)
                return default;

            var stream = new MemoryStream(bytes);
            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }
    }
}
