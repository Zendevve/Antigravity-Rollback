using System;
using System.IO;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for validating and comparing installer versions.
    /// </summary>
    public interface IInstallerVersionService
    {
        /// <summary>
        /// Gets the version from an installer file.
        /// </summary>
        /// <param name="installerPath">Path to the installer executable.</param>
        /// <returns>VersionInfo if version can be determined, null otherwise.</returns>
        VersionInfo? GetInstallerVersion(string installerPath);

        /// <summary>
        /// Compares an installer version with an expected version.
        /// </summary>
        /// <param name="installerPath">Path to the installer executable.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <returns>VersionComparisonResult indicating the relationship between versions.</returns>
        VersionComparisonResult CompareVersion(string installerPath, VersionInfo? expectedVersion);

        /// <summary>
        /// Shows a warning dialog if versions don't match, allowing user to proceed or cancel.
        /// </summary>
        /// <param name="installerPath">Path to the installer executable.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <returns>True if user wants to proceed, false if they cancel.</returns>
        bool ShowVersionWarningAndGetConfirmation(string installerPath, VersionInfo? expectedVersion);
    }

    /// <summary>
    /// Represents the result of comparing two versions.
    /// </summary>
    public enum VersionComparisonResult
    {
        /// <summary>
        /// Versions match.
        /// </summary>
        Match,

        /// <summary>
        /// Installer version is different from expected.
        /// </summary>
        Different,

        /// <summary>
        /// Could not determine installer version.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Service for validating and comparing installer versions.
    /// </summary>
    public class InstallerVersionService : IInstallerVersionService
    {
        /// <inheritdoc/>
        public VersionInfo? GetInstallerVersion(string installerPath)
        {
            if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
            {
                return null;
            }

            try
            {
                // Try to get version from file properties
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(installerPath);

                if (!string.IsNullOrEmpty(versionInfo.FileVersion))
                {
                    // Try to parse the version string
                    if (Version.TryParse(versionInfo.FileVersion, out var version))
                    {
                        return new VersionInfo
                        {
                            Major = version.Major,
                            Minor = version.Minor,
                            Build = version.Build,
                            FullVersion = version.ToString()
                        };
                    }
                }

                // Try to extract version from file name if parsing fails
                string fileName = Path.GetFileNameWithoutExtension(installerPath);
                return ExtractVersionFromFileName(fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public VersionComparisonResult CompareVersion(string installerPath, VersionInfo? expectedVersion)
        {
            var installerVersion = GetInstallerVersion(installerPath);

            if (installerVersion == null)
            {
                return VersionComparisonResult.Unknown;
            }

            if (expectedVersion == null)
            {
                return VersionComparisonResult.Unknown;
            }

            if (installerVersion.Major == expectedVersion.Major &&
                installerVersion.Minor == expectedVersion.Minor &&
                installerVersion.Build == expectedVersion.Build)
            {
                return VersionComparisonResult.Match;
            }

            return VersionComparisonResult.Different;
        }

        /// <inheritdoc/>
        public bool ShowVersionWarningAndGetConfirmation(string installerPath, VersionInfo? expectedVersion)
        {
            var installerVersion = GetInstallerVersion(installerPath);
            string installerVersionStr = installerVersion?.FullVersion ?? "unknown";
            string expectedVersionStr = expectedVersion?.FullVersion ?? "unknown";

            var result = System.Windows.MessageBox.Show(
                $"The selected installer version ({installerVersionStr}) does not match the expected version ({expectedVersionStr}).\n\n" +
                "Do you want to proceed anyway?",
                "Version Mismatch Warning",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            return result == System.Windows.MessageBoxResult.Yes;
        }

        private VersionInfo? ExtractVersionFromFileName(string fileName)
        {
            // Try to extract version from common patterns like "Antigravity_1.2.3.exe" or "Antigravity-1.2.3-setup.exe"
            // This is a simple heuristic - in a real implementation, you'd use more robust parsing

            // Look for version pattern like 1.2.3 or 1_2_3
            var parts = fileName.Split(new[] { '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);

            int major = 0, minor = 0, build = 0;
            bool foundVersion = false;

            foreach (var part in parts)
            {
                if (int.TryParse(part, out int num))
                {
                    if (major == 0)
                    {
                        major = num;
                        foundVersion = true;
                    }
                    else if (minor == 0)
                    {
                        minor = num;
                    }
                    else if (build == 0)
                    {
                        build = num;
                        break;
                    }
                }
            }

            if (foundVersion)
            {
                return new VersionInfo
                {
                    Major = major,
                    Minor = minor,
                    Build = build,
                    FullVersion = $"{major}.{minor}.{build}"
                };
            }

            return null;
        }
    }
}
