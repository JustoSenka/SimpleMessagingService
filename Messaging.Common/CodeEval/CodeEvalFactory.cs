using System;

namespace Messaging.Common.CodeEval
{
    public static class CodeEvalFactory
    {
        public static bool IsMessageTypeSupported(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.BashScript:
                case MessageType.RunProcess:
                case MessageType.PythonScript:
                case MessageType.CSharpScript:
                case MessageType.CreateFile:
                case MessageType.RemoteProcedureCall:
                case MessageType.DownloadFile:
                    return true;
                default:
                    return false;
            }
        }

        public static ICodeEval GetFromMessageType(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.BashScript:
                    return new BashCodeEval();

                case MessageType.RunProcess:
                    return new RunProcessEval();

                case MessageType.PythonScript:
                    return new PythonCodeEval();

                case MessageType.CSharpScript:
                    throw new NotImplementedException("No C# interpreter on .NetCore yet");

                case MessageType.CreateFile:
                    return new CreateFileEval();

                case MessageType.RemoteProcedureCall:
                    return new RemoteProcedureEval();

                case MessageType.DownloadFile:
                    return new DownloadFileEval();

                default:
                    throw new ArgumentException("No suitable CodeEval can be created from provided message type: " + messageType);
            }
        }
    }
}
