using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.Video;

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

        #endregion

        #region State

        private IDisposable _skipSubscription;
        private CancellationTokenSource _cts;
        private bool _disposed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_videoPlayer == null)
            {
                Debug.LogError("[CutscenePlayer] _videoPlayer is not assigned!", this);
                return;
            }

            _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            _videoPlayer.isLooping = false;
            _videoPlayer.playOnAwake = false;
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Plays the given clip full-screen. Resolves on end or skip.
        /// If clip is null, resolves immediately (no-op).
        /// </summary>
        public UniTask PlayAsync(VideoClip clip)
        {
            if (clip == null || _disposed) return UniTask.CompletedTask;
            return PlayInternal(clip);
        }

        #endregion

        #region Internal

        private async UniTask PlayInternal(VideoClip clip)
        {
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
                var skipSub = GlobalMessagePipe.GetSubscriber<VideoSkipRequested>();
                _skipSubscription = skipSub.Subscribe(_ => tcs.TrySetResult());
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