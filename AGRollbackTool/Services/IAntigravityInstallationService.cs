using System;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for detecting Antigravity installation status and managing edge cases.
    /// </summary>
    public interface IAntigravityInstallationService
    {
        /// <summary>
        /// Checks if Antigravity is currently installed on the system.
        /// </summary>
        /// <returns>True if Antigravity is installed, false otherwise.</returns>
        bool IsAntigravityInstalled();

        /// <summary>
        /// Gets the current version of Antigravity if installed.
        /// </returns>
        /// <returns>VersionInfo if installed, null otherwise.</returns>
        VersionInfo? GetInstalledVersion();

        /// <summary>
        /// Checks if there are any existing backups.
        /// </summary>
        /// <returns>True if backups exist, false otherwise.</returns>
        bool HasExistingBackups();

        /// <summary>
        /// Gets information about the current installation state for UI display.
        /// </summary>
        /// <returns>InstallationState information.</returns>
        InstallationState GetInstallationState();
    }

    /// <summary>
    /// Represents the current state of Antigravity installation.
    /// </summary>
    public class InstallationState
    {
        /// <summary>
        /// Gets or sets whether Antigravity is installed.
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Gets or sets the installed version.
        /// </summary>
        public VersionInfo? InstalledVersion { get; set; }

        /// <summary>
        /// Gets or sets whether backups exist.
        /// </summary>
        public bool HasBackups { get; set; }

        /// <summary>
        /// Gets or sets the state category for UI handling.
        /// </summary>
        public InstallationStateCategory StateCategory { get; set; }

        /// <summary>
        /// Gets or sets a message describing the current state.
        /// </summary>
        public string StateMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Categories of installation state for UI handling.
    /// </summary>
    public enum InstallationStateCategory
    {
        /// <summary>
        /// Antigravity is installed and normal operations can proceed.
        /// </summary>
        Normal,

        /// <summary>
        /// Antigravity is not installed - nothing to back up.
        /// </summary>
        NotInstalled,

        /// <summary>
        /// Antigravity is not installed but backups exist - restore only mode.
        /// </summary>
        RestoreOnly,

        /// <summary>
        /// Unknown state.
        /// </summary>
        Unknown
    }
}
