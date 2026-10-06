using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Gameplay.Upgrades
{
    /// <summary>Unity-backed frame clock used by the pure charge model.</summary>
    public sealed class UnityFrameTimeProvider : IFrameTimeProvider
    {
        /// <summary>Delta time for the current Unity frame.</summary>
        public float DeltaTime => Time.deltaTime;
    }
}