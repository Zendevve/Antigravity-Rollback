using System;
using System.Collections.Generic;
using AGRollbackTool.Models;

namespace AGRollbackTool.Tests
{
    /// <summary>
    /// Test class for data models.
    /// </summary>
    public static class ModelTests
    {
        /// <summary>
        /// Runs basic tests on the data models.
        /// </summary>
        public static void RunTests()
        {
            Console.WriteLine("Testing data models...");
            Console.WriteLine();

            TestBackupManifest();
            TestBackupEntry();
            TestRollbackSession();
            TestPhaseEnum();

            Console.WriteLine();
            Console.WriteLine("Model tests completed.");
        }

        private static void TestBackupManifest()
        {
            Console.WriteLine("Test 1: BackupManifest");
            try
            {
                var manifest = new BackupManifest
                {
                    BackupId = "test-backup-id",
                    CreatedAt = DateTime.UtcNow,
                    SourceVersion = "1.0.0",
                    TargetVersion = "2.0.0",
                    Entries = new List<BackupEntry>()
                };

                // Test properties
                if (manifest.BackupId != "test-backup-id")
                    throw new Exception("BackupId not set correctly");

                if (manifest.SourceVersion != "1.0.0")
                    throw new Exception("SourceVersion not set correctly");

                if (manifest.TargetVersion != "2.0.0")
                    throw new Exception("TargetVersion not set correctly");

                if (manifest.Entries == null)
                    throw new Exception("Entries list not initialized");

                if (manifest.Entries.Count != 0)
                    throw new Exception("Entries list should be empty initially");

                Console.WriteLine("  Result: Passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }

        private static void TestBackupEntry()
        {
            Console.WriteLine("Test 2: BackupEntry");
            try
            {
                var entry = new BackupEntry
                {
                    RelativePath = "folder/file.txt",
                    OriginalFullPath = @"C:\Users\test\folder\file.txt",
                    Sha256 = "abc123def456",
                    SizeBytes = 1024,
                    IsDirectory = false
                };

                // Test properties
                if (entry.RelativePath != "folder/file.txt")
                    throw new Exception("RelativePath not set correctly");

                if (entry.OriginalFullPath != @"C:\Users\test\folder\file.txt")
                    throw new Exception("OriginalFullPath not set correctly");

                if (entry.Sha256 != "abc123def456")
                    throw new Exception("Sha256 not set correctly");

                if (entry.SizeBytes != 1024)
                    throw new Exception("SizeBytes not set correctly");

                if (entry.IsDirectory != false)
                    throw new Exception("IsDirectory not set correctly");

                // Test directory entry
                var dirEntry = new BackupEntry
                {
                    RelativePath = "folder",
                    OriginalFullPath = @"C:\Users\test\folder",
                    Sha256 = "",
                    SizeBytes = 0,
                    IsDirectory = true
                };

                if (dirEntry.IsDirectory != true)
                    throw new Exception("IsDirectory not set correctly for directory");

                Console.WriteLine("  Result: Passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }

        private static void TestRollbackSession()
        {
            Console.WriteLine("Test 3: RollbackSession");
            try
            {
                var session = new RollbackSession();

                // Test initial state
                if (session.CurrentPhase != Phase.NotStarted)
                    throw new Exception("Initial phase should be NotStarted");

                if (session.IsCompleted)
                    throw new Exception("Should not be completed initially");

                if (session.IsFailed)
                    throw new Exception("Should not be failed initially");

                if (session.EndedAt.HasValue)
                    throw new Exception("EndedAt should not have value initially");

                if (!string.IsNullOrEmpty(session.ErrorMessage))
                    throw new Exception("ErrorMessage should be empty initially");

                // Test advancing phase
                session.AdvanceTo(Phase.Initializing);
                if (session.CurrentPhase != Phase.Initializing)
                    throw new Exception("Phase should be Initializing after AdvanceTo");

                // Test completion
                session.Complete();
                if (session.CurrentPhase != Phase.Completed)
                    throw new Exception("Phase should be Completed after Complete");

                if (!session.IsCompleted)
                    throw new Exception("Should be completed after Complete");

                if (!session.EndedAt.HasValue)
                    throw new Exception("EndedAt should have value after Complete");

                // Test error state
                var errorSession = new RollbackSession();
                errorSession.SetError("Test error");
                if (errorSession.CurrentPhase != Phase.Failed)
                    throw new Exception("Phase should be Failed after SetError");

                if (!errorSession.IsFailed)
                    throw new Exception("Should be failed after SetError");

                if (errorSession.ErrorMessage != "Test error")
                    throw new Exception("ErrorMessage not set correctly");

                if (!errorSession.EndedAt.HasValue)
                    throw new Exception("EndedAt should have value after SetError");

                Console.WriteLine("  Result: Passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result: Failed - {ex.Message}");
            }
            Console.WriteLine();
        }

        private static void TestPhaseEnum()
        {
            Console.WriteLine("Test 4: Phase Enum");
            try
            {
                // Test all enum values are defined
                var phases = new[] {
                    Phase.NotStarted,
                    Phase.Initializing,
                    Phase.CreatingBackup,
                    Phase.ApplyingUpdate,
                    Phase.VerifyingUpdate,
                    Phase.RollingBack,
                    Phase.Completed,
                    Phase.Failed
                };

                if (phases.Length != 8)
                    throw new Exception("Should have 8 phase values");

                // Test string representations
                if (Phase.NotStarted.ToString() != "NotStarted")
                    throw new Exception("NotStarted string representation incorrect");

                if (Phase.Completed.ToString() != "Completed")
                    throw new Exception("Completed string representation incorrect");

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
