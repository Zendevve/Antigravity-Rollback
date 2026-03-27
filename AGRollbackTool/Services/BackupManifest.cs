using System;
using System.Collections.Generic;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Represents a backup manifest containing metadata about backed-up files.
    /// </summary>
    public class BackupManifest
    {
        /// <summary>
        /// Gets or sets the timestamp when the backup was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the list of backup entries.
        /// </summary>
        public List<BackupEntry> Entries { get; set; } = new List<BackupEntry>();

        /// <summary>
        /// Gets or sets whether the backup was compressed.
        /// </summary>
        public bool IsCompressed { get; set; }
    }

    /// <summary>
    /// Represents an entry in the backup manifest for a single file.
    /// </>
    public class BackupEntry
    {
        /// <summary>
        /// Gets or sets the relative path of the file within the backup.
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the original full path of the file.
        /// </summary>
        public string OriginalPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the SHA-256 hash of the file content.
        /// </summary>
        public string Sha256Hash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last modified time of the file.
        /// </summary>
        public DateTime LastModified { get; set; }
    }
}
