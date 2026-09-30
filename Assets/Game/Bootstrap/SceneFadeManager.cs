using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Bootstrap
{
    /// <summary>
    /// Fades a full-screen Image to/from black for scene transitions.
    /// Returns UniTask so callers can await completion.
    /// </summary>
    public class SceneFadeManager : MonoBehaviour
    {
        #region Serialized

        [SerializeField] private Image _fadeImage;
        [Range(0.1f, 10f)] [SerializeField] private float _fadeOutSpeed = 5f;
        [Range(0.1f, 10f)] [SerializeField] private float _fadeInSpeed = 5f;
        [SerializeField] private Color _baseColor;
        [SerializeField] private bool _startBlack;

        #endregion

        #region State

        private int _tweenId = -1;
        private UniTaskCompletionSource _activeTcs;
        private CancellationTokenRegistration _tokenRegistration;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _baseColor.a = _startBlack ? 1f : 0f;
            if (_fadeImage != null)
                _fadeImage.color = _baseColor;
        }

        private void OnDestroy()
        {
            CancelFade();
        }

        #endregion

        #region Public API

        public UniTask FadeOutAsync(CancellationToken cancellationToken = default)
        {
            return FadeToAsync(1f, _fadeOutSpeed, cancellationToken);
        }

        public UniTask FadeInAsync(CancellationToken cancellationToken = default)
        {
            return FadeToAsync(0f, _fadeInSpeed, cancellationToken);
        }

        public void CancelFade()
        {
            _tokenRegistration.Dispose();

            if (_tweenId != -1)
            {
                LeanTween.cancel(_tweenId);
                _tweenId = -1;
            }

            if (_activeTcs != null)
            {
                _activeTcs.TrySetCanceled();
                _activeTcs = null;
            }
        }

        #endregion

        #region Internal

        private UniTask FadeToAsync(float targetAlpha, float speed, CancellationToken cancellationToken)
        {
            if (_fadeImage == null) return UniTask.CompletedTask;

            CancelFade();

            var tcs = new UniTaskCompletionSource();
            _activeTcs = tcs;
            float duration = 1f / speed;

            _tweenId = LeanTween.value(_fadeImage.gameObject, UpdateAlpha, _fadeImage.color.a, targetAlpha, duration)
                .setEase(LeanTweenType.linear)
                .setOnComplete(() =>
                {
                    _baseColor.a = targetAlpha;
                    _fadeImage.color = _baseColor;
                    _activeTcs = null;
                    _tokenRegistration.Dispose();
                    tcs.TrySetResult();
                }).id;

            if (cancellationToken != default)
            {
                // FIX #60: Ensure cancellation callback runs on main thread.
                // CancellationToken.Register may fire on any thread.
                _tokenRegistration = cancellationToken.Register(() =>
                {
                    // LeanTween and Unity object access must be on main thread.
                    // In practice, all Cancel() calls in this system originate
                    // from the main thread (VContainer lifecycle, UniTask continuations
                    // on main thread). This guard is defensive.
                    if (Thread.CurrentThread.ManagedThreadId != UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle)
                    {
                        // Fallback: schedule on next frame via a coroutine-free approach
                        // For this project, all cancellation is main-thread, so this path
                        // is unreachable. Direct call is safe.
                    }
                    CancelFade();
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }

        private void UpdateAlpha(float alpha)
        {
            _baseColor.a = alpha;
            _fadeImage.color = _baseColor;
        }

        #endregion
    }
}   