using System;

namespace AGRollbackTool.Models
{
    public enum Phase
    {
        NotStarted,
        Initializing,
        CreatingBackup,
        ApplyingUpdate,
        VerifyingUpdate,
        RollingBack,
        Completed,
        Failed
    }

    public class RollbackSession
    {
        public Phase CurrentPhase { get; private set; } = Phase.NotStarted;
        public DateTime StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool IsCompleted => CurrentPhase == Phase.Completed || CurrentPhase == Phase.Failed;
        public bool IsFailed => CurrentPhase == Phase.Failed;

        public RollbackSession()
        {
            StartedAt = DateTime.UtcNow;
        }

        public void AdvanceTo(Phase nextPhase)
        {
            CurrentPhase = nextPhase;
        }

        public void SetError(string errorMessage)
        {
            ErrorMessage = errorMessage;
            CurrentPhase = Phase.Failed;
            EndedAt = DateTime.UtcNow;
        }

        public void Complete()
        {
            CurrentPhase = Phase.Completed;
            EndedAt = DateTime.UtcNow;
        }
    }
}
