using System;
using UnityEngine;
using Game.Core.Interfaces;

[Serializable]
public class CharacterSceneConfig : ICharacterSceneConfig
{
    [Header("Spawn Settings")]
    public bool SpawnPlayerOnStart { get; set; } = true;
    public bool EnableEnemies { get; set; } = true;

    [Tooltip("Used if no checkpoint is found. If ForcedSpawnPosition is set, this is ignored.")]
    public Vector3 DefaultStartPosition { get; set; } = new Vector3(0, 0, 0);

    [Tooltip("If set (non-zero), this forces the spawn position and ignores checkpoints.")]
    public Vector3? ForcedSpawnPosition { get; set; } = null;

    [Header("Door Spawn")]
    [Tooltip("Offset from door position when spawning at a door.")]
    public float DoorSpawnOffset { get; set; } = 3f;
}   