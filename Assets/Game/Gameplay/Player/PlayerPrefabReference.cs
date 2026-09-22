
using UnityEngine;

namespace Game.Gameplay.Player
{
    public class PlayerPrefabReference
    {
        public GameObject Value { get; }
        public PlayerPrefabReference(GameObject value) => Value = value;
    }
}