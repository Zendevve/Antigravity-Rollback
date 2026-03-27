using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for managing network blackout of Antigravity applications via Windows Firewall.
    /// </summary>
    public interface INetworkBlackoutService
    {
        /// <summary>
        /// Blocks outbound network access for antigravity.exe and updater.exe.
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the operation requires elevated privileges.</exception>
        void BlockAntigravityNetworkAccess();

        /// <summary>
        /// Unblocks outbound network access for antigravity.exe and updater.exe by removing the firewall rules.
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the operation requires elevated privileges.</exception>
        void UnblockAntigravityNetworkAccess();

        /// <summary>
        /// Removes a specific firewall rule by name.
        /// </summary>
        /// <param name="ruleName">The name of the firewall rule to remove.</param>
        /// <returns>Result of the firewall rule removal operation.</returns>
        FirewallOperationResult RemoveFirewallRule(string ruleName);

        /// <summary>
        /// Verifies if a specific firewall rule exists and is active.
        /// </summary>
        /// <param name="ruleName">The name of the firewall rule to verify.</param>
        /// <returns>True if the rule exists and is enabled, false otherwise.</returns>
        bool VerifyFirewallRule(string ruleName);

        /// <summary>
        /// Checks if the firewall rules for blocking Antigravity network access are active.
        /// </summary>
        /// <returns>True if both rules exist and are enabled, false otherwise.</returns>
        bool AreFirewallRulesActive();
    }
}
