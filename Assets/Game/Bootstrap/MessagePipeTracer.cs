#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Editor/dev-build diagnostic. Subscribes to critical messages and
    /// stores them in a ring buffer for inspection.
    /// Uses GlobalMessagePipe intentionally — this is a diagnostic tool,
    /// not business logic. Not unit-tested.
    /// </summary>
    public class MessagePipeTracer : IStartable, IDisposable
    {
        private const int BufferSize = 128;

        private readonly string[] _buffer = new string[BufferSize];
        private int _head;
        private int _count;

        private readonly List<IDisposable> _subscriptions = new();

        void IStartable.Start()
        {
            Trace<PlayerSpawned>();
            Trace<PlayerDied>();
            Trace<PlayerDamaged>();
            Trace<CheckpointReached>();
            Trace<LevelCompleteSignal>();
            Trace<RestartLevelSignal>();
            Trace<LoadLevelSignal>();
            Trace<SceneLoaded>();
            Trace<SceneUnloaded>();
            Trace<SceneTransitionStarted>();
            Trace<SceneTransitionCompleted>();
            Trace<DoorActivated>();
            Trace<CurrencyCollected>();
            Trace<UpgradePickedUp>();
            Trace<UpgradeStateChanged>();
            Trace<BombPlaced>();
            Trace<InvisibilityToggleRequested>();
            Trace<InvisibilityStateChanged>();
        }

        private void Trace<T>() where T : struct
        {
            var sub = GlobalMessagePipe.GetSubscriber<T>();
            _subscriptions.Add(sub.Subscribe(msg =>
            {
                string entry = $"[{Time.time:F3}] {typeof(T).Name}: {msg}";
                _buffer[_head] = entry;
                _head = (_head + 1) % BufferSize;
                if (_count < BufferSize) _count++;
            }));
        }

        /// <summary>
        /// Returns the last N entries, oldest first.
        /// </summary>
        public string[] GetRecent(int count = 50)
        {
            int n = Mathf.Min(count, _count);
            var result = new string[n];
            int start = (_head - n + BufferSize) % BufferSize;
            for (int i = 0; i < n; i++)
                result[i] = _buffer[(start + i) % BufferSize];
            return result;
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, BufferSize);
            _head = 0;
            _count = 0;
        }

        public void Dispose()
        {
            foreach (var s in _subscriptions) s?.Dispose();
            _subscriptions.Clear();
        }
    }
}
#endif   