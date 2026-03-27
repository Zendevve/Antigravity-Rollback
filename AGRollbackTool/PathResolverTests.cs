using System;
using System.Collections.Generic;

namespace AGRollbackTool
{
    /// <summary>
    /// Simple test class for PathResolver service.
    /// This demonstrates how to use the PathResolver and verify its behavior.
    /// In a real project, this would be replaced with proper unit tests using a testing framework.
    /// </summary>
    public static class PathResolverTests
    {
        /// <summary>
        /// Runs basic tests on the PathResolver service.
        /// </summary>
        public static void RunTests()
        {
            var resolver = new PathResolver();

            Console.WriteLine("Testing PathResolver service...");
            Console.WriteLine();

            // Test individual path getters
            TestPathInfo(resolver.GetGeminiAntigravityPath(), "Gemini Antigravity Path");
            TestPathInfo(resolver.GetGeminiGlobalRulesPath(), "Gemini Global Rules Path");
            TestPathInfo(resolver.GetUserSettingsPath(), "User Settings Path");
            TestPathInfo(resolver.GetUserKeybindingsPath(), "User Keybindings Path");
            TestPathInfo(resolver.GetGlobalStorageStatePath(), "Global Storage State Path");
            TestPathInfo(resolver.GetApplicationBinaryPath(), "Application Binary Path");
            TestPathInfo(resolver.GetStagedUpdateCachePath(), "Staged Update Cache Path");

            Console.WriteLine();

            // Test getting all paths
            Console.WriteLine("Testing GetAllAntigravityPaths():");
            var allPaths = resolver.GetAllAntigravityPaths();
            int count = 0;
            foreach (var pathInfo in allPaths)
            {
                count++;
                Console.WriteLine($"{count}. {pathInfo.Path} - Exists: {pathInfo.Exists}");
                if (!string.IsNullOrEmpty(pathInfo.ErrorMessage))
                {
                    Console.WriteLine($"   Error: {pathInfo.ErrorMessage}");
                }
            }
            Console.WriteLine($"Total paths returned: {count}");
            Console.WriteLine();

            Console.WriteLine("PathResolver tests completed.");
        }

        /// <summary>
        /// Helper method to test and display path information.
        /// </summary>
        /// <param name="pathInfo">The path information to test.</param>
        /// <param name="description">Description of the path for display purposes.</param>
        private static void TestPathInfo(AntigravityPathInfo pathInfo, string description)
        {
            Console.WriteLine($"{description}:");
            Console.WriteLine($"  Path: {pathInfo.Path}");
            Console.WriteLine($"  Exists: {pathInfo.Exists}");
            if (!string.IsNullOrEmpty(pathInfo.ErrorMessage))
            {
                Console.WriteLine($"  Error: {pathInfo.ErrorMessage}");
            }
            Console.WriteLine();
        }
    }
}
