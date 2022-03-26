using Messaging.PersistentTcp.Utilities;
using System;

namespace Messaging.PersistentTcp
{
    [Serializable]
    public class Message
    {
        public readonly Guid Guid;

        public bool requestResponse;
        public byte[] bytes;

        public Message(byte[] bytes, bool requestResponse = false, Guid guid = default)
        {
            this.bytes = bytes;
            this.requestResponse = requestResponse;
            Guid = guid.Equals(default) ? Guid.NewGuid() : guid;
        }

        public virtual Message ConstructResponse()
        {
            return new Message("R".ToBytesUTF8(), guid: Guid);
        }

        public override string ToString() => bytes.ToStringUTF8();
    }
}
