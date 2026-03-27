using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Interface for the rollback orchestrator service that coordinates the complete rollback process.
    /// </summary>
    public interface IRollbackOrchestratorService
    {
        /// <summary>
        /// Gets the current phase of the rollback session.
        /// </summary>
        RollbackPhase CurrentPhase { get; }

        /// <summary>
        /// Gets whether a rollback session is currently in progress.
        /// </summary>
        bool IsInProgress { get; }

        /// <summary>
        /// Starts a new rollback session with the specified options.
        /// </summary>
        /// <param name="options">The rollback options.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartRollbackAsync(RollbackOptions options);

        /// <summary>
        /// Cancels the current rollback session.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CancelRollbackAsync();

        /// <summary>
        /// Gets the progress of the current rollback session.
        /// </summary>
        /// <returns>The rollback progress information.</returns>
        RollbackProgress GetProgress();

        /// <summary>
        /// Event that occurs when the rollback progress changes.
        /// </summary>
        event EventHandler<RollbackProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Event that occurs when the rollback session completes.
        /// </summary>
        event EventHandler<RollbackCompletedEventArgs> Completed;

        /// <summary>
        /// Event that occurs when the rollback session encounters an error.
        /// </summary>
        event EventHandler<RollbackErrorEventArgs> ErrorOccurred;
    }

    /// <summary>
    /// Represents the phases of a rollback session.
    /// </summary>
    public enum RollbackPhase
    {
        /// <summary>
        /// No rollback session is active.
        /// </summary>
        NotStarted,

        /// <summary>
        /// Performing backup of Antigravity data.
        /// </summary>
        Backup,

        /// <summary>
        /// Killing Antigravity processes.
        /// </summary>
        KillProcesses,

        /// <summary>
        /// Purging Antigravity installation files.
        /// </summary>
        Purge,

        /// <summary>
        /// Applying firewall blackout to prevent network access.
        /// </summary>
        FirewallBlackout,

        /// <summary>
        /// Installing the specified version of Antigravity.
        /// </summary>
        Install,

        /// <summary>
        /// Creating scaffold files and directories.
        /// </summary>
        ScaffoldCreation,

        /// <summary>
        /// Restoring data from backup.
        /// </summary>
        Restore,

        /// <summary>
        /// Injecting settings into the restored data.
        /// </summary>
        SettingsInjection,

        /// <summary>
        /// Verifying the rollback was successful.
        /// </summary>
        Verification,

        /// <summary>
        /// Rollback session completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Rollback session failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Rollback session was cancelled.
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Represents the options for a rollback session.
    /// </summary>
    public class RollbackOptions
    {
        /// <summary>
        /// Gets or sets whether to compress the backup.
        /// </>
        public bool CompressBackup { get; set; } = true;

        /// <summary>
        /// Gets or sets the target version to install.
        /// If null, the current version will be reinstalled.
        /// </summary>
        public VersionInfo TargetVersion { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the backup phase.
        /// </summary>
        public bool SkipBackup { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the kill processes phase.
        /// </summary>
        public bool SkipKillProcesses { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the purge phase.
        /// </summary>
        public bool SkipPurge { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the firewall blackout phase.
        /// </summary>
        public bool SkipFirewallBlackout { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the install phase.
        /// </summary>
        public bool SkipInstall { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the scaffold creation phase.
        /// </summary>
        public bool SkipScaffoldCreation { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the restore phase.
        /// </summary>
        public bool SkipRestore { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the settings injection phase.
        /// </summary>
        public bool SkipSettingsInjection { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the verification phase.
        /// </summary>
        public bool SkipVerification { get; set; }

        /// <summary>
        /// Gets or sets the timeout for each phase in seconds.
        /// </summary>
        public int PhaseTimeoutSeconds { get; set; } = 300; // 5 minutes default
    }

    /// <summary>
    /// Represents the progress of a rollback session.
    /// </summary>
    public class RollbackProgress
    {
        /// <summary>
        /// Gets or sets the current phase.
        /// </summary>
        public RollbackPhase CurrentPhase { get; set; }

        /// <summary>
        /// Gets or sets the percentage of completion (0-100).
        /// </summary>
        public int PercentageComplete { get; set; }

        /// <summary>
        /// Gets or sets the current phase description.
        /// </summary>
        public string CurrentPhaseDescription { get; set; }

        /// <summary>
        /// Gets or sets the elapsed time in seconds.
        /// </summary>
        public int ElapsedSeconds { get; set; }

        /// <summary>
        /// Gets or sets the estimated remaining time in seconds.
        /// </summary>
        public int EstimatedRemainingSeconds { get; set; }

        /// <summary>
        /// Gets or sets whether the operation can be cancelled.
        /// </summary>
        public bool CanCancel { get; set; } = true;

        /// <summary>
        /// Gets or sets the current step within the phase.
        /// </summary>
        public string CurrentStep { get; set; }

        /// <summary>
        /// Gets or sets the total steps in the current phase.
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// Gets or sets the current step number.
        /// </summary>
        public int CurrentStepNumber { get; set; }
    }

    /// <summary>
    /// Event arguments for progress changed events.
    /// </summary>
    public class RollbackProgressChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current progress.
        /// </summary>
        public RollbackProgress Progress { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackProgressChangedEventArgs"/> class.
        /// </summary>
        /// <param name="progress">The current progress.</param>
        public RollbackProgressChangedEventArgs(RollbackProgress progress)
        {
            Progress = progress;
        }
    }

    /// <summary>
    /// Event arguments for completed events.
    /// </summary>
    public class RollbackCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether the rollback was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the result of the rollback operation.
        /// </summary>
        public RollbackResult Result { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="success">Whether the rollback was successful.</param>
        /// <param name="result">The result of the rollback operation.</param>
        public RollbackCompletedEventArgs(bool success, RollbackResult result)
        {
            Success = success;
            Result = result;
        }
    }

    /// <summary>
    /// Event arguments for error events.
    /// </summary>
    public class RollbackErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the exception that occurred.
        /// </>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the phase during which the error occurred.
        /// </summary>
        public RollbackPhase Phase { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackErrorEventArgs"/> class.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="phase">The phase during which the error occurred.</param>
        public RollbackErrorEventArgs(Exception exception, RollbackPhase phase)
        {
            Exception = exception;
            Phase = phase;
        }
    }

    /// <summary>
    /// Represents the result of a rollback operation.
    /// </summary>
    public class RollbackResult
    {
        /// <summary>
        /// Gets or sets whether the rollback was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the backup path if backup was performed.
        /// </summary>
        public string BackupPath { get; set; }

        /// <summary>
        /// Gets or sets the version that was installed.
        /// </summary>
        public VersionInfo InstalledVersion { get; set; }

        /// <summary>
        /// Gets or sets the total time taken in seconds.
        /// </summary>
        public int TotalTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets any error message if the rollback failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the phases that were completed successfully.
        /// </summary>
        public List<RollbackPhase> CompletedPhases { get; set; } = new List<RollbackPhase>();

        /// <summary>
        /// Gets or sets the phases that failed.
        /// </summary>
        public List<RollbackPhase> FailedPhases { get; set; } = new List<RollbackPhase>();
    }
}
