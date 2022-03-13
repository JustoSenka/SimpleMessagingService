using Messaging.Server;
using System;
using System.Threading.Tasks;

namespace Messaging.ServerCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var s = new MServer();
            s.Start();

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
