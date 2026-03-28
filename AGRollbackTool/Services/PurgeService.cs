using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Serilog;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for purging Google Antigravity from a Windows system.
    /// </summary>
    public class PurgeService : IPurgeService
    {
        private readonly IProcessKiller _processKiller;
        private readonly IPathResolver _pathResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="PurgeService"/> class.
        /// </summary>
        /// <param name="processKiller">The process killer service.</param>
        /// <param name="pathResolver">The path resolver service.</param>
        public PurgeService(IProcessKiller processKiller, IPathResolver pathResolver)
        {
            _processKiller = processKiller ?? throw new ArgumentNullException(nameof(processKiller));
            _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        }

        /// <summary>
        /// Purges Google Antigravity from the system.
        /// </summary>
        /// <returns>A purge result detailing what was purged and any errors.</returns>
        public async Task<PurgeResult> PurgeAsync()
        {
            Log.Information("Starting purge operation");

            var result = new PurgeResult();

            try
            {
                Log.Debug("Step 1: Terminating Antigravity processes");
                // Step 1: Terminate all Antigravity processes first
                await TerminateAntigravityProcessesAsync(result);

                Log.Debug("Step 2: Deleting directories");
                // Step 2: Delete directories in specified order
                await DeleteStagedUpdatePayloadAsync(result);
                await DeleteUserDataAsync(result);
                await DeleteApplicationBinaryAsync(result);

                Log.Debug("Step 3: Deleting registry keys");
                // Step 3: Delete registry keys
                await DeleteRegistryKeysAsync(result);

                Log.Debug("Step 4: Removing shortcuts");
                // Step 4: Remove shortcuts
                await RemoveStartMenuShortcutsAsync(result);
                await RemoveDesktopShortcutsAsync(result);

                // Set success if no critical errors occurred
                result.Success = !result.Errors.Any(e => e.Contains("Access denied", StringComparison.OrdinalIgnoreCase) ||
                                                         e.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase));

                Log.Information("Purge operation completed. Success: {Success}, Directories deleted: {DirCount}, Registry keys deleted: {RegCount}, Shortcuts deleted: {Shortcuts}",
                    result.Success, result.DirectoriesDeleted, result.RegistryKeysDeleted, result.ShortcutsDeleted);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error during purge operation");
                result.Errors.Add($"Unexpected error during purge: {ex.Message}");
                result.Success = false;
            }

            return result;
        }

        private async Task TerminateAntigravityProcessesAsync(PurgeResult result)
        {
            try
            {
                Log.Debug("Terminating Antigravity processes");
                var processResults = _processKiller.KillAllAntigravityProcesses();
                foreach (var processInfo in processResults)
                {
                    if (processInfo.IsRunning)
                    {
                        Log.Error("Failed to terminate process: {Name}, Error: {Error}", processInfo.Name, processInfo.ErrorMessage);
                        result.Errors.Add($"Failed to terminate process {processInfo.Name}: {processInfo.ErrorMessage}");
                    }
                    else if (!string.IsNullOrEmpty(processInfo.ErrorMessage))
                    {
                        Log.Warning("Warning terminating {Name}: {Error}", processInfo.Name, processInfo.ErrorMessage);
                        // Non-critical error (like access denied) but process terminated
                        result.Errors.Add($"Warning terminating {processInfo.Name}: {processInfo.ErrorMessage}");
                    }
                }
                Log.Debug("Terminated {Count} processes", processResults.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error terminating Antigravity processes");
                result.Errors.Add($"Error terminating Antigravity processes: {ex.Message}");
            }
        }

        private async Task DeleteStagedUpdatePayloadAsync(PurgeResult result)
        {
            try
            {
                var pathInfo = _pathResolver.GetStagedUpdateCachePath();
                if (pathInfo.Exists)
                {
                    Log.Debug("Deleting staged update payload: {Path}", pathInfo.Path);
                    DeleteDirectoryRecursive(pathInfo.Path);
                    result.DirectoriesDeleted++;
                    result.PurgedItems.Add($"Deleted staged update payload: {pathInfo.Path}");
                }
                else
                {
                    Log.Debug("Staged update payload not found: {Path}", pathInfo.Path);
                    result.PurgedItems.Add($"Staged update payload not found: {pathInfo.Path}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting staged update payload");
                result.Errors.Add($"Error deleting staged update payload: {ex.Message}");
            }
        }

        private async Task DeleteUserDataAsync(PurgeResult result)
        {
            try
            {
                var pathInfo = _pathResolver.GetUserSettingsPath();
                if (pathInfo.Exists)
                {
                    // Delete the entire antigravity folder under AppData
                    string appDataAntigravityPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "antigravity");

                    if (Directory.Exists(appDataAntigravityPath))
                    {
                        Log.Debug("Deleting user data: {Path}", appDataAntigravityPath);
                        DeleteDirectoryRecursive(appDataAntigravityPath);
                        result.DirectoriesDeleted++;
                        result.PurgedItems.Add($"Deleted user data: {appDataAntigravityPath}");
                    }
                    else
                    {
                        Log.Debug("User data not found: {Path}", appDataAntigravityPath);
                        result.PurgedItems.Add($"User data not found: {appDataAntigravityPath}");
                    }
                }
                else
                {
                    Log.Debug("User data path not found: {Path}", pathInfo.Path);
                    result.PurgedItems.Add($"User data path not found: {pathInfo.Path}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting user data");
                result.Errors.Add($"Error deleting user data: {ex.Message}");
            }
        }

        private async Task DeleteApplicationBinaryAsync(PurgeResult result)
        {
            try
            {
                var pathInfo = _pathResolver.GetApplicationBinaryPath();
                if (pathInfo.Exists)
                {
                    Log.Debug("Deleting application binary: {Path}", pathInfo.Path);
                    DeleteDirectoryRecursive(pathInfo.Path);
                    result.DirectoriesDeleted++;
                    result.PurgedItems.Add($"Deleted application binary: {pathInfo.Path}");
                }
                else
                {
                    Log.Debug("Application binary not found: {Path}", pathInfo.Path);
                    result.PurgedItems.Add($"Application binary not found: {pathInfo.Path}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting application binary");
                result.Errors.Add($"Error deleting application binary: {ex.Message}");
            }
        }

        private async Task DeleteRegistryKeysAsync(PurgeResult result)
        {
            try
            {
                // Delete HKCU\Software\antigravity
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree("Software\\antigravity", false);
                    Log.Debug("Deleted registry key: HKCU\\Software\\antigravity");
                    result.RegistryKeysDeleted++;
                    result.PurgedItems.Add("Deleted registry key: HKCU\\Software\\antigravity");
                }
                catch (ArgumentException)
                {
                    Log.Debug("Registry key not found: HKCU\\Software\\antigravity");
                    result.PurgedItems.Add("Registry key not found: HKCU\\Software\\antigravity");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Error(ex, "Access denied deleting registry key HKCU\\Software\\antigravity");
                    result.Errors.Add($"Access denied deleting registry key HKCU\\Software\\antigravity: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error deleting registry key HKCU\\Software\\antigravity");
                    result.Errors.Add($"Error deleting registry key HKCU\\Software\\antigravity: {ex.Message}");
                }

                // Delete HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\antigravity
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\antigravity", false);
                    Log.Debug("Deleted registry key: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\antigravity");
                    result.RegistryKeysDeleted++;
                    result.PurgedItems.Add("Deleted registry key: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\antigravity");
                }
                catch (ArgumentException)
                {
                    Log.Debug("Registry key not found: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\antigravity");
                    result.PurgedItems.Add("Registry key not found: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\antigravity");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Error(ex, "Access denied deleting registry key HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\antigravity");
                    result.Errors.Add($"Access denied deleting registry key HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\antigravity: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error deleting registry key HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\antigravity");
                    result.Errors.Add($"Error deleting registry key HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\antigravity: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error deleting registry keys");
                result.Errors.Add($"Unexpected error deleting registry keys: {ex.Message}");
            }
        }

        private async Task RemoveStartMenuShortcutsAsync(PurgeResult result)
        {
            try
            {
                string startMenuPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs");

                if (Directory.Exists(startMenuPath))
                {
                    // Look for antigravity shortcuts
                    var shortcutFiles = Directory.GetFiles(startMenuPath, "antigravity*.lnk", SearchOption.AllDirectories);
                    Log.Debug("Found {Count} Start Menu shortcuts to delete", shortcutFiles.Length);
                    foreach (var shortcutFile in shortcutFiles)
                    {
                        try
                        {
                            File.Delete(shortcutFile);
                            result.ShortcutsDeleted++;
                            result.PurgedItems.Add($"Deleted Start Menu shortcut: {shortcutFile}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error deleting Start Menu shortcut: {Path}", shortcutFile);
                            result.Errors.Add($"Error deleting Start Menu shortcut {shortcutFile}: {ex.Message}");
                        }
                    }

                    if (shortcutFiles.Length == 0)
                    {
                        result.PurgedItems.Add("No Start Menu shortcuts found for Antigravity");
                    }
                }
                else
                {
                    result.PurgedItems.Add("Start Menu Programs directory not found");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error removing Start Menu shortcuts");
                result.Errors.Add($"Error removing Start Menu shortcuts: {ex.Message}");
            }
        }

        private async Task RemoveDesktopShortcutsAsync(PurgeResult result)
            {
                try
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    if (Directory.Exists(desktopPath))
                    {
                        // Look for antigravity shortcuts on desktop
                        var shortcutFiles = Directory.GetFiles(desktopPath, "antigravity*.lnk");
                        Log.Debug("Found {Count} Desktop shortcuts to delete", shortcutFiles.Length);
                        foreach (var shortcutFile in shortcutFiles)
                        {
                            try
                            {
                                File.Delete(shortcutFile);
                                result.ShortcutsDeleted++;
                                result.PurgedItems.Add($"Deleted Desktop shortcut: {shortcutFile}");
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error deleting Desktop shortcut: {Path}", shortcutFile);
                                result.Errors.Add($"Error deleting Desktop shortcut {shortcutFile}: {ex.Message}");
                            }
                        }

                        if (shortcutFiles.Length == 0)
                        {
                            result.PurgedItems.Add("No Desktop shortcuts found for Antigravity");
                        }
                    }
                    else
                    {
                        result.PurgedItems.Add("Desktop directory not found");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error removing Desktop shortcuts");
                    result.Errors.Add($"Error removing Desktop shortcuts: {ex.Message}");
                }
            }

        private void DeleteDirectoryRecursive(string path)
        {
            try
            {
                // Set attributes to normal to allow deletion
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                var directories = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                foreach (var dir in directories)
                {
                    File.SetAttributes(dir, FileAttributes.Normal);
                }

                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                // If we still can't delete due to permissions, throw to be handled by caller
                throw;
            }
            catch (PathTooLongException)
            {
                // Handle long paths by using extended length path prefix
                string extendedPath = @"\\?\ " + path;
                Directory.Delete(extendedPath, true);
            }
        }
    }
}
