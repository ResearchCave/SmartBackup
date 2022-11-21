using Narkhedegs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp38
{
    public sealed class EasyProcess
    {
        /// <summary>
        /// Wrapped System.Diagnostics.Process.
        /// </summary>
        private readonly Process _process;

        private readonly Task<ProcessResult> _task;

        /// <summary>
        /// Standard Output.
        /// </summary>
        public ProcessStreamReader Output { get; }

        /// <summary>
        /// Standard Error.
        /// </summary>
        public ProcessStreamReader Error { get; }

        /// <summary>
        /// Standard Input.
        /// </summary>
    //    public ProcessStreamWriter Input { get; }

        /// <summary>
        /// The result of the process including ExitCode, success indicator, Standard Output as string and 
        /// Standard Error as string.
        /// </summary>
        public ProcessResult Result => _task.Result;

     //  public Encoding ProcessProcEndocing { get; set; }

        public Process UnderlyingProcess { get; set; }
        /// <summary>
        /// Initializes a new instance of EasyProcess with the given parameters.
        /// </summary>
        /// <param name="executable">Absolute or relative path of the executable.</param>
        /// <param name="arguments">Arguments for the executable is any.</param>
        public EasyProcess(string executable,   string[] arguments, Encoding ProcEncoding)
        {         
            var processStartInformation = new ProcessStartInfo
            {
                Arguments = string.Join(" ", arguments),
                FileName = executable,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,


        //   StandardOutputEncoding = ProcEncoding
               StandardErrorEncoding=Encoding.UTF8
            };

            _process = new Process { StartInfo = processStartInformation, EnableRaisingEvents = true };

              var taskCompletionSource = new TaskCompletionSource<bool>();
            _process.Exited += (sender, eventArguments) => { taskCompletionSource.SetResult(true); };
            var processTask = taskCompletionSource.Task;

            _process.Start();
            UnderlyingProcess = _process;

            var inputOutputTasks = new List<Task>(2);

            // Wrap process's Standard Output with ProcessStreamReader.
            Output = new ProcessStreamReader(_process.StandardOutput , ProcEncoding);
            inputOutputTasks.Add(Output.Task);

            // Wrap process's Standard Error with ProcessStreamReader.
            Error = new ProcessStreamReader(_process.StandardError, ProcEncoding);
            inputOutputTasks.Add(Error.Task);

            //      Input = new ProcessStreamWriter(_process.StandardInput);
            _task = CreateCombinedTask(processTask, inputOutputTasks);
        }

        /// <summary>
        /// Combines process task and input output tasks.
        /// </summary>
        /// <param name="processTask">Task that waits for the process to exit. </param>
        /// <param name="inputOutputTasks">Tasks that read Standard Output and Standard Error.</param>
        /// <returns></returns>
        private async Task<ProcessResult> CreateCombinedTask(Task processTask, List<Task> inputOutputTasks)
        {
            int exitCode;
            try
            {
                await processTask.ConfigureAwait(false);
                exitCode = _process.ExitCode;
            }
            finally
            {
                _process.Dispose();
            }

            await Task.WhenAll(inputOutputTasks).ConfigureAwait(false);

            return new ProcessResult(exitCode, this);
        }
    }

    public sealed class ProcessResult
    {
        private readonly Lazy<string> _standardOutput, _standardError;

        /// <summary>
        /// Initializes a new instance of ProcessResult class.
        /// </summary>
        /// <param name="exitCode">Exit code for the process.</param>
        /// <param name="process">Instance of EasyProcess.</param>
        public ProcessResult(int exitCode, EasyProcess process)
        {
            ExitCode = exitCode;
            _standardOutput = new Lazy<string>(() => process.Output.ReadToEnd());
            _standardError = new Lazy<string>(() => process.Error.ReadToEnd());
        }

        /// <summary>
        /// The exit code of the process.
        /// </summary>
        public int ExitCode { get; private set; }

        /// <summary>
        /// Returns true if the exit code is 0 (indicating success).
        /// </summary>
        public bool Success => ExitCode == 0;

        /// <summary>
        /// If available, the full standard output text of the command.
        /// </summary>
        public string StandardOutput => _standardOutput.Value;

        /// <summary>
        /// If available, the full standard error text of the command.
        /// </summary>
        public string StandardError => _standardError.Value;
    }

}
