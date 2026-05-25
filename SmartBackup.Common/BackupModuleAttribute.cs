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

        //public BackupModuleAttribute(string type, Type configurationType)
        //{
        //    if (!typeof(BackupModuleConfiguration).IsAssignableFrom(configurationType))
        //    {
        //        throw new ArgumentException(
        //            $"{configurationType.FullName} must inherit from {typeof(BackupModuleConfiguration).FullName}",
        //            nameof(configurationType));
        //    }

        //    Type = type;
        //    ConfigurationType = configurationType;
        //}
    }

    public class PushModuleAttribute : Attribute
    {
        public string Type { get; set; }
        public Type ConfigurationType { get; set; }
    }
}
