using System;
using Game.Core.Messages;

namespace Game.Core.Interfaces
{
    /// <summary>Read-only invisibility charge state exposed to scene components.</summary>
    public interface IInvisibilityModel : IDisposable
    {
        /// <summary>Current charge in the inclusive range zero to one hundred.</summary>
        float ChargePercent { get; }

        /// <summary>Current charge-derived visibility tier.</summary>
        InvisibilityTier CurrentTier { get; }

        /// <summary>Whether an invisibility charger is actively charging.</summary>
        bool IsCharging { get; }
    }
}
