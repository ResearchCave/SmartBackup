using Microsoft.IdentityModel.Abstractions;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using SmartBackup.Common;
using SmartBackup.Model;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Serilog;

namespace SmartBackup.MSSQL
{
    [BackupModule(Type = "MSSQL" ,   ConfigurationType = typeof(MSSQLInfo))]
    public class MSSQLBackupOperation :   BackupOperation
    {
        readonly ILogger log;
      //  readonly Archiver.IFileArchiver fa;
        public MSSQLBackupOperation(ILogger _log )
        {
           
            // fa  = _fa;
            this.log = _log;
        }
        public MSSQLInfo Info { get { return (MSSQLInfo)Data; } set { Data = value; } }
        public override bool EncryptionSupported { get { return false ; } }

        public override void Configure(object o)
        {
            JsonElement jitem = (JsonElement)o;
            Info = jitem.Deserialize<MSSQLInfo>(SerializationOptions);
            
        }
       
        public override async  Task BackupAsync()
        {
            if (Info.BackupType == SQLBackupType.Sql)
            {
                await ExportSqlAsync();
                return;
            }

            await BackupNativeAsync();
        }

        private Task BackupNativeAsync()
        {
            Server myServer = null;
            Database myDatabase = null;
            string BackupName = Info.Database + ".bak";

            Microsoft.SqlServer.Management.Common.ServerConnection conn = CreateConnection();

            string backupPath = Info.BackupPath;
            if (!String.IsNullOrEmpty(Info.TempPath))
            {
                string tmpPath = Info.TempPath;
                if(tmpPath.Contains("%temp%", StringComparison.OrdinalIgnoreCase)) { 
                tmpPath = tmpPath.Replace("%temp%", Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);
                }
                if (!Directory.Exists(tmpPath)) Directory.CreateDirectory(tmpPath);

                backupPath = tmpPath;
            }

            /*
            BackupEncryptionOptions beo = new BackupEncryptionOptions();
            beo.Algorithm = BackupEncryptionAlgorithm.Aes256;
            beo.NoEncryption = false;
            beo.EncryptorType = BackupEncryptorType.ServerAsymmetricKey;
            */

            conn.DatabaseName = Info.Database; // You cannot restore a database that you are connected to
            myServer = new Server(conn);

            string BackupFile = Path.Combine(backupPath, BackupName);
            Backup bkpDBFull = new Backup();
            /* Specify whether you want to back up database or files or log */
            bkpDBFull.Action = BackupActionType.Database;
            /* Specify the name of the database to back up */
            bkpDBFull.Database = Info.Database;
            /* You can take backup on several media type (disk or tape), here I am
             * using File type and storing backup on the file system */
             bkpDBFull.SkipTapeHeader= true;
            bkpDBFull.FormatMedia = false ;
         
            bkpDBFull.CompressionOption = BackupCompressionOptions.On;
            bkpDBFull.Devices.AddDevice(BackupFile, Microsoft.SqlServer.Management.Smo.DeviceType.File);
            bkpDBFull.BackupSetName = $"{Info.Database} Database Backup";
          
          //  bkpDBFull.BackupSetDescription = "Pasaj database - Full Backup";
            /* You can specify the expiration date for your backup data
             * after that date backup data would not be relevant */
            //  bkpDBFull.ExpirationDate = DateTime.Today.AddDays(10);

            /* You can specify Initialize = false (default) to create a new 
             * backup set which will be appended as last backup set on the media. You
             * can specify Initialize = true to make the backup as first set on the
             * medium and to overwrite any other existing backup sets if the all the
             * backup sets have expired and specified backup set name matches with
             * the name on the medium */
            if (Info.BackupType== SQLBackupType.Full)
            {
                bkpDBFull.Initialize = true ;
            }
            else if (Info.BackupType == SQLBackupType.Incremental)
            {
                bkpDBFull.Initialize = false  ;
            }
           

            /* Wiring up events for progress monitoring */
            bkpDBFull.PercentComplete += BkpDBFull_PercentComplete; ;
            bkpDBFull.Complete += BkpDBFull_Complete;  ;
           
            /* SqlBackup method starts to take back up
             * You can also use SqlBackupAsync method to perform the backup 
             * operation asynchronously */

            bkpDBFull.SqlBackup(myServer);
           log.Information("Backup file saved in :{0}" , BackupFile);



            if (!String.IsNullOrEmpty(Info.TempPath))
            {
                string destFn = Path.Combine(Info.BackupPath, Path.GetFileName(BackupFile));

                System.IO.File.Copy(BackupFile, destFn, true);
              log.Information("Copied MSSQL Backup Temp Artifact to: {0}" , destFn);

                if(System.IO.File.Exists(BackupFile)) { 

                System.IO.File.Delete(BackupFile);
                   log.Information("Cleaning up...");
                }
            }

            return Task.CompletedTask;
        }

