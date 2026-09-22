#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
using System.Collections.Generic;
using Game.Core.Interfaces;

namespace Game.Bootstrap
{
    /// <summary>
    /// Release build no-op. Zero cost. Swap for Firebase/Sentry later.
    /// </summary>
    public class NoOpAnalyticsReporter : IAnalyticsReporter
    {
        public void TrackEvent(string eventName, Dictionary<string, string> properties) { }
        public void TrackError(string context, string message) { }
    }
}
#endif   