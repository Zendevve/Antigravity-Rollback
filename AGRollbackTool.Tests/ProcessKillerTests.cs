using System;
using System.Collections.Generic;
using AGRollbackTool.Services;

namespace AGRollbackTool.Tests
{
    /// <summary>
    /// Test class for ProcessKiller service.
    /// This demonstrates how to use the ProcessKiller and verify its behavior.
    /// In a real project, this would be replaced with proper unit tests using a testing framework.
    /// </summary>
    public static class ProcessKillerTests
    {
        /// <summary>
        /// Runs basic tests on the ProcessKiller service.
        /// </summary>
        public static void RunTests()
        {
            var killer = new ProcessKiller();

            Console.WriteLine("Testing ProcessKiller service...");
            Console.WriteLine();

            // Test individual process killing methods
            TestProcessKillResult(killer.KillAntigravity(), "Antigravity Process");
            TestProcessKillResult(killer.KillAntigravityHelper(), "Antigravity Helper Process");
            TestProcessKillResult(killer.KillAntigravityUtility(), "Antigravity Utility Process");
            TestProcessKillResult(killer.KillAntigravityCrashpad(), "Antigravity Crashpad Process");

            Console.WriteLine();

            // Test killing all processes
            Console.WriteLine("Testing KillAllAntigravityProcesses():");
            var allResults = killer.KillAllAntigravityProcesses();
            int count = 0;
            foreach (var result in allResults)
            {
                count++;
                Console.WriteLine($"{count}. {result.Name}:");
                Console.WriteLine($"   Process Count: {result.Id}");
                Console.WriteLine($"   Is Running After Kill: {result.IsRunning}");
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine($"   Error: {result.ErrorMessage}");
                }
                Console.WriteLine();
            }
            Console.WriteLine($"Total processes processed: {count}");
            Console.WriteLine();

            Console.WriteLine("ProcessKiller tests completed.");
        }

        /// <summary>
        /// Helper method to test and display process kill results.
        /// </summary>
        /// <param name="result">The process kill result to test.</param>
        /// <param name="description">Description of the process for display purposes.</param>
        private static void TestProcessKillResult(ProcessInfo result, string description)
        {
            Console.WriteLine($"{description}:");
            Console.WriteLine($"  Process Count Found: {result.Id}");
            Console.WriteLine($"  Is Running After Kill Attempt: {result.IsRunning}");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"  Error: {result.ErrorMessage}");
            }
            Console.WriteLine();
        }
    }
}
