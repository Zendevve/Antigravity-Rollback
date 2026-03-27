using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Interface for backup service that handles Google Antigravity data backup.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Performs a backup of Antigravity data to a timestamped folder.
        /// </summary>
        /// <param name="compress">Whether to compress the backup into a ZIP file.</param>
        /// <returns>The path to the backup folder or ZIP file.</returns>
        /// <exception cref="IOException">If an I/O error occurs during backup.</exception>
        /// <exception cref="UnauthorizedAccessException">If access to a source or destination path is denied.</exception>
        Task<string> BackupAsync(bool compress = false);
    }
}
