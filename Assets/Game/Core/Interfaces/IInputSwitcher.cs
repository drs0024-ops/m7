


using Game.Core.Enums;

namespace Game.Core.Interfaces
{
    public interface IInputSwitcher
    {
        void SwitchToMenu();
        void SwitchToPlayer();
        void SwitchTo(GameState state);

    }
}