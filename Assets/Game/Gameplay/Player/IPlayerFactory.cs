using UnityEngine;

namespace Game.Gameplay.Player
{
    public interface IPlayerFactory
    {
        PlayerStateDriverShell CreatePlayer(Vector3 position, Quaternion rotation);
    }
}