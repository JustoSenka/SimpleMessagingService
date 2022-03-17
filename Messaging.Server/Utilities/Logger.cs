using Messaging.Common;
using System;

namespace Messaging.Server.Utilities
{
    public class Logger
    {
        public static void LogServer(string msg, params object[] args)
        {
            Console.WriteLine($" -- {msg}", args);
        }
        public static void LogClient(ConnectedClient<Message> client, string msg, params object[] args)
        {
            Console.WriteLine($"[{client.endpoint}]:[{client.id}]: {msg}", args);
        }
    }
}
