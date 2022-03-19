﻿using Messaging.Client;
using Messaging.Common;
using Messaging.PersistentTcp.Utilities;
using System;
using System.Threading.Tasks;

namespace Messaging.ClientCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var s = new MessagingClient();
            s.AutoReconnect = true;
            await s.Connect();

            while (true)
            {
                var line = Console.ReadLine();
                await s.Send(new MessageCommand(MessageType.Text, line.ToBytesUTF8()));
            }

            Console.WriteLine("\n Press Enter to quit...");
            Console.Read();
        }
    }
}
