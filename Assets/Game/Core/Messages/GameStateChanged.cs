using Game.Core.Enums;
namespace Game.Core.Messages
{

    public readonly struct GameStateChanged
    {
        public GameState State { get; }
        public GameStateChanged(GameState state) => State = state;
    }
}