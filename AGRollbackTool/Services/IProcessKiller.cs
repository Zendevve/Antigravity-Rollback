using System.Collections.Generic;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Interface for the ProcessKiller service.
    /// </summary>
    public interface IProcessKiller
    {
        /// <summary>
        /// Kills the antigravity.exe process and returns information about the operation.
        /// </summary>
        ProcessInfo KillAntigravity();

        /// <summary>
        /// Kills the antigravity-helper.exe process and returns information about the operation.
        /// </summary>
        ProcessInfo KillAntigravityHelper();

        /// <summary>
        /// Kills the antigravity-utility.exe process and returns information about the operation.
        /// </summary>
        ProcessInfo KillAntigravityUtility();

        /// <summary>
        /// Kills the antigravity-crashpad.exe process and returns information about the operation.
        /// </summary>
        ProcessInfo KillAntigravityCrashpad();

        /// <summary>
        /// Kills all known Antigravity processes and returns a list of process information.
        /// </summary>
        List<ProcessInfo> KillAllAntigravityProcesses();
    }
}
