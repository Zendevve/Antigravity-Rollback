using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Moq;
using AGRollbackTool;
using AGRollbackTool.Services;

namespace AGRollbackTool.Tests
{
    /// <summary>
    /// Unit tests for AntigravityInstallationService.
    /// </summary>
    public class AntigravityInstallationServiceTests
    {
        private readonly Mock<IPathResolver> _mockPathResolver;
        private readonly AntigravityInstallationService _service;

        public AntigravityInstallationServiceTests()
        {
            _mockPathResolver = new Mock<IPathResolver>();
            _service = new AntigravityInstallationService(_mockPathResolver.Object);
        }

        #region IsAntigravityInstalled Tests

        [Fact]
        public void IsAntigravityInstalled_WhenAppBinaryPathExistsWithExe_ReturnsTrue()
        {
            // Arrange
            var appBinaryPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "antigravity"),
                true);
            _mockPathResolver.Setup(x => x.GetApplicationBinaryPath()).Returns(appBinaryPath);

            // Create a temp file to simulate the executable
            string tempExePath = Path.Combine(appBinaryPath.Path, "antigravity.exe");
            File.WriteAllText(tempExePath, "test");
            try
            {
                // Act
                var result = _service.IsAntigravityInstalled();

                // Assert
                Assert.True(result);
            }
            finally
            {
                File.Delete(tempExePath);
            }
        }

        [Fact]
        public void IsAntigravityInstalled_WhenAppBinaryPathExistsButNoExe_ReturnsFalse()
        {
            // Arrange
            var appBinaryPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "antigravity"),
                true);
            _mockPathResolver.Setup(x => x.GetApplicationBinaryPath()).Returns(appBinaryPath);

            // Act
            var result = _service.IsAntigravityInstalled();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAntigravityInstalled_WhenAppBinaryPathDoesNotExist_FallsBackToGeminiPath()
        {
            // Arrange
            var appBinaryPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "antigravity"),
                false);
            var geminiPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity"),
                true);

            _mockPathResolver.Setup(x => x.GetApplicationBinaryPath()).Returns(appBinaryPath);
            _mockPathResolver.Setup(x => x.GetGeminiAntigravityPath()).Returns(geminiPath);

            // Act
            var result = _service.IsAntigravityInstalled();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAntigravityInstalled_WhenNeitherPathExists_ReturnsFalse()
        {
            // Arrange
            var appBinaryPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "antigravity"),
                false);
            var geminiPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity"),
                false);

            _mockPathResolver.Setup(x => x.GetApplicationBinaryPath()).Returns(appBinaryPath);
            _mockPathResolver.Setup(x => x.GetGeminiAntigravityPath()).Returns(geminiPath);

            // Act
            var result = _service.IsAntigravityInstalled();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetInstalledVersion Tests

        [Fact]
        public void GetInstalledVersion_ReturnsNull()
        {
            // Act
            var result = _service.GetInstalledVersion();

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region HasExistingBackups Tests

        [Fact]
        public void HasExistingBackups_WhenBackupDirectoryDoesNotExist_ReturnsFalse()
        {
            // Arrange - we need to test with a backup root that doesn't exist
            // The service uses a fixed path, so we test the logic by checking behavior

            // Act
            var result = _service.HasExistingBackups();

            // Assert - this will return false because the default backup path won't exist in test environment
            Assert.False(result);
        }

        [Fact]
        public void HasExistingBackups_WhenBackupDirectoryExistsWithSubdirectories_ReturnsTrue()
        {
            // Arrange
            string tempBackupRoot = Path.Combine(Path.GetTempPath(), "AG_Backups_Test_" + Guid.NewGuid());
            string tempBackupDir = Path.Combine(tempBackupRoot, "backup1");
            Directory.CreateDirectory(tempBackupDir);
            try
            {
                // Create service with custom backup path using reflection
                var serviceWithCustomPath = new AntigravityInstallationService(_mockPathResolver.Object);
                var field = typeof(AntigravityInstallationService).GetField("_backupRootPath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(serviceWithCustomPath, tempBackupRoot);

                // Act
                var result = serviceWithCustomPath.HasExistingBackups();

                // Assert
                Assert.True(result);
            }
            finally
            {
                Directory.Delete(tempBackupRoot, true);
            }
        }

        [Fact]
        public void HasExistingBackups_WhenBackupDirectoryExistsWithZipFiles_ReturnsTrue()
        {
            // Arrange
            string tempBackupRoot = Path.Combine(Path.GetTempPath(), "AG_Backups_Test_" + Guid.NewGuid());
            Directory.CreateDirectory(tempBackupRoot);
            string tempZip = Path.Combine(tempBackupRoot, "backup.zip");
            File.WriteAllText(tempZip, "test");
            try
            {
                var serviceWithCustomPath = new AntigravityInstallationService(_mockPathResolver.Object);
                var field = typeof(AntigravityInstallationService).GetField("_backupRootPath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(serviceWithCustomPath, tempBackupRoot);

                // Act
                var result = serviceWithCustomPath.HasExistingBackups();

                // Assert
                Assert.True(result);
            }
            finally
            {
                Directory.Delete(tempBackupRoot, true);
            }
        }

        [Fact]
        public void HasExistingBackups_WhenBackupDirectoryExistsButEmpty_ReturnsFalse()
        {
            // Arrange
            string tempBackupRoot = Path.Combine(Path.GetTempPath(), "AG_Backups_Test_" + Guid.NewGuid());
            Directory.CreateDirectory(tempBackupRoot);
            try
            {
                var serviceWithCustomPath = new AntigravityInstallationService(_mockPathResolver.Object);
                var field = typeof(AntigravityInstallationService).GetField("_backupRootPath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(serviceWithCustomPath, tempBackupRoot);

                // Act
                var result = serviceWithCustomPath.HasExistingBackups();

                // Assert
                Assert.False(result);
            }
            finally
            {
                Directory.Delete(tempBackupRoot, true);
            }
        }

        [Fact]
        public void HasExistingBackups_WhenDirectoryAccessDenied_ReturnsFalse()
        {
            // This test verifies the exception handling
            // Arrange - we simulate an inaccessible directory by passing an invalid path
            var serviceWithCustomPath = new AntigravityInstallationService(_mockPathResolver.Object);
            var field = typeof(AntigravityInstallationService).GetField("_backupRootPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // Use a path that should cause access issues
            field.SetValue(serviceWithCustomPath, "C:\\Invalid\\Path\\That\\Does\\Not\\Exist\\AG Backups");

            // Act
            var result = serviceWithCustomPath.HasExistingBackups();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetInstallationState Tests

        [Fact]
        public void GetInstallationState_WhenInstalledWithBackups_ReturnsNormalState()
        {
            // Arrange
            var appBinaryPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "antigravity"),
                true);
            _mockPathResolver.Setup(x => x.GetApplicationBinaryPath()).Returns(appBinaryPath);

            string tempBackupRoot = Path.Combine(Path.GetTempPath(), "AG_Backups_Test_" + Guid.NewGuid());
            string tempBackupDir = Path.Combine(tempBackupRoot, "backup1");
            Directory.CreateDirectory(tempBackupDir);
            try
            {
                var serviceWithCustomPath = new AntigravityInstallationService(_mockPathResolver.Object);
                var field = typeof(AntigravityInstallationService).GetField("_backupRootPath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(serviceWithCustomPath, tempBackupRoot);

                // Act
                var result = serviceWithCustomPath.GetInstallationState();

                // Assert
                Assert.True(result.IsInstalled);
                Assert.True(result.HasBackups);
                Assert.Equal(InstallationStateCategory.Normal, result.StateCategory);
                Assert.Contains("installed", result.StateMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("backup", result.StateMessage, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(tempBackupRoot, true);
            }
        }

        [Fact]
        public void GetInstallationState_WhenInstalledWithoutBackups_ReturnsNormalState()
        {
            // Arrange
            var appBinaryPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "antigravity"),
                true);
            _mockPathResolver.Setup(x => x.GetApplicationBinaryPath()).Returns(appBinaryPath);

            string tempBackupRoot = Path.Combine(Path.GetTempPath(), "AG_Backups_Test_" + Guid.NewGuid());
            Directory.CreateDirectory(tempBackupRoot);
            try
            {
                var serviceWithCustomPath = new AntigravityInstallationService(_mockPathResolver.Object);
                var field = typeof(AntigravityInstallationService).GetField("_backupRootPath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(serviceWithCustomPath, tempBackupRoot);

                // Act
                var result = serviceWithCustomPath.GetInstallationState();

                // Assert
                Assert.True(result.IsInstalled);
                Assert.False(result.HasBackups);
                Assert.Equal(InstallationStateCategory.Normal, result.StateCategory);
            }
            finally
            {
                Directory.Delete(tempBackupRoot, true);
            }
        }

        [Fact]
        public void GetInstallationState_WhenNotInstalledWithBackups_ReturnsRestoreOnlyState()
        {
            // Arrange
            var appBinaryPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "antigravity"),
                false);
            var geminiPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity"),
                false);
            _mockPathResolver.Setup(x => x.GetApplicationBinaryPath()).Returns(appBinaryPath);
            _mockPathResolver.Setup(x => x.GetGeminiAntigravityPath()).Returns(geminiPath);

            string tempBackupRoot = Path.Combine(Path.GetTempPath(), "AG_Backups_Test_" + Guid.NewGuid());
            string tempBackupDir = Path.Combine(tempBackupRoot, "backup1");
            Directory.CreateDirectory(tempBackupDir);
            try
            {
                var serviceWithCustomPath = new AntigravityInstallationService(_mockPathResolver.Object);
                var field = typeof(AntigravityInstallationService).GetField("_backupRootPath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(serviceWithCustomPath, tempBackupRoot);

                // Act
                var result = serviceWithCustomPath.GetInstallationState();

                // Assert
                Assert.False(result.IsInstalled);
                Assert.True(result.HasBackups);
                Assert.Equal(InstallationStateCategory.RestoreOnly, result.StateCategory);
                Assert.Contains("not", result.StateMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("restore", result.StateMessage, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(tempBackupRoot, true);
            }
        }

        [Fact]
        public void GetInstallationState_WhenNotInstalledWithoutBackups_ReturnsNotInstalledState()
        {
            // Arrange
            var appBinaryPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "antigravity"),
                false);
            var geminiPath = new AntigravityPathInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity"),
                false);
            _mockPathResolver.Setup(x => x.GetApplicationBinaryPath()).Returns(appBinaryPath);
            _mockPathResolver.Setup(x => x.GetGeminiAntigravityPath()).Returns(geminiPath);

            string tempBackupRoot = Path.Combine(Path.GetTempPath(), "AG_Backups_Test_" + Guid.NewGuid());
            Directory.CreateDirectory(tempBackupRoot);
            try
            {
                var serviceWithCustomPath = new AntigravityInstallationService(_mockPathResolver.Object);
                var field = typeof(AntigravityInstallationService).GetField("_backupRootPath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(serviceWithCustomPath, tempBackupRoot);

                // Act
                var result = serviceWithCustomPath.GetInstallationState();

                // Assert
                Assert.False(result.IsInstalled);
                Assert.False(result.HasBackups);
                Assert.Equal(InstallationStateCategory.NotInstalled, result.StateCategory);
                Assert.Contains("not installed", result.StateMessage, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(tempBackupRoot, true);
            }
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenPathResolverIsNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AntigravityInstallationService(null));
        }

        #endregion
    }
}
