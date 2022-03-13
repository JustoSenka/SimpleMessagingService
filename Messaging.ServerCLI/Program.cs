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
                    await s.Send(id, msg);
                }
            }

            Console.WriteLine("\n Press Enter to quit...");
            Console.Read();
        }
    }
}
