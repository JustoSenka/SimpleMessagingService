using System;

namespace Messaging.PersistentTcp.Utilities
{
    public class InvalidMessageException : Exception
    {
        public InvalidMessageException() : base(){}
        public InvalidMessageException(string msg) : base(msg){ }
    }
}
