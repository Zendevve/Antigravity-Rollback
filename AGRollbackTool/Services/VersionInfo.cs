using System;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Data transfer object for version information.
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// The version string (e.g., "1.2.3").
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Indicates whether the version was successfully detected.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Error message if version detection failed.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionInfo"/> class.
        /// </summary>
        /// <param name="version">The version string.</param>
        /// <param name="success">Whether the version detection was successful.</param>
        /// <param name="errorMessage">Error message if any.</param>
        public VersionInfo(string version, bool success, string errorMessage = null)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Success = success;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }
}
