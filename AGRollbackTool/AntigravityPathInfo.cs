using System;

namespace AGRollbackTool
{
    /// <summary>
    /// Data transfer object for Antigravity path information.
    /// </summary>
    public class AntigravityPathInfo
    {
        /// <summary>
        /// The resolved path string.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Indicates whether the path exists.
        /// </summary>
        public bool Exists { get; }

        /// <summary>
        /// Error message if the path could not be accessed or resolved.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AntigravityPathInfo"/> class.
        /// </summary>
        /// <param name="path">The resolved path.</param>
        /// <param name="exists">Whether the path exists.</param>
        /// <param name="errorMessage">Error message if any.</param>
        public AntigravityPathInfo(string path, bool exists, string errorMessage = null)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Exists = exists;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }
}
