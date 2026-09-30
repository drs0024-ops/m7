using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Interfaces;
using Game.Core.Enums;
using MessagePipe;
using Game.Core.Messages;
using VContainer;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Bridges the Unity Input System to the rest of the game.
    /// Exposes per-frame input state via properties and handles
    /// action-map switching between gameplay and menu contexts.
    /// </summary>
    public class InputManager : MonoBehaviour, IInputSwitcher, IInputState, IDisposable
    {
        #region Constants

        private const float DEADZONE_SQR = 0.01f;

        #endregion

        #region Dependencies

        [Inject]
        private IPublisher<InputBackPressed> _backPublisher;

        #endregion

        #region Input Actions

        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private InputAction _attackAction;
        private InputAction _interactAction;
        private InputAction _invisibilityAction;
        private InputAction _downAction;
        private InputAction _menuAction;
        private InputAction _confirmActionMenu;
        private InputActionMap _playerMap;
        private InputActionMap _menuMap;
        private InputAction _moveActionPlayer;
        private InputAction _moveActionMenu;
        private InputAction _jumpActionPlayer;
        private InputAction _jumpActionMenu;

        #endregion

        #region State

        private bool _disposed;

        #endregion

        #region Public API

        public PlayerInput PlayerInput => _playerInput;

        public Vector2 Movement { get; private set; }
        public bool JumpWasPressed { get; private set; }
        public bool JumpIsHeld { get; private set; }
        public bool JumpWasReleased { get; private set; }
        public bool RunIsHeld { get; private set; }
        public bool AttackWasPressed { get; private set; }
        public bool InteractWasPressed { get; private set; }
        public bool InvisibilityWasPressed { get; private set; }
        public bool DownWasPressed { get; private set; }
        public bool EscapeWasPressed { get; private set; }
        public bool EscapeIsHeld { get; private set; }
        public bool ConfirmWasPressed { get; private set; }
        public bool MoveWasPressedUp { get; private set; }
        public bool MoveWasPressedDown { get; private set; }

        public bool IsInvisibilityPressed {get; private set;}

        public void ResetAll()
        {
            Movement = Vector2.zero;
            JumpWasPressed = false;
            JumpIsHeld = false;
            JumpWasReleased = false;
            RunIsHeld = false;
            AttackWasPressed = false;
            InteractWasPressed = false;
            InvisibilityWasPressed = false;
            DownWasPressed = false;
            EscapeWasPressed = false;
            EscapeIsHeld = false;
            ConfirmWasPressed = false;
            MoveWasPressedUp = false;
            MoveWasPressedDown = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        #endregion

        #region IInputSwitcher

        public void SwitchToMenu()
        {
            ResetAll();
            _playerMap.Disable();
            _menuMap.Enable();
            _moveAction = _moveActionMenu;
            _jumpAction = _jumpActionMenu;
        }

        public void SwitchToPlayer()
        {
            ResetAll();
            _menuMap.Disable();
            _playerMap.Enable();
            _moveAction = _moveActionPlayer;
            _jumpAction = _jumpActionPlayer;
        }

        public void SwitchTo(GameState state)
        {
            switch (state)
            {
                case GameState.Gameplay:
                    SwitchToPlayer();
                    break;
                default:
                    SwitchToMenu();
                    break;
            }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("[InputManager] PlayerInput component missing!", this);
                enabled = false;
#if UNITY_EDITOR
                throw new InvalidOperationException("InputManager requires a PlayerInput component.");
#else
                return;
#endif
            }

            var actions = _playerInput.actions;
            _playerMap = actions.FindActionMap("Player");
            _menuMap = actions.FindActionMap("Menu");

            _moveActionPlayer = _playerMap?.FindAction("Move");
            _moveActionMenu = _menuMap?.FindAction("Move");

            _jumpActionPlayer = _playerMap?.FindAction("Jump");
            _jumpActionMenu = _menuMap?.FindAction("Jump");

            _runAction = _playerMap?.FindAction("Run");
            _attackAction = _playerMap?.FindAction("Attack");
            _interactAction = _playerMap?.FindAction("Interact");
            _invisibilityAction = _playerMap?.FindAction("Invisable");
            _downAction = _playerMap?.FindAction("Down");
            _menuAction = _menuMap?.FindAction("Cancel");
            _confirmActionMenu = _menuMap?.FindAction("Confirm");

            // Initialize based on which map is actually enabled
            if (_menuMap != null && _menuMap.enabled)
            {
                _moveAction = _moveActionMenu;
                _jumpAction = _jumpActionMenu;
            }
            else
            {
                _moveAction = _moveActionPlayer;
                _jumpAction = _jumpActionPlayer;
            }
        }

        private void OnEnable()
        {
            _playerInput?.actions.FindActionMap("Player")?.Enable();
            _moveAction?.Enable();
            _jumpAction?.Enable();
            _runAction?.Enable();
            _attackAction?.Enable();
            _interactAction?.Enable();
            _invisibilityAction?.Enable();
            _downAction?.Enable();
            _menuAction?.Enable();
            _confirmActionMenu?.Enable();
        }

        private void OnDisable()
        {
            ResetAll();

            _moveAction?.Disable();
            _jumpAction?.Disable();
            _runAction?.Disable();
            _attackAction?.Disable();
            _interactAction?.Disable();
            _invisibilityAction?.Disable();
            _downAction?.Disable();
            _menuAction?.Disable();
            _playerInput?.actions.FindActionMap("Player")?.Disable();
            _confirmActionMenu?.Disable();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Per-Frame

        private void Update()
        {
            if (_disposed) return;

            // Read move value once
            Vector2 moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            Movement = moveInput.sqrMagnitude < DEADZONE_SQR ? Vector2.zero : moveInput;

            JumpIsHeld = _jumpAction?.IsPressed() ?? false;
            RunIsHeld = _runAction?.IsPressed() ?? false;

            JumpWasPressed = _jumpAction?.WasPressedThisFrame() ?? false;
            JumpWasReleased = _jumpAction?.WasReleasedThisFrame() ?? false;
            AttackWasPressed = _attackAction?.WasPressedThisFrame() ?? false;
            InteractWasPressed = _interactAction?.WasPressedThisFrame() ?? false;
            DownWasPressed = _downAction?.WasPressedThisFrame() ?? false;
            InvisibilityWasPressed = _invisibilityAction?.WasPressedThisFrame() ?? false;

            EscapeWasPressed = _menuAction?.WasPressedThisFrame() ?? false;
            if (EscapeWasPressed)
                _backPublisher.Publish(default);

            EscapeIsHeld = _menuAction?.IsPressed() ?? false;

            MoveWasPressedUp = _moveAction != null && moveInput.y > 0.5f && _moveAction.WasPressedThisFrame();
            MoveWasPressedDown = _moveAction != null && moveInput.y < -0.5f && _moveAction.WasPressedThisFrame();

            ConfirmWasPressed = _confirmActionMenu?.WasPressedThisFrame() ?? false;
        }

        #endregion
    }
}   