using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamCleaner.Models
{
    public class ScannerSettings
    {
        public int RunIntervalMinutes { get; set; }
        public string[] TargetDirectories { get; set; } = [];
        public string[] AllowedExtensions { get; set; } = [];
        public string OutputPath { get; set; } = string.Empty;
        public bool RemoveOlderFiles { get; set; }
        public string MoveToBackupFolder { get; set; } = string.Empty;
    }
}
