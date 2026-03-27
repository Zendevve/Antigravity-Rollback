using System;
using System.Collections.Generic;
using System.IO;

namespace AGRollbackTool
{
    /// <summary>
    /// Service for resolving Antigravity-related paths.
    /// </summary>
    public class PathResolver : IPathResolver
    {
        /// <summary>
        /// Gets the raw chat JSON path.
        /// </summary>
        public AntigravityPathInfo GetGeminiAntigravityPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity");

            return GetPathInfo(path);
        }

        /// <summary>
        /// Gets the global agent rules path.
        /// </summary>
        public AntigravityPathInfo GetGeminiGlobalRulesPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "GEMINI.md");

            return GetPathInfo(path);
        }

        /// <summary>
        /// Gets the user settings path.
        /// </summary>
        public AntigravityPathInfo GetUserSettingsPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "antigravity",
                "User",
                "settings.json");

            return GetPathInfo(path);
        }

        /// <summary>
        /// Gets the user keybindings path.
        /// </summary>
        public AntigravityPathInfo GetUserKeybindingsPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "antigravity",
                "User",
                "keybindings.json");

            return GetPathInfo(path);
        }

        /// <summary>
        /// Gets the global storage state path.
        /// </summary>
        public AntigravityPathInfo GetGlobalStorageStatePath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "antigravity",
                "User",
                "globalStorage",
                "state.vscdb");

            return GetPathInfo(path);
        }

        /// <summary>
        /// Gets the application binary path.
        /// </summary>
        public AntigravityPathInfo GetApplicationBinaryPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "antigravity");

            return GetPathInfo(path);
        }

        /// <summary>
        /// Gets the staged update cache path.
        /// </summary>
        public AntigravityPathInfo GetStagedUpdateCachePath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "antigravity-updater");

            return GetPathInfo(path);
        }

        /// <summary>
        /// Gets all Antigravity paths.
        /// </summary>
        public IEnumerable<AntigravityPathInfo> GetAllAntigravityPaths()
        {
            yield return GetGeminiAntigravityPath();
            yield return GetGeminiGlobalRulesPath();
            yield return GetUserSettingsPath();
            yield return GetUserKeybindingsPath();
            yield return GetGlobalStorageStatePath();
            yield return GetApplicationBinaryPath();
            yield return GetStagedUpdateCachePath();
        }

        /// <summary>
        /// Helper method to get path information with error handling.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>AntigravityPathInfo with existence status and error message if any.</returns>
        private AntigravityPathInfo GetPathInfo(string path)
        {
            try
            {
                bool exists = Directory.Exists(path) || File.Exists(path);
                return new AntigravityPathInfo(path, exists);
            }
            catch (Exception ex)
            {
                // Handle cases where the path is inaccessible (e.g., due to permissions)
                return new AntigravityPathInfo(path, false, $"Error accessing path: {ex.Message}");
            }
        }
    }
}
