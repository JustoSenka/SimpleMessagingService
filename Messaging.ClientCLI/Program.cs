using System;
using System.Threading.Tasks;
using Messaging.Client;

namespace Messaging.ClientCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var s = new MessagingClient();
            await s.Connect();

            while (true)
            {
                var line = Console.ReadLine();
                await s.Send(line);
            }

            Console.WriteLine("\n Press Enter to quit...");
            Console.Read();
        }
    }
}
