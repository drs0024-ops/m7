using UnityEngine;
using Game.Core.Interfaces;

namespace Game.Tests
{
    public class FakeCheckpointProvider : ICheckpointProvider
    {
        public GameObject gameObject { get; set; }
        public Vector3 SpawnPosition = Vector3.zero;

        public Vector3 GetSpawnPosition() => SpawnPosition;
        public void ForceSpawnAt(Vector3 position) => SpawnPosition = position;
    }
}
 