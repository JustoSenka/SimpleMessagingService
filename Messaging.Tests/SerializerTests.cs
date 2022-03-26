using Messaging.PersistentTcp;
using Messaging.PersistentTcp.Serializers;
using Messaging.PersistentTcp.Utilities;
using NUnit.Framework;

namespace Messaging.Tests
{
    public class SerializerTests
    {
        [Test]
        public void Serialize_Message_WithJsonSerializer()
        {
            var msg = new Message("test".ToBytesUTF8());
            var s = new JsonSerializer<Message>();
            var newMsg = s.Deserialize(s.Serialize(msg));

            Assert.AreEqual(msg.bytes.ToStringUTF8(), newMsg.bytes.ToStringUTF8(), "Message text did not match");
            Assert.AreEqual(msg.requestResponse, newMsg.requestResponse, "Message requestResponse flag did not match");
            Assert.AreEqual(msg.Guid, newMsg.Guid, "Message guid did not match");
        }

        [Test]
        public void Serialize_Message_WithBinarySerializer()
        {
            var msg = new Message("test".ToBytesUTF8());
            var s = new BinarySerializer<Message>();
            var newMsg = s.Deserialize(s.Serialize(msg));

            Assert.AreEqual(msg.bytes.ToStringUTF8(), newMsg.bytes.ToStringUTF8(), "Message text did not match");
            Assert.AreEqual(msg.requestResponse, newMsg.requestResponse, "Message requestResponse flag did not match");
            Assert.AreEqual(msg.Guid, newMsg.Guid, "Message guid did not match");
        }
    }
}
