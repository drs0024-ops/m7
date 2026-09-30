using System;
using Game.Core.Data;
using Game.Core.Messages;
using MessagePipe;
using VContainer.Unity;

namespace Game.Gameplay.Achievements
{
    /// <summary>
    /// Tracks current objective and achievement progress. Silent — no HUD feedback.
    /// </summary>
    public class ObjectiveTracker : IStartable, IDisposable
    {
        #region Dependencies

        private readonly IPublisher<AchievementUnlocked> _achievementUnlockedPublisher;
        private readonly IPublisher<NextAchievementRevealed> _nextAchievementRevealedPublisher;

        #endregion

        #region State

        private Objective _currentObjective;
        private bool _disposed;

        #endregion

        #region Public API

        public bool IsComplete => _currentObjective == null || _currentObjective.IsComplete;
        public Achievement CurrentAchievement => _currentObjective?.Current ?? default;
        public int Progress => _currentObjective?.currentAchievementIndex ?? 0;
        public int Total => _currentObjective?.achievements?.Count ?? 0;

        public ObjectiveTracker(
            IPublisher<AchievementUnlocked> achievementUnlockedPublisher,
            IPublisher<NextAchievementRevealed> nextAchievementRevealedPublisher)
        {
            _achievementUnlockedPublisher = achievementUnlockedPublisher;
            _nextAchievementRevealedPublisher = nextAchievementRevealedPublisher;
        }

        public void SetObjective(Objective objective)
        {
            if (_disposed) return;
            _currentObjective = objective;
        }

        public void UnlockCurrent()
        {
            if (_disposed) return;
            if (_currentObjective == null || _currentObjective.IsComplete) return;

            var current = _currentObjective.Current;
            _achievementUnlockedPublisher.Publish(new AchievementUnlocked(current));

            _currentObjective.currentAchievementIndex++;

            if (!_currentObjective.IsComplete)
                _nextAchievementRevealedPublisher.Publish(new NextAchievementRevealed(_currentObjective.Current));
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            // No-op — publishers are already injected.
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _disposed = true;
        }

        #endregion
    }
}   