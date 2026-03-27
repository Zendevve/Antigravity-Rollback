using System.Collections.Generic;

namespace AGRollbackTool
{
    /// <summary>
    /// Interface for resolving Antigravity-related paths.
    /// </summary>
    public interface IPathResolver
    {
        /// <summary>
        /// Gets the raw chat JSON path.
        /// </summary>
        AntigravityPathInfo GetGeminiAntigravityPath();

        /// <summary>
        /// Gets the global agent rules path.
        /// </summary>
        AntigravityPathInfo GetGeminiGlobalRulesPath();

        /// <summary>
        /// Gets the user settings path.
        /// </summary>
        AntigravityPathInfo GetUserSettingsPath();

        /// <summary>
        /// Gets the user keybindings path.
        /// </summary>
        AntigravityPathInfo GetUserKeybindingsPath();

        /// <summary>
        /// Gets the global storage state path.
        /// </summary>
        AntigravityPathInfo GetGlobalStorageStatePath();

        /// <summary>
        /// Gets the application binary path.
        /// </summary>
        AntigravityPathInfo GetApplicationBinaryPath();

        /// <summary>
        /// Gets the staged update cache path.
        /// </summary>
        AntigravityPathInfo GetStagedUpdateCachePath();

        /// <summary>
        /// Gets all Antigravity paths.
        /// </summary>
        IEnumerable<AntigravityPathInfo> GetAllAntigravityPaths();
    }
}
