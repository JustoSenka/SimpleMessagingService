using Messaging.Common.Utilities;
using System;

namespace Messaging.Common
{
    [Serializable]
    public struct Message
    {
        public Guid Guid { get; private set; }

        public MessageType messageType;
        public byte[] bytes;

        public Message(MessageType messageType, byte[] bytes)
        {
            this.messageType = messageType;
            this.bytes = bytes;
            Guid = Guid.NewGuid();
        }

        public Message ConstructResponse(byte[] bytes)
        {
            return new Message(MessageType.Response, bytes)
            {
                Guid = this.Guid
            };
        }

        public override string ToString()
        {
            return bytes.ToStringUTF8();
        }
    }

    public enum MessageType
    {
        Connection = 0,
        RemoteProcedureCall,
        CSharpScript,
        BashScript,
        RunProcess,
        PythonScript,
        CreateFile,
        DownloadFile,
        Response,
        Kill,
    }
}
