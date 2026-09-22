using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core.Messages;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer.Unity;
using MessagePipe;
using Game.Core.Enums;

namespace Game.UI
{
    public class AdvancedHUD : MonoBehaviour, IStartable, IDisposable
    {
        [Header("Show On Change Only")]
        [SerializeField] private bool _healthShowOnChangeOnly;
        [SerializeField] private bool _coinShowOnChangeOnly;
        [SerializeField] private bool _staminaShowOnChangeOnly;
        [SerializeField] private bool _timerShowOnChangeOnly;
        [SerializeField] private bool _objectiveShowOnChangeOnly;
        [SerializeField] private float _showDuration = 2f;

        [Header("UI Elements")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private Slider _staminaSlider;
        [SerializeField] private Image _staminaFillImage;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _objectiveText;
        [SerializeField] private GameObject[] _keyIcons;

        [Header("Panels")]
        [SerializeField] private CanvasGroup _healthPanel;
        [SerializeField] private CanvasGroup _coinPanel;
        [SerializeField] private CanvasGroup _staminaPanel;
        [SerializeField] private CanvasGroup _timerPanel;
        [SerializeField] private CanvasGroup _objectivePanel;

        [SerializeField] private float _fadeDuration = 0.3f;

        // ─── Subscribers ───────────────────────────────────────────
        private  ISubscriber<EntityHealthChanged> _healthSubscriber;
        private  ISubscriber<CurrencyChanged> _currencySubscriber;
        private  ISubscriber<StaminaChanged> _staminaSubscriber;
        private  ISubscriber<LevelTimerUpdated> _timerSubscriber;
        private  ISubscriber<ObjectiveProgress> _objectiveSubscriber;
        private  ISubscriber<GameStateChanged> _gameStateSubscriber;
        private readonly List<IDisposable> _subscriptions = new();

        // ─── Zero-alloc hide tokens ────────────────────────────────
        private const int HealthIdx = 0;
        private const int CoinIdx = 1;
        private const int StaminaIdx = 2;
        private const int TimerIdx = 3;
        private const int ObjectiveIdx = 4;

        private readonly int[] _hideTokens = new int[5];
        private bool _disposed;

        public AdvancedHUD()
        {
        }

        void IStartable.Start()
        {
            _healthSubscriber = GlobalMessagePipe.GetSubscriber<EntityHealthChanged>();
            _currencySubscriber = GlobalMessagePipe.GetSubscriber<CurrencyChanged>();
            _staminaSubscriber = GlobalMessagePipe.GetSubscriber<StaminaChanged>();
            _timerSubscriber = GlobalMessagePipe.GetSubscriber<LevelTimerUpdated>();
            _objectiveSubscriber = GlobalMessagePipe.GetSubscriber<ObjectiveProgress>();
            _gameStateSubscriber = GlobalMessagePipe.GetSubscriber<GameStateChanged>();
            _subscriptions.Add(_healthSubscriber.Subscribe(OnHealthChanged));
            _subscriptions.Add(_currencySubscriber.Subscribe(OnCurrencyChanged));
            _subscriptions.Add(_staminaSubscriber.Subscribe(OnStaminaChanged));
            _subscriptions.Add(_timerSubscriber.Subscribe(OnTimerUpdated));
            _subscriptions.Add(_objectiveSubscriber.Subscribe(OnObjectiveProgress));
            _subscriptions.Add(_gameStateSubscriber.Subscribe(OnGameStateChanged));
        }

        #region Message Handlers

        private void OnHealthChanged(EntityHealthChanged message)
        {
            if (_healthSlider != null)
            {
                _healthSlider.maxValue = message.MaxHealth;
                _healthSlider.value = message.CurrentHealth;
            }

            if (_healthText != null)
                _healthText.text = $"{Mathf.CeilToInt(message.CurrentHealth)} / {Mathf.CeilToInt(message.MaxHealth)}";

            if (_healthShowOnChangeOnly)
                ShowPanelTemporarily(_healthPanel, HealthIdx);
        }

        private void OnCurrencyChanged(CurrencyChanged message)
        {
            if (_coinText != null)
                _coinText.text = $"x {message.Amount}";

            if (_coinShowOnChangeOnly)
                ShowPanelTemporarily(_coinPanel, CoinIdx);
        }

        private void OnStaminaChanged(StaminaChanged message)
        {
            if (_staminaSlider != null)
            {
                _staminaSlider.maxValue = message.Max;
                _staminaSlider.value = message.Current;
            }

            if (_staminaFillImage != null)
            {
                float percent = message.Max > 0f ? message.Current / message.Max : 1f;
                _staminaFillImage.color = percent < 0.2f ? Color.red : Color.green;
            }

            if (_staminaShowOnChangeOnly)
                ShowPanelTemporarily(_staminaPanel, StaminaIdx);
        }

        private void OnTimerUpdated(LevelTimerUpdated message)
        {
            if (_timerText != null)
            {
                int minutes = Mathf.FloorToInt(message.Time / 60f);
                int seconds = Mathf.FloorToInt(message.Time % 60f);
                _timerText.text = $"{minutes:00}:{seconds:00}";
            }

            if (_timerShowOnChangeOnly)
                ShowPanelTemporarily(_timerPanel, TimerIdx);
        }

        private void OnObjectiveProgress(ObjectiveProgress message)
        {
            if (_objectiveText != null)
                _objectiveText.text = $"{message.Collected} / {message.Total}";

            if (_keyIcons != null)
            {
                for (int i = 0; i < _keyIcons.Length; i++)
                {
                    if (_keyIcons[i] != null)
                        _keyIcons[i].SetActive(i < message.Collected);
                }
            }

            if (_objectiveShowOnChangeOnly)
                ShowPanelTemporarily(_objectivePanel, ObjectiveIdx);
        }

        private void OnGameStateChanged(GameStateChanged message)
        {
            bool show = message.State == GameState.Gameplay;
            FadeAllPanels(show);
        }

        #endregion

        #region Panel Visibility

        private void FadeAllPanels(bool fadeIn)
        {
            FadePanel(_healthPanel, fadeIn);
            FadePanel(_coinPanel, fadeIn);
            FadePanel(_staminaPanel, fadeIn);
            FadePanel(_timerPanel, fadeIn);
            FadePanel(_objectivePanel, fadeIn);
        }

        private void FadePanel(CanvasGroup panel, bool fadeIn)
        {
            if (panel == null) return;

            panel.gameObject.SetActive(true);
            LeanTween.cancel(panel.gameObject);
            LeanTween.alphaCanvas(panel, fadeIn ? 1f : 0f, _fadeDuration)
                .setEase(LeanTweenType.easeInOutQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    if (!fadeIn && !_disposed)
                        panel.gameObject.SetActive(false);
                });
        }

        private void ShowPanelTemporarily(CanvasGroup panel, int index)
        {
            if (panel == null) return;

            _hideTokens[index]++;
            int myToken = _hideTokens[index];

            panel.gameObject.SetActive(true);
            LeanTween.cancel(panel.gameObject);
            LeanTween.alphaCanvas(panel, 1f, _fadeDuration)
                .setEase(LeanTweenType.easeOutQuad)
                .setIgnoreTimeScale(true);

            HidePanelAfterDelay(panel, myToken, index).Forget();
        }

        private async UniTask HidePanelAfterDelay(CanvasGroup panel, int myToken, int index)
        {
            await UniTask.Delay((int)(_showDuration * 1000f),
                ignoreTimeScale: true);

            if (myToken != _hideTokens[index] || _disposed) return;

            LeanTween.cancel(panel.gameObject);
            LeanTween.alphaCanvas(panel, 0f, _fadeDuration)
                .setEase(LeanTweenType.easeInQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    if (!_disposed)
                        panel.gameObject.SetActive(false);
                });
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var d in _subscriptions)
                d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (_subscriptions.Count != 0)
                Dispose();
        }

        #endregion
    }
}   