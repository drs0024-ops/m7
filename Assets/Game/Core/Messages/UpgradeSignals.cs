using UnityEngine;
public class UpgradeSignals {}

namespace Game.Core.Messages
{

     #region Invisibility

    /// <summary>Visibility tier derived from current invisibility charge.</summary>
    public enum InvisibilityTier
    {
        /// <summary>Charge is below the translucent threshold.</summary>
        Visible = 0,
        /// <summary>Charge is at least the first threshold.</summary>
        Translucent = 1,
        /// <summary>Charge is at least the second threshold.</summary>
        CloakedSlowed = 2,
        /// <summary>Charge is at least the final threshold.</summary>
        Cloaked = 3
    }

    /// <summary>Publishes the current invisibility charge percentage.</summary>
    public readonly struct InvisibilityChargeChanged
    {
        /// <summary>Current charge in the inclusive range zero to one hundred.</summary>
        public float ChargePercent { get; }

        /// <summary>Creates a charge update.</summary>
        public InvisibilityChargeChanged(float chargePercent) => ChargePercent = chargePercent;
    }

    /// <summary>Signals that an invisibility broadcast has begun.</summary>
    public readonly struct InvisibilityBroadcastStarted
    {
        /// <summary>World position of the charger.</summary>
        public Vector2 Position { get; }
        /// <summary>Current broadcast radius.</summary>
        public float Radius { get; }

        /// <summary>Creates a broadcast-start notification.</summary>
        public InvisibilityBroadcastStarted(Vector2 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        /// <summary>Creates a broadcast-start notification from scalar coordinates.</summary>
        public InvisibilityBroadcastStarted(float x, float y, float radius)
            : this(new Vector2(x, y), radius) { }
    }

    /// <summary>Signals that an invisibility broadcast has ended.</summary>
    public readonly struct InvisibilityBroadcastStopped
    {
        /// <summary>World position of the charger that stopped broadcasting.</summary>
        public Vector2 Position { get; }

        /// <summary>Creates a broadcast-stop notification.</summary>
        public InvisibilityBroadcastStopped(Vector2 position) => Position = position;

        /// <summary>Creates a broadcast-stop notification from scalar coordinates.</summary>
        public InvisibilityBroadcastStopped(float x, float y) : this(new Vector2(x, y)) { }
    }

    /// <summary>Buffered snapshot of the latest active charger broadcast.</summary>
    public readonly struct InvisibilityBroadcastStateChanged
    {
        /// <summary>Whether a charger is currently broadcasting.</summary>
        public bool IsActive { get; }
        /// <summary>Current charger position.</summary>
        public Vector2 Position { get; }
        /// <summary>Current broadcast radius.</summary>
        public float Radius { get; }

        /// <summary>Creates a broadcast-state snapshot.</summary>
        public InvisibilityBroadcastStateChanged(bool isActive, Vector2 position, float radius)
        {
            IsActive = isActive;
            Position = position;
            Radius = radius;
        }

        /// <summary>Creates a broadcast-state snapshot from scalar coordinates.</summary>
        public InvisibilityBroadcastStateChanged(bool isActive, float x, float y, float radius)
            : this(isActive, new Vector2(x, y), radius) { }
    }

    /// <summary>Command from a charger to begin, update, or stop charging.</summary>
    public readonly struct InvisibilityChargingChanged
    {
        /// <summary>Whether the player is on an unlocked charger.</summary>
        public bool IsCharging { get; }
        /// <summary>Charger instance identifier.</summary>
        public int StationId { get; }
        /// <summary>Charge rate in percentage points per second.</summary>
        public float ChargeRate { get; }
        /// <summary>Maximum total charge this station allows.</summary>
        public float MaxChargePercent { get; }
        /// <summary>Maximum dwell time in seconds; zero means unlimited.</summary>
        public float MaxChargeTimeSeconds { get; }
        /// <summary>Station world position.</summary>
        public Vector2 Position { get; }
        /// <summary>Enemy-attraction radius.</summary>
        public float BroadcastRadius { get; }

        /// <summary>Creates a charging-state command.</summary>
        public InvisibilityChargingChanged(bool isCharging, int stationId, float chargeRate,
            float maxChargePercent, float maxChargeTimeSeconds, Vector2 position, float broadcastRadius)
        {
            IsCharging = isCharging;
            StationId = stationId;
            ChargeRate = chargeRate;
            MaxChargePercent = maxChargePercent;
            MaxChargeTimeSeconds = maxChargeTimeSeconds;
            Position = position;
            BroadcastRadius = broadcastRadius;
        }
    }

    /// <summary>Requests charge loss after accepted damage to the player.</summary>
    public readonly struct InvisibilityInterrupted
    {
        /// <summary>Charge percentage points to drain.</summary>
        public float DrainPercent { get; }

        /// <summary>Creates an interruption.</summary>
        public InvisibilityInterrupted(float drainPercent) => DrainPercent = drainPercent;
    }

    /// <summary>Publishes an invisibility-tier boundary crossing.</summary>
    public readonly struct InvisibilityTierChanged
    {
        /// <summary>New charge-derived tier.</summary>
        public InvisibilityTier NewTier { get; }

        /// <summary>Creates a tier change.</summary>
        public InvisibilityTierChanged(InvisibilityTier newTier) => NewTier = newTier;
    }

    /// <summary>Requests that one enemy investigate a charger broadcast.</summary>
    public readonly struct EnemyInvestigationRequested
    {
        /// <summary>Instance ID of the enemy receiving this request.</summary>
        public int EnemyInstanceId { get; }
        /// <summary>World position to investigate.</summary>
        public Vector2 Position { get; }

        /// <summary>Creates an investigation request.</summary>
        public EnemyInvestigationRequested(int enemyInstanceId, Vector2 position)
        {
            EnemyInstanceId = enemyInstanceId;
            Position = position;
        }
    }

    /// <summary>Cancels investigation for one enemy.</summary>
    public readonly struct EnemyInvestigationCancelled
    {
        /// <summary>Instance ID of the enemy whose investigation is cancelled.</summary>
        public int EnemyInstanceId { get; }

        /// <summary>Creates an investigation-cancel request.</summary>
        public EnemyInvestigationCancelled(int enemyInstanceId) => EnemyInstanceId = enemyInstanceId;
    }

    public readonly struct BombPlaced
    {
        public readonly Vector2 Position;
        public BombPlaced(Vector2 position) => Position = position;
    }

    #endregion
    #region Pickup

    public readonly struct CloakToggleRequested
    {
        public readonly bool Cloak;
        public CloakToggleRequested(bool cloak) => Cloak = cloak;
    }

    public readonly struct UpgradePickedUp
    {
        public string UpgradeID { get; }
        public UpgradePickedUp(string upgradeID) => UpgradeID = upgradeID;
    }

    #endregion

    #region State

    public readonly struct UpgradeActivated
    {
        public string UpgradeId { get; }
        public UpgradeActivated(string upgradeId) => UpgradeId = upgradeId;
    }

    public readonly struct UpgradeUnlocked
    {
        public readonly string UpgradeId;
        public UpgradeUnlocked(string upgradeId) => UpgradeId = upgradeId;
    }

    public readonly struct UpgradeStateChanged
    {
        public string UpgradeID { get; }
        public bool IsActive { get; }
        public float Duration { get; }
        public UpgradeStateChanged(string upgradeID, bool isActive, float duration)
        {
            UpgradeID = upgradeID;
            IsActive = isActive;
            Duration = duration;
        }
    }

    #endregion

    #region Apply / Expire

    public readonly struct UpgradeApplied
    {
        public string UpgradeId { get; }
        public float Duration { get; }
        public UpgradeApplied(string upgradeId, float duration)
        {
            UpgradeId = upgradeId;
            Duration = duration;
        }
    }

    public readonly struct UpgradeExpired
    {
        public string UpgradeId { get; }
        public UpgradeExpired(string upgradeId)
        {
            UpgradeId = upgradeId;
        }
    }

    #endregion
}