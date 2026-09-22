using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Data;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class DialogueController : MonoBehaviour, IDialogueController, IDisposable
    {
        [SerializeField] private TextMeshProUGUI _npcNameText;
        [SerializeField] private TextMeshProUGUI _npcDialogueText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _typeSpeed = 10f;
        [SerializeField] private float _fadeSpeed = 0.5f;
        [SerializeField] private float _panelTweenTime = 0.5f;
        [SerializeField] private float _autoCloseTimer = 5f;
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private readonly IPublisher<TimeScalePause> _pausePublisher;
        private readonly IPublisher<TimeScaleResume> _resumePublisher;
        private readonly Queue<string> _paragraphs = new();
        private readonly List<IDisposable> _disposables = new();

        private string _currentText;
        private string _cursorChar = "█";
        private CancellationTokenSource _typeCts;
        private CancellationTokenSource _autoCloseCts;
        private CancellationTokenSource _blinkCts;
        private bool _isTyping;
        private bool _conversationActive;
        private bool _panelReady;

        public bool IsTyping => _isTyping;
        public bool IsConversationActive => _conversationActive;

        public DialogueController()
        {
            _pausePublisher = GlobalMessagePipe.GetPublisher<TimeScalePause>();
            _resumePublisher = GlobalMessagePipe.GetPublisher<TimeScaleResume>();
        }

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        #region IDialogueController

        public void ShowDialogue(DialogueText dialogue, bool autoClose)
        {
            if (_panelReady)
            {
                StartConversation(dialogue, autoClose);
                return;
            }

            gameObject.SetActive(true);
            OpenPanelAsync(dialogue, autoClose).Forget();
        }

        public void DisplayNextParagraph(DialogueText dialogue)
        {
            if (!_conversationActive)
            {
                ShowDialogue(dialogue, autoClose: false);
                return;
            }

            Advance();
        }

        public void StopTyping()
        {
            if (_isTyping)
                FinishTyping();
        }

        #endregion

        #region Panel

        private async UniTask OpenPanelAsync(DialogueText dialogue, bool autoClose)
        {
            _panelReady = false;
            transform.localScale = new Vector3(0.75f, 1f, 1f);
            _canvasGroup.alpha = 0f;

            var tcs = new UniTaskCompletionSource();
            LeanTween.scale(gameObject, Vector3.one, _panelTweenTime)
                .setEase(LeanTweenType.easeOutSine)
                .setOnComplete(() => tcs.TrySetResult());

            await FadeCanvasAsync(1f);
            await tcs.Task;

            _panelReady = true;
            StartConversation(dialogue, autoClose);
        }

        private async UniTask ClosePanelAsync()
        {
            await FadeCanvasAsync(0f);
            gameObject.SetActive(false);
        }

        private async UniTask FadeCanvasAsync(float targetAlpha)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeSpeed)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeSpeed);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, _fadeCurve.Evaluate(t));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            _canvasGroup.alpha = targetAlpha;
        }

        #endregion

        #region Conversation

        private void StartConversation(DialogueText dialogue, bool autoClose)
        {
            _conversationActive = true;
            _npcNameText.text = dialogue.speakerName;
            _paragraphs.Clear();

            for (int i = 0; i < dialogue.paragraphs.Length; i++)
                _paragraphs.Enqueue(dialogue.paragraphs[i]);

            if (autoClose)
            {
                _autoCloseCts?.Cancel();
                _autoCloseCts?.Dispose();
                _autoCloseCts = new CancellationTokenSource();
                AutoCloseAsync(_autoCloseTimer, _autoCloseCts.Token).Forget();
            }
            else
            {
                _pausePublisher.Publish(default);
            }

            Advance();
        }

        private void Advance()
        {
            StopBlinking();

            if (_isTyping)
            {
                FinishTyping();
                return;
            }

            if (_paragraphs.Count > 0)
            {
                _currentText = _paragraphs.Dequeue();
                TypeTextAsync(_currentText).Forget();
            }
        }

        private void EndConversation()
        {
            StopBlinking();

            _paragraphs.Clear();
            _npcNameText.text = "";
            _npcDialogueText.text = "";
            _conversationActive = false;
            _panelReady = false;

            _autoCloseCts?.Cancel();
            _autoCloseCts?.Dispose();
            _resumePublisher.Publish(default);

            ClosePanelAsync().Forget();
        }

        #endregion

        #region Typewriter

        private async UniTask TypeTextAsync(string text)
        {
            _typeCts?.Cancel();
            _typeCts = new CancellationTokenSource();
            var token = _typeCts.Token;

            _isTyping = true;
            _npcDialogueText.text = string.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                if (token.IsCancellationRequested) break;

                _npcDialogueText.text = text.Substring(0, i + 1) + _cursorChar;

                float delay = 100f / _typeSpeed;
                char c = text[i];
                if (c == '.' || c == '!' || c == '?') delay *= 3f;
                else if (c == ',' || c == ';') delay *= 1.5f;

                await UniTask.Delay((int)delay, cancellationToken: token);
            }

            if (!token.IsCancellationRequested)
            {
                _npcDialogueText.text = text;
                _isTyping = false;
                StartBlinkingCursor(text);
            }
        }

        private void FinishTyping()
        {
            _typeCts?.Cancel();
            _npcDialogueText.text = _currentText;
            _isTyping = false;
            if (_currentText != null)
                StartBlinkingCursor(_currentText);
        }

        private void StartBlinkingCursor(string fullText)
        {
            _blinkCts?.Cancel();
            _blinkCts = new CancellationTokenSource();
            BlinkCursorAsync(fullText, _blinkCts.Token).Forget();
        }

        private async UniTask BlinkCursorAsync(string fullText, CancellationToken token)
        {
            bool visible = true;

            while (!token.IsCancellationRequested)
            {
                _npcDialogueText.text = visible ? fullText + _cursorChar : fullText;
                visible = !visible;
                await UniTask.Delay(500, cancellationToken: token);
            }
        }

        private void StopBlinking()
        {
            _blinkCts?.Cancel();
            _blinkCts?.Dispose();
        }

        private async UniTask AutoCloseAsync(float delay, CancellationToken token)
        {
            try
            {
                await UniTask.Delay((int)(delay * 1000f), cancellationToken: token);
                EndConversation();
            }
            catch (OperationCanceledException) { }
        }

        #endregion

        #region Input

        private void Update()
        {
            if (!_conversationActive) return;

            bool advanceInput = Input.GetKeyDown(KeyCode.Space) ||
                                Input.GetKeyDown(KeyCode.Return) ||
                                Input.GetMouseButtonDown(0);

            if (!advanceInput) return;

            if (_isTyping)
            {
                FinishTyping();
            }
            else if (_paragraphs.Count > 0)
            {
                Advance();
            }
            else
            {
                EndConversation();
            }
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _typeCts?.Cancel();
            _typeCts?.Dispose();
            _autoCloseCts?.Cancel();
            _autoCloseCts?.Dispose();
            _blinkCts?.Cancel();
            _blinkCts?.Dispose();

            foreach (var d in _disposables)
                d?.Dispose();
            _disposables.Clear();
        }

        private void OnDestroy() => Dispose();

        #endregion
    }
}   