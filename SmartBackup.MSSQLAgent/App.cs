

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;  
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Serilog; 
using SmartBackup.Archiver;
using SmartBackup.Common;
using SmartBackup.Model;
//using SmartBackup.MongoDb;
//using SmartBackup.HyperV;

using SmartBackup.MSSQL;
using SmartBackup.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace SmartBackup.Agent
{
    public class App
    {
        const string schemaFilename = "config-schema.json";
        const string configFilename = "AgentConfig.json";
        readonly SmartBackup.Agent.Options opts;
        readonly ILogger log;
        readonly IServiceProvider sp; 
        readonly ProcessManager processManager;
        readonly EnvironmentInfo envinf; 

        public System.Diagnostics.Stopwatch sw=new System.Diagnostics.Stopwatch();

        public App(

            ILogger _log,
            SmartBackup.Agent.Options _opts,
            IServiceProvider _sp, 
            ProcessManager _processManager,
            EnvironmentInfo _envinf 
            )
        {
            envinf = _envinf;
            opts = _opts;
            log = _log;
            sp = _sp; 
            processManager = _processManager; 

        } 



        public void PreInit()
        {

          

            var BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, typeof(MySQLBackupOperation));
            BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, typeof(MSSQLBackupOperation));
            //BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, typeof(VMBackupOperation)); 
            //BackupOp = (IBackupOperation)ActivatorUtilities.CreateInstance(sp, typeof(MongoBackupOperation));
            


        }
        static System.Threading.Mutex singleton = new Mutex(true, "RCSmartBackupAgent");

         public  async Task HandleClientAsync(TcpClient client)
        {

          log.Information("Client Connected : {0}", client.Client.RemoteEndPoint);
            using (NetworkStream networkStream = client.GetStream())
            using (BinaryReader reader = new BinaryReader(networkStream))
            {
                char[] heloBytes = reader.ReadChars(4);
                 
                {
                    string receivedString = new string(heloBytes);
                    if (receivedString.Equals("H1SB", StringComparison.Ordinal))
                    {
                        // The received string is "helo"
                        Console.WriteLine("A SmartBackup Client Arrived");

                       ulong LoginjsonLen =   reader.ReadUInt32();

                        SmartBackup.Common.LoginInfoHashed lih = new LoginInfoHashed();
                        lih.Username=   reader.ReadString();
                        lih.PasswordHash = reader.ReadBytes(32);




                        try
                        {
                            // Deserialize JSON and handle client in here
                            // Send file and report progress
                        }
                        finally
                        {
                            client.Close();
                        }


                    }
                    else
                    {
                        // The received string is not "helo"
                        Console.WriteLine("Unexpected helo from client: " + receivedString);
                        client.Close();
                        return;
                    }
                }
            }
                 

        }

        public async Task<int> Run()
        {

            log.Information("ResearchCave - SmartBackup 2022 Agent\t{0}", "https://www.researchcave.com");
            log.Information("Backup Started on {0}", envinf.MachineName);



            var listener = new TcpListener(IPAddress.Any, 9000);
            listener.Start();

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                HandleClientAsync(client);
            }



            PreInit();
            return 0;
           
        }

      
    }
}
