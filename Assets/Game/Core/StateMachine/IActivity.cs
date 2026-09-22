

/// <summary>
/// Defines an activity (e.g., VFX, Audio, Logic) that can be activated/deactivated during transitions.
/// </summary>
namespace Game.Core.StateMachine
{
    public interface IActivity
    {
        void Activate();
        void Deactivate();
    }
}