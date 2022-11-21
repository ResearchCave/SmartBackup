using ConsoleApp38;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks; 

namespace SmartBackup.Common
{
    public class ProcessManager
    {
        public ProcessManager() { 
        
        } 
        public int CloseProcesses(string processName)
        {
            if(processName.Contains('.'))
            {
                processName=Path.GetFileNameWithoutExtension(processName);
            }

            int ProcessKillTimeout = 5000;
            int cnt = 0;
            foreach (var process in Process.GetProcessesByName(processName))
            {
                Console.WriteLine(String.Format("Closing Process: {0} PID:{1}", processName, process.Id));

                try
                {
                    process.Close();
                }
                catch (Exception x)
                {
                    Console.WriteLine(String.Format("CloseProcesses:{0}", x.Message));
                }

                try
                {
                    if (!process.WaitForExit(ProcessKillTimeout))
                    {
                        process.Kill();
                    }
                }
                catch (Exception x)
                {
                    Console.WriteLine(String.Format("ProcessCloseError:{0}", x.Message));
                }

                cnt++;
            }
            return cnt;
        }

        public Process StartProcess(string ProcessPath, string  args)
       {
            Process process = new Process();
            // Configure the process using the StartInfo properties.
            process.StartInfo.FileName = ProcessPath;
            process.StartInfo.Arguments = args;
           // process.StartInfo.UseShellExecute=true;
            process.Start();
            return process;
        }

        public sealed class ExtentedStringWriter : System.IO.StringWriter
        {
            private readonly Encoding stringWriterEncoding;
            public ExtentedStringWriter(StringBuilder builder, Encoding desiredEncoding)
                : base(builder)
            {
                this.stringWriterEncoding = desiredEncoding;
            }
            public override Task WriteAsync(char[] buffer, int index, int count)
            {
                string str = new string(buffer, 0, count);
                Console.Write(str);
                return base.WriteAsync(buffer, index, count);
            }

            public override Encoding Encoding
            {
                get
                {
                    return this.stringWriterEncoding;
                }
            }
        }
        static ExtentedStringWriter tw = null;
        public int StreamCommand(string cmd, string[] argv)
        {
            var process1 = new EasyProcess(cmd, argv, Console.OutputEncoding);
            StringBuilder sb = new StringBuilder();
            tw = new ExtentedStringWriter(sb, Console.OutputEncoding);
            process1.Output.PipeToAsync(tw);
            process1.Error.PipeToAsync(tw);
            return process1.Result.ExitCode;
            //  string lst = process1.Result.StandardOutput;
        }


    }
}
