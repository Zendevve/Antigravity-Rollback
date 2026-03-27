using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for detecting the version of Google Antigravity.
    /// </summary>
    public class VersionDetectorService : IVersionDetectorService
    {
        private readonly IPathResolver _pathResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionDetectorService"/> class.
        /// </summary>
        /// <param name="pathResolver">The path resolver service.</param>
        public VersionDetectorService(IPathResolver pathResolver)
        {
            _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        }

        /// <inheritdoc/>
        public VersionInfo DetectVersion()
        {
            try
            {
                // Try to get version from package.json or product.json in the Antigravity directory
                var versionFromJson = DetectVersionFromJsonFiles();
                if (versionFromJson.Success)
                {
                    return versionFromJson;
                }

                // Fallback: Try to get version from file version info of the main executable
                var versionFromFile = DetectVersionFromFileInfo();
                if (versionFromFile.Success)
                {
                    return versionFromFile;
                }

                // Fallback: Try to get version from Windows Registry
                var versionFromRegistry = DetectVersionFromRegistry();
                if (versionFromRegistry.Success)
                {
                    return versionFromRegistry;
                }

                // If all methods fail, return failure
                return new VersionInfo("0.0.0", false, "Unable to detect Antigravity version using available methods");
            }
            catch (Exception ex)
            {
                return new VersionInfo("0.0.0", false, $"Error detecting version: {ex.Message}");
            }
        }

        /// <summary>
        /// Detects version from package.json or product.json files.
        /// </summary>
        /// <returns>Version information.</returns>
        private VersionInfo DetectVersionFromJsonFiles()
        {
            try
            {
                // Get the Antigravity installation path
                var appPathInfo = _pathResolver.GetApplicationBinaryPath();
                if (!appPathInfo.Exists)
                {
                    return new VersionInfo("0.0.0", false, "Antigravity application path not found");
                }

                string appDirectory = Path.GetDirectoryName(appPathInfo.Path);
                if (string.IsNullOrEmpty(appDirectory))
                {
                    return new VersionInfo("0.0.0", false, "Unable to determine Antigravity application directory");
                }

                // Try package.json first
                string packageJsonPath = Path.Combine(appDirectory, "package.json");
                if (File.Exists(packageJsonPath))
                {
                    string jsonContent = File.ReadAllText(packageJsonPath);
                    using JsonDocument document = JsonDocument.Parse(jsonContent);
                    if (document.RootElement.TryGetProperty("version", out JsonElement versionElement))
                    {
                        string version = versionElement.GetString();
                        if (!string.IsNullOrEmpty(version))
                        {
                            return new VersionInfo(version, true);
                        }
                    }
                }

                // Try product.json as fallback
                string productJsonPath = Path.Combine(appDirectory, "product.json");
                if (File.Exists(productJsonPath))
                {
                    string jsonContent = File.ReadAllText(productJsonPath);
                    using JsonDocument document = JsonDocument.Parse(jsonContent);
                    if (document.RootElement.TryGetProperty("version", out JsonElement versionElement))
                    {
                        string version = versionElement.GetString();
                        if (!string.IsNullOrEmpty(version))
                        {
                            return new VersionInfo(version, true);
                        }
                    }
                    // Also try productName and productVersion fields sometimes used
                    else if (document.RootElement.TryGetProperty("productVersion", out JsonElement productVersionElement))
                    {
                        string version = productVersionElement.GetString();
                        if (!string.IsNullOrEmpty(version))
                        {
                            return new VersionInfo(version, true);
                        }
                    }
                }

                return new VersionInfo("0.0.0", false, "Version not found in package.json or product.json");
            }
            catch (Exception ex)
            {
                return new VersionInfo("0.0.0", false, $"Error reading JSON version files: {ex.Message}");
            }
        }

        /// <summary>
        /// Detects version from file version information of the main executable.
        /// </summary>
        /// <returns>Version information.</returns>
        private VersionInfo DetectVersionFromFileInfo()
        {
            try
            {
                // Get the Antigravity application binary path
                var appPathInfo = _pathResolver.GetApplicationBinaryPath();
                if (!appPathInfo.Exists)
                {
                    return new VersionInfo("0.0.0", false, "Antigravity application binary not found");
                }

                if (!File.Exists(appPathInfo.Path))
                {
                    return new VersionInfo("0.0.0", false, "Antigravity application binary file does not exist");
                }

                // Get file version info
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(appPathInfo.Path);
                string version = versionInfo.ProductVersion;

                if (!string.IsNullOrEmpty(version))
                {
                    return new VersionInfo(version, true);
                }

                return new VersionInfo("0.0.0", false, "Product version not found in file version info");
            }
            catch (Exception ex)
            {
                return new VersionInfo("0.0.0", false, $"Error reading file version info: {ex.Message}");
            }
        }

        /// <summary>
        /// Detects version from Windows Registry.
        /// </summary>
        /// <returns>Version information.</returns>
        private VersionInfo DetectVersionFromRegistry()
        {
            try
            {
                // Try to find Antigravity in common registry locations
                string[] registryPaths = {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (string registryPath in registryPaths)
                {
                    using (RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey(registryPath))
                    {
                        if (uninstallKey == null)
                        {
                            continue;
                        }

                        foreach (string subkeyName in uninstallKey.GetSubKeyNames())
                        {
                            using (RegistryKey subkey = uninstallKey.OpenSubKey(subkeyName))
                            {
                                if (subkey == null)
                                {
                                    continue;
                                }

                                // Look for Antigravity-related display names
                                object displayNameObj = subkey.GetValue("DisplayName");
                                if (displayNameObj != null && displayNameObj.ToString()?.Contains("Antigravity", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    object displayVersionObj = subkey.GetValue("DisplayVersion");
                                    if (displayVersionObj != null)
                                    {
                                        string version = displayVersionObj.ToString();
                                        if (!string.IsNullOrEmpty(version))
                                        {
                                            return new VersionInfo(version, true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Also try to look in Google-specific registry paths
                string[] googleRegistryPaths = {
                    @"SOFTWARE\Google\Antigravity",
                    @"SOFTWARE\Wow6432Node\Google\Antigravity"
                };

                foreach (string registryPath in googleRegistryPaths)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            object versionObj = key.GetValue("Version");
                            if (versionObj != null)
                            {
                                string version = versionObj.ToString();
                                if (!string.IsNullOrEmpty(version))
                                {
                                    return new VersionInfo(version, true);
                                }
                            }
                        }
                    }
                }

                return new VersionInfo("0.0.0", false, "Version not found in registry");
            }
            catch (Exception ex)
            {
                return new VersionInfo("0.0.0", false, $"Error reading version from registry: {ex.Message}");
            }
        }
    }
}