        private Task ExportSqlAsync()
        {
            if (String.IsNullOrWhiteSpace(Info.BackupPath))
                throw new InvalidOperationException("BackupPath is required for MSSQL SQL export.");

            Directory.CreateDirectory(Info.BackupPath);

            string safeName = SanitizeFileName(String.IsNullOrWhiteSpace(Info.Name) ? Info.Database : Info.Name);
            string safeDatabase = SanitizeFileName(Info.Database);
            string fileName = $"{safeName}_{safeDatabase}.sql";
            string finalFile = Path.Combine(Info.BackupPath, fileName);
            string tempDirectory = ResolveSqlExportTempPath(Info.BackupPath);
            string tempFile = Path.Combine(tempDirectory, fileName + ".tmp");

            Directory.CreateDirectory(tempDirectory);

            if (File.Exists(tempFile))
                File.Delete(tempFile);

            log.Information("Exporting MSSQL database {Database} to SQL script: {Path}", Info.Database, finalFile);
            string toolPath = ResolveMssqlScripterPythonPath();
            log.Information("Using bundled mssql-scripter Python: {Path}", toolPath);

            try
            {
                RunMssqlScripter(toolPath, tempFile);

                MoveTempFileToFinal(tempFile, finalFile);
                log.Information("MSSQL SQL export saved in: {Path}", finalFile);
                ReportProgress(100);
                return Task.CompletedTask;
            }
            catch
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                    log.Warning("SQL export failed. Cleaned up temp file: {Path}", tempFile);
                }
                throw;
            }
            finally
            {
            }
        }

        private string ResolveMssqlScripterPythonPath()
        {
            string pluginDir = Path.GetDirectoryName(typeof(MSSQLBackupOperation).Assembly.Location) ?? AppContext.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(pluginDir, "tools", "mssql-scripter", "venv", "Scripts", "python.exe"),
                Path.Combine(pluginDir, "tools", "mssql-scripter", "venv", "bin", "python")
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            throw new InvalidOperationException(
                "mssql-scripter Python environment was not found in plugin folder. Expected under tools/mssql-scripter/venv. " +
                "Re-run publish to bundle CLI tools.");
        }

        private void RunMssqlScripter(string pythonPath, string outputFile)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(pythonPath) ?? AppContext.BaseDirectory
            };

            psi.Environment["PYTHONIOENCODING"] = "utf-8";
            psi.Environment["PYTHONUTF8"] = "1";

            psi.ArgumentList.Add("-m");
            psi.ArgumentList.Add("mssqlscripter");
            psi.ArgumentList.Add("-S");
            psi.ArgumentList.Add(Info.Server);
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add(Info.Database);
            psi.ArgumentList.Add("--schema-and-data");
            psi.ArgumentList.Add("--display-progress");

            if (!String.IsNullOrWhiteSpace(Info.Username))
            {
                psi.ArgumentList.Add("-U");
                psi.ArgumentList.Add(Info.Username);
                psi.ArgumentList.Add("-P");
                psi.ArgumentList.Add(Info.Password ?? String.Empty);
            }
            else
            {
                psi.ArgumentList.Add("-E");
            }

            List<string> diagnostics = new List<string>();
            int exitCode;

            using (Process process = new Process { StartInfo = psi })
            using (FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                process.Start();
                Task outputTask = process.StandardOutput.BaseStream.CopyToAsync(fs);

                while (!process.StandardError.EndOfStream)
                {
                    string? line = process.StandardError.ReadLine();
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        diagnostics.Add(line);
                        log.Information("mssql-scripter: {Line}", line);
                    }
                }

                outputTask.GetAwaiter().GetResult();
                fs.Flush();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            if (exitCode != 0)
                throw new InvalidOperationException($"mssql-scripter failed with exit code {exitCode}.");

            string combinedDiagnostics = String.Join(Environment.NewLine, diagnostics);
            if (ContainsMssqlScripterFailure(combinedDiagnostics))
                throw new InvalidOperationException("mssql-scripter failed: " + GetFailureSummary(diagnostics));

            ValidateMssqlScripterOutput(outputFile);
        }

        private static bool ContainsMssqlScripterFailure(string text)
        {
            return text.Contains(" encountered error:", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Error details:", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Login failed", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetFailureSummary(List<string> diagnostics)
        {
            string? summary = diagnostics.FirstOrDefault(x => ContainsMssqlScripterFailure(x));
            return String.IsNullOrWhiteSpace(summary) ? "unknown error" : summary;
        }

        private static void ValidateMssqlScripterOutput(string outputFile)
        {
            FileInfo fileInfo = new FileInfo(outputFile);
            if (!fileInfo.Exists || fileInfo.Length == 0)
                throw new InvalidOperationException("mssql-scripter did not produce SQL output.");

            using StreamReader reader = new StreamReader(outputFile, detectEncodingFromByteOrderMarks: true);
            char[] buffer = new char[Math.Min(8192, (int)fileInfo.Length)];
            int read = reader.Read(buffer, 0, buffer.Length);
            string head = new string(buffer, 0, read);

            if (ContainsMssqlScripterFailure(head))
                throw new InvalidOperationException("mssql-scripter wrote an error response instead of SQL output.");
        }

        private string ResolveSqlExportTempPath(string fallbackPath)
        {
            if (String.IsNullOrWhiteSpace(Info.TempPath))
                return fallbackPath;

            string tempPath = Info.TempPath;
            if (tempPath.Contains("%temp%", StringComparison.OrdinalIgnoreCase))
                tempPath = tempPath.Replace("%temp%", Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);

            return tempPath;
        }

        private static void MoveTempFileToFinal(string tempFile, string finalFile)
        {
            try
            {
                File.Move(tempFile, finalFile, true);
            }
            catch (IOException)
            {
                File.Copy(tempFile, finalFile, true);
                File.Delete(tempFile);
            }
        }

        private Microsoft.SqlServer.Management.Common.ServerConnection CreateConnection()
        {
            Microsoft.SqlServer.Management.Common.ServerConnection conn =  new Microsoft.SqlServer.Management.Common.ServerConnection();
            conn.ServerInstance = Info.Server;

            if ( Info.Username!= null ) {
                conn.LoginSecure = false;
                conn.Authentication = Microsoft.SqlServer.Management.Common.SqlConnectionInfo.AuthenticationMethod.SqlPassword;
                conn.Login = Info.Username;
                if(Info.Password!= null ) {
                    conn.Password = Info.Password;
                }
            }
            else
            {
                conn.LoginSecure = true;
            }

            return conn;
        }

        private void WriteHeader(StreamWriter writer, Database database)
        {
            writer.WriteLine($"-- SmartBackup SQL export for {Bracket(Info.Database)}");
            writer.WriteLine($"-- Exported at {DateTimeOffset.Now:O}");
            writer.WriteLine("SET ANSI_NULLS ON");
            writer.WriteLine("GO");
            writer.WriteLine("SET QUOTED_IDENTIFIER ON");
            writer.WriteLine("GO");
            writer.WriteLine($"IF DB_ID(N'{EscapeSqlString(Info.Database)}') IS NULL");
            writer.WriteLine($"    CREATE DATABASE {Bracket(Info.Database)}");
            writer.WriteLine("GO");
            writer.WriteLine($"USE {Bracket(Info.Database)}");
            writer.WriteLine("GO");
            writer.WriteLine();
        }

        private void ScriptSchema(Server server, Database database, StreamWriter writer)
        {
            Scripter scripter = new Scripter(server);
            scripter.Options.ScriptDrops = false;
            scripter.Options.WithDependencies = false;
            scripter.Options.Indexes = true;
            scripter.Options.DriAll = true;
            scripter.Options.Triggers = true;
            scripter.Options.FullTextIndexes = true;
            scripter.Options.SchemaQualify = true;
            scripter.Options.IncludeHeaders = true;
            scripter.Options.Permissions = true;
            scripter.Options.ExtendedProperties = true;
            scripter.Options.ScriptBatchTerminator = true;
            scripter.Options.NoCommandTerminator = false;
            scripter.Options.ScriptSchema = true;
            scripter.Options.ScriptData = false;

            writer.WriteLine("-- Schema");
            ScriptSchemaPhase(scripter, writer, "Schemas", database.Schemas.Cast<Schema>().Where(x => !x.IsSystemObject).Select(x => x.Urn));
            ScriptSchemaPhase(scripter, writer, "UserDefinedDataTypes", database.UserDefinedDataTypes.Cast<UserDefinedDataType>().Select(x => x.Urn));
            ScriptSchemaPhase(scripter, writer, "UserDefinedTableTypes", database.UserDefinedTableTypes.Cast<UserDefinedTableType>().Select(x => x.Urn));
            ScriptSchemaPhase(scripter, writer, "Tables", database.Tables.Cast<Table>().Where(x => !x.IsSystemObject).Select(x => x.Urn));
            ScriptSchemaPhase(scripter, writer, "Views", database.Views.Cast<View>().Where(x => !x.IsSystemObject).Select(x => x.Urn));
            ScriptSchemaPhase(scripter, writer, "Functions", database.UserDefinedFunctions.Cast<UserDefinedFunction>().Where(x => !x.IsSystemObject).Select(x => x.Urn));
            ScriptSchemaPhase(scripter, writer, "StoredProcedures", database.StoredProcedures.Cast<StoredProcedure>().Where(x => !x.IsSystemObject).Select(x => x.Urn));
            ScriptSchemaPhase(scripter, writer, "DatabaseTriggers", database.Triggers.Cast<DatabaseDdlTrigger>().Select(x => x.Urn));
            writer.WriteLine();
        }

        private void ScriptSchemaPhase(Scripter scripter, StreamWriter writer, string phaseName, IEnumerable<Urn> urns)
        {
            Urn[] phaseUrns = urns.ToArray();
            if (phaseUrns.Length == 0)
            {
                log.Information("SQL export schema phase {Phase}: no objects", phaseName);
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            log.Information("SQL export schema phase {Phase}: scripting {Count} objects", phaseName, phaseUrns.Length);
            WriteScript(writer, scripter.Script(phaseUrns));
            sw.Stop();
            log.Information("SQL export schema phase {Phase}: done in {Seconds:0.00}s", phaseName, sw.Elapsed.TotalSeconds);
        }

        private void ScriptTableData(Server server, Database database, StreamWriter writer)
        {
            List<Table> tables = database.Tables
                .Cast<Table>()
                .Where(t => !t.IsSystemObject)
                .ToList();

            if (tables.Count == 0)
                return;

            writer.WriteLine("-- Data");
            foreach (Table table in tables)
                writer.WriteLine($"ALTER TABLE {Bracket(table.Schema)}.{Bracket(table.Name)} NOCHECK CONSTRAINT ALL");
            writer.WriteLine("GO");
            writer.WriteLine();

            int done = 0;
            foreach (Table table in tables)
            {
                log.Information("SQL export: scripting data for table {Schema}.{Table} ({Current}/{Total})",
                    table.Schema, table.Name, done + 1, tables.Count);
                writer.WriteLine($"-- Data for {Bracket(table.Schema)}.{Bracket(table.Name)}");
                ScriptTableDataWithTransfer(database, table, writer);
                writer.WriteLine();

                done++;
                ReportProgress(Math.Min(99, (done * 100) / tables.Count));
            }

            foreach (Table table in tables)
                writer.WriteLine($"ALTER TABLE {Bracket(table.Schema)}.{Bracket(table.Name)} WITH CHECK CHECK CONSTRAINT ALL");
            writer.WriteLine("GO");
        }

        private void ScriptTableDataWithTransfer(Database database, Table table, StreamWriter writer)
        {
            List<Column> insertableColumns = table.Columns
                .Cast<Column>()
                .Where(c => !c.Computed && !IsRowVersionColumn(c))
                .ToList();

            if (insertableColumns.Count == 0)
            {
                writer.WriteLine("-- No insertable columns.");
                return;
            }

            Transfer transfer = new Transfer(database)
            {
                CopyAllObjects = false,
                CopyData = true,
                CopySchema = false
            };

            transfer.ObjectList.Add(table);
            transfer.Options.ScriptSchema = false;
            transfer.Options.ScriptData = true;
            transfer.Options.ScriptBatchTerminator = true;
            transfer.Options.NoCommandTerminator = false;
            transfer.Options.SchemaQualify = true;

            WriteScript(writer, transfer.ScriptTransfer());
            log.Information("SQL export: table {Schema}.{Table} scripted with SMO Transfer",
                table.Schema, table.Name);
        }

        private static bool IsRowVersionColumn(Column column)
        {
            string typeName = column.DataType.Name;
            return typeName.Equals("timestamp", StringComparison.OrdinalIgnoreCase)
                || typeName.Equals("rowversion", StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteScript(StreamWriter writer, StringCollection script)
        {
            foreach (string line in script)
            {
                if (String.IsNullOrWhiteSpace(line))
                    continue;

                writer.WriteLine(line);

                if (!line.TrimEnd().EndsWith("GO", StringComparison.OrdinalIgnoreCase))
                    writer.WriteLine("GO");
            }
        }

        private static string Bracket(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        private static string EscapeSqlString(string value)
        {
            return value.Replace("'", "''");
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            return value;
        }

        private void BkpDBFull_Complete(object sender, Microsoft.SqlServer.Management.Common.ServerMessageEventArgs e)
        {
            Backup b = sender as Backup;
            ReportProgress(100);
        }

        private void BkpDBFull_PercentComplete(object sender, PercentCompleteEventArgs e)
        {
            ReportProgress(e.Percent);
        }

        public override string GetBackupFileName()
        {
            return Info.Name;
        //    throw new NotImplementedException();
        }
    }
}
