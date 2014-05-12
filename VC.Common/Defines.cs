using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC
{
    public static class Defines
    {
        public static string[] EmailsDefault = new string[] { "eebyrne@gmail.com" };

        public static readonly string RootDirectory = @"C:\Network\";
        public static readonly string RootLocalDirectory = @"C:\Temp\";

        public static readonly string LogsRootDirectory = RootDirectory + @"Logs\";
        public static readonly string LogsRootLocalDirectory = RootLocalDirectory + @"Logs\";
    }
}
