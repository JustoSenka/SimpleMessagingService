using Messaging.Common.Utilities;
using Messaging.PersistentTcp;
using Messaging.PersistentTcp.Utilities;
using Newtonsoft.Json;
using System.IO;
using System.Threading;

namespace Messaging.Common.CodeEval
{
    public class DownloadFileEval : ICodeEval
    {
        public string Run(string sourceFilePath)
        {
            int tries = 7;
            var msg = "";
            while (tries > 0)
            {
                tries--;
                try
                {
                    return File.ReadAllBytes(sourceFilePath).ToStringUTF8();
                }
                catch (IOException e)
                {
                    Thread.Sleep(1000);
                    msg = e.Message;
                }
            }

            return msg;
        }

        public static string ToJson(CreateFileOptions o)
        {
            return JsonConvert.SerializeObject(o);
        }

        public static CreateFileOptions FromJson(string json)
        {
            return JsonConvert.DeserializeObject<CreateFileOptions>(json);
        }
    }
}
