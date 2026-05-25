using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamCleaner.Models
{
    public class DuplicateFileAction
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
