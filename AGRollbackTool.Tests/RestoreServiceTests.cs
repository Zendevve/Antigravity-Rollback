using System;
using System.IO;
using System.Threading.Tasks;
using AGRollbackTool.Services;

namespace AGRollbackTool.Tests
{
    /// <summary>
    /// Test class for RestoreService.
    /// This demonstrates how to use the RestoreService and verify its behavior.
    /// In a real project, this would be replaced with proper unit tests using a testing framework.
    /// </summary>
    public static class RestoreServiceTests
    {
        /// <summary>
        /// Runs basic tests on the RestoreService.
        /// </summary>
        public static void RunTests()
        {
            Console.WriteLine("Testing RestoreService...");
            Console.WriteLine();

            // Test 1: Restore from non-existent path
            TestRestoreFromNonExistentPath().Wait();

            // Test 2: Restore from path without manifest
            TestRestoreFromPathWithoutManifest().Wait();

            // Note: More comprehensive tests would require setting up actual backups first
            // For now, we're testing error handling and basic functionality

            Console.WriteLine("RestoreService tests completed.");
        }

        private static async Task TestRestoreFromNonExistentPath()
        {
            Console.WriteLine("Test 1: Restore from non-existent path");
            try
            {
                var restoreService = new RestoreService();
                var result = await restoreService.RestoreAsync("C:\\NonExistentPath\\Backup");
                Console.WriteLine($"  Success: {result.Success}");
                Console.WriteLine($"  Files Restored: {result.FilesRestored}");
                Console.WriteLine($"  Files Failed: {result.FilesFailed}");
                Console.WriteLine($"  Hash Mismatches: {result.HashMismatches}");
                Console.WriteLine($"  Errors: {string.Join("; ", result.Errors)}");

                // Should fail because path doesn't exist
                if (!result.Success && result.Errors.Count > 0)
                {
                    Console.WriteLine("  Result: Passed (correctly failed)");
                }
                else
                {
                    Console.WriteLine("  Result: Failed (should have failed)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }

        private static async Task TestRestoreFromPathWithoutManifest()
        {
            Console.WriteLine("Test 2: Restore from path without manifest");
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), $"AGRestoreTest_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                // Create a dummy file but no manifest
                string dummyFile = Path.Combine(tempDir, "dummy.txt");
                await File.WriteAllTextAsync(dummyFile, "test content");

                var restoreService = new RestoreService();
                var result = await restoreService.RestoreAsync(tempDir);
                Console.WriteLine($"  Success: {result.Success}");
                Console.WriteLine($"  Files Restored: {result.FilesRestored}");
                Console.WriteLine($"  Files Failed: {result.FilesFailed}");
                Console.WriteLine($"  Hash Mismatches: {result.HashMismatches}");
                Console.WriteLine($"  Errors: {string.Join("; ", result.Errors)}");

                // Should fail because no manifest
                if (!result.Success && result.Errors.Count > 0)
                {
                    Console.WriteLine("  Result: Passed (correctly failed)");
                }
                else
                {
                    Console.WriteLine("  Result: Failed (should have failed)");
                }

                // Clean up
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }

        // Additional test for ZIP handling could be added here
        // But would require creating a backup first, which is more complex
    }
}
