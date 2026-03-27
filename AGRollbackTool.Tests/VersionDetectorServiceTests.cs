using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AGRollbackTool.Services;
using Moq;
using Xunit;

namespace AGRollbackTool.Tests.Services
{
    public class VersionDetectorServiceTests
    {
        private readonly Mock<IPathResolver> _mockPathResolver;
        private readonly VersionDetectorService _versionDetectorService;

        public VersionDetectorServiceTests()
        {
            _mockPathResolver = new Mock<IPathResolver>();
            _versionDetectorService = new VersionDetectorService(_mockPathResolver.Object);
        }

        [Fact]
        public void DetectVersion_ReturnsVersionFromPackageJson_WhenAvailable()
        {
            // Arrange
            var appPathInfo = new AntigravityPathInfo("C:\\TestApp", true);
            _mockPathResolver.Setup(p => p.GetApplicationBinaryPath()).Returns(appPathInfo);

            // Create a temporary directory for testing
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string packageJsonPath = Path.Combine(tempDir, "package.json");

            try
            {
                // Write a test package.json
                var packageJson = new { version = "1.2.3" };
                string json = JsonSerializer.Serialize(packageJson);
                File.WriteAllText(packageJsonPath, json);

                // Override the path resolver to return our temp directory
                var tempAppPathInfo = new AntigravityPathInfo(tempDir, true);
                _mockPathResolver.Setup(p => p.GetApplicationBinaryPath()).Returns(tempAppPathInfo);

                // Act
                var result = _versionDetectorService.DetectVersion();

                // Assert
                Assert.True(result.Success);
                Assert.Equal("1.2.3", result.Version);
            }
            finally
            {
                // Clean up
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void DetectVersion_ReturnsVersionFromProductJson_WhenPackageJsonNotAvailable()
        {
            // Arrange
            var appPathInfo = new AntigravityPathInfo("C:\\TestApp", true);
            _mockPathResolver.Setup(p => p.GetApplicationBinaryPath()).Returns(appPathInfo);

            // Create a temporary directory for testing
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string productJsonPath = Path.Combine(tempDir, "product.json");

            try
            {
                // Write a test product.json
                var productJson = new { productVersion = "2.3.4" };
                string json = JsonSerializer.Serialize(productJson);
                File.WriteAllText(productJsonPath, json);

                // Override the path resolver to return our temp directory
                var tempAppPathInfo = new AntigravityPathInfo(tempDir, true);
                _mockPathResolver.Setup(p => p.GetApplicationBinaryPath()).Returns(tempAppPathInfo);

                // Act
                var result = _versionDetectorService.DetectVersion();

                // Assert
                Assert.True(result.Success);
                Assert.Equal("2.3.4", result.Version);
            }
            finally
            {
                // Clean up
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void DetectVersion_ReturnsVersionFromFileInfo_WhenJsonFilesNotAvailable()
        {
            // Arrange
            // We'll mock the file version info by creating a dummy file
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string dummyExePath = Path.Combine(tempDir, "antigravity.exe");

            try
            {
                // Create a dummy executable file
                File.WriteAllText(dummyExePath, "dummy");

                // Get the file version info of this dummy file (will be 0.0.0.0)
                // For testing, we'll use a known file that has version info
                string systemExePath = Path.Combine(Environment.SystemDirectory, "notepad.exe");
                if (!File.Exists(systemExePath))
                {
                    // Fallback to windows directory
                    systemExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "notepad.exe");
                }

                var appPathInfo = new AntigravityPathInfo(systemExePath, true);
                _mockPathResolver.Setup(p => p.GetApplicationBinaryPath()).Returns(appPathInfo);

                // Act
                var result = _versionDetectorService.DetectVersion();

                // Assert - Notepad should have a version
                Assert.True(result.Success);
                Assert.NotEqual("0.0.0", result.Version);
                Assert.False(string.IsNullOrEmpty(result.Version));
            }
            finally
            {
                // Clean up
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void DetectVersion_ReturnsFailure_WhenAllMethodsFail()
        {
            // Arrange
            var appPathInfo = new AntigravityPathInfo("C:\\NonexistentPath", false, "Path not found");
            _mockPathResolver.Setup(p => p.GetApplicationBinaryPath()).Returns(appPathInfo);

            // Act
            var result = _versionDetectorService.DetectVersion();

            // Assert
            Assert.False(result.Success);
            Assert.Equal("0.0.0", result.Version);
            Assert.Contains("Unable to detect Antigravity version", result.ErrorMessage);
        }

        [Fact]
        public void DetectVersion_HandlesJsonParseException_Gracefully()
        {
            // Arrange
            var appPathInfo = new AntigravityPathInfo("C:\\TestApp", true);
            _mockPathResolver.Setup(p => p.GetApplicationBinaryPath()).Returns(appPathInfo);

            // Create a temporary directory for testing
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string packageJsonPath = Path.Combine(tempDir, "package.json");

            try
            {
                // Write invalid JSON to package.json
                File.WriteAllText(packageJsonPath, "{ invalid json }");

                // Override the path resolver to return our temp directory
                var tempAppPathInfo = new AntigravityPathInfo(tempDir, true);
                _mockPathResolver.Setup(p => p.GetApplicationBinaryPath()).Returns(tempAppPathInfo);

                // Act
                var result = _versionDetectorService.DetectVersion();

                // Assert
                Assert.False(result.Success);
                Assert.Contains("Error reading JSON version files", result.ErrorMessage);
            }
            finally
            {
                // Clean up
                Directory.Delete(tempDir, true);
            }
        }
    }
}
