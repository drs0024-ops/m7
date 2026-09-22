using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Messages;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer.Unity;

namespace Game.UI
{
    public class GuidanceTextController : MonoBehaviour, IStartable, IDisposable
    {
        [SerializeField] private TextMeshProUGUI _guidanceText;
        [SerializeField] private TextMeshProUGUI _codeFragmentText;
        [SerializeField] private float _typewriterInterval = 0.03f;
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private float _codeFragmentDisplayTime = 2f;

        private ISubscriber<GuidanceReceivedMessage> _guidanceSub;
        private ISubscriber<GuidanceSilentMessage> _silentSub;
        private ISubscriber<CodeFragmentDisplayedMessage> _codeFragmentSub;
        private ISubscriber<GuidanceRetriggerMessage> _retriggerSub;
        private readonly List<IDisposable> _subscriptions = new();
        private CancellationTokenSource _cts;
        private bool _disposed;
        private string _lastGuidanceText = string.Empty;

        public GuidanceTextController() { }

        void IStartable.Start()
        {
            _cts = new CancellationTokenSource();

            _guidanceSub = GlobalMessagePipe.GetSubscriber<GuidanceReceivedMessage>();
            _silentSub = GlobalMessagePipe.GetSubscriber<GuidanceSilentMessage>();
            _codeFragmentSub = GlobalMessagePipe.GetSubscriber<CodeFragmentDisplayedMessage>();
            _retriggerSub = GlobalMessagePipe.GetSubscriber<GuidanceRetriggerMessage>();

            _subscriptions.Add(_guidanceSub.Subscribe(OnGuidanceReceived));
            _subscriptions.Add(_silentSub.Subscribe(_ => OnGuidanceSilent()));
            _subscriptions.Add(_codeFragmentSub.Subscribe(OnCodeFragmentDisplayed));
            _subscriptions.Add(_retriggerSub.Subscribe(_ => OnGuidanceRetrigger()));

            _guidanceText.alpha = 0f;
            _codeFragmentText.alpha = 0f;
            _codeFragmentText.gameObject.SetActive(false);
        }

        private void OnGuidanceReceived(GuidanceReceivedMessage msg)
        {
            if (_disposed) return;

            _cts.Cancel();
            _cts = new CancellationTokenSource();

            _codeFragmentText.gameObject.SetActive(false);
            _codeFragmentText.alpha = 0f;

            _lastGuidanceText = msg.Text;
            TypewriterEffect(msg.Text, _cts.Token).Forget();
        }

        private void OnGuidanceRetrigger()
        {
            if (_disposed || string.IsNullOrEmpty(_lastGuidanceText)) return;

            _cts.Cancel();
            _cts = new CancellationTokenSource();
            TypewriterEffect(_lastGuidanceText, _cts.Token).Forget();
        }

        private void OnGuidanceSilent()
        {
            if (_disposed) return;

            _cts.Cancel();
            _cts = new CancellationTokenSource();

            LeanTween.alpha(_guidanceText.gameObject, 0f, _fadeOutDuration)
                .setEase(LeanTweenType.easeOutQuad);
        }

        private void OnCodeFragmentDisplayed(CodeFragmentDisplayedMessage msg)
        {
            if (_disposed) return;

            _cts.Cancel();
            _cts = new CancellationTokenSource();

            _guidanceText.alpha = 0f;

            _codeFragmentText.text = msg.Fragment;
            _codeFragmentText.alpha = 1f;
            _codeFragmentText.gameObject.SetActive(true);

            ShowCodeFragmentThenResume(_cts.Token).Forget();
        }

        private async UniTaskVoid ShowCodeFragmentThenResume(CancellationToken token)
        {
            await UniTask.Delay((int)(_codeFragmentDisplayTime * 1000f), cancellationToken: token);
            if (_disposed || token.IsCancellationRequested) return;

            _codeFragmentText.alpha = 0f;
            _codeFragmentText.gameObject.SetActive(false);
        }

        private async UniTaskVoid TypewriterEffect(string text, CancellationToken token)
        {
            _guidanceText.alpha = 1f;
            _guidanceText.text = string.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                if (token.IsCancellationRequested) return;
                _guidanceText.text += text[i];
                await UniTask.Delay((int)(_typewriterInterval * 1000f), cancellationToken: token);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();

            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }
    }
}   