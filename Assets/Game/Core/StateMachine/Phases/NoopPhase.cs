
/// <summary>
    /// A no-op sequence for immediate transitions.
    /// </summary>
namespace Game.Core.StateMachine.Phases
{
    public class NoopPhase : ISequence
    {
        public bool IsDone => true;
        public void Start() { }
        public bool Update() => true;
    }
}