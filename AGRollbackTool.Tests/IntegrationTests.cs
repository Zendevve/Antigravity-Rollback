using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AGRollbackTool.Services;
using Moq;
using Xunit;

namespace AGRollbackTool.Tests
{
    /// <summary>
    /// Integration tests for the complete backup → purge → restore workflow.
    /// </summary>
    public class IntegrationTests
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

        public IntegrationTests()
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
        public async Task Backup_Purge_Restore_Workflow_ShouldExecuteCorrectly_WithHashVerification()
        {
            // Arrange
            var options = new RollbackOptions();
            string expectedBackupPath = @"C:\Backups\20260327_120000";

            // Setup backup service to return a backup path
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>()))
                .ReturnsAsync(expectedBackupPath);

            // Setup purge service to return success
            _mockPurgeService.Setup(s => s.PurgeAsync())
                .ReturnsAsync(new PurgeResult { Success = true });

            // Setup restore service to return success
            _mockRestoreService.Setup(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new RestoreResult { Success = true, FilesRestored = 5, FilesFailed = 0, HashMismatches = 0 });

            // Setup other services to return success/defaults
            _mockProcessKiller.Setup(s => s.KillAllAntigravityProcesses())
                .Returns(new List<ProcessInfo>());

            _mockInstallRunnerService.Setup(s => s.RunInstallationAsync(It.IsAny<string>()))
                .ReturnsAsync(new InstallRunnerResult { Success = true });

            _mockVersionDetectorService.Setup(s => s.GetCurrentVersionAsync())
                .ReturnsAsync(new VersionInfo { Major = 1, Minor = 0, Build = 0 });

            // Act
            await _orchestrator.StartRollbackAsync(options);

            // Assert
            // Verify backup was called
            _mockBackupService.Verify(s => s.BackupAsync(It.IsAny<bool>()), Times.Once);

            // Verify purge was called
            _mockPurgeService.Verify(s => s.PurgeAsync(), Times.Once);

            // Verify restore was called with the correct backup path
            _mockRestoreService.Verify(s => s.RestoreAsync(
                It.Is<string>(path => path == expectedBackupPath),
                It.Is<bool>(verify => verify == true)), Times.Once);

            // Verify the orchestrator completed successfully
            Assert.Equal(RollbackPhase.Completed, _orchestrator.CurrentPhase);
            Assert.False(_orchestrator.IsInProgress);
        }

        [Fact]
        public async Task Backup_Purge_Restore_Workflow_ShouldVerifyDataIntegrity_ThroughHashes()
        {
            // Arrange
            var options = new RollbackOptions();
            string expectedBackupPath = @"C:\Backups\20260327_120000";

            // Setup backup service to return a backup path
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>()))
                .ReturnsAsync(expectedBackupPath);

            // Setup purge service to return success
            _mockPurgeService.Setup(s => s.PurgeAsync())
                .ReturnsAsync(new PurgeResult { Success = true });

            // Setup restore service to verify hashes and return success with no mismatches
            _mockRestoreService.Setup(s => s.RestoreAsync(
                It.IsAny<string>(),
                It.Is<bool>(verify => verify == true))) // verifyHashes should be true
                .ReturnsAsync(new RestoreResult
                {
                    Success = true,
                    FilesRestored = 5,
                    FilesFailed = 0,
                    HashMismatches = 0 // This verifies hash integrity
                });

            // Setup other services
            _mockProcessKiller.Setup(s => s.KillAllAntigravityProcesses())
                .Returns(new List<ProcessInfo>());

            _mockInstallRunnerService.Setup(s => s.RunInstallationAsync(It.IsAny<string>()))
                .ReturnsAsync(new InstallRunnerResult { Success = true });

            _mockVersionDetectorService.Setup(s => s.GetCurrentVersionAsync())
                .ReturnsAsync(new VersionInfo { Major = 1, Minor = 0, Build = 0 });

            // Act
            await _orchestrator.StartRollbackAsync(options);

            // Assert
            // Verify that restore was called with verifyHashes = true
            _mockRestoreService.Verify(s => s.RestoreAsync(
                It.IsAny<string>(),
                It.Is<bool>(verify => verify == true)), Times.Once);

            // Verify successful completion
            Assert.Equal(RollbackPhase.Completed, _orchestrator.CurrentPhase);
        }

        [Fact]
        public async Task Backup_Purge_Restore_Workflow_ShouldHandle_BackupFailure()
        {
            // Arrange
            var options = new RollbackOptions();

            // Setup backup service to throw an exception
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>()))
                .ThrowsAsync(new IOException("Backup failed due to insufficient disk space"));

            // Act
            await _orchestrator.StartRollbackAsync(options);

            // Assert
            // Verify backup was called
            _mockBackupService.Verify(s => s.BackupAsync(It.IsAny<bool>()), Times.Once);

            // Verify purge and restore were NOT called due to backup failure
            _mockPurgeService.Verify(s => s.PurgeAsync(), Times.Never);
            _mockRestoreService.Verify(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            // Verify the orchestrator failed
            Assert.Equal(RollbackPhase.Failed, _orchestrator.CurrentPhase);
            Assert.False(_orchestrator.IsInProgress);
        }

        [Fact]
        public async Task Backup_Purge_Restore_Workflow_ShouldHandle_PurgeFailure()
        {
            // Arrange
            var options = new RollbackOptions();
            string expectedBackupPath = @"C:\Backups\20260327_120000";

            // Setup backup service to return a backup path
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>()))
                .ReturnsAsync(expectedBackupPath);

            // Setup purge service to return failure
            _mockPurgeService.Setup(s => s.PurgeAsync())
                .ReturnsAsync(new PurgeResult { Success = false, Errors = new List<string> { "Access denied to registry" } });

            // Setup other services
            _mockProcessKiller.Setup(s => s.KillAllAntigravityProcesses())
                .Returns(new List<ProcessInfo>());

            // Act
            await _orchestrator.StartRollbackAsync(options);

            // Assert
            // Verify backup was called
            _mockBackupService.Verify(s => s.BackupAsync(It.IsAny<bool>()), Times.Once);

            // Verify purge was called
            _mockPurgeService.Verify(s => s.PurgeAsync(), Times.Once);

            // Verify restore was NOT called due to purge failure
            _mockRestoreService.Verify(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            // Verify the orchestrator failed
            Assert.Equal(RollbackPhase.Failed, _orchestrator.CurrentPhase);
            Assert.False(_orchestrator.IsInProgress);
        }

        [Fact]
        public async Task Backup_Purge_Restore_Workflow_ShouldHandle_RestoreFailure()
        {
            // Arrange
            var options = new RollbackOptions();
            string expectedBackupPath = @"C:\Backups\20260327_120000";

            // Setup backup service to return a backup path
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>()))
                .ReturnsAsync(expectedBackupPath);

            // Setup purge service to return success
            _mockPurgeService.Setup(s => s.PurgeAsync())
                .ReturnsAsync(new PurgeResult { Success = true });

            // Setup restore service to return failure
            _mockRestoreService.Setup(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new RestoreResult
                {
                    Success = false,
                    FilesRestored = 3,
                    FilesFailed = 2,
                    HashMismatches = 0,
                    Errors = new List<string> { "File access denied" }
                });

            // Setup other services
            _mockProcessKiller.Setup(s => s.KillAllAntigravityProcesses())
                .Returns(new List<ProcessInfo>());

            _mockInstallRunnerService.Setup(s => s.RunInstallationAsync(It.IsAny<string>()))
                .ReturnsAsync(new InstallRunnerResult { Success = true });

            _mockVersionDetectorService.Setup(s => s.GetCurrentVersionAsync())
                .ReturnsAsync(new VersionInfo { Major = 1, Minor = 0, Build = 0 });

            // Act
            await _orchestrator.StartRollbackAsync(options);

            // Assert
            // Verify backup was called
            _mockBackupService.Verify(s => s.BackupAsync(It.IsAny<bool>()), Times.Once);

            // Verify purge was called
            _mockPurgeService.Verify(s => s.PurgeAsync(), Times.Once);

            // Verify restore was called
            _mockRestoreService.Verify(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);

            // Verify the orchestrator failed due to restore failure
            Assert.Equal(RollbackPhase.Failed, _orchestrator.CurrentPhase);
            Assert.False(_orchestrator.IsInProgress);
        }

        [Fact]
        public async Task Backup_Purge_Restore_Workflow_ShouldSkipPhases_BasedOnOptions()
        {
            // Arrange
            var options = new RollbackOptions
            {
                SkipBackup = true,
                SkipPurge = true,
                SkipRestore = true
            };

            // Setup services - they should not be called due to skip options
            _mockBackupService.Setup(s => s.BackupAsync(It.IsAny<bool>()))
                .ReturnsAsync(@"C:\Backups\20260327_120000");

            _mockPurgeService.Setup(s => s.PurgeAsync())
                .ReturnsAsync(new PurgeResult { Success = true });

            _mockRestoreService.Setup(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new RestoreResult { Success = true });

            // Setup other services
            _mockProcessKiller.Setup(s => s.KillAllAntigravityProcesses())
                .Returns(new List<ProcessInfo>());

            _mockInstallRunnerService.Setup(s => s.RunInstallationAsync(It.IsAny<string>()))
                .ReturnsAsync(new InstallRunnerResult { Success = true });

            _mockVersionDetectorService.Setup(s => s.GetCurrentVersionAsync())
                .ReturnsAsync(new VersionInfo { Major = 1, Minor = 0, Build = 0 });

            // Act
            await _orchestrator.StartRollbackAsync(options);

            // Assert
            // Verify backup was NOT called due to SkipBackup = true
            _mockBackupService.Verify(s => s.BackupAsync(It.IsAny<bool>()), Times.Never);

            // Verify purge was NOT called due to SkipPurge = true
            _mockPurgeService.Verify(s => s.PurgeAsync(), Times.Never);

            // Verify restore was NOT called due to SkipRestore = true
            _mockRestoreService.Verify(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            // Verify the orchestrator still completed (other phases ran)
            Assert.Equal(RollbackPhase.Completed, _orchestrator.CurrentPhase);
            Assert.False(_orchestrator.IsInProgress);
        }
    }
}
