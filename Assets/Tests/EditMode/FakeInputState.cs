using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Tests
{
    public class FakeInputState : IInputState
    {
        public Vector2 Movement { get; set; } = Vector2.zero;

        public bool IsInvisibilityPressed { get; set; } = false;

        public bool InvisibilityWasPressed { get; set; } = false;
    }
}   