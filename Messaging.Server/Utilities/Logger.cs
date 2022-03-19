using Messaging.Common;
using Messaging.PersistentTcp;
using System;

namespace Messaging.Server.Utilities
{
    public class Logger
    {
        public static void LogServer(string msg, params object[] args)
        {
            Console.WriteLine($" -- {msg}", args);
        }
        public static void LogClient(ClientInfo<MessageCommand> client, string msg, params object[] args)
        {
            Console.WriteLine($"[{client.endpoint}]:[{client.id}]: {msg}", args);
        }
    }
}
