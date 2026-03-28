using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for managing network blackout of Antigravity applications via Windows Firewall.
    /// </summary>
    public class NetworkBlackoutService : INetworkBlackoutService
    {
        private readonly IPathResolver _pathResolver;
        private const string AntigravityRuleName = "AGRollbackTool Block antigravity.exe Outbound";
        private const string UpdaterRuleName = "AGRollbackTool Block updater.exe Outbound";

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkBlackoutService"/> class.
        /// </summary>
        /// <param name="pathResolver">The path resolver service.</param>
        public NetworkBlackoutService(IPathResolver pathResolver)
        {
            _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        }

        /// <summary>
        /// Blocks outbound network access for antigravity.exe and updater.exe.
        /// If firewall rules already exist and are active, this method will skip gracefully.
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the operation requires elevated privileges.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the application binary path is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown when firewall operations fail.</exception>
        public void BlockAntigravityNetworkAccess()
        {
            Log.Information("Starting network blackout operation");

            try
            {
                // Check if firewall rules already exist and are active - if so, skip gracefully
                if (AreFirewallRulesActive())
                {
                    Log.Information("Firewall rules already active, skipping creation");
                    // Rules already exist and are active - no need to create again
                    return;
                }

                var appBinaryPathInfo = _pathResolver.GetApplicationBinaryPath();
                if (!appBinaryPathInfo.Exists)
                {
                    Log.Error("Application binary path not found: {Path}", appBinaryPathInfo.Path);
                    throw new DirectoryNotFoundException($"Application binary path not found: {appBinaryPathInfo.Path}");
                }

                string antigravityExe = Path.Combine(appBinaryPathInfo.Path, "antigravity.exe");
                string updaterExe = Path.Combine(appBinaryPathInfo.Path, "updater.exe");

                Log.Debug("Creating firewall rule for antigravity.exe: {Path}", antigravityExe);
                // Ensure firewall rules are created and enabled for both executables
                EnsureFirewallRuleEnabled(AntigravityRuleName, antigravityExe);

                Log.Debug("Creating firewall rule for updater.exe: {Path}", updaterExe);
                EnsureFirewallRuleEnabled(UpdaterRuleName, updaterExe);

                // Verify both rules are active
                if (!AreFirewallRulesActive())
                {
                    Log.Error("Failed to activate one or more firewall rules");
                    throw new InvalidOperationException("Failed to activate one or more firewall rules.");
                }

                Log.Information("Network blackout applied successfully. antigravity.exe and updater.exe blocked.");
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access denied
            {
                Log.Error(ex, "Access denied when modifying firewall settings. Administrator privileges required.");
                throw new System.ComponentModel.Win32Exception(ex.NativeErrorCode,
                    "Access denied. Administrator privileges are required to modify firewall settings.", ex);
            }
        }

        /// <summary>
        /// Unblocks outbound network access for antigravity.exe and updater.exe by removing the firewall rules.
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the operation requires elevated privileges.</exception>
        /// <exception cref="InvalidOperationException">Thrown when firewall operations fail.</exception>
        public void UnblockAntigravityNetworkAccess()
        {
            Log.Information("Starting network unblock operation");

            try
            {
                // Remove firewall rules by name
                Log.Debug("Removing firewall rule: {RuleName}", AntigravityRuleName);
                EnsureFirewallRuleRemoved(AntigravityRuleName);
                Log.Debug("Removing firewall rule: {RuleName}", UpdaterRuleName);
                EnsureFirewallRuleRemoved(UpdaterRuleName);

                // Verify both rules are removed
                if (AreFirewallRulesActive())
                {
                    Log.Error("Failed to remove one or more firewall rules");
                    throw new InvalidOperationException("Failed to remove one or more firewall rules.");
                }

                Log.Information("Network unblock completed successfully. Firewall rules removed.");
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access denied
            {
                Log.Error(ex, "Access denied when modifying firewall settings. Administrator privileges required.");
                throw new System.ComponentModel.Win32Exception(ex.NativeErrorCode,
                    "Access denied. Administrator privileges are required to modify firewall settings.", ex);
            }
        }

        /// <summary>
        /// Checks if the firewall rules for blocking Antigravity network access are active.
        /// </summary>
        /// <returns>True if both rules exist and are enabled, false otherwise.</returns>
        public bool AreFirewallRulesActive()
        {
            bool antigravityActive = IsFirewallRuleActive(AntigravityRuleName);
            bool updaterActive = IsFirewallRuleActive(UpdaterRuleName);
            Log.Debug("Firewall rules status - antigravity.exe: {Status}, updater.exe: {Status2}", antigravityActive, updaterActive);
            return antigravityActive && updaterActive;
        }

        /// <summary>
        /// Removes a specific firewall rule by name.
        /// </summary>
        /// <param name="ruleName">The name of the firewall rule to remove.</param>
        /// <returns>Result of the firewall rule removal operation.</returns>
        public FirewallOperationResult RemoveFirewallRule(string ruleName)
        {
            Log.Information("Removing firewall rule: {RuleName}", ruleName);

            var result = new FirewallOperationResult();

            try
            {
                if (string.IsNullOrWhiteSpace(ruleName))
                {
                    Log.Warning("Rule name is null, empty, or whitespace");
                    result.Success = false;
                    result.Message = "Rule name cannot be null, empty, or whitespace.";
                    result.AddError("Rule name is required.");
                    return result;
                }

                if (FirewallRuleExists(ruleName))
                {
                    Log.Debug("Firewall rule exists, attempting to delete: {RuleName}", ruleName);
                    var (exitCode, output) = RunNetShCommand(
                        $"advfirewall firewall delete rule name=\"{ruleName}\"");

                    if (exitCode == 0)
                    {
                        Log.Information("Firewall rule removed successfully: {RuleName}", ruleName);
                        result.Success = true;
                        result.Message = $"Firewall rule '{ruleName}' removed successfully.";
                        result.AddAffectedRule(ruleName);
                    }
                    else
                    {
                        Log.Error("Failed to remove firewall rule {RuleName}. Exit code: {ExitCode}, Output: {Output}", ruleName, exitCode, output);
                        result.Success = false;
                        result.Message = $"Failed to remove firewall rule '{ruleName}'. Exit code: {exitCode}, Output: {output}";
                        result.AddError($"Netsh command failed with exit code {exitCode}: {output}");
                    }
                }
                else
                {
                    // Rule doesn't exist, consider this a successful removal
                    Log.Debug("Firewall rule does not exist, nothing to remove: {RuleName}", ruleName);
                    result.Success = true;
                    result.Message = $"Firewall rule '{ruleName}' does not exist, nothing to remove.";
                    result.AddWarning($"Rule '{ruleName}' was not found.");
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access denied
            {
                Log.Error(ex, "Access denied when removing firewall rule: {RuleName}", ruleName);
                result.Success = false;
                result.Message = "Access denied. Administrator privileges are required to modify firewall settings.";
                result.AddError("Access denied. Administrator privileges are required to modify firewall settings.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error occurred while removing firewall rule: {RuleName}", ruleName);
                result.Success = false;
                result.Message = $"Unexpected error occurred while removing firewall rule '{ruleName}': {ex.Message}";
                result.AddError($"Unexpected error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Verifies if a specific firewall rule exists and is active.
        /// </summary>
        /// <param name="ruleName">The name of the firewall rule to verify.</param>
        /// <returns>True if the rule exists and is enabled, false otherwise.</returns>
        public bool VerifyFirewallRule(string ruleName)
        {
            if (string.IsNullOrWhiteSpace(ruleName))
            {
                return false;
            }

            return IsFirewallRuleActive(ruleName);
        }

        #region Helper Methods

        /// <summary>
        /// Ensures the firewall rule exists and is enabled. Creates it if it doesn't exist, enables it if it exists but is disabled.
        /// </summary>
        /// <param name="ruleName">The name of the firewall rule.</param>
        /// <param name="programPath">The full path to the executable the rule applies to.</param>
        private void EnsureFirewallRuleEnabled(string ruleName, string programPath)
        {
            if (!FirewallRuleExists(ruleName))
            {
                // Create the rule
                var (exitCode, output) = RunNetShCommand(
                    $"advfirewall firewall add rule name=\"{ruleName}\" dir=out action=block program=\"{programPath}\" enable=yes");
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to create firewall rule '{ruleName}'. Exit code: {exitCode}, Output: {output}");
                }
            }
            else if (!IsFirewallRuleEnabled(ruleName))
            {
                // Enable the existing rule
                var (exitCode, output) = RunNetShCommand(
                    $"advfirewall firewall set rule name=\"{ruleName}\" new enable=yes");
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to enable firewall rule '{ruleName}'. Exit code: {exitCode}, Output: {output}");
                }
            }
        }

        /// <summary>
        /// Ensures the firewall rule is removed if it exists.
        /// </summary>
        /// <param name="ruleName">The name of the firewall rule to remove.</param>
        private void EnsureFirewallRuleRemoved(string ruleName)
        {
            if (FirewallRuleExists(ruleName))
            {
                var (exitCode, output) = RunNetShCommand(
                    $"advfirewall firewall delete rule name=\"{ruleName}\"");
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to delete firewall rule '{ruleName}'. Exit code: {exitCode}, Output: {output}");
                }
            }
        }

        /// <summary>
        /// Checks if a firewall rule exists (regardless of enabled state).
        /// </summary>
        /// <param name="ruleName">The name of the firewall rule to check.</param>
        /// <returns>True if the rule exists, false otherwise.</returns>
        private bool FirewallRuleExists(string ruleName)
        {
            var (exitCode, output) = RunNetShCommand($"advfirewall firewall show rule name=\"{ruleName}\"");
            return exitCode == 0 && output.Contains(ruleName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a firewall rule is enabled.
        /// Assumes the rule exists; returns false if the rule does not exist or is not enabled.
        /// </summary>
        /// <param name="ruleName">The name of the firewall rule to check.</param>
        /// <returns>True if the rule exists and is enabled, false otherwise.</returns>
        private bool IsFirewallRuleEnabled(string ruleName)
        {
            var (exitCode, output) = RunNetShCommand($"advfirewall firewall show rule name=\"{ruleName}\"");
            if (exitCode == 0 && output.Contains(ruleName, StringComparison.OrdinalIgnoreCase))
            {
                // Parse for Enabled line
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("Enabled:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(':');
                        if (parts.Length >= 2)
                        {
                            var value = parts[1].Trim();
                            return value.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a firewall rule exists and is enabled.
        /// </summary>
        /// <param name="ruleName">The name of the firewall rule to check.</param>
        /// <returns>True if the rule exists and is enabled, false otherwise.</returns>
        private bool IsFirewallRuleActive(string ruleName)
        {
            return FirewallRuleExists(ruleName) && IsFirewallRuleEnabled(ruleName);
        }

        /// <summary>
        /// Runs a netsh command and returns the exit code and combined output.
        /// </summary>
        /// <param name="arguments">The arguments to pass to netsh.</param>
        /// <returns>A tuple containing the exit code and the combined standard output and error.</returns>
        private static (int exitCode, string output) RunNetShCommand(string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return (process.ExitCode, output + error);
            }
        }

        #endregion
    }
}
