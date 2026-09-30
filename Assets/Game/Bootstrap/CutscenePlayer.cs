using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.Video;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Plays an opaque full-screen cutscene video in Bootstrap.
    /// PlayAsync resolves when the video ends or is skipped.
    /// Lives in Bootstrap (persistent). Reused for all cutscenes.
    /// </summary>
    public class CutscenePlayer : MonoBehaviour, IDisposable
    {
        #region Dependencies

        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private bool _allowSkip = true;

        [Inject] private ISubscriber<VideoSkipRequested> _skipSub;

        #endregion

        #region State

        private IDisposable _skipSubscription;
        private CancellationTokenSource _cts;
        private bool _disposed;
        private bool _isPlaying;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_videoPlayer == null)
            {
                Debug.LogError("[CutscenePlayer] _videoPlayer is not assigned!", this);
                enabled = false;
                return;
            }

            _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            _videoPlayer.isLooping = false;
            _videoPlayer.playOnAwake = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Plays the given clip full-screen. Resolves on end or skip.
        /// If clip is null, resolves immediately (no-op).
        /// If already playing, cancels the previous and starts the new one.
        /// </summary>
        public UniTask PlayAsync(VideoClip clip)
        {
            if (clip == null || _disposed || !enabled) return UniTask.CompletedTask;

            // Cancel any in-flight play
            if (_isPlaying)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
                _skipSubscription?.Dispose();
                _skipSubscription = null;
            }

            return PlayInternal(clip);
        }

        #endregion

        #region Internal

        private async UniTask PlayInternal(VideoClip clip)
        {
            _isPlaying = true;
            _cts = new CancellationTokenSource();
            var tcs = new UniTaskCompletionSource();

            _videoPlayer.clip = clip;
            _videoPlayer.Play();

            void OnEnd(VideoPlayer vp)
            {
                vp.loopPointReached -= OnEnd;
                tcs.TrySetResult();
            }
            _videoPlayer.loopPointReached += OnEnd;

            if (_allowSkip)
            {
                _skipSubscription = _skipSub.Subscribe(_ => tcs.TrySetResult());
            }

            try
            {
                await tcs.Task.AttachExternalCancellation(_cts.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _videoPlayer.loopPointReached -= OnEnd;
                _videoPlayer.Stop();
                _skipSubscription?.Dispose();
                _skipSubscription = null;
                _cts.Dispose();
                _cts = null;
                _isPlaying = false;
            }
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _skipSubscription?.Dispose();
            _skipSubscription = null;

            if (_videoPlayer != null)
                _videoPlayer.Stop();
        }

        #endregion
    }
}   