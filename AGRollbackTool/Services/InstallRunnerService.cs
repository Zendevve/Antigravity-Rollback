using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AGRollbackTool.Services;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for running the Antigravity installer and managing post-installation steps.
    /// </summary>
    public class InstallRunnerService : IInstallRunnerService
    {
        private readonly IProcessKiller _processKiller;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallRunnerService"/> class.
        /// </summary>
        public InstallRunnerService(IProcessKiller processKiller)
        {
            _processKiller = processKiller ?? throw new ArgumentNullException(nameof(processKiller));
        }

        /// <inheritdoc/>
        public async Task<InstallRunnerResult> RunInstallationAsync(string installerPath)
        {
            var result = new InstallRunnerResult();

            try
            {
                // Stage 1: Validate installer
                result.FailedStage = InstallRunnerStage.ValidatingInstaller;
                if (!ValidateInstaller(installerPath))
                {
                    result.Message = $"Installer validation failed: File not found or invalid at {installerPath}";
                    return result;
                }

                // Stage 2: Run installer silently
                result.FailedStage = InstallRunnerStage.RunningInstaller;
                var installSuccess = await RunSilentInstallerAsync(installerPath);
                if (!installSuccess)
                {
                    result.Message = "Failed to start the installer process.";
                    return result;
                }

                // Stage 3: Wait for installation to complete
                result.FailedStage = InstallRunnerStage.WaitingForInstallCompletion;
                var installComplete = await WaitForInstallationCompletionAsync();
                if (!installComplete)
                {
                    result.Message = "Installation timed out or failed to complete.";
                    return result;
                }

                // Stage 4: Launch AG once → wait 3 seconds → kill it (to create folder scaffold)
                result.FailedStage = InstallRunnerStage.LaunchingAntigravity;
                var agLaunchSuccess = LaunchAndTerminateAntigravityForScaffold();
                if (!agLaunchSuccess)
                {
                    result.Message = "Failed to launch and terminate Antigravity for scaffold creation.";
                    return result;
                }

                // Stage 5: Verify scaffold directories exist
                result.FailedStage = InstallRunnerStage.VerifyingScaffold;
                var scaffoldVerified = VerifyScaffoldDirectories();
                if (!scaffoldVerified)
                {
                    result.Message = "Antigravity scaffold directories were not created properly.";
                    return result;
                }

                // Success
                result.FailedStage = InstallRunnerStage.Completed;
                result.Success = true;
                result.Message = "Antigravity installation completed successfully.";
                return result;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = $"Unexpected error during installation: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Validates the installer file exists and is a valid executable.
        /// </summary>
        /// <param name="installerPath">Path to the installer.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private bool ValidateInstaller(string installerPath)
        {
            if (string.IsNullOrWhiteSpace(installerPath))
                return false;

            if (!File.Exists(installerPath))
                return false;

            // Additional validation: check if it's an executable file
            var extension = Path.GetExtension(installerPath).ToLowerInvariant();
            return extension == ".exe";
        }

        /// <summary>
        /// Runs the installer silently with the specified parameters.
        /// </summary>
        /// <param name="installerPath">Path to the installer.</param>
        /// <returns>True if process started successfully, false otherwise.</returns>
        private async Task<bool> RunSilentInstallerAsync(string installerPath)
        {
            try
            {
                var installDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs",
                    "antigravity");

                // Ensure the install directory exists
                Directory.CreateDirectory(installDir);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = $"/S /D=\"{installDir}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                // Wait a bit for the process to start
                await Task.Delay(1000);

                return !process.HasExited || process.ExitCode == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for installation to complete by polling for directory creation and process exit.
        /// </summary>
        /// <returns>True if installation completed successfully, false if timed out.</returns>
        private async Task<bool> WaitForInstallationCompletionAsync()
        {
            const int maxWaitTimeSeconds = 120; // 2 minutes timeout
            const int pollIntervalSeconds = 5;
            int elapsedSeconds = 0;

            var installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "antigravity");

            while (elapsedSeconds < maxWaitTimeSeconds)
            {
                // Check if the installation directory exists and has content
                if (Directory.Exists(installDir) && Directory.GetFileSystemEntries(installDir).Length > 0)
                {
                    // Additional check: wait for any installer processes to exit
                    // Common installer process names
                    var installerProcessNames = new[] { "antigravity", "antigravity-helper", "antigravity-utility" };
                    bool installerProcessesRunning = false;

                    foreach (var name in installerProcessNames)
                    {
                        var processes = Process.GetProcessesByName(name);
                        if (processes.Length > 0)
                        {
                            installerProcessesRunning = true;
                            break;
                        }
                    }

                    if (!installerProcessesRunning)
                    {
                        // Give it a moment to settle
                        await Task.Delay(2000);
                        return true;
                    }
                }

                await Task.Delay(pollIntervalSeconds * 1000);
                elapsedSeconds += pollIntervalSeconds;
            }

            return false;
        }

        /// <summary>
        /// Launches Antigravity, waits 3 seconds, then kills it to create the folder scaffold.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        private bool LaunchAndTerminateAntigravityForScaffold()
        {
            try
            {
                var antigravityExePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs",
                    "antigravity",
                    "antigravity.exe");

                if (!File.Exists(antigravityExePath))
                {
                    return false;
                }

                // Launch Antigravity
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = antigravityExePath,
                    UseShellExecute = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    return false;
                }

                // Wait 3 seconds
                Thread.Sleep(3000);

                // Kill the process and all related Antigravity processes
                _processKiller.KillAllAntigravityProcesses();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Verifies that the Antigravity scaffold directories exist in %APPDATA%.
        /// </summary>
        /// <returns>True if scaffold directories exist, false otherwise.</returns>
        private bool VerifyScaffoldDirectories()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "antigravity");

                // Check if the main antigravity directory exists
                if (!Directory.Exists(appDataPath))
                {
                    return false;
                }

                // Check for key subdirectories that should be created
                var requiredPaths = new[]
                {
                    Path.Combine(appDataPath, "User"),
                    Path.Combine(appDataPath, "User", "globalStorage")
                };

                foreach (var path in requiredPaths)
                {
                    if (!Directory.Exists(path))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
