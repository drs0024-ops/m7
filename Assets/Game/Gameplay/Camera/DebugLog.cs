// Core/DebugLog.cs
using System.Diagnostics;

namespace Game.Core
{
    public static class DebugLog
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string tag, object message)
            => UnityEngine.Debug.Log($"[{tag}] {message}");

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warn(string tag, object message)
            => UnityEngine.Debug.LogWarning($"[{tag}] {message}");

        public static void Error(string tag, object message)
            => UnityEngine.Debug.LogError($"[{tag}] {message}");
    }
}   