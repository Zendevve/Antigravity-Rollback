using System;

namespace AGRollbackTool.Models
{
    public class BackupEntry
    {
        public string RelativePath { get; set; }
        public string OriginalFullPath { get; set; }
        public string Sha256 { get; set; }
        public long SizeBytes { get; set; }
        public bool IsDirectory { get; set; }
    }
}
