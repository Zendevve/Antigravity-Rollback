using System;
using System.IO;
using System.Threading.Tasks;
using AGRollbackTool.Services;

namespace AGRollbackTool.Tests
{
    /// <summary>
    /// Test class for SettingsInjectorService.
    /// This demonstrates how to use the SettingsInjectorService and verify its behavior.
    /// In a real project, this would be replaced with proper unit tests using a testing framework.
    /// </summary>
    public static class SettingsInjectorServiceTests
    {
        /// <summary>
        /// Runs basic tests on the SettingsInjectorService.
        /// </summary>
        public static void RunTests()
        {
            Console.WriteLine("Testing SettingsInjectorService...");
            Console.WriteLine();

            // Test 1: Create settings file when it doesn't exist
            TestCreateSettingsFile().Wait();

            // Test 2: Update existing settings file
            TestUpdateExistingSettings().Wait();

            // Test 3: Handle malformed JSON
            TestHandleMalformedJson().Wait();

            Console.WriteLine("SettingsInjectorService tests completed.");
        }

        private static async Task TestCreateSettingsFile()
        {
            Console.WriteLine("Test 1: Create settings file when it doesn't exist");
            try
            {
                // Use a temporary path for testing
                string tempPath = Path.Combine(Path.GetTempPath(), "AGRollbackTool_Test", "settings.json");

                // Clean up if exists
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                // Also clean up the directory
                string tempDir = Path.GetDirectoryName(tempPath);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }

                // Create a mock path resolver that returns our test path
                var mockPathResolver = new MockPathResolver(tempPath);

                var service = new SettingsInjectorService(mockPathResolver);
                var result = await service.InjectSettingsAsync();

                Console.WriteLine($"  Success: {result.Success}");
                Console.WriteLine($"  Message: {result.Message}");
                Console.WriteLine($"  UpdateModeChanged: {result.UpdateModeChanged}");
                Console.WriteLine($"  ShowReleaseNotesChanged: {result.ShowReleaseNotesChanged}");
                Console.WriteLine($"  File created: {File.Exists(tempPath)}");

                if (File.Exists(tempPath))
                {
                    string content = await File.ReadAllTextAsync(tempPath);
                    Console.WriteLine($"  File content: {content}");
                }

                Console.WriteLine("  Result: " + (result.Success && result.UpdateModeChanged && result.ShowReleaseNotesChanged ? "Passed" : "Failed"));

                // Clean up
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }

        private static async Task TestUpdateExistingSettings()
        {
            Console.WriteLine("Test 2: Update existing settings file");
            try
            {
                // Use a temporary path for testing
                string tempPath = Path.Combine(Path.GetTempPath(), "AGRollbackTool_Test2", "settings.json");

                // Clean up if exists
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                // Also clean up the directory
                string tempDir = Path.GetDirectoryName(tempPath);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }

                // Create an initial settings file with some content
                string initialJson = @"{
  ""theme"": ""dark"",
  ""fontSize"": 14,
  ""update"": {
    ""mode"": ""manual"",
    ""showReleaseNotes"": true
  }
}";

                Directory.CreateDirectory(tempDir);
                await File.WriteAllTextAsync(tempPath, initialJson);

                // Create a mock path resolver that returns our test path
                var mockPathResolver = new MockPathResolver(tempPath);

                var service = new SettingsInjectorService(mockPathResolver);
                var result = await service.InjectSettingsAsync();

                Console.WriteLine($"  Success: {result.Success}");
                Console.WriteLine($"  Message: {result.Message}");
                Console.WriteLine($"  UpdateModeChanged: {result.UpdateModeChanged}");
                Console.WriteLine($"  OldUpdateMode: {result.OldUpdateMode}");
                Console.WriteLine($"  NewUpdateMode: {result.NewUpdateMode}");
                Console.WriteLine($"  ShowReleaseNotesChanged: {result.ShowReleaseNotesChanged}");
                Console.WriteLine($"  OldShowReleaseNotes: {result.OldShowReleaseNotes}");
                Console.WriteLine($"  NewShowReleaseNotes: {result.NewShowReleaseNotes}");

                if (File.Exists(tempPath))
                {
                    string content = await File.ReadAllTextAsync(tempPath);
                    Console.WriteLine($"  Updated file content: {content}");
                }

                bool passed = result.Success &&
                             result.UpdateModeChanged &&
                             result.OldUpdateMode == "manual" &&
                             result.NewUpdateMode == "none" &&
                             result.ShowReleaseNotesChanged &&
                             result.OldShowReleaseNotes == true &&
                             result.NewShowReleaseNotes == false;

                Console.WriteLine("  Result: " + (passed ? "Passed" : "Failed"));

                // Clean up
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }

        private static async Task TestHandleMalformedJson()
        {
            Console.WriteLine("Test 3: Handle malformed JSON");
            try
            {
                // Use a temporary path for testing
                string tempPath = Path.Combine(Path.GetTempPath(), "AGRollbackTool_Test3", "settings.json");

                // Clean up if exists
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                // Also clean up the directory
                string tempDir = Path.GetDirectoryName(tempPath);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }

                // Create a malformed JSON file
                string malformedJson = @"{
  ""theme"": ""dark"",
  ""fontSize"": 14,
  ""update"": {
    ""mode"": ""manual"",
    ""showReleaseNotes"": true
  }
  // Missing closing brace
";

                Directory.CreateDirectory(tempDir);
                await File.WriteAllTextAsync(tempPath, malformedJson);

                // Create a mock path resolver that returns our test path
                var mockPathResolver = new MockPathResolver(tempPath);

                var service = new SettingsInjectorService(mockPathResolver);
                var result = await service.InjectSettingsAsync();

                Console.WriteLine($"  Success: {result.Success}");
                Console.WriteLine($"  Message: {result.Message}");
                Console.WriteLine($"  Errors count: {result.Errors.Count}");

                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"  First error: {result.Errors[0]}");
                }

                bool passed = !result.Success && result.Errors.Count > 0;
                Console.WriteLine("  Result: " + (passed ? "Passed" : "Failed"));

                // Clean up
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Mock path resolver for testing.
    /// </summary>
    internal class MockPathResolver : IPathResolver
    {
        private readonly string _userSettingsPath;

        public MockPathResolver(string userSettingsPath)
        {
            _userSettingsPath = userSettingsPath;
        }

        public AntigravityPathInfo GetGeminiAntigravityPath()
        {
            return new AntigravityPathInfo("", false, "Not implemented for test");
        }

        public AntigravityPathInfo GetGeminiGlobalRulesPath()
        {
            return new AntigravityPathInfo("", false, "Not implemented for test");
        }

        public AntigravityPathInfo GetUserSettingsPath()
        {
            bool exists = File.Exists(_userSettingsPath);
            return new AntigravityPathInfo(_userSettingsPath, exists);
        }

        public AntigravityPathInfo GetUserKeybindingsPath()
        {
            return new AntigravityPathInfo("", false, "Not implemented for test");
        }

        public AntigravityPathInfo GetGlobalStorageStatePath()
        {
            return new AntigravityPathInfo("", false, "Not implemented for test");
        }

        public AntigravityPathInfo GetApplicationBinaryPath()
        {
            return new AntigravityPathInfo("", false, "Not implemented for test");
        }

        public AntigravityPathInfo GetStagedUpdateCachePath()
        {
            return new AntigravityPathInfo("", false, "Not implemented for test");
        }

        public System.Collections.Generic.IEnumerable<AntigravityPathInfo> GetAllAntigravityPaths()
        {
            yield return GetUserSettingsPath();
        }
    }
}
