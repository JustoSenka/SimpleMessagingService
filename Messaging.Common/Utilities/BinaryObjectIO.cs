using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Messaging.Common.Utilities
{
    public static class BinaryObjectIO
    {
        public static byte[] SerializeToBytes<T>(T obj)
        {
            if (obj == null)
                return null;

            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            stream.Flush();
            return stream.ToArray();
        }

        public static T DeserializeFromBytes<T>(byte[] bytes)
        {
            if (bytes == null)
                return default(T);

            var stream = new MemoryStream(bytes);
            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }
    }
}
