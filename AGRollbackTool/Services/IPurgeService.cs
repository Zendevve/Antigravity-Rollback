using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Result of a purge operation.
    /// </summary>
    public class PurgeResult
    {
        public bool Success { get; set; }
        public int DirectoriesDeleted { get; set; }
        public int RegistryKeysDeleted { get; set; }
        public int ShortcutsDeleted { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> PurgedItems { get; } = new List<string>();
    }

    /// <summary>
    /// Interface for purging Google Antigravity from a Windows system.
    /// </summary>
    public interface IPurgeService
    {
        /// <summary>
        /// Purges Google Antigravity from the system.
        /// </summary>
        /// <returns>A purge result detailing what was purged and any errors.</returns>
        Task<PurgeResult> PurgeAsync();
    }
}
