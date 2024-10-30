using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Agent
{
    public class Options
    {
 

        [Option('v',  "validate",   Required = false, HelpText = "Validate config file against config schema." , Default = false)]
        public bool Validate { get; set; }
      
        [Option('g',"generateschema",  Required = false, HelpText = "Creates Json Schema containing all the options", Default = false)]
        public bool GenerateSchema { get; set; }

    }
}
