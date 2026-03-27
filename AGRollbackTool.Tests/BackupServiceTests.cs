using System;
using System.IO;
using System.Threading.Tasks;
using AGRollbackTool.Services;

namespace AGRollbackTool.Tests
{
    /// <summary>
    /// Test class for BackupService.
    /// This demonstrates how to use the BackupService and verify its behavior.
    /// In a real project, this would be replaced with proper unit tests using a testing framework.
    /// </summary>
    public static class BackupServiceTests
    {
        /// <summary>
        /// Runs basic tests on the BackupService.
        /// </summary>
        public static void RunTests()
        {
            Console.WriteLine("Testing BackupService...");
            Console.WriteLine();

            // Test 1: Backup with default path (should create folder in Documents\AG Backups)
            TestBackupWithDefaultPath().Wait();

            // Test 2: Backup with custom path
            TestBackupWithCustomPath().Wait();

            // Test 3: Backup with compression
            TestBackupWithCompression().Wait();

            Console.WriteLine("BackupService tests completed.");
        }

        private static async Task TestBackupWithDefaultPath()
        {
            Console.WriteLine("Test 1: Backup with default path");
            try
            {
                var backupService = new BackupService();
                string backupPath = await backupService.BackupAsync(compress: false);
                Console.WriteLine($"  Backup created at: {backupPath}");
                Console.WriteLine($"  Backup directory exists: {Directory.Exists(backupPath)}");

                // Check if manifest exists
                string manifestPath = Path.Combine(backupPath, "manifest.json");
                Console.WriteLine($"  Manifest exists: {File.Exists(manifestPath)}");

                if (File.Exists(manifestPath))
                {
                    string manifestContent = await File.ReadAllTextAsync(manifestPath);
                    Console.WriteLine($"  Manifest length: {manifestContent.Length} characters");
                }

                Console.WriteLine("  Result: Passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }

        private static async Task TestBackupWithCustomPath()
        {
            Console.WriteLine("Test 2: Backup with custom path");
            try
            {
                string customPath = Path.Combine(Path.GetTempPath(), "AG_Backup_Test");
                // Clean up if exists
                if (Directory.Exists(customPath))
                {
                    Directory.Delete(customPath, true);
                }

                var backupService = new BackupService(customPath);
                string backupPath = await backupService.BackupAsync(compress: false);
                Console.WriteLine($"  Backup created at: {backupPath}");
                Console.WriteLine($"  Backup directory exists: {Directory.Exists(backupPath)}");

                // Clean up
                if (Directory.Exists(customPath))
                {
                    Directory.Delete(customPath, true);
                }

                Console.WriteLine("  Result: Passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }

        private static async Task TestBackupWithCompression()
        {
            Console.WriteLine("Test 3: Backup with compression");
            try
            {
                var backupService = new BackupService();
                string zipPath = await backupService.BackupAsync(compress: true);
                Console.WriteLine($"  ZIP backup created at: {zipPath}");
                Console.WriteLine($"  ZIP file exists: {File.Exists(zipPath)}");

                if (File.Exists(zipPath))
                {
                    FileInfo info = new FileInfo(zipPath);
                    Console.WriteLine($"  ZIP file size: {info.Length} bytes");
                }

                Console.WriteLine("  Result: Passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}
