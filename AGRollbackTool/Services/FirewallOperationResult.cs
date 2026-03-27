using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Data transfer object for reporting the result of firewall operations.
    /// </summary>
    public class FirewallOperationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the message describing the result of the operation.
        /// </>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets any errors that occurred during the operation.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets any warnings that occurred during the operation.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets information about the firewall rules affected.
        /// </summary>
        public List<string> AffectedRules { get; set; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FirewallOperationResult"/> class.
        /// </summary>
        public FirewallOperationResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirewallOperationResult"/> class.
        /// </summary>
        /// <param name="success">Whether the operation was successful.</param>
        /// <param name="message">A message describing the result.</param>
        public FirewallOperationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        /// <summary>
        /// Adds an error to the result.
        /// </summary>
        /// <param name="error">The error message to add.</param>
        public void AddError(string error)
        {
            Errors.Add(error);
        }

        /// <summary>
        /// Adds a warning to the result.
        /// </summary>
        /// <param name="warning">The warning message to add.</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// Adds an affected rule to the result.
        /// </summary>
        /// <param name="rule">The rule description to add.</param>
        public void AddAffectedRule(string rule)
        {
            AffectedRules.Add(rule);
        }

        /// <summary>
        /// Determines whether the operation had any errors.
        /// </summary>
        /// <returns>True if there are any errors, false otherwise.</returns>
        public bool HasErrors()
        {
            return Errors.Any();
        }

        /// <summary>
        /// Determines whether the operation had any warnings.
        /// </summary>
        /// <returns>True if there are any warnings, false otherwise.</returns>
        public bool HasWarnings()
        {
            return Warnings.Any();
        }
    }
}
