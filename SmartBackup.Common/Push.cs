 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartBackup.Model
{
     public class Push  
    {
        [Required]

        public ErrorActionPreferences ErrorActionPreference { get; set; } = ErrorActionPreferences.Stop;

        [DisplayName("Push Path")]
        [Description("The target folder to rsync/robocopy the resulting backup files.")]
        public string PushPath { get; set; }

 

    }
}
