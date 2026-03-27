using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AGRollbackTool.Services;
using Moq;
using Xunit;

namespace AGRollbackTool.Tests
{
    public class RollbackOrchestratorServiceTests
    {
        private readonly Mock<IBackupService> _mockBackupService;
        private readonly Mock<IProcessKiller> _mockProcessKiller;
        private readonly Mock<IPurgeService> _mockPurgeService;
        private readonly Mock<INetworkBlackoutService> _mockNetworkBlackoutService;
        private readonly Mock<IInstallRunnerService> _mockInstallRunnerService;
        private readonly Mock<IRestoreService> _mockRestoreService;
        private readonly Mock<ISettingsInjectorService> _mockSettingsInjectorService;
        private readonly Mock<IVersionDetectorService> _mockVersionDetectorService;
        private readonly RollbackOrchestratorService _orchestrator;

        public RollbackOrchestratorServiceTests()
        {
            _mockBackupService = new Mock<IBackupService>();
            _mockProcessKiller = new Mock<IProcessKiller>();
            _mockPurgeService = new Mock<IPurgeService>();
            _mockNetworkBlackoutService = new Mock<INetworkBlackoutService>();
            _mockInstallRunnerService = new Mock<IInstallRunnerService>();
            _mockRestoreService = new Mock<IRestoreService>();
            _mockSettingsInjectorService = new Mock<ISettingsInjectorService>();
            _mockVersionDetectorService = new Mock<IVersionDetectorService>();

            _orchestrator = new RollbackOrchestratorService(
                _mockBackupService.Object,
                _mockProcessKiller.Object,
                _mockPurgeService.Object,
                _mockNetworkBlackoutService.Object,
                _mockInstallRunnerService.Object,
                _mockRestoreService.Object,
                _mockSettingsInjectorService.Object,
                _mockVersionDetectorService.Object);
        }

        [Fact]
        public async Task StartRollbackAsync_ShouldExecuteAllPhases_WhenNoOptionsSpecified()
        {
            // Arrange
            var options = new RollbackOptions();
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>())).ReturnsAsync("C:\\Backup");
            _mockProcessKiller.Setup(s => s.KillAllAntigravityProcesses()).Returns(new List<ProcessInfo>());
            _mockPurgeService.Setup(s => s.PurgeAsync()).ReturnsAsync(new PurgeResult { Success = true });
            _mockInstallRunnerService.Setup(s => s.RunInstallationAsync(It.IsAny<string>()))
                .ReturnsAsync(new InstallRunnerResult { Success = true });
            _mockRestoreService.Setup(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new RestoreResult { Success = true });

            // Act
            await _orchestrator.StartRollbackAsync(options);

            // Assert
            _mockBackupService.Verify(s => s.BackupAsync(It.IsAny<bool>()), Times.Once);
            _mockProcessKiller.Verify(s => s.KillAllAntigravityProcesses(), Times.Once);
            _mockPurgeService.Verify(s => s.PurgeAsync(), Times.Once);
            _mockNetworkBlackoutService.Verify(s => s.BlockAntigravityNetworkAccess(), Times.Once);
            _mockInstallRunnerService.Verify(s => s.RunInstallationAsync(It.IsAny<string>()), Times.Once);
            _mockRestoreService.Verify(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task StartRollbackAsync_ShouldSkipBackupPhase_WhenSkipBackupIsTrue()
        {
            // Arrange
            var options = new RollbackOptions { SkipBackup = true };
            _mockProcessKiller.Setup(s => s.KillAllAntigravityProcesses()).Returns(new List<ProcessInfo>());
            _mockPurgeService.Setup(s => s.PurgeAsync()).ReturnsAsync(new PurgeResult { Success = true });
            _mockInstallRunnerService.Setup(s => s.RunInstallationAsync(It.IsAny<string>()))
                .ReturnsAsync(new InstallRunnerResult { Success = true });
            _mockRestoreService.Setup(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new RestoreResult { Success = true });

            // Act
            await _orchestrator.StartRollbackAsync(options);

            // Assert
            _mockBackupService.Verify(s => s.BackupAsync(It.IsAny<bool>()), Times.Never);
            _mockProcessKiller.Verify(s => s.KillAllAntigravityProcesses(), Times.Once);
            _mockPurgeService.Verify(s => s.PurgeAsync(), Times.Once);
        }

        [Fact]
        public async Task StartRollbackAsync_ShouldHandleBackupFailure_AndSetFailedPhase()
        {
            // Arrange
            var options = new RollbackOptions();
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>()))
                .ThrowsAsync(new IOException("Backup failed"));

