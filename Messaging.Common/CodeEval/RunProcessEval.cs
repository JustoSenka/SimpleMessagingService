using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Messaging.Common.CodeEval
{
    public class RunProcessEval : ICodeEval
    {
        public string Run(string command)
        {
            var o = FromJson(command);

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = o.ProcessName,
                    Arguments = o.Args,
                    RedirectStandardOutput = o.WaitForExit,
                    RedirectStandardError = o.WaitForExit,
                    UseShellExecute = !o.WaitForExit,
                    CreateNoWindow = o.WaitForExit,
                }
            };

            var list = new List<string>();
            if (o.WaitForExit)
            {
                process.OutputDataReceived += (sender, data) => OnDataReceived(data, list);
                process.ErrorDataReceived += (sender, data) => OnDataReceived(data, list);
            }

            process.Start();

            if (o.WaitForExit)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                var result = string.Join(Environment.NewLine, list);  // (string.IsNullOrEmpty(error)) ? stdout : stdout + Environment.NewLine + Environment.NewLine + error;
                return result.Trim('\n', ' ', '\r');
            }

            return process.Id.ToString();
        }

        private void OnDataReceived(DataReceivedEventArgs data, IList<string> list)
        {
            if (data.Data == null || string.IsNullOrEmpty(data.Data.Trim()))
                return;

            list.Add(data.Data);
            Console.WriteLine(data.Data);
            Debug.WriteLine(data.Data);
        }

        public static string ToJson(RunProcessOptions o)
        {
            return JsonConvert.SerializeObject(o);
        }

        public static RunProcessOptions FromJson(string json)
        {
            return JsonConvert.DeserializeObject<RunProcessOptions>(json);
        }
    }

    public class RunProcessOptions
    {
        public string ProcessName;
        public string Args;
        public bool WaitForExit;

        public RunProcessOptions(string processName, string args, bool waitForExit)
        {
            ProcessName = processName;
            Args = args;
            WaitForExit = waitForExit;
        }
    }
}
