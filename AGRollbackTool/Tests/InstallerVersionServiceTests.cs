using System;
using System.IO;
using Xunit;
using AGRollbackTool.Services;

namespace AGRollbackTool.Tests
{
    /// <summary>
    /// Unit tests for InstallerVersionService.
    /// </summary>
    public class InstallerVersionServiceTests
    {
        private readonly InstallerVersionService _service;

        public InstallerVersionServiceTests()
        {
            _service = new InstallerVersionService();
        }

        #region GetInstallerVersion Tests

        [Fact]
        public void GetInstallerVersion_WhenPathIsNull_ReturnsNull()
        {
            // Act
            var result = _service.GetInstallerVersion(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetInstallerVersion_WhenPathIsEmpty_ReturnsNull()
        {
            // Act
            var result = _service.GetInstallerVersion("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetInstallerVersion_WhenPathIsWhiteSpace_ReturnsNull()
        {
            // Act
            var result = _service.GetInstallerVersion("   ");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetInstallerVersion_WhenFileDoesNotExist_ReturnsNull()
        {
            // Act
            var result = _service.GetInstallerVersion("C:\\NonExistent\\file.exe");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetInstallerVersion_WhenFileExists_ReturnsVersionOrNull()
        {
            // Arrange - Create a temp file
            string tempFile = Path.Combine(Path.GetTempPath(), "test_installer_" + Guid.NewGuid() + ".exe");
            File.WriteAllText(tempFile, "test");
            try
            {
                // Act
                var result = _service.GetInstallerVersion(tempFile);

                // Assert - may return null if file doesn't have version info, but shouldn't crash
                // This test verifies the method doesn't throw
                Assert.NotNull(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetInstallerVersion_WhenFileNameContainsVersion_ReturnsExtractedVersion()
        {
            // Arrange - Create a temp file with version in name
            string tempFile = Path.Combine(Path.GetTempPath(), "Antigravity_1.2.3_setup.exe");
            File.WriteAllText(tempFile, "test");
            try
            {
                // Act
                var result = _service.GetInstallerVersion(tempFile);

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.Version);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetInstallerVersion_WhenFileNameContainsUnderscoreVersion_ReturnsExtractedVersion()
        {
            // Arrange - Create a temp file with underscore version
            string tempFile = Path.Combine(Path.GetTempPath(), "Antigravity-2_5_0.exe");
            File.WriteAllText(tempFile, "test");
            try
            {
                // Act
                var result = _service.GetInstallerVersion(tempFile);

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.Version);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion

        #region CompareVersion Tests

        [Fact]
        public void CompareVersion_WhenInstallerVersionIsUnknown_ReturnsUnknown()
        {
            // Arrange
            string tempFile = Path.Combine(Path.GetTempPath(), "test_nonexistent.exe");

            // Act
            var result = _service.CompareVersion(tempFile, new VersionInfo("1.0.0", true));

            // Assert
            Assert.Equal(VersionComparisonResult.Unknown, result);
        }

        [Fact]
        public void CompareVersion_WhenExpectedVersionIsNull_ReturnsUnknown()
        {
            // Arrange - Create a temp file
            string tempFile = Path.Combine(Path.GetTempPath(), "Antigravity_1.2.3.exe");
            File.WriteAllText(tempFile, "test");
            try
            {
                // Act
                var result = _service.CompareVersion(tempFile, null);

                // Assert
                Assert.Equal(VersionComparisonResult.Unknown, result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void CompareVersion_WhenInstallerFileNotFound_ReturnsUnknown()
        {
            // Arrange
            var expectedVersion = new VersionInfo("1.0.0", true);

            // Act
            var result = _service.CompareVersion("C:\\NonExistent\\file.exe", expectedVersion);

            // Assert
            Assert.Equal(VersionComparisonResult.Unknown, result);
        }

        #endregion

        #region ExtractVersionFromFileName Tests

        [Fact]
        public void GetInstallerVersion_WithNoVersionInFileName_ReturnsNull()
        {
            // Arrange - Create a temp file with no version
            string tempFile = Path.Combine(Path.GetTempPath(), "Antigravity_setup.exe");
            File.WriteAllText(tempFile, "test");
            try
            {
                // Act
                var result = _service.GetInstallerVersion(tempFile);

                // Assert - should attempt to extract from filename
                // May return null if no version pattern found
                Assert.Null(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetInstallerVersion_WithVersionInMiddleOfFileName_ReturnsExtractedVersion()
        {
            // Arrange
            string tempFile = Path.Combine(Path.GetTempPath(), "Setup_Antigravity_v1.0.0_Release.exe");
            File.WriteAllText(tempFile, "test");
            try
            {
                // Act
                var result = _service.GetInstallerVersion(tempFile);

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.Version);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion
    }
}
