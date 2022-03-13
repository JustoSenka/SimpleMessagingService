using System;


namespace Messaging.Client.Utilities
{
    public class Logger
    {
        public static void LogServer(string msg, params object[] args)
        {
            Console.WriteLine($"[Server]: {msg}", args);
        }

        public static void LogClient(string msg, params object[] args)
        {
            Console.WriteLine($" -- {msg}", args);
        }
    }
}