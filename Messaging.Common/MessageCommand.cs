using Messaging.PersistentTcp;
using Messaging.PersistentTcp.Utilities;
using System;

namespace Messaging.Common
{
    [Serializable]
    public class MessageCommand : Message
    {
        public MessageType messageType;

        public MessageCommand(byte[] bytes, bool requestResponse = false) : base(bytes, requestResponse) { }

        public MessageCommand(MessageType messageType, byte[] bytes, bool requestResponse = false) : base(bytes, requestResponse)
        {
            this.messageType = messageType;
        }

        public override string ToString() => bytes.ToStringUTF8();
    }

    public enum MessageType
    {
        Text = 0,
        RemoteProcedureCall,
        CSharpScript,
        BashScript,
        RunProcess,
        PythonScript,
        CreateFile,
        DownloadFile,
        Kill,
    }
}
