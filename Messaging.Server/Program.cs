using Messaging.Common;
using Messaging.Common.Utilities;
using Messaging.PersistentTcp;
using Messaging.Server;
using System;
using System.Threading.Tasks;

namespace Messaging.ServerCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var s = new MessagingServer();
            s.Start();

            while (true)
            {
                var line = Console.ReadLine();
                if (int.TryParse(line.Split(' ')[0], out int id))
                {
                    var msg = line.Substring(id.ToString().Length + 1);
                    await s.Send(id, new MessageCommand(MessageType.Text, line.ToBytesUTF8()));
                }
            }

            Console.WriteLine("\n Press Enter to quit...");
            Console.Read();
        }
    }
}
