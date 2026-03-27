using System;
using System.Collections.Generic;

namespace AGRollbackTool.Models
{
    public class BackupManifest
    {
        public string BackupId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SourceVersion { get; set; }
        public string TargetVersion { get; set; }
        public List<BackupEntry> Entries { get; set; } = new List<BackupEntry>();
    }
}
