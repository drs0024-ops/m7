using System;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    public class Checkpoint : MonoBehaviour, ICheckpoint, IDisposable
    {
        [SerializeField] private string _checkpointId = "CP_Default";
        [SerializeField] private bool _oneTimeTrigger = true;
        [SerializeField] private Transform _spawnOffset;

        private IPublisher<CheckpointReached> _checkpointReachedPublisher;
        private bool _hasBeenReached;
        private bool _disposed;

        [Inject]
        private void Inject(IPublisher<CheckpointReached> checkpointReachedPublisher)
        {
            _checkpointReachedPublisher = checkpointReachedPublisher;
        }

        #region ICheckpoint

        public string GetId() => _checkpointId;
        public Vector3 GetSpawnPosition() => _spawnOffset != null ? _spawnOffset.position : transform.position;

        public void ResetState()
        {
            _hasBeenReached = false;
            gameObject.SetActive(true);
        }

        public void Activate()
        {
            _hasBeenReached = true;
        }

        #endregion

        private void Reset()
        {
            if (GetComponent<BoxCollider2D>() == null)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (_hasBeenReached && _oneTimeTrigger) return;

            Activate();
            _checkpointReachedPublisher.Publish(new CheckpointReached(_checkpointId, GetSpawnPosition()));
        }

        private void OnDestroy() => ((IDisposable)this).Dispose();

        void IDisposable.Dispose()
        {
            _disposed = true;
        }
    }
}   