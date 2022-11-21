using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SmartBackup.Common;
using System.IO;
using System.Diagnostics;
using Serilog;

namespace SmartBackup.Services
{
    public  class PluginManager
    {
        public class Plugin
        {
            
            public Plugin(Type module, Type Info ) {
              
                BackupModule = module;
                BackupInfo = Info;
            }
            public Type  BackupModule { get; set; }
            public Type BackupInfo { get; set; }
        }
        public Dictionary<string, Plugin> Modules { get; private set; }
      public string  AppPath { get; set; }


        private static List<Assembly> GetAllAssembly()
        {

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

            HashSet<string> loadedAssemblies = new();

            foreach (var item in allAssemblies)
            {
                loadedAssemblies.Add(item.FullName!);
            }

            Queue<Assembly> assembliesToCheck = new();
            assembliesToCheck.Enqueue(Assembly.GetEntryAssembly()!);

            while (assembliesToCheck.Any())
            {
                var assemblyToCheck = assembliesToCheck.Dequeue();
                foreach (var reference in assemblyToCheck!.GetReferencedAssemblies())
                {
                    if (!loadedAssemblies.Contains(reference.FullName))
                    {
                        var assembly = Assembly.Load(reference);

                        assembliesToCheck.Enqueue(assembly);

                        loadedAssemblies.Add(reference.FullName);

                        allAssemblies.Add(assembly);
                    }
                }
            }

            return allAssemblies;
        }



        List<Assembly> allAssemblies = null;
        public PluginManager()
        {

            Modules = new Dictionary<string, Plugin>(StringComparer.InvariantCultureIgnoreCase);
            //   AppPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            AppPath = Path.GetDirectoryName(Environment.ProcessPath);


            allAssemblies = GetAllAssembly();


            List<Assembly> aa = GetAllAssembly();

            var asms = Assembly.GetExecutingAssembly().ExportedTypes;

            /*
            foreach (var reference in asms)
            {
                try
                {
                    var assembly = Assembly.Load(reference);
                    allAssemblies.Add(assembly);
                    Log.Logger.Information("Loaded assembly {0} : {1}", reference.Name);
                }
                catch (Exception x)
                {
                    Log.Logger.Verbose("Unable to load assembly {0} : {1}", reference.Name, x.Message);
                }
            }
            */

           // LoadPluginsFromFolder(AppPath);
            /*
            string modulesFolder = Path.Combine(AppPath, "Modules");

            if (Directory.Exists(modulesFolder))
            {
                LoadPluginsFromFolder(modulesFolder);
            }
            else
            {
                LoadPluginsFromFolder(AppPath);
            }*/

        }

        private void LoadPluginsFromFolder(string folder)
        {
            foreach (string dll in Directory.GetFiles(folder, "*.dll"))
            {
                try
                {

                    // Assembly.UnsafeLoadFrom
                    Assembly asm = Assembly.UnsafeLoadFrom(dll);
                    if (!allAssemblies.Any(o => o.FullName == asm.FullName))
                    {
                        allAssemblies.Add(asm);

                    }
                }
                catch (BadImageFormatException bx)
                {

                }
                catch (Exception x)
                {
                    Log.Logger.Verbose("Unable to load assembly {0} : {1}", dll, x.Message);
                }
            }
        }

        public void  Init()
        {
            Modules.Clear();

            foreach (Assembly assembly in allAssemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    var attribs = type.GetCustomAttributes(typeof(BackupModuleAttribute), false);
                    if (attribs != null && attribs.Any())
                    { 
                        foreach (var attr in attribs)
                        {
                            BackupModuleAttribute bma = (BackupModuleAttribute)attr;

                           bool added= Modules.TryAdd(bma.Type, new Plugin( type, bma.ConfigurationType));

                        }
                    }
                }
            }
             

        }
        public Type? GetBackupModule(string TypeStr)
        {
            if(Modules.ContainsKey(TypeStr))
            {
                return Modules[TypeStr].BackupModule; 
            }
            else
            {
                return null;
            }
           

        }

    }
}
