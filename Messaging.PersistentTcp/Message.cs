using Messaging.PersistentTcp.Utilities;
using System;

namespace Messaging.PersistentTcp
{
    [Serializable]
    public class Message
    {
        public Guid Guid { get; private set; }
        
        public bool requestResponse;
        public byte[] bytes;

        public Message(byte[] bytes, bool requestResponse = false)
        {
            this.bytes = bytes;
            this.requestResponse = requestResponse;
            Guid = Guid.NewGuid();
        }

        public Message ConstructResponse(byte[] bytes)
        {
            return new Message(bytes)
            {
                Guid = this.Guid
            };
        }

        public override string ToString() => bytes.ToStringUTF8();
    }
}
