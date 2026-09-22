#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Core.Enums;

namespace Game.Bootstrap
{
    /// <summary>
    /// Static holder for real-time game flow debug data.
    /// Written by GameFlowDebugCollector, read by GameFlowDebugWindow.
    /// Zero runtime cost — excluded from builds via #if UNITY_EDITOR.
    /// </summary>
    public static class GameFlowDebugInfo
    {
        public static GameState CurrentState = GameState.MainMenu;
        public static GamePhase CurrentPhase = GamePhase.None;
        public static string CurrentInputMap = "Menu";
        public static bool IsTransitioning = false;

        public static readonly List<string> TransitionHistory = new(10);
        private const int MaxHistory = 10;

        public static void RecordTransition(GameState from, GameState to)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss.fff}] {from} → {to}";
            TransitionHistory.Add(entry);
            if (TransitionHistory.Count > MaxHistory)
                TransitionHistory.RemoveAt(0);
        }

        public static void Reset()
        {
            CurrentState = GameState.MainMenu;
            CurrentPhase = GamePhase.None;
            CurrentInputMap = "Menu";
            IsTransitioning = false;
            TransitionHistory.Clear();
        }
    }
}
#endif   