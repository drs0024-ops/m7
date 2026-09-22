using System;
using System.Collections;
using UnityEngine;
using VContainer;
using Game.Core.Interfaces;
using System.Collections.Generic;

namespace Game.Gameplay.Player
{
    [SelectionBase]
    public class PlayerAttack : MonoBehaviour, IDisposable
    {
        [SerializeField] private Transform _attackTransform;
        [SerializeField] private float _attackRange = 1.5f;
        [SerializeField] private LayerMask _attackableLayer;
        [SerializeField] private float _damageAmount = 1f;
        [SerializeField] private float _timeBetweenAttacks = 0.15f;

        private HashSet<IDamagable> _damagedThisSlash = new();


        public bool ShouldBeDamaging { get; private set; }

        private InputManager _inputManager;
        private float _attackTimeCounter;
        private Animator _anim;
        private Coroutine _damageCoroutine;

        [Inject]
        private void Inject(InputManager inputManager)
        {
            _inputManager = inputManager;
        }

        private void Start()
        {
            _anim = GetComponent<Animator>();
            _attackTimeCounter = _timeBetweenAttacks;
        }

        private void Update()
        {
            if (_inputManager.AttackWasPressed && _attackTimeCounter >= _timeBetweenAttacks)
            {
                _attackTimeCounter = 0f;
                Attack();
            }

            _attackTimeCounter += Time.deltaTime;
        }

        private void Attack()
        {
            var hits = Physics2D.CircleCastAll(
                _attackTransform.position, _attackRange, transform.right, 0f, _attackableLayer);

            for (int i = 0; i < hits.Length; i++)
            {
                var target = hits[i].collider.gameObject.GetComponentInParent<IDamagable>();
                if (target != null)
                    target.Damage(_damageAmount, transform.right);
            }
        }

        public IEnumerator DamageWhileSlashIsActive()
        {
            ShouldBeDamaging = true;

            while (ShouldBeDamaging)
            {
                var hits = Physics2D.CircleCastAll(
                    _attackTransform.position, _attackRange, transform.right, 0f, _attackableLayer);

                for (int i = 0; i < hits.Length; i++)
                {
                    var target = hits[i].collider.gameObject.GetComponentInParent<IDamagable>();
                    if (target != null && _damagedThisSlash.Add(target))
                        target.Damage(_damageAmount, transform.right);
                }

                yield return null;
            }

            ShouldBeDamaging = false;
        }   

        private void OnDestroy()
        {
            if (_damageCoroutine != null)
                StopCoroutine(_damageCoroutine);
        }

        public void Dispose()
        {
            if (_damageCoroutine != null)
                StopCoroutine(_damageCoroutine);
        }

        #region Animation Triggers

        public void OnSlashStart()
        {
            _damagedThisSlash.Clear();
            _damageCoroutine = StartCoroutine(DamageWhileSlashIsActive());
        }

        public void OnSlashEnd()
        {
            if (_damageCoroutine != null)
                StopCoroutine(_damageCoroutine);
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (_attackTransform == null) return;
            Gizmos.DrawWireSphere(_attackTransform.position, _attackRange);
        }
    }
}   