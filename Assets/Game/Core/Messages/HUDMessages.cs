
using UnityEngine;
using Game.Core.Enums;

public class HUDMessages {}

namespace Game.Core.Messages
{
    public readonly struct GuidanceReceivedMessage
    {
        public readonly string Text;
        public readonly float Duration;
        public GuidanceReceivedMessage(string text, float duration)
        {
            Text = text;
            Duration = duration;
        }
    }

    public readonly struct GuidanceSilentMessage
    {
        public static GuidanceSilentMessage Default => new();
    }

    public readonly struct SignalStrengthChangedMessage
    {
        public readonly int Bars;
        public SignalStrengthChangedMessage(int bars) => Bars = bars;
    }

    public readonly struct OrbCollectedMessage
    {
        public readonly OrbType Type;
        public readonly int NewTotal;
        public OrbCollectedMessage(OrbType type, int newTotal)
        {
            Type = type;
            NewTotal = newTotal;
        }
    }

    public readonly struct PlayerDamagedMessage
    {
        public readonly float DamageAmount;
        public readonly bool IsNearDeath;
        public PlayerDamagedMessage(float damageAmount, bool isNearDeath)
        {
            DamageAmount = damageAmount;
            IsNearDeath = isNearDeath;
        }
    }

    public readonly struct CodeFragmentDisplayedMessage
    {
        public readonly string Fragment;
        public CodeFragmentDisplayedMessage(string fragment) => Fragment = fragment;
    }

    public readonly struct FlinchTriggeredMessage
    {
        public static FlinchTriggeredMessage Default => new();
    }

    public readonly struct GamePhaseChangedMessage
    {
        public readonly GamePhase NewPhase;
        public GamePhaseChangedMessage(GamePhase newPhase) => NewPhase = newPhase;
    }

    public readonly struct StaminaChanged
    {
        public readonly float Current;
        public readonly float Max;
        public StaminaChanged(float current, float max)
        {
            Current = current;
            Max = max;
        }
    }

    public readonly struct LevelTimerUpdated
    {
        public readonly float Time;
        public LevelTimerUpdated(float time) => Time = time;
    }

    public readonly struct ObjectiveProgress
    {
        public readonly int Collected;
        public readonly int Total;
        public ObjectiveProgress(int collected, int total)
        {
            Collected = collected;
            Total = total;
        }
    }

    public readonly struct OrbPickedUp
    {
        public readonly OrbType Type;
        public OrbPickedUp(OrbType type) => Type = type;
    }

    public struct HudUnfrozenMessage { }

    public struct GuidanceRetriggerMessage { }

    public struct ClimaxTriggered { }

    public struct ClimaxCompleted { }


    public struct ScreenSwapped { }

    public readonly struct CRTIntensityChanged
    {
        public readonly float Intensity;
        public CRTIntensityChanged(float intensity) => Intensity = intensity;
    }

    public struct CRTColorChanged
    {
        public Color ScreenTint;
        public Color BarTint;
    }

    public readonly struct CRTModeChanged
    {
        public readonly bool MenuOnly;
        public CRTModeChanged(bool menuOnly) => MenuOnly = menuOnly;
    }


}