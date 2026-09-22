using Game.Core.Enums;
using UnityEngine;

namespace Game.Core.Messages 
{
    public class PlayerSignals
    {

    }
    
    #region Spawn

    /// <summary>
    ///  _spawnPublisher.Publish(default);
    /// </summary> <summary>
    /// 
    /// </summary>
    public readonly struct PlayerSpawnRequest
    {
        public bool SpawnAtDoor { get; }
        public DoorToSpawnAt Door { get; }
        public bool FromRight { get; }
        public bool ForceRespawn { get; }
        public bool UseCustomPosition { get; }
        public Vector3 CustomPosition { get; }

        public PlayerSpawnRequest(DoorToSpawnAt door, bool fromRight, bool forceRespawn = false)
        {
            SpawnAtDoor = true;
            Door = door;
            FromRight = fromRight;
            ForceRespawn = forceRespawn;
            UseCustomPosition = false;
            CustomPosition = Vector3.zero;
        }

        public PlayerSpawnRequest(Vector3 customPosition, bool forceRespawn = false)
        {
            SpawnAtDoor = false;
            Door = default;
            FromRight = false;
            ForceRespawn = forceRespawn;
            UseCustomPosition = true;
            CustomPosition = customPosition;
        }
    }   
    public readonly struct PlayerSpawned
    {
        public readonly Transform Player;
        public PlayerSpawned(Transform player) => Player = player;
    }

    #endregion

    #region State

    public readonly struct PlayerStateChanged
    {
        public PlayerState State { get; }
        public PlayerStateChanged(PlayerState state) => State = state;
    }

    public readonly struct PlayerFacingChanged
    {
        public bool IsFacingRight { get; }
        public PlayerFacingChanged(bool isFacingRight) => IsFacingRight = isFacingRight;
    }

    #endregion

    #region Movement

    public readonly struct PlayerLanded
    {
        public static PlayerLanded Default => new();
    }

    public readonly struct PlayerJumped
    {
        public int JumpCount { get; }
        public PlayerJumped(int jumpCount) => JumpCount = jumpCount;
    }

    public readonly struct PlayerDoubleJumped
    {
        public static PlayerDoubleJumped Default => new();
    }

    #endregion

    #region Damage

    public readonly struct PlayerDamaged
    {
        public readonly float DamageAmount;
        public readonly Vector3 HitDirection;
        public readonly float InputAxisX;
        public readonly bool IsNearDeath;

        public PlayerDamaged(float damageAmount, Vector3 hitDirection, float inputAxisX, bool isNearDeath = false)
        {
            DamageAmount = damageAmount;
            HitDirection = hitDirection;
            InputAxisX = inputAxisX;
            IsNearDeath = isNearDeath;
        }
    }

    public readonly struct PlayerDied
    {
        public readonly Vector2 DeathPosition;

        public PlayerDied(Vector2 deathPosition)
        {
            DeathPosition = deathPosition;
        }
    }
    #endregion

    #region Checkpoint

    public readonly struct CheckpointReached
    {
        public readonly string CheckpointId;
        public readonly Vector3 Position;

        public CheckpointReached(string checkpointId, Vector3 position)
        {
            CheckpointId = checkpointId;
            Position = position;
        }
    }

    #endregion

    public readonly struct BouncePlatformHit
    {
        public readonly UnityEngine.Transform Player;
        public BouncePlatformHit(UnityEngine.Transform player) => Player = player;
    }
   
}