using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AGRollbackTool.Models;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for detecting Antigravity installation status and managing edge cases.
    /// </summary>
    public class AntigravityInstallationService : IAntigravityInstallationService
    {
        private readonly IPathResolver _pathResolver;
        private readonly string _backupRootPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AntigravityInstallationService"/> class.
        /// </summary>
        /// <param name="pathResolver">The path resolver service.</param>
        public AntigravityInstallationService(IPathResolver pathResolver)
        {
            _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));

            // Default backup root: %USERPROFILE%\Documents\AG Backups\
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _backupRootPath = Path.Combine(documentsPath, "AG Backups");
        }

        /// <inheritdoc/>
        public bool IsAntigravityInstalled()
        {
            // Check if any of the key Antigravity paths exist
            var appBinaryPath = _pathResolver.GetApplicationBinaryPath();
            if (appBinaryPath.Exists)
            {
                // Check for the actual executable
                string exePath = Path.Combine(appBinaryPath.Path, "antigravity.exe");
                return File.Exists(exePath);
            }

            // Also check for user data paths as a fallback
            var geminiPath = _pathResolver.GetGeminiAntigravityPath();
            return geminiPath.Exists;
        }

        /// <inheritdoc/>
        public VersionInfo? GetInstalledVersion()
        {
            // For now, we'll return null as version detection would require running the app
            // In a real implementation, this would read version from the app's metadata
            return null;
        }

        /// <inheritdoc/>
        public bool HasExistingBackups()
        {
            try
            {
                if (!Directory.Exists(_backupRootPath))
                {
                    return false;
                }

                // Check for any backup directories or zip files
                var directories = Directory.GetDirectories(_backupRootPath);
                var zipFiles = Directory.GetFiles(_backupRootPath, "*.zip");

                return directories.Length > 0 || zipFiles.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public InstallationState GetInstallationState()
        {
            bool isInstalled = IsAntigravityInstalled();
            bool hasBackups = HasExistingBackups();

            var state = new InstallationState
            {
                IsInstalled = isInstalled,
                HasBackups = hasBackups,
                InstalledVersion = isInstalled ? GetInstalledVersion() : null
            };

            // Determine the state category
            if (isInstalled)
            {
                state.StateCategory = InstallationStateCategory.Normal;
                state.StateMessage = hasBackups
                    ? "Antigravity is installed. You can backup, rollback, or restore from a backup."
                    : "Antigravity is installed. You can backup your data or perform a rollback.";
            }
            else if (hasBackups)
            {
                state.StateCategory = InstallationStateCategory.RestoreOnly;
                state.StateMessage = "Antigravity is not currently installed. " +
                    "Backups exist on this system. You can restore from a backup to reinstall Antigravity.";
            }
            else
            {
                state.StateCategory = InstallationStateCategory.NotInstalled;
                state.StateMessage = "Antigravity is not installed and no backups exist. " +
                    "Nothing to back up. To use this tool, first install Antigravity.";
            }

            return state;
        }
    }
}
