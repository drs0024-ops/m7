using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Data;
using Game.Core.Enums;
using Game.Core.Messages;
using Game.Gameplay.Player;
using Game.Gameplay.Save;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    /// <summary>
    /// Persistent dialogue panel. Subscribes to DialogueRequested, manages
    /// typewriter, panel animation, input, time-scale, and auto-close.
    /// Publishes DialogueStarted / DialogueEnded for other systems.
    /// </summary>
    public class DialogueController : MonoBehaviour, IDisposable
    {
        #region Dependencies

        [Header("Dependencies")]
        [Inject] private InputManager _inputManager;
        [Inject] private IPublisher<TimeScalePause> _pausePublisher;
        [Inject] private IPublisher<TimeScaleResume> _resumePublisher;
        [Inject] private IPublisher<DialogueStarted> _startedPublisher;
        [Inject] private IPublisher<DialogueEnded> _endedPublisher;
        [Inject] private ISubscriber<DialogueRequested> _requestSub;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _npcNameText;
        [SerializeField] private TextMeshProUGUI _npcDialogueText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _borderImage;

        [Header("Panel Animation")]
        [SerializeField] private AnimationCurve _panelCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _panelTweenTime = 0.5f;
        [SerializeField] private float _fadeSpeed = 0.5f;

        [Header("Border")]
        [SerializeField] private Color _borderColor = Color.white;

        [Header("Typewriter")]
        [SerializeField] private float _defaultTypeSpeed = 10f;
        [SerializeField] private string _cursorChar = "█";

        [Header("Auto Close")]
        [SerializeField] private float _autoCloseTimer = 5f;

        private bool _skipTypewriter;

        #endregion

        #region State

        private DialogueState _state = DialogueState.Idle;
        private readonly Queue<DialogueRequested> _queue = new();
        private readonly Queue<string> _paragraphs = new();

        private string _currentText;
        private float _activeTypeSpeed;
        private CancellationTokenSource _typeCts;
        private CancellationTokenSource _autoCloseCts;
        private CancellationTokenSource _blinkCts;
        private bool _isDisposed;

        private readonly List<IDisposable> _disposables = new(1);
        private IObjectResolver _container;

        #endregion

        #region Contruct
        [Inject]
        private void Construct(
            IObjectResolver container)
        {
            _container = container;
        }

        #endregion

        #region Public API

        public DialogueState State => _state;
        public bool IsConversationActive => _state != DialogueState.Idle && _state != DialogueState.Closing;

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _typeCts?.Cancel();
            _typeCts?.Dispose();
            _autoCloseCts?.Cancel();
            _autoCloseCts?.Dispose();
            _blinkCts?.Cancel();
            _blinkCts?.Dispose();

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                Debug.LogError($"[DialogueController] _canvasGroup not assigned on {gameObject.name}.", this);
                enabled = false;
                return;
            }

            if (_borderImage != null)
                _borderImage.color = _borderColor;
        }

        private void Start()
        {
            if (!enabled) return;

            _disposables.Add(_requestSub.Subscribe(OnDialogueRequested));
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnDisable()
        {
            _typeCts?.Cancel();
            _blinkCts?.Cancel();
            _autoCloseCts?.Cancel();
        }

        private void Update()
        {
            if (_state != DialogueState.Waiting) return;
            if (!_inputManager.ConfirmWasPressed && !_inputManager.InteractWasPressed) return;

            if (_paragraphs.Count > 0)
                Advance();
            else
                EndConversation();
        }

        #endregion

        #region Message Handlers

        private void OnDialogueRequested(DialogueRequested msg)
        {
            if (_state == DialogueState.Idle)
                ProcessRequest(msg);
            else
                _queue.Enqueue(msg);
        }

        #endregion

        #region Conversation

        private void ProcessRequest(DialogueRequested msg)
        {
            if (msg.Dialogue == null) return;

            if (_state != DialogueState.Idle) return;

            gameObject.SetActive(true);
            OpenPanelAsync(msg.Dialogue, msg.AutoClose).Forget();
        }

        private async UniTask OpenPanelAsync(DialogueDataSO dialogue, bool autoClose)
        {
            _state = DialogueState.Opening;
            transform.localScale = new Vector3(0.75f, 1f, 1f);
            _canvasGroup.alpha = 0f;

            var tcs = new UniTaskCompletionSource();
            LeanTween.scale(gameObject, Vector3.one, _panelTweenTime)
                .setEase(LeanTweenType.easeOutSine)
                .setOnComplete(() => tcs.TrySetResult());

            await FadeCanvasAsync(1f);
            await tcs.Task;

            _state = DialogueState.Typing;

            int speedSetting = _container.Resolve<SettingsSavable>().GetTextSpeed();
            _skipTypewriter = speedSetting == 3;
            _activeTypeSpeed = speedSetting switch
            {
                0 => dialogue.typeSpeed > 0f ? dialogue.typeSpeed * 0.5f : _defaultTypeSpeed * 0.5f,
                1 => dialogue.typeSpeed > 0f ? dialogue.typeSpeed : _defaultTypeSpeed,
                2 => dialogue.typeSpeed > 0f ? dialogue.typeSpeed * 2f : _defaultTypeSpeed * 2f,
                _ => _defaultTypeSpeed
            };

            _pausePublisher.Publish(TimeScalePause.Default);
            _startedPublisher.Publish(new DialogueStarted(dialogue.speakerName, dialogue.name));

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

            Advance();
        }

        private void Advance()
        {
            StopBlinking();

            if (_paragraphs.Count > 0)
            {
                _currentText = _paragraphs.Dequeue();
                _state = DialogueState.Typing;
                TypeTextAsync(_currentText).Forget();
            }
        }

        private void EndConversation()
        {
            StopBlinking();

            _state = DialogueState.Closing;
            _paragraphs.Clear();
            _npcNameText.text = "";
            _npcDialogueText.text = "";

            _autoCloseCts?.Cancel();
            _autoCloseCts?.Dispose();

            _resumePublisher.Publish(TimeScaleResume.Default);
            _endedPublisher.Publish(DialogueEnded.Default);

            ClosePanelAsync().Forget();
        }

        #endregion

        #region Panel

        private async UniTask ClosePanelAsync()
        {
            await FadeCanvasAsync(0f);
            gameObject.SetActive(false);
            _state = DialogueState.Idle;

            if (_queue.Count > 0)
            {
                var next = _queue.Dequeue();
                ProcessRequest(next);
            }
        }

        private async UniTask FadeCanvasAsync(float targetAlpha)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeSpeed)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeSpeed);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, _panelCurve.Evaluate(t));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            _canvasGroup.alpha = targetAlpha;
        }

        #endregion

        #region Typewriter

        private async UniTask TypeTextAsync(string text)
        {
            _typeCts?.Cancel();
            _typeCts = new CancellationTokenSource();
            var token = _typeCts.Token;

            if (_skipTypewriter)
            {
                _npcDialogueText.text = text;
                _state = DialogueState.Waiting;
                StartBlinkingCursor(text);
                return;
            }

            _npcDialogueText.text = string.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                if (token.IsCancellationRequested) return;

                _npcDialogueText.text = text.Substring(0, i + 1) + _cursorChar;

                float delay = 1000f / _activeTypeSpeed;
                char c = text[i];
                if (c == '.' || c == '!' || c == '?') delay *= 3f;
                else if (c == ',' || c == ';') delay *= 1.5f;

                await UniTask.Delay((int)delay, cancellationToken: token);
            }

            if (!token.IsCancellationRequested)
            {
                _npcDialogueText.text = text;
                _state = DialogueState.Waiting;
                StartBlinkingCursor(text);
            }
        }

        private void FinishTyping()
        {
            _typeCts?.Cancel();
            _npcDialogueText.text = _currentText;
            _state = DialogueState.Waiting;
            if (_currentText != null)
                StartBlinkingCursor(_currentText);
        }

        private void StartBlinkingCursor(string fullText)
        {
            _blinkCts?.Cancel();
            _blinkCts?.Dispose();
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
                if (_state == DialogueState.Waiting)
                    EndConversation();
            }
            catch (OperationCanceledException) { }
        }

        #endregion
    }
}   