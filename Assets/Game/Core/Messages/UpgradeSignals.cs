using UnityEngine;
public class UpgradeSignals {}

namespace Game.Core.Messages
{

     #region Invisibility

    public readonly struct InvisibilityToggleRequested
    {
        public static InvisibilityToggleRequested Default => new();
    }

    public readonly struct BombPlaced
    {
        public readonly Vector2 Position;
        public BombPlaced(Vector2 position) => Position = position;
    }

    #endregion
    #region Pickup

    public readonly struct InvisibilityStateChanged
{
    public readonly float TargetFactor;
    public readonly bool IsTimedEffect;

    public InvisibilityStateChanged(float targetFactor, bool isTimedEffect = false)
    {
        TargetFactor = targetFactor;
        IsTimedEffect = isTimedEffect;
    }
}

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