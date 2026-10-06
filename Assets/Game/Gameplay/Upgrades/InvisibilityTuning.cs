using System;

namespace Game.Gameplay.Upgrades
{
    /// <summary>Immutable global charge and visibility-tier tuning.</summary>
    public sealed class InvisibilityTuning
    {
        /// <summary>Default charge rate in percentage points per second.</summary>
        public float ChargeRate { get; }
        /// <summary>Passive charge decay in percentage points per second.</summary>
        public float DecayRate { get; }
        /// <summary>Charge-rate multiplier while crouching.</summary>
        public float CrouchChargeMultiplier { get; }
        /// <summary>Charge percentage points drained by one accepted player hit.</summary>
        public float InterruptionDrainPercent { get; }
        /// <summary>Threshold at which the Translucent tier begins.</summary>
        public float TranslucentThreshold { get; }
        /// <summary>Threshold at which the CloakedSlowed tier begins.</summary>
        public float CloakedSlowedThreshold { get; }
        /// <summary>Threshold at which the Cloaked tier begins.</summary>
        public float CloakedThreshold { get; }

        /// <summary>Creates validated global invisibility tuning.</summary>
        public InvisibilityTuning(float chargeRate, float decayRate, float crouchChargeMultiplier,
            float interruptionDrainPercent,
            float translucentThreshold, float cloakedSlowedThreshold, float cloakedThreshold)
        {
            if (!IsFinite(chargeRate)) throw new ArgumentOutOfRangeException(nameof(chargeRate));
            if (!IsFinite(decayRate)) throw new ArgumentOutOfRangeException(nameof(decayRate));
            if (!IsFinite(crouchChargeMultiplier)) throw new ArgumentOutOfRangeException(nameof(crouchChargeMultiplier));
            if (!IsFinite(interruptionDrainPercent)) throw new ArgumentOutOfRangeException(nameof(interruptionDrainPercent));
            if (!IsFinite(translucentThreshold) || !IsFinite(cloakedSlowedThreshold) || !IsFinite(cloakedThreshold))
                throw new ArgumentOutOfRangeException(nameof(translucentThreshold), "Tier thresholds must be finite.");
            if (chargeRate < 0f) throw new ArgumentOutOfRangeException(nameof(chargeRate));
            if (decayRate < 0f) throw new ArgumentOutOfRangeException(nameof(decayRate));
            if (crouchChargeMultiplier < 0f) throw new ArgumentOutOfRangeException(nameof(crouchChargeMultiplier));
            if (interruptionDrainPercent < 0f) throw new ArgumentOutOfRangeException(nameof(interruptionDrainPercent));
            if (translucentThreshold < 0f || translucentThreshold >= cloakedSlowedThreshold
                || cloakedSlowedThreshold >= cloakedThreshold || cloakedThreshold > 100f)
                throw new ArgumentOutOfRangeException(nameof(translucentThreshold),
                    "Tier thresholds must satisfy 0 <= translucent < cloaked-slowed < cloaked <= 100.");

            ChargeRate = chargeRate;
            DecayRate = decayRate;
            CrouchChargeMultiplier = crouchChargeMultiplier;
            InterruptionDrainPercent = interruptionDrainPercent;
            TranslucentThreshold = translucentThreshold;
            CloakedSlowedThreshold = cloakedSlowedThreshold;
            CloakedThreshold = cloakedThreshold;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}