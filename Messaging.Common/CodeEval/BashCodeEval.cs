using System;
using System.Diagnostics;

namespace Messaging.Common.CodeEval
{
    public class BashCodeEval : ICodeEval
    {
        public string Run(string command)
        {
            // Replace all '\' in path to be double '\\' (bash in windows does not work otherwise)
            // Replace all " in args to be \", since it is encapsulated into bash call
            var escapedArgs = command.Replace(@"\", @"\\").Replace("\"", "\\\"");

            var fileName = "bash";
            var arguments = $"-c \"{escapedArgs}\"";

            // Unity downloader seems to crash when executed from bash command. 7zip makes the crash initially
            // Executing directly seems to work fine. Super strange. Threw Invalid Input Handle exception
            if (command.StartsWith("unity-downloader-cli", StringComparison.InvariantCultureIgnoreCase))
            {
                fileName = "unity-downloader-cli";
                arguments = command.Replace(fileName, "").Trim();
            }

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            var result = (string.IsNullOrEmpty(error)) ? stdout : stdout + Environment.NewLine + Environment.NewLine + error;

            return result.Trim('\n', ' ', '\r');
        }
    }
}
