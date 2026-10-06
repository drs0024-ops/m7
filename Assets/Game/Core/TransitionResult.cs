
namespace Game.Core
{
    /// <summary>
    /// Immutable result of a scene transition attempt.
    /// Carries success/failure information to callers without exceptions.
    /// </summary>
    public readonly struct TransitionResult
    {
        public readonly bool Success;
        public readonly bool Cancelled;
        public readonly string FailureReason;

        public TransitionResult(bool success, bool cancelled, string failureReason)
        {
            Success = success;
            Cancelled = cancelled;
            FailureReason = failureReason;
        }

        public static readonly TransitionResult Ok = new TransitionResult(true, false, null);
        public static readonly TransitionResult CancelledResult = new TransitionResult(false, true, "Transition cancelled.");

        public static TransitionResult Failure(string reason) => new TransitionResult(false, false, reason);
    }
}   