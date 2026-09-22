using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Data;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.Video;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay
{
    /// <summary>
    /// Plays the intro video with optional external audio track.
    /// Publishes VideoFinished on completion (or immediately if already played).
    /// Implements ISaveable to persist "has been played" across sessions.
    /// </summary>
    public class VideoPlayerMaster : MonoBehaviour, IStartable, ISaveable, IDisposable
    {
        #region Dependencies

        [SerializeField] private VideoClip _videoClip;
        [SerializeField] private bool _skipIfAlreadyPlayed = true;
        [SerializeField] private AudioClip _externalAudioClip;

        [Inject] private ISaveableRegistry _registry;

        private IPublisher<VideoFinished> _videoFinishedPublisher;

        #endregion

        #region State

        private VideoPlayer _videoPlayer;
        private AudioSource _audioSource;
        private VideoSaveData _currentData = new();
        private bool _hasFinished;
        private CancellationTokenSource _cts;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        #endregion

        #region ISaveable

        public string SaveId => "VideoPlayer";

        public ISaveData GetSaveData()
        {
            return new VideoSaveData
            {
                HasPlayed = _currentData.HasPlayed || _hasFinished
            };
        }

        public void LoadFromData(ISaveData data)
        {
            if (data is VideoSaveData videoData)
                _currentData = videoData;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _registry.Register(this);
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            _cts = new CancellationTokenSource();
            _videoFinishedPublisher = GlobalMessagePipe.GetPublisher<VideoFinished>();

            var skipSub = GlobalMessagePipe.GetSubscriber<VideoSkipRequested>();
            _subscriptions.Add(skipSub.Subscribe(_ => FinishVideo()));

            if (_skipIfAlreadyPlayed && _currentData.HasPlayed)
            {
                FinishVideo();
                return;
            }

            _videoPlayer = GetComponent<VideoPlayer>();
            if (_videoPlayer == null)
            {
                Debug.LogError("[VideoPlayerMaster] VideoPlayer component not found.", this);
                FinishVideo();
                return;
            }

            if (_externalAudioClip != null)
            {
                SetupExternalAudio();
            }
            else
            {
                FallbackToNativeAudio();
            }
        }

        #endregion

        #region Playback

        private void SetupExternalAudio()
        {
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.clip = _videoClip;
            _videoPlayer.isLooping = true;
            _videoPlayer.Play();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.clip = _externalAudioClip;
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.Play();

            WaitForAudioToFinish(_cts.Token).Forget();
        }

        private void FallbackToNativeAudio()
        {
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            _videoPlayer.clip = _videoClip;
            _videoPlayer.isLooping = false;
            _videoPlayer.loopPointReached += OnVideoEndReached;
            _videoPlayer.Play();
        }

        private async UniTask WaitForAudioToFinish(CancellationToken token)
        {
            if (_audioSource == null || _externalAudioClip == null)
            {
                FinishVideo();
                return;
            }

            try
            {
                await UniTask.WaitUntil(() =>
                    !_audioSource.isPlaying ||
                    _audioSource.time >= _externalAudioClip.length - 0.01f,
                    cancellationToken: token);

                await UniTask.Delay(100, cancellationToken: token);
                FinishVideo();
            }
            catch (OperationCanceledException) { }
        }

        private void OnVideoEndReached(VideoPlayer vp)
        {
            vp.loopPointReached -= OnVideoEndReached;
            FinishVideo();
        }

        private void FinishVideo()
        {
            if (_hasFinished) return;
            _hasFinished = true;
            _currentData.HasPlayed = true;

            if (_videoPlayer != null)
                _videoPlayer.Stop();

            if (_audioSource != null)
                _audioSource.Stop();

            _videoFinishedPublisher.Publish(default);
        }

        public void ResetVideoProgress()
        {
            _currentData.HasPlayed = false;
            _hasFinished = false;
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _registry.Unregister(this);
            if (_disposed) return;
            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();

            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached -= OnVideoEndReached;
                _videoPlayer.Stop();
            }

            if (_audioSource != null)
                _audioSource.Stop();
        }

        #endregion
    }
}   