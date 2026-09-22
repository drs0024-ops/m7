using UnityEngine;

namespace Game.Gameplay.World
{
    public abstract class DestructableSOBase : ScriptableObject
    {
        [Header("Destruction Effects")]
        public AudioClip DestroyClip;

        public abstract void Destroy(GameObject objectThatDestroyed);
    }
}   