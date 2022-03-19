using Messaging.PersistentTcp.Utilities;
using Newtonsoft.Json;

namespace Messaging.PersistentTcp.Serializers
{
    public class JsonSerializer<T> : ISerializer<T>
    {
        public byte[] Serialize(T obj) => JsonConvert.SerializeObject(obj).ToBytesUTF8();
        public T Deserialize(byte[] bytes) => JsonConvert.DeserializeObject<T>(bytes.ToStringUTF8());
    }
}
