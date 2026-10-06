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

        public string LastCheckpoint { get; private set; } = "None observed";
        public string LastSaveStatus { get; private set; } = "Waiting for save request";
        public string LastSaveEvent { get; private set; } = "None";

        void IStartable.Start()
        {
            Trace<PlayerSpawned>();
            Trace<PlayerDied>();
            Trace<PlayerDamaged>();
            Trace<CheckpointReached>();
            Trace<SaveRequest>();
            Trace<SaveCompleted>();
            Trace<SaveFailed>();
            Trace<LoadCompleted>();
            Trace<LoadFailed>();
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
            Trace<InvisibilityChargeChanged>();
            Trace<InvisibilityBroadcastStarted>();
            Trace<InvisibilityBroadcastStopped>();
            Trace<InvisibilityInterrupted>();
            Trace<InvisibilityTierChanged>();
        }

        private void Trace<T>() where T : struct
        {
            var sub = GlobalMessagePipe.GetSubscriber<T>();
            _subscriptions.Add(sub.Subscribe(msg =>
            {
                string details = FormatMessage(msg);
                UpdateSaveState(msg);
                AddEntry($"[{Time.time:F3}] {typeof(T).Name}: {details}");
            }));
        }

        private static string FormatMessage<T>(T message) where T : struct
        {
            if (message is CheckpointReached checkpoint)
                return $"id={checkpoint.CheckpointId}, position={checkpoint.Position}";
            if (message is SaveRequest request)
                return $"source={request.Source}, immediate={request.Immediate}";
            if (message is SaveFailed saveFailed)
                return saveFailed.Error;
            if (message is LoadFailed loadFailed)
                return loadFailed.Error;

            return message.ToString();
        }

        private void UpdateSaveState<T>(T message) where T : struct
        {
            switch (message)
            {
                case CheckpointReached checkpoint:
                    LastCheckpoint = $"{checkpoint.CheckpointId} at {checkpoint.Position}";
                    break;
                case SaveRequest request:
                    LastSaveStatus = request.Immediate ? "Saving immediately" : "Save queued (debounced)";
                    LastSaveEvent = $"Requested by {request.Source} at {Time.time:F3}s";
                    break;
                case SaveCompleted:
                    LastSaveStatus = "Saved successfully";
                    LastSaveEvent = $"Completed at {Time.time:F3}s";
                    break;
                case SaveFailed saveFailed:
                    LastSaveStatus = "Save failed";
                    LastSaveEvent = saveFailed.Error;
                    break;
            }
        }

        private void AddEntry(string entry)
        {
            _buffer[_head] = entry;
            _head = (_head + 1) % BufferSize;
            if (_count < BufferSize) _count++;
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
            LastCheckpoint = "None observed";
            LastSaveStatus = "Waiting for save request";
            LastSaveEvent = "None";
        }

        public void Dispose()
        {
            foreach (var s in _subscriptions) s?.Dispose();
            _subscriptions.Clear();
        }
    }
}
#endif   