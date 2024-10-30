 

using CommandLine; 
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders; 
using Serilog;
using Serilog.Events;
using SmartBackup.Archiver;
using SmartBackup.Common;
using SmartBackup.Model;
using SmartBackup.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq; 

namespace SmartBackup.MSSQLAgent
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            bool stopProcessing = false;

            var parsed = Parser.Default.ParseArguments<SmartBackup.Agent.Options>(args)
            .WithParsed<SmartBackup.Agent.Options>(o =>
            {


            }
                 ).WithNotParsed<SmartBackup.Agent.Options>(o =>
                 {
                     List<CommandLine.Error> errors = o.ToList();
                     stopProcessing = errors.Any(e => e.StopsProcessing);

                     foreach (CommandLine.Error error in errors)
                     {
                         Console.WriteLine($"Error command line parameter '{error.Tag}'");
                     }

                 });

            if (stopProcessing)
            {
                return -1;
            }

            if (parsed == null) return -1;
            if (parsed.Errors.Count() > 0)
            {
                foreach (var item in parsed.Errors)
                {
                    Console.WriteLine(item.ToString());
                }
                return -1;
            }
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, parsed);
            // create service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // run app
            return await (serviceProvider.GetService<SmartBackup.Agent.App>().Run());
        }

        private static void ConfigureServices(IServiceCollection services, ParserResult<SmartBackup.Agent.Options> opts)
        {
            //string AppPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            services.AddSingleton(opts.Value);
            EnvironmentInfo envinfo = new EnvironmentInfo();

            #region Logging

            //  string CurrentDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string CurrentDir = Path.GetDirectoryName(Environment.ProcessPath);

            //  Console.WriteLine("CurrentDir:"+ CurrentDir);
            DateTime now = DateTime.Now;

            string timestamp = now.ToString("yyyy-MM-dd_HH-mm");
            string logFileName = Path.Combine(CurrentDir, "Logs", $"backup-{timestamp}.txt");

            envinfo.StartTime = now;
            envinfo.AppPath = CurrentDir;
            envinfo.CurrentLogPath = logFileName;

            var logConfig = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.File(logFileName,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
            rollingInterval: RollingInterval.Infinite,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug
            );

            {
                logConfig = logConfig
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Literate,
                 restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
                 outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                );
            }

            Serilog.Log.Logger = logConfig.CreateLogger();

            services.AddSingleton<EnvironmentInfo>(envinfo);
             
            services.AddSingleton(Serilog.Log.Logger);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Serilog.Log.CloseAndFlush();
            #endregion




 
            services.AddTransient<ProcessManager>();

            services.AddTransient<SmartBackup.Agent.App>();
        }

    }
}