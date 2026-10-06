using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Enemies.Runtime
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnemyPerceptionSensor : MonoBehaviour
    {
        [SerializeField] private string _targetTag = "Player";

        private readonly HashSet<int> _overlappingColliders = new();
        private IPublisher<EnemyPerceptionChanged> _perceptionPublisher;
        private EnemyInstanceId _enemyId;
        private Transform _origin;
        private Transform _target;
        private bool _initialized;

        [Inject]
        private void Inject(IPublisher<EnemyPerceptionChanged> perceptionPublisher)
        {
            _perceptionPublisher = perceptionPublisher;
        }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Initialize(EnemyInstanceId enemyId, Transform origin)
        {
            if (_initialized) return;
            if (origin == null)
            {
                Debug.LogError($"[EnemyPerceptionSensor] Missing origin on '{name}'.", this);
                enabled = false;
                return;
            }
            if (_perceptionPublisher == null)
            {
                Debug.LogError($"[EnemyPerceptionSensor] MessagePipe publisher was not injected on '{name}'.", this);
                enabled = false;
                return;
            }

            _enemyId = enemyId;
            _origin = origin;
            _initialized = true;
        }

        private void OnTriggerEnter2D(Collider2D other) => Observe(other);

        private void OnTriggerStay2D(Collider2D other) => Observe(other);

        private void Observe(Collider2D other)
        {
            if (!_initialized || other == null) return;

            Transform target = other.transform.root;
            if (!target.CompareTag(_targetTag)) return;
            if (_target != null && _target != target) return;

            _target = target;
            _overlappingColliders.Add(other.GetInstanceID());
            PublishTargetObservation();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_initialized || other == null || _target == null) return;
            if (other.transform.root != _target) return;

            _overlappingColliders.Remove(other.GetInstanceID());
            if (_overlappingColliders.Count > 0)
            {
                PublishTargetObservation();
                return;
            }

            PublishNoTarget();
        }

        private void OnDisable()
        {
            if (!_initialized || _target == null) return;
            PublishNoTarget();
        }

        private void PublishTargetObservation()
        {
            if (_target == null || _origin == null) return;

            _perceptionPublisher.Publish(new EnemyPerceptionChanged(
                _enemyId,
                true,
                _target.gameObject.GetInstanceID(),
                _target.position,
                Vector2.Distance(_origin.position, _target.position),
                _origin.position));
        }

        private void PublishNoTarget()
        {
            _perceptionPublisher.Publish(new EnemyPerceptionChanged(
                _enemyId,
                false,
                0,
                Vector2.zero,
                0f,
                _origin != null ? (Vector2)_origin.position : Vector2.zero));
            _target = null;
            _overlappingColliders.Clear();
        }
    }
}