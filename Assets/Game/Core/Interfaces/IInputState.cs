
using UnityEngine;

namespace Game.Core.Interfaces
{
    public interface IInputState
    {
        Vector2 Movement { get; }
        bool IsInvisibilityPressed { get; }
        bool InvisibilityWasPressed { get; }
    }
}   