// Tracks current objective and achievement progress. Silent — no HUD feedback.
using System;
using System.Collections.Generic;
using Game.Core.Data;
using Game.Core.Messages;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Achievements
{
    public class ObjectiveTracker : IStartable, IDisposable
    {
        // NOT readonly — deferred pattern  
        private IPublisher<AchievementUnlocked> _achievementUnlockedPublisher;
        private IPublisher<NextAchievementRevealed> _nextAchievementRevealedPublisher;
        private readonly List<IDisposable> _subscriptions = new();

        private Objective _currentObjective;
        private bool _disposed;

        public bool IsComplete => _currentObjective == null || _currentObjective.IsComplete;
        public Achievement CurrentAchievement => _currentObjective?.Current ?? default;   
        public int Progress => _currentObjective?.currentAchievementIndex ?? 0;
        public int Total => _currentObjective?.achievements?.Count ?? 0;

        [Inject]
        public ObjectiveTracker() { }

        void IStartable.Start()
        {
            _achievementUnlockedPublisher = GlobalMessagePipe.GetPublisher<AchievementUnlocked>();
            _nextAchievementRevealedPublisher = GlobalMessagePipe.GetPublisher<NextAchievementRevealed>();
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }
    }
}   