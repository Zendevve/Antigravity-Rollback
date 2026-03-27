using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Interface for restoring Google Antigravity data from backups.
    /// </summary>
    public interface IRestoreService
    {
        /// <summary>
        /// Restores data from a backup folder or zip file.
        /// </summary>
        /// <param name="backupPath">Path to the backup folder or zip file.</param>
        /// <param name="verifyHashes">Whether to verify SHA-256 hashes before restoring.</param>
        /// <returns>A restore result detailing what was restored and any errors.</returns>
        Task<RestoreResult> RestoreAsync(string backupPath, bool verifyHashes = true);
    }
}
