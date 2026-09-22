/// <summary>
    /// Defines a sequence of operations for state transitions.
    /// Replaced Task-based async with synchronous updates for Unity Main Thread safety.
    /// </summary>
    namespace Game.Core.StateMachine
{
    public interface ISequence
    {
        bool IsDone { get; }
        void Start();
        bool Update();
    }
}