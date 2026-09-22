// Centralized input: reads InputSystem actions, exposes snapshot properties, publishes back-button events.
using Game.Core.Messages;
using MessagePipe;
using UnityEngine.InputSystem;
using System;
using Game.Core.Interfaces;
using UnityEngine;
using Game.Core.Enums;

namespace Game.Gameplay.Player
{
    public class InputManager : MonoBehaviour, IInputSwitcher, IDisposable
    {
        private const float DEADZONE_SQR = 0.01f;

        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private InputAction _attackAction;
        private InputAction _interactAction;
        private InputAction _invisibilityAction;
        private InputAction _downAction;
        private InputAction _menuAction;
        private InputAction _jumpActionPlayer;
        private InputAction _jumpActionMenu;
        private InputAction _confirmActionMenu;
        private InputActionMap _playerMap;
        private InputActionMap _menuMap;

        // NOT readonly — deferred pattern
        private IPublisher<InputBackPressed> _backPublisher;
        private bool _disposed;
        private InputAction _moveActionPlayer;
        private InputAction _moveActionMenu;

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

            // ✅ Initialize based on which map is actually enabled
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

        private void Start()  // ← replaces IStartable.Start()
        {
            _backPublisher = GlobalMessagePipe.GetPublisher<InputBackPressed>();
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

        private void Update()
        {
            Debug.Log($"[Input] InstanceID={GetInstanceID()}, MoveWasPressedDown={MoveWasPressedDown}");   
            if (_disposed) return;

            // ── DIAGNOSTIC (remove later) ──
            var moveVal = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            if (moveVal.y != 0f)
            {
                Debug.Log($"[Input] val={moveVal}, wasPressed={_moveAction.WasPressedThisFrame()}, map={_moveAction.actionMap.name}, action={_moveAction.name}");
            }
            // ── END DIAGNOSTIC ──

            Movement = _moveAction != null
                ? (_moveAction.ReadValue<Vector2>().sqrMagnitude < DEADZONE_SQR ? Vector2.zero : _moveAction.ReadValue<Vector2>())
                : Vector2.zero;


            JumpIsHeld = _jumpAction?.IsPressed() ?? false;
            RunIsHeld = _runAction?.IsPressed() ?? false;

            JumpWasPressed = _jumpAction?.WasPressedThisFrame() ?? false;
            JumpWasReleased = _jumpAction?.WasReleasedThisFrame() ?? false;
            AttackWasPressed = _attackAction?.WasPressedThisFrame() ?? false;
            InteractWasPressed = _interactAction?.WasPressedThisFrame() ?? false;
            DownWasPressed = _downAction?.WasPressedThisFrame() ?? false;
            InvisibilityWasPressed = _invisibilityAction?.WasPressedThisFrame() ?? false;

            EscapeWasPressed = _menuAction?.WasPressedThisFrame() ?? false;
            if (EscapeWasPressed && _backPublisher != null)
                _backPublisher.Publish(default);

            EscapeIsHeld = _menuAction?.IsPressed() ?? false;

            MoveWasPressedUp = _moveAction != null && _moveAction.ReadValue<Vector2>().y > 0.5f && _moveAction.WasPressedThisFrame();
            MoveWasPressedDown = _moveAction != null && _moveAction.ReadValue<Vector2>().y < -0.5f && _moveAction.WasPressedThisFrame();
            ConfirmWasPressed = _confirmActionMenu?.WasPressedThisFrame() ?? false;

            // Log which maps are currently enabled:
            foreach (var map in _playerInput.actions.actionMaps)
            {
                if (map.enabled)
                    Debug.Log($"[Input] Active map: {map.name}");
            }
        }

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
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        private void OnDestroy()
        {
            if (!_disposed) Dispose();
        }

        public void SwitchToMenu()
        {
            _playerMap.Disable();
            _menuMap.Enable();
            _moveAction = _moveActionMenu;
            _jumpAction = _jumpActionMenu;
        }

        public void SwitchToPlayer()
        {
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
    }
}   