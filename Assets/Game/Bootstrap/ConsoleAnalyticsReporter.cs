#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Editor/dev implementation. Logs to Console with [Analytics] prefix.
    /// Swap for a real backend by implementing IAnalyticsReporter.
    /// </summary>
    public class ConsoleAnalyticsReporter : IAnalyticsReporter
    {
        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            string props = FormatProperties(properties);
            Debug.Log($"[Analytics] {eventName}{props}");
        }

        public void TrackError(string context, string message)
        {
            Debug.LogWarning($"[Analytics][Error] {context}: {message}");
        }

        private static string FormatProperties(Dictionary<string, string> properties)
        {
            if (properties == null || properties.Count == 0) return "";
            var parts = new System.Text.StringBuilder(" {");
            bool first = true;
            foreach (var kvp in properties)
            {
                if (!first) parts.Append(", ");
                parts.Append($"\"{kvp.Key}\":\"{kvp.Value}\"");
                first = false;
            }
            parts.Append("}");
            return parts.ToString();
        }
    }
}
#endif   