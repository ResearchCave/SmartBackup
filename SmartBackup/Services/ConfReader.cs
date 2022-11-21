using Newtonsoft.Json;
using SmartBackup.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartBackup.Services
{
    public class ConfReader : IConfReader
    {
        const string ConfigFileName = "config.json";
        public Config Configuration { get; private set; }
        private bool loaded = false;
        public ConfReader()
        {

        }
        private string GetConfigPath()
        {
            string AppPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string ConfigPath = Path.Combine(AppPath, ConfigFileName);
            return ConfigPath;
        }


        public void Load()
        {
            if (loaded) return;
            loaded = true;

            string ConfigPath = GetConfigPath();

            string jsstring = System.IO.File.ReadAllText(ConfigPath);
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                Converters ={
                 new JsonStringEnumConverter()
                },
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            Configuration = System.Text.Json.JsonSerializer.Deserialize<Config>(jsstring, options);
         
            /*
            if( String.IsNullOrEmpty(Configuration.TempPath))
            {
                Configuration.TempPath = Path.GetTempPath();
            }
            */

        }

    }
}
