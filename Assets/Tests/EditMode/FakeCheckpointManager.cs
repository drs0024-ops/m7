using Game.Gameplay.World;
using UnityEngine;

namespace Game.Tests
{
    public class FakeCheckpointManager : CheckpointManager
    {
        public new Vector3 GetSpawnPosition() => Vector3.zero;
    }
}   