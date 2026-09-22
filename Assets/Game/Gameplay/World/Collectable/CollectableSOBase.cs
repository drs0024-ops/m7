using UnityEngine;

namespace Game.Gameplay.World
{
    public abstract class CollectableSOBase : ScriptableObject
    {
        [Header("Collection Effects")]
        public AudioClip CollectionClip;

        public abstract void Collect(GameObject objectThatCollected);
    }
}   