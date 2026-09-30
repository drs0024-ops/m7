using System;
using UnityEngine;

[Serializable]
public class CharacterSceneConfig
{
    [Header("Spawn Settings")]
    public bool SpawnPlayerOnStart = true;
    public bool EnableEnemies = true;

    [Tooltip("Used if no checkpoint is found. If ForcedSpawnPosition is set, this is ignored.")]
    public Vector3 DefaultStartPosition = new Vector3(0, 0, 0);

    [Tooltip("If set (non-zero), this forces the spawn position and ignores checkpoints.")]
    public Vector3? ForcedSpawnPosition = null;

    [Header("Door Spawn")]
    [Tooltip("Offset from door position when spawning at a door.")]
    public float DoorSpawnOffset = 3f;
}   