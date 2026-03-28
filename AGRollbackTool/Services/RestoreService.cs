using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Result of a restore operation.
    /// </summary>
    public class RestoreResult
    {
        public bool Success { get; set; }
        public int FilesRestored { get; set; }
        public int FilesFailed { get; set; }
        public int HashMismatches { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> RestoredFiles { get; } = new List<string>();
    }

    /// <summary>
    /// Service for restoring Google Antigravity data from backups.
    /// </summary>
    public class RestoreService : IRestoreService
    {
        /// <summary>
        /// Restores data from a backup folder or zip file.
        /// </summary>
        /// <param name="backupPath">Path to the backup folder or zip file.</param>
        /// <param name="verifyHashes">Whether to verify SHA-256 hashes before restoring.</param>
        /// <returns>A restore result detailing what was restored and any errors.</returns>
        public async Task<RestoreResult> RestoreAsync(string backupPath, bool verifyHashes = true)
        {
            Log.Information("Starting restore operation. Backup path: {BackupPath}, Verify hashes: {VerifyHashes}", backupPath, verifyHashes);

            var result = new RestoreResult();

            if (string.IsNullOrWhiteSpace(backupPath))
            {
                Log.Warning("Backup path is null or empty");
                result.Errors.Add("Backup path cannot be null or empty.");
                return result;
            }

            if (!File.Exists(backupPath) && !Directory.Exists(backupPath))
            {
                Log.Warning("Backup path does not exist: {BackupPath}", backupPath);
                result.Errors.Add($"Backup path does not exist: {backupPath}");
                return result;
            }

            string backupDir = backupPath;
            string tempZipExtractPath = null;

            try
            {
                // Check if the backupPath is a zip file
                if (File.Exists(backupPath) && Path.GetExtension(backupPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Debug("Extracting backup zip file to temporary directory");
                    // Extract zip to a temporary directory
                    tempZipExtractPath = Path.Combine(Path.GetTempPath(), $"AGRestore_{Guid.NewGuid()}");
                    ZipFile.ExtractToDirectory(backupPath, tempZipExtractPath);
                    backupDir = tempZipExtractPath;
                    Log.Debug("Zip extracted to: {TempPath}", tempZipExtractPath);
                }

                // Check if manifest.json exists in the backup directory
                string manifestPath = Path.Combine(backupDir, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    Log.Error("Manifest not found in backup: {ManifestPath}", manifestPath);
                    result.Errors.Add($"Manifest not found in backup: {manifestPath}");
                    return result;
                }

                // Read and deserialize the manifest
                BackupManifest manifest;
                await using FileStream manifestStream = File.OpenRead(manifestPath);
                manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream);
                if (manifest == null)
                {
                    Log.Error("Failed to deserialize manifest from: {ManifestPath}", manifestPath);
                    result.Errors.Add("Failed to deserialize manifest.");
                    return result;
                }

                Log.Information("Manifest loaded. Entries count: {Count}", manifest.Entries.Count);

                // Process each entry in the manifest
                foreach (var entry in manifest.Entries)
                {
                    string backupFilePath = Path.Combine(backupDir, entry.RelativePath);
                    if (!File.Exists(backupFilePath))
                    {
                        Log.Warning("Backup file not found: {RelativePath}", entry.RelativePath);
                        result.Errors.Add($"Backup file not found: {entry.RelativePath}");
                        result.FilesFailed++;
                        continue;
                    }

                    bool hashMatches = true;
                    if (verifyHashes)
                    {
                        Log.Debug("Verifying hash for: {RelativePath}", entry.RelativePath);
                        string backupFileHash = ComputeSha256Hash(backupFilePath);
                        if (!backupFileHash.Equals(entry.Sha256Hash, StringComparison.OrdinalIgnoreCase))
                        {
                            Log.Warning("Hash mismatch for file {RelativePath}. Expected: {Expected}, Actual: {Actual}",
                                entry.RelativePath, entry.Sha256Hash, backupFileHash);
                            result.Errors.Add($"Hash mismatch for file {entry.RelativePath}. Expected: {entry.Sha256Hash}, Actual: {backupFileHash}");
                            result.HashMismatches++;
                            hashMatches = false;
                        }
                    }

                    if (hashMatches)
                    {
                        try
                        {
                            Log.Debug("Restoring file: {OriginalPath}", entry.OriginalPath);
                            // Ensure the destination directory exists
                            string destDir = Path.GetDirectoryName(entry.OriginalPath);
                            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            {
                                Directory.CreateDirectory(destDir);
                            }

                            // Copy the file (overwrite if exists)
                            File.Copy(backupFilePath, entry.OriginalPath, overwrite: true);
                            result.FilesRestored++;
                            result.RestoredFiles.Add(entry.OriginalPath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to restore file: {RelativePath}", entry.RelativePath);
                            result.Errors.Add($"Failed to restore file {entry.RelativePath}: {ex.Message}");
                            result.FilesFailed++;
                        }
                    }
                    else
                    {
                        // Hash mismatch already counted and error added
                        result.FilesFailed++;
                    }
                }

                result.Success = result.FilesFailed == 0 && result.HashMismatches == 0;
                Log.Information("Restore operation completed. Success: {Success}, Files restored: {FilesRestored}, Files failed: {FilesFailed}, Hash mismatches: {HashMismatches}",
                    result.Success, result.FilesRestored, result.FilesFailed, result.HashMismatches);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during restore operation");
                result.Errors.Add($"Unexpected error during restore: {ex.Message}");
                result.Success = false;
            }
            finally
            {
                // Clean up temporary extraction directory if we created one
                if (!string.IsNullOrEmpty(tempZipExtractPath) && Directory.Exists(tempZipExtractPath))
                {
                    try
                    {
                        Directory.Delete(tempZipExtractPath, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }

            return result;
        }

        private static string ComputeSha256Hash(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
