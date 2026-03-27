using System;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Data transfer object for process information.
    /// </summary>
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsRunning { get; set; }
        public string? ExitCode { get; set; }
        public string? ErrorMessage { get; set; }

        public ProcessInfo(int id, string name)
        {
            Id = id;
            Name = name;
            IsRunning = false;
        }
    }
}
