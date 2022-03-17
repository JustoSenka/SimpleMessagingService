using Newtonsoft.Json;
using System;
using System.IO;

namespace Messaging.Common.CodeEval
{
    public class CreateFileEval : ICodeEval
    {
        public string Run(string command)
        {
            try
            {
                var o = FromJson(command);

                var folderPath = new FileInfo(o.DestinationPath).Directory.FullName;
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                File.WriteAllBytes(o.DestinationPath, o.Contents);
                return "File created: " + o.DestinationPath;
            }
            catch (Exception e)
            {
                return e.Message;
            }
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

    [Serializable]
    public class CreateFileOptions
    {
        public string SourcePath;
        public string DestinationPath;
        public byte[] Contents;

        public CreateFileOptions(string sourcePath, string destinationPath, byte[] contents)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            Contents = contents;
        }
    }
}
