using System.Collections;
using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Gameplay.World
{
    public class FallingPlatform : MonoBehaviour, ITriggerCheckable
    {
        [SerializeField] private float _gravityMultiplier = 1f;
        [SerializeField] private float _fallWait = 1f;
        [SerializeField] private float _mass = 0.5f;
        [SerializeField] private float _decelerationFactor = 1f;
        [SerializeField] private Collider2D _aggroCollider;

        [Header("Destroy")]
        [SerializeField] private bool _destroyAfterFall;
        [SerializeField] private float _destroyWaitTime = 3f;

        private Rigidbody2D _rb;
        private Collider2D _col2D;
        private bool _isFalling;
        private bool _hasDropped;

        public bool IsAggroed { get; private set; }
        public bool IsWithinStrikingDistance { get; private set; }
        bool ITriggerCheckable.IsAggroed { get => IsAggroed; set => IsAggroed = value; }
        public bool IsWithinStrickingDistance { get; set; }

        private const float StopThreshold = 0.001f;
        private float _timeNotMoving;
        private bool _wasNotMoving;

        private void Start()
        {
            _rb = GetComponentInParent<Rigidbody2D>();
            _rb.mass = _mass;
            _rb.angularDamping = 10f;
            _col2D = GetComponent<Collider2D>();
        }

        private void FixedUpdate()
        {
            bool isMovingY = Mathf.Abs(_rb.linearVelocity.y) > StopThreshold;

            var velocity = _rb.linearVelocity;
            velocity.x = 0f;
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, velocity, _decelerationFactor * Time.fixedDeltaTime);

            if (!isMovingY)
            {
                _timeNotMoving += Time.fixedDeltaTime;
                _wasNotMoving = true;
            }
            else
            {
                _timeNotMoving = 0f;
                _wasNotMoving = false;
            }

            if (_timeNotMoving > 2f)
                _rb.linearVelocity = Vector2.zero;
        }

        public void SetAggroStatus(bool isAggroed)
        {
            if (_hasDropped) return;

            IsAggroed = isAggroed;
            if (isAggroed)
            {
                _hasDropped = true;
                StartCoroutine(Fall());
            }
        }

        public void SetStrikingDistance(bool isWithinStrikingDistance)
        {
            IsWithinStrikingDistance = isWithinStrikingDistance;
        }

        private IEnumerator Fall()
        {
            _isFalling = true;
            yield return new WaitForSeconds(_fallWait);

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = Physics2D.gravity * _gravityMultiplier;

            _aggroCollider.enabled = false;

            if (_destroyAfterFall)
                Destroy(gameObject, _destroyWaitTime);
        }

        public void SetStrikingDistanceBool(bool isWithinStrikingDistance)
        {
            throw new System.NotImplementedException();
        }
    }
}   