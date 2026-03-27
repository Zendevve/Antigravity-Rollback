using System;
using System.Threading.Tasks;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for running the Antigravity installer and managing post-installation steps.
    /// </summary>
    public interface IInstallRunnerService
    {
        /// <summary>
        /// Runs the installation process for Antigravity.
        /// </summary>
        /// <param name="installerPath">Full path to the Antigravity installer executable.</param>
        /// <returns>Result of the installation process.</returns>
        Task<InstallRunnerResult> RunInstallationAsync(string installerPath);
    }

    /// <summary>
    /// Result of the installation process.
    /// </summary>
    public class InstallRunnerResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public InstallRunnerStage FailedStage { get; set; }
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Stages of the installation process for failure tracking.
    /// </summary>
    public enum InstallRunnerStage
    {
        NotStarted,
        ValidatingInstaller,
        RunningInstaller,
        WaitingForInstallCompletion,
        LaunchingAntigravity,
        VerifyingScaffold,
        Completed
    }
}
