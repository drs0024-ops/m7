using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core.Enums;
using Game.Core.Interfaces;
using Game.Core.Messages;
using Game.Gameplay.Player;
using Game.Gameplay.Save;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Game.UI
{
    /// <summary>
    /// Controller for the MainMenu panel (lives in MainMenu scene).
    /// Handles New Game, Load Game, Options, and Exit navigation.
    /// Options is a panel in the Bootstrap scene — opened/closed via messages.
    /// New Game / Load Game use CRT transition (CRTStateRequested).
    /// Drives CRT idle/hidden state based on game phase.
    /// </summary>
    public class MenuMaster : MonoBehaviour, IDisposable
    {
        #region Fields

        [Header("Panels")]
        [SerializeField] private GameObject _mainMenuPanel;

        [Header("CRT")]
        [SerializeField] private CRTBootAnimation _crtBoot;

        [Header("Dependencies")]
        [Inject] private InputManager _inputManager;
        [Inject] private BootTextController _bootText;
        [Inject] private IAudioManager _audio;
        [Inject] private SaveManager _saveManager;

        [Header("Transition")]
        [SerializeField] private bool _useCRTTransition = true;
        [SerializeField] private float _transitionTimeout = 3f;

        private IPublisher<StartGameRequested> _startGamePublisher;
        private IPublisher<LoadRequest> _loadRequestPublisher;
        private IPublisher<CRTStateRequested> _crtPublisher;
        private IPublisher<OptionsOpenRequested> _optionsOpenPublisher;

        private readonly List<IDisposable> _subscriptions = new(3);
        private bool _disposed;
        private bool _transitioning;
        private float _transitionTimer;
        private bool _wasInGameplay;
        private bool _menuShown;
        private Action _pendingSwap;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _startGamePublisher = GlobalMessagePipe.GetPublisher<StartGameRequested>();
            _loadRequestPublisher = GlobalMessagePipe.GetPublisher<LoadRequest>();
            _crtPublisher = GlobalMessagePipe.GetPublisher<CRTStateRequested>();
            _optionsOpenPublisher = GlobalMessagePipe.GetPublisher<OptionsOpenRequested>();

            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<ScreenSwapped>()
                .Subscribe(OnScreenSwapped));

            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<GamePhaseChangedMessage>()
                .Subscribe(OnPhaseChanged));

            _subscriptions.Add(GlobalMessagePipe.GetSubscriber<OptionsPanelStateChanged>()
                .Subscribe(msg =>
                {
                    if (!msg.IsOpen)
                        TransitionTo(() => _mainMenuPanel.SetActive(true));
                }));

            ShowMainMenu(_wasInGameplay).Forget();
        }

        private void Update()
        {
            if (_disposed) return;

            if (_transitioning)
            {
                _transitionTimer += Time.unscaledDeltaTime;
                if (_transitionTimer >= _transitionTimeout)
                {
                    Debug.LogWarning("[Menu] Transition timed out — forcing recovery.");
                    ForceCompleteTransition();
                }
                return;
            }

            HandleSelecting();
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
            _audio.StopMusic();
        }

        #endregion

        #region Transition

        private void TransitionTo(Action swapAction)
        {
            if (_transitioning) return;

            if (!_useCRTTransition)
            {
                swapAction();
                return;
            }

            _transitioning = true;
            _transitionTimer = 0f;
            _pendingSwap = swapAction;
            _crtPublisher.Publish(new CRTStateRequested(CRTState.Transition));
        }

        private void OnScreenSwapped(ScreenSwapped _)
        {
            if (!_transitioning) return;
            ForceCompleteTransition();
        }

        private void ForceCompleteTransition()
        {
            _pendingSwap?.Invoke();
            _pendingSwap = null;
            _transitioning = false;
            _transitionTimer = 0f;
        }

        #endregion

        #region Input Handling

        private void HandleSelecting()
        {
            if (!_bootText.IsSelecting || !_inputManager.ConfirmWasPressed) return;

            switch (_bootText.GetSelectedAction())
            {
                case 0: OnNewGameClicked();  break;
                case 1: OnLoadGameClicked(); break;
                case 2: OnOptionsClicked();  break;
                case 3: OnExitClicked();     break;
            }
        }

        #endregion

        #region Phase Handling

        private void OnPhaseChanged(GamePhaseChangedMessage msg)
        {
            if (_disposed) return;

            bool isInGameplay = (msg.NewPhase & GamePhase.Gameplay) != 0
                             || (msg.NewPhase & GamePhase.Climax) != 0
                             || (msg.NewPhase & GamePhase.Ending) != 0;

            if (isInGameplay)
            {
                _wasInGameplay = true;
                _menuShown = false;
                SetActiveSafe(_mainMenuPanel, false);
                _crtPublisher.Publish(new CRTStateRequested(CRTState.Hidden));
                _audio.StopMusic();
            }
            else if ((msg.NewPhase & GamePhase.Letter) == 0 && !_menuShown)
            {
                ShowMainMenu(_wasInGameplay).Forget();
                _wasInGameplay = false;
            }
        }

        #endregion

        #region Button Callbacks

        public void OnNewGameClicked()
        {
            if (_disposed) return;
            _audio.PlaySFX(SoundType.MenuClick);
            TransitionTo(() => _startGamePublisher.Publish(default));
        }

        public void OnLoadGameClicked()
        {
            if (_disposed) return;
            _audio.PlaySFX(SoundType.MenuClick);
            TransitionTo(() => _loadRequestPublisher.Publish(default));
        }

        public void OnOptionsClicked()
        {
            if (_disposed) return;
            _audio.PlaySFX(SoundType.MenuClick);
            TransitionTo(() =>
            {
                _mainMenuPanel.SetActive(false);
                _optionsOpenPublisher.Publish(default);
            });
        }

        public void OnExitClicked()
        {
            if (_disposed) return;
            _audio.PlaySFX(SoundType.MenuClick);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Panel Toggle

        private async UniTaskVoid ShowMainMenu(bool fromGameplay)
        {
            if (_menuShown || _disposed) return;
            _menuShown = true;

            try
            {
                _mainMenuPanel.SetActive(true);

                _audio.PlayMusic(MusicType.MainMenu);
                await _crtBoot.Play();
                await UniTask.Delay((int)(_crtBoot.PostDelay * 1000), ignoreTimeScale: true);

                if (_disposed) return;

                _crtPublisher.Publish(new CRTStateRequested(CRTState.MenuIdle));

                if (fromGameplay)
                    _bootText.SetReturnTitle(_saveManager.HasSave
                        ? "SYSTEM BOOT: SAVE DETECTED"
                        : "SYSTEM BOOT: NORMAL");

                _bootText.StartSequence(fromGameplay);

                if (!_saveManager.HasSave)
                    _bootText.SetItemEnabled(1, false);

                _audio.PlayMusic(MusicType.MainMenu);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Menu] ShowMainMenu failed: {e}");
            }
        }

        #endregion

        #region Utility

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        #endregion
    }
}   