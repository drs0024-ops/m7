using System.Collections.Generic;

namespace Game.Core.Interfaces
{
    /// <summary>
    /// Abstraction for analytics/telemetry. Implement with Console (dev) or
    /// Firebase/Sentry/Amplitude (production) without changing the orchestrator.
    /// </summary>
    public interface IAnalyticsReporter
    {
        void TrackEvent(string eventName, Dictionary<string, string> properties);
        void TrackError(string context, string message);
    }
}   