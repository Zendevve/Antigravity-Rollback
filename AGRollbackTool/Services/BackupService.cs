using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using AGRollbackTool.Services;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for backing up Google Antigravity data.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly string _backupRootPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupService"/> class.
        /// </summary>
        public BackupService()
        {
            // Default backup root: %USERPROFILE%\Documents\AG Backups\
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _backupRootPath = Path.Combine(documentsPath, "AG Backups");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupService"/> class with a custom backup root.
        /// </summary>
        /// <param name="backupRootPath">The root path where backups will be stored.</param>
        public BackupService(string backupRootPath)
        {
            _backupRootPath = backupRootPath ?? throw new ArgumentNullException(nameof(backupRootPath));
        }

        /// <inheritdoc/>
        public async Task<string> BackupAsync(bool compress = false)
        {
            // Create timestamped backup folder
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupDir = Path.Combine(_backupRootPath, timestamp);
            Directory.CreateDirectory(backupDir);

            // Define source paths to backup
            var sourcePaths = new List<SourcePathInfo>
            {
                new SourcePathInfo(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity"),
                    true), // isDirectory
                new SourcePathInfo(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "GEMINI.md"),
                    false), // isFile
                new SourcePathInfo(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "antigravity", "User", "settings.json"),
                    false),
                new SourcePathInfo(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "antigravity", "User", "keybindings.json"),
                    false),
                new SourcePathInfo(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "antigravity", "User", "globalStorage", "state.vscdb"),
                    false)
            };

            var manifest = new BackupManifest
            {
                Timestamp = DateTime.UtcNow,
                IsCompressed = false
            };

            try
            {
                foreach (var source in sourcePaths)
                {
                    if (!source.Exists)
                    {
                        // Skip missing paths but continue with others
                        continue;
                    }

                    if (source.IsDirectory)
                    {
                        await CopyDirectoryRecursiveAsync(source.Path, backupDir, manifest);
                    }
                    else
                    {
                        await CopyFileAsync(source.Path, backupDir, manifest);
                    }
                }

                // Write manifest to backup directory
                string manifestPath = Path.Combine(backupDir, "manifest.json");
                await WriteManifestAsync(manifest, manifestPath);

                // Add manifest itself to the manifest? (optional)
                // We'll add it after writing so it's included in the backup if we compress later
                // But for simplicity, we'll not include the manifest in itself for now.

                if (compress)
                {
                    string zipPath = Path.Combine(_backupRootPath, $"{timestamp}.zip");
                    ZipFile.CreateFromDirectory(backupDir, zipPath);
                    Directory.Delete(backupDir, true); // Delete the backup directory after zipping
                    return zipPath;
                }

                return backupDir;
            }
            catch (Exception ex)
            {
                // Clean up backup directory on failure
                if (Directory.Exists(backupDir))
                {
                    Directory.Delete(backupDir, true);
                }
                throw new IOException("Backup failed", ex);
            }
        }

        private async Task CopyFileAsync(string sourceFilePath, string backupDir, BackupManifest manifest)
        {
            try
            {
                // Get relative path from the source root to maintain structure
                // For files, we'll copy them directly to the backup root?
                // But we want to preserve the directory structure relative to a common root?
                // The PRD doesn't specify, so we'll preserve the full relative path from the user's profile or appdata?
                // Instead, we'll copy the file to a path that mirrors its location relative to a known root.
                // We'll use the source file's full path and replace the known root (UserProfile or ApplicationData) with an empty string?
                // This is getting complex.

                // Simpler: For each source, we copy it to the backup directory with the same name.
                // But if we have two files with the same name in different directories, they'll clash.
                // Example: settings.json and keybindings.json are in the same directory, so that's okay.
                // But the antigravity directory might have many files.

                // We decided to copy directories recursively and files individually, but we need to avoid name collisions.

                // Let's change approach: We'll define a base root for each source and then replicate the structure under the backup directory.

                // We'll do:
                //   For each source, we compute a relative path from a known root (either UserProfile or ApplicationData)
                //   Then we combine that relative path with the backup directory.

                // However, we have two different roots: UserProfile and ApplicationData.

                // We'll handle each source individually and use its own root.

                // Determine the root for this source
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                string relativePath;
                if (sourceFilePath.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = Path.GetRelativePath(userProfile, sourceFilePath);
                }
                else if (sourceFilePath.StartsWith(appData, StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = Path.GetRelativePath(appData, sourceFilePath);
                }
                else
                {
                    // Fallback: just use the file name
                    relativePath = Path.GetFileName(sourceFilePath);
                }

                string destFilePath = Path.Combine(backupDir, relativePath);
                string destDir = Path.GetDirectoryName(destFilePath);
                if (!string.IsNullOrEmpty(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // Copy the file
                await using FileStream sourceStream = File.OpenRead(sourceFilePath);
                await using FileStream destStream = File.Create(destFilePath);
                await sourceStream.CopyToAsync(destStream);

                // Compute SHA-256 hash
                string sha256 = ComputeSha256Hash(sourceFilePath);

                // Get file info
                FileInfo info = new FileInfo(sourceFilePath);

                // Add to manifest
                manifest.Entries.Add(new BackupEntry
                {
                    RelativePath = relativePath,
                    OriginalPath = sourceFilePath,
                    Size = info.Length,
                    Sha256Hash = sha256,
                    LastModified = info.LastWriteTimeUtc
                });
            }
            catch (Exception ex)
            {
                // Log and continue? For now, we'll throw to stop the backup.
                throw new IOException($"Failed to backup file {sourceFilePath}", ex);
            }
        }

        private async Task CopyDirectoryRecursiveAsync(string sourceDirPath, string backupDir, BackupManifest manifest)
        {
            try
            {
                // Get all files in the directory recursively
                IEnumerable<string> files = Directory.EnumerateFiles(sourceDirPath, "*.*", SearchOption.AllDirectories);

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                foreach (string file in files)
                {
                    string relativePath;
                    if (file.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
                    {
                        relativePath = Path.GetRelativePath(userProfile, file);
                    }
                    else if (file.StartsWith(appData, StringComparison.OrdinalIgnoreCase))
                    {
                        relativePath = Path.GetRelativePath(appData, file);
                    }
                    else
                    {
                        // Fallback: relative to the source directory
                        relativePath = Path.GetRelativePath(sourceDirPath, file);
                    }

                    string destFilePath = Path.Combine(backupDir, relativePath);
                    string destDir = Path.GetDirectoryName(destFilePath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // Copy the file
                    await using FileStream sourceStream = File.OpenRead(file);
                    await using FileStream destStream = File.Create(destFilePath);
                    await sourceStream.CopyToAsync(destStream);

                    // Compute SHA-256 hash
                    string sha256 = ComputeSha256Hash(file);

                    // Get file info
                    FileInfo info = new FileInfo(file);

                    // Add to manifest
                    manifest.Entries.Add(new BackupEntry
                    {
                        RelativePath = relativePath,
                        OriginalPath = file,
                        Size = info.Length,
                        Sha256Hash = sha256,
                        LastModified = info.LastWriteTimeUtc
                    });
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to backup directory {sourceDirPath}", ex);
            }
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

        private async Task WriteManifestAsync(BackupManifest manifest, string manifestPath)
        {
            // Options for pretty JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            await using FileStream stream = File.Create(manifestPath);
            await JsonSerializer.SerializeAsync(stream, manifest, options);
        }
    }

    /// <summary>
    /// Helper class to hold source path information.
    /// </summary>
    internal class SourcePathInfo
    {
        public string Path { get; }
        public bool IsDirectory { get; }
        public bool Exists => IsDirectory ? Directory.Exists(Path) : File.Exists(Path);

        public SourcePathInfo(string path, bool isDirectory)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            IsDirectory = isDirectory;
        }
    }
}
