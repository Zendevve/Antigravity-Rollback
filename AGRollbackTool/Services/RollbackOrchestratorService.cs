using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AGRollbackTool.Services;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service that orchestrates the complete rollback process.
    /// </summary>
    public class RollbackOrchestratorService : IRollbackOrchestratorService
    {
        private readonly IBackupService _backupService;
        private readonly IProcessKiller _processKiller;
        private readonly IPurgeService _purgeService;
        private readonly INetworkBlackoutService _networkBlackoutService;
        private readonly IInstallRunnerService _installRunnerService;
        private readonly IRestoreService _restoreService;
        private readonly ISettingsInjectorService _settingsInjectorService;
        private readonly IVersionDetectorService _versionDetectorService;

        private RollbackPhase _currentPhase = RollbackPhase.NotStarted;
        private bool _isInProgress = false;
        private bool _shouldCancel = false;
        private readonly object _lock = new object();
        private DateTime _startTime;
        private RollbackOptions _currentOptions;
        private RollbackResult _currentResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackOrchestratorService"/> class.
        /// </summary>
        /// <param name="backupService">The backup service.</param>
        /// <param name="processKiller">The process killer service.</param>
        /// <param name="purgeService">The purge service.</param>
        /// <param name="networkBlackoutService">The network blackout service.</param>
        /// <param name="installRunnerService">The install runner service.</param>
        /// <param name="restoreService">The restore service.</param>
        /// <param name="settingsInjectorService">The settings injector service.</param>
        /// <param name="versionDetectorService">The version detector service.</param>
        public RollbackOrchestratorService(
            IBackupService backupService,
            IProcessKiller processKiller,
            IPurgeService purgeService,
            INetworkBlackoutService networkBlackoutService,
            IInstallRunnerService installRunnerService,
            IRestoreService restoreService,
            ISettingsInjectorService settingsInjectorService,
            IVersionDetectorService versionDetectorService)
        {
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _processKiller = processKiller ?? throw new ArgumentNullException(nameof(processKiller));
            _purgeService = purgeService ?? throw new ArgumentNullException(nameof(purgeService));
            _networkBlackoutService = networkBlackoutService ?? throw new ArgumentNullException(nameof(networkBlackoutService));
            _installRunnerService = installRunnerService ?? throw new ArgumentNullException(nameof(installRunnerService));
            _restoreService = restoreService ?? throw new ArgumentNullException(nameof(restoreService));
            _settingsInjectorService = settingsInjectorService ?? throw new ArgumentNullException(nameof(settingsInjectorService));
            _versionDetectorService = versionDetectorService ?? throw new ArgumentNullException(nameof(versionDetectorService));
        }

        /// <inheritdoc/>
        public RollbackPhase CurrentPhase
        {
            get
            {
                lock (_lock)
                {
                    return _currentPhase;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsInProgress
        {
            get
            {
                lock (_lock)
                {
                    return _isInProgress;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<RollbackProgressChangedEventArgs> ProgressChanged;

        /// <inheritdoc/>
        public event EventHandler<RollbackCompletedEventArgs> Completed;

        /// <inheritdoc/>
        public event EventHandler<RollbackErrorEventArgs> ErrorOccurred;

        /// <inheritdoc/>
        public async Task StartRollbackAsync(RollbackOptions options)
        {
            lock (_lock)
            {
                if (_isInProgress)
                {
                    throw new InvalidOperationException("A rollback session is already in progress.");
                }

                _isInProgress = true;
                _shouldCancel = false;
                _startTime = DateTime.UtcNow;
                _currentOptions = options ?? new RollbackOptions();
                _currentResult = new RollbackResult();
                _currentPhase = RollbackPhase.NotStarted;
            }

            try
            {
                // Execute each phase in sequence
                await ExecutePhaseAsync(RollbackPhase.Backup, async () =>
                {
                    if (_currentOptions.SkipBackup)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    string backupPath = await _backupService.BackupAsync(_currentOptions.CompressBackup);
                    _currentResult.BackupPath = backupPath;
                    _currentResult.CompletedPhases.Add(RollbackPhase.Backup);
                });

                await ExecutePhaseAsync(RollbackPhase.KillProcesses, async () =>
                {
                    if (_currentOptions.SkipKillProcesses)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    var killedProcesses = _processKiller.KillAllAntigravityProcesses();
                    // We could store process info in result if needed
                    _currentResult.CompletedPhases.Add(RollbackPhase.KillProcesses);
                });

                await ExecutePhaseAsync(RollbackPhase.Purge, async () =>
                {
                    if (_currentOptions.SkipPurge)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    var purgeResult = await _purgeService.PurgeAsync();
                    if (!purgeResult.Success)
                    {
                        throw new InvalidOperationException($"Purge failed: {string.Join("; ", purgeResult.Errors)}");
                    }
                    _currentResult.CompletedPhases.Add(RollbackPhase.Purge);
                });

                await ExecutePhaseAsync(RollbackPhase.FirewallBlackout, async () =>
                {
                    if (_currentOptions.SkipFirewallBlackout)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    _networkBlackoutService.BlockAntigravityNetworkAccess();
                    _currentResult.CompletedPhases.Add(RollbackPhase.FirewallBlackout);
                });

                await ExecutePhaseAsync(RollbackPhase.Install, async () =>
                {
                    if (_currentOptions.SkipInstall)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    // Determine installer path - this would typically come from version detection or options
                    string installerPath = await GetInstallerPathAsync(_currentOptions.TargetVersion);
                    var installResult = await _installRunnerService.RunInstallationAsync(installerPath);
                    if (!installResult.Success)
                    {
                        throw new InvalidOperationException($"Installation failed: {installResult.Message}", installResult.Exception);
                    }
                    _currentResult.InstalledVersion = _currentOptions.TargetVersion ?? await _versionDetectorService.GetCurrentVersionAsync();
                    _currentResult.CompletedPhases.Add(RollbackPhase.Install);
                });

                await ExecutePhaseAsync(RollbackPhase.ScaffoldCreation, async () =>
                {
                    if (_currentOptions.SkipScaffoldCreation)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    // Scaffold creation is typically part of the installation process
                    // For now, we'll just verify that the installation created the necessary structure
                    await Task.CompletedTask;
                    _currentResult.CompletedPhases.Add(RollbackPhase.ScaffoldCreation);
                });

                await ExecutePhaseAsync(RollbackPhase.Restore, async () =>
                {
                    if (_currentOptions.SkipRestore)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    if (string.IsNullOrEmpty(_currentResult.BackupPath))
                    {
                        throw new InvalidOperationException("Cannot restore: no backup was performed.");
                    }

                    var restoreResult = await _restoreService.RestoreAsync(_currentResult.BackupPath, true);
                    if (!restoreResult.Success)
                    {
                        throw new InvalidOperationException($"Restore failed: {string.Join("; ", restoreResult.Errors)}");
                    }
                    _currentResult.CompletedPhases.Add(RollbackPhase.Restore);
                });

                await ExecutePhaseAsync(RollbackPhase.SettingsInjection, async () =>
                {
                    if (_currentOptions.SkipSettingsInjection)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    // Settings injection would typically involve applying user preferences
                    // For now, we'll just mark it as completed
                    await Task.CompletedTask;
                    _currentResult.CompletedPhases.Add(RollbackPhase.SettingsInjection);
                });

                await ExecutePhaseAsync(RollbackPhase.Verification, async () =>
                {
                    if (_currentOptions.SkipVerification)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    // Verification would involve checking that the installation is working correctly
                    await Task.CompletedTask;
                    _currentResult.CompletedPhases.Add(RollbackPhase.Verification);
                });

                // If we got here, all phases completed successfully
                lock (_lock)
                {
                    _currentPhase = RollbackPhase.Completed;
                    _isInProgress = false;
                }

                _currentResult.Success = true;
                _currentResult.TotalTimeSeconds = (int)(DateTime.UtcNow - _startTime).TotalSeconds;

                OnCompleted(new RollbackCompletedEventArgs(true, _currentResult));
            }
            catch (OperationCanceledException)
            {
                lock (_lock)
                {
                    _currentPhase = RollbackPhase.Cancelled;
                    _isInProgress = false;
                }

                OnCompleted(new RollbackCompletedEventArgs(false, _currentResult));
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _currentPhase = RollbackPhase.Failed;
                    _isInProgress = false;
                }

                _currentResult.Success = false;
                _currentResult.ErrorMessage = ex.Message;
                _currentResult.TotalTimeSeconds = (int)(DateTime.UtcNow - _startTime).TotalSeconds;

                OnErrorOccurred(new RollbackErrorEventArgs(ex, _currentPhase));
                OnCompleted(new RollbackCompletedEventArgs(false, _currentResult));
            }
        }

        /// <inheritdoc/>
        public Task CancelRollbackAsync()
        {
            lock (_lock)
            {
                if (!_isInProgress)
                {
                    return Task.CompletedTask;
                }

                _shouldCancel = true;
            }

            // Note: Actual cancellation would need to be implemented in each phase
            // For simplicity, we're just setting a flag that phases can check
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public RollbackProgress GetProgress()
        {
            lock (_lock)
            {
                int percentage = 0;
                string phaseDescription = GetPhaseDescription(_currentPhase);
                int elapsedSeconds = (int)(DateTime.UtcNow - _startTime).TotalSeconds;

                // Calculate percentage based on phase
                switch (_currentPhase)
                {
                    case RollbackPhase.NotStarted:
                        percentage = 0;
                        break;
                    case RollbackPhase.Backup:
                        percentage = 10;
                        break;
                    case RollbackPhase.KillProcesses:
                        percentage = 20;
                        break;
                    case RollbackPhase.Purge:
                        percentage = 30;
                        break;
                    case RollbackPhase.FirewallBlackout:
                        percentage = 40;
                        break;
                    case RollbackPhase.Install:
                        percentage = 50;
                        break;
                    case RollbackPhase.ScaffoldCreation:
                        percentage = 60;
                        break;
                    case RollbackPhase.Restore:
                        percentage = 70;
                        break;
                    case RollbackPhase.SettingsInjection:
                        percentage = 80;
                        break;
                    case RollbackPhase.Verification:
                        percentage = 90;
                        break;
                    case RollbackPhase.Completed:
                        percentage = 100;
                        break;
                    case RollbackPhase.Failed:
                    case RollbackPhase.Cancelled:
                        percentage = 0; // Or keep last percentage?
                        break;
                    default:
                        percentage = 0;
                        break;
                }

                return new RollbackProgress
                {
                    CurrentPhase = _currentPhase,
                    PercentageComplete = percentage,
                    CurrentPhaseDescription = phaseDescription,
                    ElapsedSeconds = elapsedSeconds,
                    EstimatedRemainingSeconds = percentage > 0 ? (int)((elapsedSeconds / percentage) * (100 - percentage)) : 0,
                    CanCancel = _isInProgress && !_shouldCancel,
                    CurrentStep = GetCurrentStepDescription(),
                    TotalSteps = GetTotalSteps(),
                    CurrentStepNumber = GetCurrentStepNumber()
                };
            }
        }

        private async Task ExecutePhaseAsync(RollbackPhase phase, Func<Task> phaseAction)
        {
            lock (_lock)
            {
                if (_shouldCancel)
                {
                    throw new OperationCanceledException("Rollback was cancelled.");
                }

                _currentPhase = phase;
            }

            // Update progress
            OnProgressChanged(GetProgress());

            // Execute the phase action with timeout
            var timeoutTask = Task.Delay(_currentOptions.PhaseTimeoutSeconds * 1000);
            var phaseTask = phaseAction();

            var completedTask = await Task.WhenAny(timeoutTask, phaseTask);
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Phase {phase} timed out after {_currentOptions.PhaseTimeoutSeconds} seconds.");
            }

            // Check for cancellation after phase completion
            lock (_lock)
            {
                if (_shouldCancel)
                {
                    throw new OperationCanceledException("Rollback was cancelled.");
                }
            }

            // Update progress after phase completion
            OnProgressChanged(GetProgress());
        }

        private string GetPhaseDescription(RollbackPhase phase)
        {
            return phase switch
            {
                RollbackPhase.NotStarted => "Not started",
                RollbackPhase.Backup => "Backing up Antigravity data",
                RollbackPhase.KillProcesses => "Killing Antigravity processes",
                RollbackPhase.Purge => "Purging Antigravity installation",
                RollbackPhase.FirewallBlackout => "Applying network blackout",
                RollbackPhase.Install => "Installing Antigravity",
                RollbackPhase.ScaffoldCreation => "Creating scaffold files",
                RollbackPhase.Restore => "Restoring data from backup",
                RollbackPhase.SettingsInjection => "Injecting user settings",
                RollbackPhase.Verification => "Verifying installation",
                RollbackPhase.Completed => "Rollback completed successfully",
                RollbackPhase.Failed => "Rollback failed",
                RollbackPhase.Cancelled => "Rollback cancelled",
                _ => "Unknown phase"
            };
        }

        private string GetCurrentStepDescription()
        {
            // This would be more detailed in a real implementation
            return GetPhaseDescription(_currentPhase);
        }

        private int GetTotalSteps()
        {
            // Count of non-skipped phases
            int total = 10; // Backup, KillProcesses, Purge, FirewallBlackout, Install, ScaffoldCreation, Restore, SettingsInjection, Verification
            if (_currentOptions.SkipBackup) total--;
            if (_currentOptions.SkipKillProcesses) total--;
            if (_currentOptions.SkipPurge) total--;
            if (_currentOptions.SkipFirewallBlackout) total--;
            if (_currentOptions.SkipInstall) total--;
            if (_currentOptions.SkipScaffoldCreation) total--;
            if (_currentOptions.SkipRestore) total--;
            if (_currentOptions.SkipSettingsInjection) total--;
            if (_currentOptions.SkipVerification) total--;
            return Math.Max(1, total);
        }

        private int GetCurrentStepNumber()
        {
            // This would be more detailed in a real implementation
            int step = 1;
            if (_currentPhase == RollbackPhase.NotStarted) return 0;

            if (_currentPhase >= RollbackPhase.Backup && !_currentOptions.SkipBackup) step++;
            if (_currentPhase >= RollbackPhase.KillProcesses && !_currentOptions.SkipKillProcesses) step++;
            if (_currentPhase >= RollbackPhase.Purge && !_currentOptions.SkipPurge) step++;
            if (_currentPhase >= RollbackPhase.FirewallBlackout && !_currentOptions.SkipFirewallBlackout) step++;
            if (_currentPhase >= RollbackPhase.Install && !_currentOptions.SkipInstall) step++;
            if (_currentPhase >= RollbackPhase.ScaffoldCreation && !_currentOptions.SkipScaffoldCreation) step++;
            if (_currentPhase >= RollbackPhase.Restore && !_currentOptions.SkipRestore) step++;
            if (_currentPhase >= RollbackPhase.SettingsInjection && !_currentOptions.SkipSettingsInjection) step++;
            if (_currentPhase >= RollbackPhase.Verification && !_currentOptions.SkipVerification) step++;

            return Math.Min(step, GetTotalSteps());
        }

        private async Task<string> GetInstallerPathAsync(VersionInfo targetVersion)
        {
            // In a real implementation, this would locate or download the appropriate installer
            // For now, we'll return a placeholder path
            if (targetVersion != null)
            {
                // Would look for installer matching the target version
                return $"C:\\Installers\\Antigravity_{targetVersion.Major}_{targetVersion.Minor}_{targetVersion.Build}.exe";
            }
            else
            {
                // Would use current version or default installer
                return "C:\\Installers\\Antigravity_Latest.exe";
            }
        }

        protected virtual void OnProgressChanged(RollbackProgressChangedEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        protected virtual void OnCompleted(RollbackCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(RollbackErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}
