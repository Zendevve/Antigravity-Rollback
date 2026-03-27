using System.Collections.Generic;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Data transfer object for reporting the result of settings injection.
    /// </summary>
    public class SettingsChangeResult
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// A message describing the outcome or any errors encountered.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the update.mode setting was changed.
        /// </summary>
        public bool UpdateModeChanged { get; set; }

        /// <summary>
        /// The old value of update.mode before the change, if it existed.
        /// </summary>
        public string? OldUpdateMode { get; set; }

        /// <summary>
        /// The new value of update.mode after the change.
        /// </summary>
        public string NewUpdateMode { get; set; } = "none";

        /// <summary>
        /// Indicates whether the update.showReleaseNotes setting was changed.
        /// </summary>
        public bool ShowReleaseNotesChanged { get; set; }

        /// <summary>
        /// The old value of update.showReleaseNotes before the change, if it existed.
        /// </summary>
        public bool? OldShowReleaseNotes { get; set; }

        /// <summary>
        /// The new value of update.showReleaseNotes after the change.
        /// </summary>
        public bool NewShowReleaseNotes { get; set; } = false;

        /// <summary>
        /// Any errors encountered during the operation.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}
