

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options; 
using Namotion.Reflection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Serilog;
using ShellProgressBar;
using SmartBackup.Archiver;
using SmartBackup.Common;
using SmartBackup.HyperV;
using SmartBackup.Model;
using SmartBackup.MongoDb;
using SmartBackup.MSSQL;
using SmartBackup.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SmartBackup
{
    public class App
    {
        const string schemaFilename = "config-schema.json";
        const string configFilename = "config.json";
        readonly SmartBackup.Options opts;
        readonly ILogger log;
        readonly IServiceProvider sp;
        readonly IConfReader ConfigurationReader;
        readonly PluginManager PluginMgr;
        readonly ProcessManager processManager;
        readonly EnvironmentInfo envinf;
        readonly IEmailSender emailSender;

        public System.Diagnostics.Stopwatch sw=new System.Diagnostics.Stopwatch();

        public App(

            ILogger _log,
            SmartBackup.Options _opts,
            IServiceProvider _sp,
            IConfReader _ConfigurationReader,
            PluginManager _PluginMgr,
            ProcessManager _processManager,
            EnvironmentInfo _envinf,
            IEmailSender _emailSender
            )
        {
            envinf = _envinf;
            opts = _opts;
            log = _log;
            sp = _sp;
            ConfigurationReader = _ConfigurationReader;
            PluginMgr = _PluginMgr;
            processManager = _processManager;
            emailSender = _emailSender;

        }
        ProgressBar progressBar = null;



        public void PreInit()
        {



            var BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, typeof(MySQLBackupOperation));
            BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, typeof(MSSQLBackupOperation));
            BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, typeof(VMBackupOperation));
            BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, typeof(MongoBackupOperation));



        }
        static System.Threading.Mutex singleton = new Mutex(true, "RCSmartBackup");
        public int Run()
        {

            log.Information("ResearchCave - SmartBackup 2022\t{0}", "https://www.researchcave.com");
            log.Information("Backup Started on {0}", envinf.MachineName);

         

    
         


            PreInit();

            const int totalTicks = 100;
            var options = new ProgressBarOptions
            {
                DisplayTimeInRealTime = true,
                ProgressBarOnBottom = true,
                CollapseWhenFinished = false,
                ProgressCharacter = '─',
                EnableTaskBarProgress = true,
            };
            //   progressBar = new ProgressBar(totalTicks, "Initial message", options);

            PluginMgr.Init();

            SMTPSettings smtp = ConfigurationReader.Configuration?.SMTP;
            if (opts.SMTPTest)
            {
                Console.WriteLine("Sending Email Report...");

                try
                {
                    Dictionary<string, string> dic = new Dictionary<string, string>();
                    emailSender.SendEmail($"Backup Report Test: {envinf.StartTime.Date}", "This is a test E-mail from ResearchCave SmartBackup", null, dic, null);
                    Console.WriteLine("Report test sent to {0}", smtp.To);
                }
                catch (Exception x)
                {
                    log.Error("Error sending report email: {0}", x.Message);
                }

                return 1;
            }
            #region GenerateSchema
            if (opts.GenerateSchema)
            {
                SchemaGenerator sg = sp.GetService<SchemaGenerator>();
                string schemastr = sg.GenerateSchemaForClass(typeof(Config));
                System.IO.File.WriteAllText(schemaFilename, schemastr);
                log.Information(String.Format("Schema written to {0}", schemaFilename));

                if (!opts.Validate) return -1;
            }

            if (opts.Validate)
            {
                try
                {
                    string configstr = System.IO.File.ReadAllText(configFilename);
                    string schemastr = System.IO.File.ReadAllText(schemaFilename);
                    JSchema schema = JSchema.Parse(schemastr);
                    JObject person = JObject.Parse(configstr);
                    bool valid = false; // person.IsValid(schema);
                    try
                    {
                        person.Validate(schema);
                        valid = true;
                    }
                    catch (Exception x) //Newtonsoft.Json.Schema.JSchemaValidationException
                    {
                        //show line number in config
                        log.Warning(x.Message);
                    }

                    if (valid)
                    {
                        log.Information(String.Format("Config '{0}' validated against config schema '{1}'", configFilename, schemaFilename));
                    }
                    else
                    {
                        log.Error(String.Format("Config '{0}' could not be validated against config schema '{1}'", configFilename, schemaFilename));
                        return 2;
                    }
                }
                catch (Exception x)
                {
                    log.Error(String.Format("Error validating schema:{0}", x));
                }
                return 0;
            }

            #endregion
            try
            {
                ConfigurationReader.Load();
            }
            catch (Exception x)
            {
                log.Fatal("Configuration Error:{0}", x);
                return 3;
            }




            if (!singleton.WaitOne(TimeSpan.Zero, true))
            {
                log.Fatal("Application already running. Exiting.");
                //there is already another instance running!
                return  255;
            }


            #region TryCreateBackupPath
            if (String.IsNullOrEmpty(ConfigurationReader.Configuration.BackupPath))
            {
                log.Fatal("No 'BackupPath' setting in configuration");
                return 4;
            }
            else if (!Directory.Exists(ConfigurationReader.Configuration.BackupPath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(ConfigurationReader.Configuration.BackupPath);

                }
                catch (Exception x)
                {

                    log.Fatal("Could not create BackupPath: {0} setting in configuration; ERR: {1}", ConfigurationReader.Configuration.BackupPath, x.Message);
                    return 5;
                }
            }
            #endregion

            if (ConfigurationReader.Configuration.Items == null)
            {

                log.Fatal("'Items' setting not found in configuration");
                return 6;
            }
            else
            if (!ConfigurationReader.Configuration.Items.Any())
            {
                log.Warning("'Items' setting not found in configuration");
            }





            List<IBackupOperation> BackupOperations = new List<IBackupOperation>();

            sw.Start();

            int jobno = 0;
            foreach (var item in ConfigurationReader.Configuration.Items)
            {
                jobno++;
                IBackupOperation BackupOp = null;

                string BackupType = "";
                string BackupName = "";

                JsonElement jitem = (JsonElement)item;

                if (jitem.ValueKind == JsonValueKind.String)
                {
                    BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, typeof(ArchiveOperation));
                    BackupType = "File";
                    BackupName = jitem.GetString();

                    log.Information("Initializing Backup Operation {0} [{1}]", BackupName, BackupType);
                }
                else
                {
                    if (jitem.TryGetProperty("Name", out JsonElement n))
                    {
                        if (n.ValueKind == JsonValueKind.String)
                        {
                            BackupName = n.GetString();
                        }
                    }


                    if (jitem.TryGetProperty("Type", out JsonElement value))
                    {
                        if (value.ValueKind == JsonValueKind.String)
                        {
                            BackupType = value.GetString();
                            Type? ModuleType = PluginMgr.GetBackupModule(BackupType);

                            if (ModuleType != null)
                            {
                                BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, ModuleType);
                                log.Information("Initializing Backup Operation {0} [{1}]", BackupName, BackupType);
                            }
                            else
                            {
                                log.Fatal("Unable to resolve Backup Module {0}", BackupType);
                                return 7;
                            }
                        }
                        else
                        {
                            log.Fatal("Unexpected 'Type' property of Item, Expected string value");
                        }
                    }
                    else
                    {
                        //Type not found, expecting File or Directory
                    }
                }

                if (BackupOp == null)
                {
                    log.Fatal("Item is missing 'Type' Parameter", BackupType);
                    return 8;
                }

                BackupOp.PreConfigure(sp);
                BackupOp.Configure(jitem);
                BackupOp.PostConfigure();

                BackupOp.UpdateProgress += BackupOp_UpdateProgress;

                BackupOperations.Add(BackupOp);
            }

            log.Information("Configuration Complete, Starting Backup Jobs");
            int total = BackupOperations.Count;
            int cnt = 0;
            foreach (IBackupOperation BackupOp in BackupOperations)
            {
                try
                {
                    log.Information("Starting Backup job {0}/{1}: {2}", cnt, total, BackupOp.ToString());
                    BackupOp.Backup();
                    cnt++;
                }
                catch (Exception x)
                {
                    log.Error(x, "Error: {0}", x);
                }
            }

            sw.Stop();
            log.Information("Backup jobs finished. {0}/{1} Elapsed Time:{2}", cnt, total, sw.Elapsed);

            //Close log file so we can send it via Email
            Serilog.Log.CloseAndFlush();



            if (smtp != null)
            {
                Console.WriteLine("Sending Email Report...");
                Dictionary<string, string> dic = new Dictionary<string, string>();

                try
                {
                    emailSender.SendEmail($"Backup Report for {envinf.MachineName} on {envinf.StartTime.Date}", "Your backup report is attached", envinf.CurrentLogPath, dic, null);
                    Console.WriteLine("Report sent.");
                }
                catch (Exception x)
                {
                    log.Error("Error sending report email: {0}", x.Message);
                }
            }

            return 0;
        }

        private void BackupOp_UpdateProgress(object? sender, BackupOperation.UpdateProgressEventArgs e)
        {
            if (progressBar != null)
            {
                var progress = progressBar.AsProgress<float>();

                progress.Report(e.Progress);
            }
            log.Verbose("Progress:" + e.Progress.ToString("N2"));
        }
    }
}
