using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using AGRollbackTool;

namespace AGRollbackTool.Tests
{
    /// <summary>
    /// Unit tests for PathResolver service.
    /// </summary>
    public class PathResolverTests
    {
        private readonly PathResolver _resolver;

        public PathResolverTests()
        {
            _resolver = new PathResolver();
        }

        #region GetGeminiAntigravityPath Tests

        [Fact]
        public void GetGeminiAntigravityPath_ReturnsPathWithUserProfile()
        {
            // Act
            var result = _resolver.GetGeminiAntigravityPath();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(".gemini", result.Path);
            Assert.Contains("antigravity", result.Path);
        }

        [Fact]
        public void GetGeminiAntigravityPath_ReturnsPathInUserProfile()
        {
            // Arrange
            string expectedPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".gemini",
                "antigravity");

            // Act
            var result = _resolver.GetGeminiAntigravityPath();

            // Assert
            Assert.Equal(expectedPath, result.Path);
        }

        #endregion

        #region GetGeminiGlobalRulesPath Tests

        [Fact]
        public void GetGeminiGlobalRulesPath_ReturnsPathWithGeminiMd()
        {
            // Act
            var result = _resolver.GetGeminiGlobalRulesPath();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(".gemini", result.Path);
            Assert.Contains("GEMINI.md", result.Path);
        }

        #endregion

        #region GetUserSettingsPath Tests

        [Fact]
        public void GetUserSettingsPath_ReturnsPathInAppData()
        {
            // Act
            var result = _resolver.GetUserSettingsPath();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), result.Path);
            Assert.Contains("settings.json", result.Path);
        }

        #endregion

        #region GetUserKeybindingsPath Tests

        [Fact]
        public void GetUserKeybindingsPath_ReturnsPathWithKeybindings()
        {
            // Act
            var result = _resolver.GetUserKeybindingsPath();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("keybindings.json", result.Path);
        }

        #endregion

        #region GetGlobalStorageStatePath Tests

        [Fact]
        public void GetGlobalStorageStatePath_ReturnsPathWithStateVscdb()
        {
            // Act
            var result = _resolver.GetGlobalStorageStatePath();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("globalStorage", result.Path);
            Assert.Contains("state.vscdb", result.Path);
        }

        #endregion

        #region GetApplicationBinaryPath Tests

        [Fact]
        public void GetApplicationBinaryPath_ReturnsPathInLocalAppData()
        {
            // Act
            var result = _resolver.GetApplicationBinaryPath();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), result.Path);
            Assert.Contains("antigravity", result.Path, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region GetStagedUpdateCachePath Tests

        [Fact]
        public void GetStagedUpdateCachePath_ReturnsPathForUpdater()
        {
            // Act
            var result = _resolver.GetStagedUpdateCachePath();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("antigravity-updater", result.Path);
        }

        #endregion

        #region GetAllAntigravityPaths Tests

        [Fact]
        public void GetAllAntigravityPaths_ReturnsAllPaths()
        {
            // Act
            var result = _resolver.GetAllAntigravityPaths();
            var pathList = new List<AntigravityPathInfo>(result);

            // Assert
            Assert.NotNull(pathList);
            Assert.Equal(7, pathList.Count);
        }

        [Fact]
        public void GetAllAntigravityPaths_ContainsAllExpectedPaths()
        {
            // Act
            var result = _resolver.GetAllAntigravityPaths();

            // Assert - verify all expected path types are returned
            Assert.Contains(result, p => p.Path.Contains("antigravity"));
            Assert.Contains(result, p => p.Path.Contains("GEMINI.md"));
            Assert.Contains(result, p => p.Path.Contains("settings.json"));
            Assert.Contains(result, p => p.Path.Contains("keybindings.json"));
            Assert.Contains(result, p => p.Path.Contains("state.vscdb"));
            Assert.Contains(result, p => p.Path.Contains("antigravity-updater"));
        }

        #endregion

        #region PathInfo Tests

        [Fact]
        public void AntigravityPathInfo_Constructor_SetsProperties()
        {
            // Arrange & Act
            var pathInfo = new AntigravityPathInfo("C:\\test\\path", true, "error message");

            // Assert
            Assert.Equal("C:\\test\\path", pathInfo.Path);
            Assert.True(pathInfo.Exists);
            Assert.Equal("error message", pathInfo.ErrorMessage);
        }

        [Fact]
        public void AntigravityPathInfo_Constructor_WithNullPath_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AntigravityPathInfo(null, true));
        }

        [Fact]
        public void AntigravityPathInfo_DefaultErrorMessage_IsEmpty()
        {
            // Arrange & Act
            var pathInfo = new AntigravityPathInfo("C:\\test", true);

            // Assert
            Assert.Equal(string.Empty, pathInfo.ErrorMessage);
        }

        #endregion
    }
}
