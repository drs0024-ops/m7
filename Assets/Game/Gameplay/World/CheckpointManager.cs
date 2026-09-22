using System;
using System.Collections.Generic;
using Game.Core.Data;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    public class CheckpointManager : MonoBehaviour, IStartable, ISaveable, IDisposable
    {
        // readonly — scene-placed component, injected during scope build
        // (SetProvider has already fired by this point)
        private readonly ISubscriber<CheckpointReached> _checkpointReachedSub;
        private readonly IPublisher<SaveRequest> _saveRequestPublisher;
        private readonly IPublisher<PlayerSpawnRequest> _playerSpawnRequestPublisher;

        private readonly List<IDisposable> _disposables = new();
        private readonly ISaveableRegistry _registry;

        [SerializeField] private ICheckpoint[] _checkpoints;
        [SerializeField] private Transform _startSpawnPoint;

        private Vector3 _lastCheckpointPosition;
        private string _lastCheckpointId;
        private bool _disposed;

        public string SaveId => throw new NotImplementedException();

        [Inject]
        public CheckpointManager(ISaveableRegistry registry)
        {
            _checkpointReachedSub = GlobalMessagePipe.GetSubscriber<CheckpointReached>();
            _saveRequestPublisher = GlobalMessagePipe.GetPublisher<SaveRequest>();
            _playerSpawnRequestPublisher = GlobalMessagePipe.GetPublisher<PlayerSpawnRequest>();
            _registry = registry;
        }

        void IStartable.Start()
        {
            _registry.Register(this);

            if (_checkpoints == null || _checkpoints.Length == 0)
                Debug.LogWarning("[CheckpointManager] No checkpoints found in scene.");
            else
            {
                for (int i = 0; i < _checkpoints.Length; i++)
                    _checkpoints[i].ResetState();

                // Defer visual activation here — scene is loaded, checkpoints exist
                if (!string.IsNullOrEmpty(_lastCheckpointId))
                {
                    for (int i = 0; i < _checkpoints.Length; i++)
                    {
                        if (_checkpoints[i].GetId() == _lastCheckpointId)
                        {
                            _checkpoints[i].Activate();
                            break;
                        }
                    }
                }
            }

            _disposables.Add(_checkpointReachedSub.Subscribe(OnCheckpointReached));
        }

        #region ICheckpointManager

        public Vector3 GetSpawnPosition()
        {
            if (_lastCheckpointPosition != Vector3.zero)
                return _lastCheckpointPosition;

            if (_startSpawnPoint != null)
                return _startSpawnPoint.position;

            Debug.LogError("[CheckpointManager] No checkpoint saved AND no Start Spawn Point!");
            return Vector3.zero;
        }

        public void ForceSpawnAt(Vector3 position)
        {
            _lastCheckpointPosition = position;
            _playerSpawnRequestPublisher.Publish(new PlayerSpawnRequest(position, forceRespawn: true));
        }

        #endregion

        #region ISaveable

        public ISaveData GetSaveData() => new CheckpointSaveData
        {
            CheckpointId = _lastCheckpointId,
            Position = _lastCheckpointPosition,
            IsValid = !string.IsNullOrEmpty(_lastCheckpointId)
        };

        public void LoadFromData(ISaveData data)
        {
            if (data is CheckpointSaveData saveData && saveData.IsValid)
            {
                _lastCheckpointId = saveData.CheckpointId;
                _lastCheckpointPosition = saveData.Position;
                // Do NOT Activate() here — scene isn't loaded yet.
                // IStartable.Start() handles visual activation.
            }
            else
            {
                _lastCheckpointPosition = Vector3.zero;
                _lastCheckpointId = string.Empty;
            }
        }

        #endregion

        #region Message Handlers

        private void OnCheckpointReached(CheckpointReached message)
        {
            if (_disposed) return;

            ICheckpoint checkpoint = null;
            if (_checkpoints != null)
            {
                for (int i = 0; i < _checkpoints.Length; i++)
                {
                    if (_checkpoints[i].GetId() == message.CheckpointId)
                    {
                        checkpoint = _checkpoints[i];
                        break;
                    }
                }
            }

            if (checkpoint == null)
            {
                Debug.LogWarning($"[CheckpointManager] Unknown checkpoint: {message.CheckpointId}");
                return;
            }

            SetCheckpoint(checkpoint);
        }

        private void SetCheckpoint(ICheckpoint checkpoint)
        {
            checkpoint.Activate();
            _lastCheckpointPosition = checkpoint.GetSpawnPosition();
            _lastCheckpointId = checkpoint.GetId();

            _saveRequestPublisher.Publish(new SaveRequest("CheckpointManager", false));
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _registry.Unregister(this);

            foreach (var d in _disposables) d?.Dispose();
            _disposables.Clear();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }

        #endregion
    }

}