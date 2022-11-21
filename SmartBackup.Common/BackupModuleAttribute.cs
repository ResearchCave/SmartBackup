using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBackup.Common
{
   [AttributeUsage(AttributeTargets.Class , AllowMultiple = false )]

   public    class BackupModuleAttribute : Attribute
    {
        public string Type { get; set; }
        public Type ConfigurationType { get; set; }
    } 
}
