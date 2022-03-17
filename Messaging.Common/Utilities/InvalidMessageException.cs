using System;

namespace Messaging.Common.Utilities
{
    public class InvalidMessageException : Exception
    {
        public InvalidMessageException() : base(){}
        public InvalidMessageException(string msg) : base(msg){ }
    }
}
