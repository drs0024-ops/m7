
using UnityEngine;

namespace Game.Core.Interfaces
{
    public interface IInputState
    {
        /// <summary>Current normalized player movement vector.</summary>
        Vector2 Movement { get; }

        /// <summary>Whether the crouch/down action is currently held.</summary>
        bool IsCrouching { get; }
    }
}   