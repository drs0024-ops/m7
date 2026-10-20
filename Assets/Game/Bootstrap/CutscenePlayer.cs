using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Gameplay.Audio;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using UnityEngine.Video;
using VContainer;

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
        [Inject] private IPublisher<CutsceneAudioDuck> _audioDuckPublisher;
        [Inject] private IPublisher<CutsceneAudioRestore> _audioRestorePublisher;   
        [Inject] private AudioConfigSO _audioConfig;

        #endregion

        #region Cameras

        [SerializeField] private Camera _cutsceneCamera;
        [SerializeField] private Camera _gameplayCamera;

        #endregion

        #region State

        private IDisposable _skipSubscription;
        private CancellationTokenSource _cts;
        private AudioSource _videoAudioSource;
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

            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.isLooping = false;
            _videoPlayer.playOnAwake = false;

            _videoAudioSource = GetComponent<AudioSource>();
            if (_videoAudioSource == null)
                _videoAudioSource = gameObject.AddComponent<AudioSource>();

            _videoAudioSource.playOnAwake = false;
            _videoAudioSource.loop = false;
            _videoAudioSource.spatialBlend = 0f;
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
            if (clip == null)
            {
                Debug.LogWarning("[IntroTrace] CutscenePlayer.PlayAsync skipped: clip is null.");
                return UniTask.CompletedTask;
            }

            if (_disposed)
            {
                Debug.LogWarning("[IntroTrace] CutscenePlayer.PlayAsync skipped: player is disposed.");
                return UniTask.CompletedTask;
            }

            if (!enabled)
            {
                Debug.LogWarning("[IntroTrace] CutscenePlayer.PlayAsync skipped: component is disabled.", this);
                return UniTask.CompletedTask;
            }

            if (_videoPlayer == null)
            {
                Debug.LogError("[IntroTrace] CutscenePlayer.PlayAsync skipped: VideoPlayer is null or destroyed.", this);
                return UniTask.CompletedTask;
            }

            Debug.Log($"[IntroTrace] CutscenePlayer starting '{clip.name}' with VideoPlayer '{_videoPlayer.name}'.", this);

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

            // Switch cameras
            _gameplayCamera.enabled = false;
            _cutsceneCamera.gameObject.SetActive(true);
            _cutsceneCamera.enabled = true;

            // Duck audio for cutscene
            _audioDuckPublisher.Publish(new CutsceneAudioDuck(0.15f, 0.4f));

            _videoPlayer.clip = clip;
            ConfigureAudioOutput();

            void OnEnd(VideoPlayer vp)
            {
                vp.loopPointReached -= OnEnd;
                tcs.TrySetResult();
            }

            void OnError(VideoPlayer vp, string message)
            {
                Debug.LogError($"[CutscenePlayer] Video playback failed for '{clip.name}': {message}", this);
                vp.errorReceived -= OnError;
                tcs.TrySetResult();
            }

            _videoPlayer.loopPointReached += OnEnd;
            _videoPlayer.errorReceived += OnError;

            if (_allowSkip)
                _skipSubscription = _skipSub.Subscribe(_ => tcs.TrySetResult());

            _videoPlayer.Play();
            WatchForClipEndAsync(clip, tcs, _cts.Token).Forget();

            try
            {
                double timeoutSeconds = clip.length > 0d
                    ? clip.length + Math.Max(10d, clip.length * 0.25d)
                    : 120d;
                UniTask timeoutTask = UniTask.Delay(
                    TimeSpan.FromSeconds(timeoutSeconds),
                    ignoreTimeScale: true,
                    cancellationToken: _cts.Token);

                int completedTask = await UniTask.WhenAny(tcs.Task, timeoutTask);
                if (completedTask == 1)
                {
                    Debug.LogError(
                        $"[CutscenePlayer] Timed out waiting for '{clip.name}' " +
                        $"(length={clip.length:F1}, frames={clip.frameCount}, " +
                        $"prepared={_videoPlayer.isPrepared}, playing={_videoPlayer.isPlaying}, " +
                        $"time={_videoPlayer.time:F1}, frame={_videoPlayer.frame}).", this);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                _videoPlayer.loopPointReached -= OnEnd;
                _videoPlayer.errorReceived -= OnError;
                _videoPlayer.Stop();
                _videoAudioSource.Stop();
                _skipSubscription?.Dispose();
                _skipSubscription = null;
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
                _isPlaying = false;

                // Restore cameras
                _cutsceneCamera.enabled = false;
                _cutsceneCamera.gameObject.SetActive(false);
                _gameplayCamera.enabled = true;

                // Restore audio after cutscene
                _audioRestorePublisher.Publish(new CutsceneAudioRestore(0.4f));
            }
        }   

        private async UniTask WatchForClipEndAsync(
            VideoClip clip,
            UniTaskCompletionSource completion,
            CancellationToken cancellationToken)
        {
            long lastFrame = clip.frameCount > 0 ? (long)clip.frameCount - 1L : -1L;
            double endTime = Math.Max(0d, clip.length - 0.05d);
            bool playbackStarted = false;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_videoPlayer == null) return;

                    long frame = _videoPlayer.frame;
                    double time = _videoPlayer.time;
                    playbackStarted |= _videoPlayer.isPlaying || frame >= 0 || time > 0d;

                    if (playbackStarted &&
                        ((clip.length > 0d && time >= endTime) || (lastFrame >= 0 && frame >= lastFrame)))
                    {
                        completion.TrySetResult();
                        return;
                    }

                    await UniTask.DelayFrame(1, cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
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
            if (_videoAudioSource != null)
                _videoAudioSource.Stop();
        }

        #endregion

        #region Audio Output

        private void ConfigureAudioOutput()
        {
            var mixer = _audioConfig != null ? _audioConfig.audioMixer : null;
            var masterGroups = mixer != null ? mixer.FindMatchingGroups("Master") : null;
            if (masterGroups != null && masterGroups.Length > 0)
                _videoAudioSource.outputAudioMixerGroup = masterGroups[0];
            else
                Debug.LogWarning("[CutscenePlayer] AudioConfigSO has no Master mixer group; video audio will use the default output.", this);

            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            if (_videoPlayer.controlledAudioTrackCount == 0)
                _videoPlayer.controlledAudioTrackCount = 1;
            _videoPlayer.SetTargetAudioSource(0, _videoAudioSource);
        }

        #endregion
    }
}   