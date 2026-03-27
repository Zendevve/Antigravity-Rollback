using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Service for forcefully terminating Antigravity processes and verifying termination.
    /// </summary>
    public class ProcessKiller : IProcessKiller
    {
        /// <summary>
        /// Kills the antigravity.exe process and returns information about the operation.
        /// </summary>
        public ProcessInfo KillAntigravity()
        {
            return KillProcessByName("antigravity");
        }

        /// <summary>
        /// Kills the antigravity-helper.exe process and returns information about the operation.
        /// </summary>
        public ProcessInfo KillAntigravityHelper()
        {
            return KillProcessByName("antigravity-helper");
        }

        /// <summary>
        /// Kills the antigravity-utility.exe process and returns information about the operation.
        /// </summary>
        public ProcessInfo KillAntigravityUtility()
        {
            return KillProcessByName("antigravity-utility");
        }

        /// <summary>
        /// Kills the antigravity-crashpad.exe process and returns information about the operation.
        /// </summary>
        public ProcessInfo KillAntigravityCrashpad()
        {
            return KillProcessByName("antigravity-crashpad");
        }

        /// <summary>
        /// Kills all known Antigravity processes and returns a list of process information.
        /// </summary>
        public List<ProcessInfo> KillAllAntigravityProcesses()
        {
            var processNames = new List<string>
            {
                "antigravity",
                "antigravity-helper",
                "antigravity-utility",
                "antigravity-crashpad"
            };

            var results = new List<ProcessInfo>();
            foreach (var name in processNames)
            {
                results.Add(KillProcessByName(name));
            }

            return results;
        }

        /// <summary>
        /// Helper method to kill processes by name and return operation information.
        /// </summary>
        private ProcessInfo KillProcessByName(string processName)
        {
            var info = new ProcessInfo(0, processName);

            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    info.IsRunning = false;
                    return info;
                }

                info.Id = processes.Length;
                var errors = new List<string>();

                foreach (var process in processes)
                {
                    try
                    {
                        // Attempt graceful termination first
                        if (!process.HasExited)
                        {
                            if (!process.CloseMainWindow())
                            {
                                // If graceful termination fails, force terminate
                                process.Kill();
                            }

                            // Wait for exit with timeout
                            if (!process.WaitForExit(5000)) // 5 seconds timeout
                            {
                                // Force kill if still not exited
                                process.Kill();
                                process.WaitForExit();
                            }
                        }
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access is denied
                    {
                        errors.Add($"Access denied terminating process {process.Id}. Run as administrator.");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("has exited", StringComparison.OrdinalIgnoreCase))
                    {
                        // Process already exited, this is fine
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error terminating process {process.Id}: {ex.Message}");
                    }
                }

                // Verify termination
                var remainingProcesses = Process.GetProcessesByName(processName);
                info.IsRunning = remainingProcesses.Length > 0;

                if (errors.Any())
                {
                    info.ErrorMessage = string.Join("; ", errors);
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access is denied
            {
                info.ErrorMessage = $"Access denied. Run as administrator to terminate {processName} processes.";
            }
            catch (Exception ex)
            {
                info.ErrorMessage = $"Unexpected error: {ex.Message}";
            }

            return info;
        }
    }
}
