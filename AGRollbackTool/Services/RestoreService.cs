using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

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
            var result = new RestoreResult();

            if (string.IsNullOrWhiteSpace(backupPath))
            {
                result.Errors.Add("Backup path cannot be null or empty.");
                return result;
            }

            if (!File.Exists(backupPath) && !Directory.Exists(backupPath))
            {
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
                    // Extract zip to a temporary directory
                    tempZipExtractPath = Path.Combine(Path.GetTempPath(), $"AGRestore_{Guid.NewGuid()}");
                    ZipFile.ExtractToDirectory(backupPath, tempZipExtractPath);
                    backupDir = tempZipExtractPath;
                }

                // Check if manifest.json exists in the backup directory
                string manifestPath = Path.Combine(backupDir, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    result.Errors.Add($"Manifest not found in backup: {manifestPath}");
                    return result;
                }

                // Read and deserialize the manifest
                BackupManifest manifest;
                await using FileStream manifestStream = File.OpenRead(manifestPath);
                manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream);
                if (manifest == null)
                {
                    result.Errors.Add("Failed to deserialize manifest.");
                    return result;
                }

                // Process each entry in the manifest
                foreach (var entry in manifest.Entries)
                {
                    string backupFilePath = Path.Combine(backupDir, entry.RelativePath);
                    if (!File.Exists(backupFilePath))
                    {
                        result.Errors.Add($"Backup file not found: {entry.RelativePath}");
                        result.FilesFailed++;
                        continue;
                    }

                    bool hashMatches = true;
                    if (verifyHashes)
                    {
                        string backupFileHash = ComputeSha256Hash(backupFilePath);
                        if (!backupFileHash.Equals(entry.Sha256Hash, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Errors.Add($"Hash mismatch for file {entry.RelativePath}. Expected: {entry.Sha256Hash}, Actual: {backupFileHash}");
                            result.HashMismatches++;
                            hashMatches = false;
                        }
                    }

                    if (hashMatches)
                    {
                        try
                        {
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
            }
            catch (Exception ex)
            {
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
