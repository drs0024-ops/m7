using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Gameplay.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerCloak : MonoBehaviour, IDisposable
    {
        [Header("Cloak Settings")]
        public float cloakDuration = 1.5f;
        public bool startVisible = true;

        private SpriteRenderer _sr;
        private MaterialPropertyBlock _propBlock;
        private int _cloakID;
        private bool _isCloaking = false;
        private bool _isVisible = true;
        private CancellationTokenSource _cts;
        private bool _isDisposed = false;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _cloakID = Shader.PropertyToID("_CloakFactor");
            _propBlock = new MaterialPropertyBlock();

            if (!startVisible) StartCloak();
            else _sr.GetPropertyBlock(_propBlock);
        }

        public void StartCloak()
        {
            if (_isCloaking || !_isVisible) return;
            CancelCurrent();
            _isCloaking = true;
            CloakRoutine().Forget();
        }

        public void Reveal()
        {
            if (!_isCloaking && _isVisible) return;
            CancelCurrent();
            _isCloaking = true;
            RevealRoutine().Forget();
        }

        private void CancelCurrent()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async UniTask CloakRoutine()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _sr.GetPropertyBlock(_propBlock);
            float startVal = _propBlock.GetFloat(_cloakID);
            float timer = 0f;

            while (timer < cloakDuration && !token.IsCancellationRequested)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / cloakDuration);
                float newVal = Mathf.Lerp(startVal, 1f, t);

                _propBlock.SetFloat(_cloakID, newVal);
                _sr.SetPropertyBlock(_propBlock);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            if (!token.IsCancellationRequested)
            {
                _propBlock.SetFloat(_cloakID, 1f);
                _sr.SetPropertyBlock(_propBlock);
                _isVisible = false;
            }
            _isCloaking = false;
        }

        private async UniTask RevealRoutine()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _sr.GetPropertyBlock(_propBlock);
            float startVal = _propBlock.GetFloat(_cloakID);
            float timer = 0f;

            while (timer < cloakDuration && !token.IsCancellationRequested)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / cloakDuration);
                float newVal = Mathf.Lerp(startVal, 0f, t);

                _propBlock.SetFloat(_cloakID, newVal);
                _sr.SetPropertyBlock(_propBlock);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }

            if (!token.IsCancellationRequested)
            {
                _propBlock.SetFloat(_cloakID, 0f);
                _sr.SetPropertyBlock(_propBlock);
                _isVisible = true;
            }
            _isCloaking = false;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            CancelCurrent();
        }

        private void OnDestroy()
        {
            if (!_isDisposed)
                Dispose();
        }
    }   
}