using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Tests
{
    public class FakePlayerFactory : IPlayerFactory
    {
        public PlayerStateDriverShell CreatePlayer(Vector3 position, Quaternion rotation) => null;
    }
}   