using System;
using Microsoft.Extensions.Configuration;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Interface for accessing application configuration settings.
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the backup directory path.
        /// </summary>
        string BackupDirectory { get; }

        /// <summary>
        /// Gets the maximum number of backups to retain.
        /// </summary>
        int MaxBackups { get; }

        /// <summary>
        /// Gets the phase timeout in minutes.
        /// </summary>
        int PhaseTimeoutMinutes { get; }

        /// <summary>
        /// Gets the installation timeout in minutes.
        /// </summary>
        int InstallTimeoutMinutes { get; }

        /// <summary>
        /// Gets the Antigravity installation path.
        /// </summary>
        string InstallationPath { get; }

        /// <summary>
        /// Gets the Antigravity user data path.
        /// </summary>
        string UserDataPath { get; }

        /// <summary>
        /// Gets the Antigravity app data path.
        /// </summary>
        string AppDataPath { get; }

        /// <summary>
        /// Gets the log level.
        /// </summary>
        string LogLevel { get; }

        /// <summary>
        /// Gets the log directory path.
        /// </summary>
        string LogDirectory { get; }

        /// <summary>
        /// Gets the phase timeout in seconds (computed from PhaseTimeoutMinutes).
        /// </summary>
        int PhaseTimeoutSeconds { get; }

        /// <summary>
        /// Gets the install timeout in seconds (computed from InstallTimeoutMinutes).
        /// </summary>
        int InstallTimeoutSeconds { get; }
    }

    /// <summary>
    /// Configuration service that loads settings from appsettings.json.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
        /// </summary>
        /// <param name="configuration">The Microsoft.Extensions.Configuration instance.</param>
        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc/>
        public string BackupDirectory => _configuration["BackupSettings:BackupDirectory"] ?? "C:\\ProgramData\\Antigravity\\Backups";

        /// <inheritdoc/>
        public int MaxBackups
        {
            get
            {
                if (int.TryParse(_configuration["BackupSettings:MaxBackups"], out int value))
                    return value;
                return 10;
            }
        }

        /// <inheritdoc/>
        public int PhaseTimeoutMinutes
        {
            get
            {
                if (int.TryParse(_configuration["TimeoutSettings:PhaseTimeoutMinutes"], out int value))
                    return value;
                return 30;
            }
        }

        /// <inheritdoc/>
        public int InstallTimeoutMinutes
        {
            get
            {
                if (int.TryParse(_configuration["TimeoutSettings:InstallTimeoutMinutes"], out int value))
                    return value;
                return 60;
            }
        }

        /// <inheritdoc/>
        public string InstallationPath => _configuration["AntigravityPaths:InstallationPath"] ?? "C:\\Program Files\\Antigravity";

        /// <inheritdoc/>
        public string UserDataPath => _configuration["AntigravityPaths:UserDataPath"] ?? "%APPDATA%\\Antigravity";

        /// <inheritdoc/>
        public string AppDataPath => _configuration["AntigravityPaths:AppDataPath"] ?? "%LOCALAPPDATA%\\Antigravity";

        /// <inheritdoc/>
        public string LogLevel => _configuration["LogSettings:LogLevel"] ?? "Information";

        /// <inheritdoc/>
        public string LogDirectory => _configuration["LogSettings:LogDirectory"] ?? "logs";

        /// <inheritdoc/>
        public int PhaseTimeoutSeconds => PhaseTimeoutMinutes * 60;

        /// <inheritdoc/>
        public int InstallTimeoutSeconds => InstallTimeoutMinutes * 60;
    }
}