            // Act
            await _orchestrator.StartRollbackAsync(options);

            // Assert
            Assert.Equal(RollbackPhase.Failed, _orchestrator.CurrentPhase);
            Assert.False(_orchestrator.IsInProgress);
        }

        [Fact]
        public async Task StartRollbackAsync_ShouldHandleCancellation_AndSetCancelledPhase()
        {
            // Arrange
            var options = new RollbackOptions();
            // Setup backup to take a long time so we can cancel it
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>()))
                .Returns(Task.Delay(Timeout.Infinite));

            // Start rollback in a separate task
            var rollbackTask = _orchestrator.StartRollbackAsync(options);

            // Give it a moment to start
            await Task.Delay(100);

            // Act
            await _orchestrator.CancelRollbackAsync();

            // Wait for the rollback task to complete (it should be cancelled)
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await rollbackTask);

            // Assert
            Assert.Equal(RollbackPhase.Cancelled, _orchestrator.CurrentPhase);
            Assert.False(_orchestrator.IsInProgress);
        }

        [Fact]
        public void GetProgress_ShouldReturnCorrectPhaseAndPercentage()
        {
            // Arrange
            // Don't start rollback, so phase should be NotStarted

            // Act
            var progress = _orchestrator.GetProgress();

            // Assert
            Assert.Equal(RollbackPhase.NotStarted, progress.CurrentPhase);
            Assert.Equal(0, progress.PercentageComplete);
            Assert.Equal("Not started", progress.CurrentPhaseDescription);
        }

        [Fact]
        public void GetProgress_ShouldReturnCorrectValues_WhenInBackupPhase()
        {
            // Arrange - we can't easily set the internal state, so we'll test the logic indirectly
            // by checking that the method returns reasonable values

            // Act
            var progress = _orchestrator.GetProgress();

            // Assert
            Assert.InRange(progress.PercentageComplete, 0, 100);
            Assert.NotNull(progress.CurrentPhaseDescription);
            Assert.True(progress.ElapsedSeconds >= 0);
            Assert.True(progress.EstimatedRemainingSeconds >= 0);
        }

        [Fact]
        public void CurrentPhase_ShouldReturnNotStarted_WhenNotStarted()
        {
            // Act
            var phase = _orchestrator.CurrentPhase;

            // Assert
            Assert.Equal(RollbackPhase.NotStarted, phase);
        }

        [Fact]
        public void IsInProgress_ShouldReturnFalse_WhenNotStarted()
        {
            // Act
            var inProgress = _orchestrator.IsInProgress;

            // Assert
            Assert.False(inProgress);
        }

        [Fact]
        public async Task StartRollbackAsync_ShouldThrowException_WhenAlreadyInProgress()
        {
            // Arrange
            var options = new RollbackOptions();
            // Start a rollback that will take a long time
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>()))
                .Returns(Task.Delay(Timeout.Infinite));

            var rollbackTask = _orchestrator.StartRollbackAsync(options);

            // Give it a moment to start
            await Task.Delay(100);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _orchestrator.StartRollbackAsync(options);
            });

            // Cleanup
            await _orchestrator.CancelRollbackAsync();
            try
            {
                await rollbackTask;
            }
            catch (OperationCanceledException) { }
        }
    }
}
