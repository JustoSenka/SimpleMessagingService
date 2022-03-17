using System;
using System.Diagnostics;
using System.IO;

namespace Messaging.Common.CodeEval
{
    public class PythonCodeEval : ICodeEval
    {
        public string Run(string command)
        {
            var pythonFileName = "lastPythonCommand.py";
            File.WriteAllText(pythonFileName, command);

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"{pythonFileName}",
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
