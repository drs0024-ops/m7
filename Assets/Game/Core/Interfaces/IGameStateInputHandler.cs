/// <summary>
/// Contract for the Input Handler responsible for translating input events into Game State requests.
/// </summary>
namespace Game.Core.Interfaces
{
    public interface IGameStateInputHandler
    {
        // Currently acts as a marker interface to ensure the component is registered.
        // Future input methods (e.g., ForcePause(), SimulateEscape()) can be added here.
    } 
}  