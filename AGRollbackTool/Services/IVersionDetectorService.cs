using System;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Interface for detecting the version of Google Antigravity.
    /// </summary>
    public interface IVersionDetectorService
    {
        /// <summary>
        /// Detects the version of Google Antigravity.
        /// </summary>
        /// <returns>Version information.</returns>
        VersionInfo DetectVersion();
    }
}
