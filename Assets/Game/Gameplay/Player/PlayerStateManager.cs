using System;
using Game.Core.Enums;
using Game.Core.Messages;
using Game.Core.StateMachine;
using MessagePipe;
using VContainer;

namespace Game.Gameplay.Player
{
    public class PlayerStateManager : IDisposable
    {
        public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

        private readonly StateMachine _machine;
        private readonly IPublisher<PlayerStateChanged> _playerStateChangedPublisher;
        private bool _disposed;

        [Inject]
        public PlayerStateManager(StateMachine machine)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _playerStateChangedPublisher = GlobalMessagePipe.GetPublisher<PlayerStateChanged>();

            if (_machine.Sequencer != null)
                _machine.Sequencer.OnStateChange += OnHsmStateChange;
        }

        private void OnHsmStateChange(State newState)
        {
            if (_disposed || newState == null) return;

            var mappedState = MapStateToEnum(newState);
            if (CurrentState != mappedState)
            {
                CurrentState = mappedState;
                _playerStateChangedPublisher.Publish(new PlayerStateChanged(CurrentState));
            }
        }

        public void RequestState(PlayerState newState)
        {
            if (_disposed || CurrentState == newState || _machine.Root == null) return;

            var targetState = GetHsmStateFromEnum(newState);
            if (targetState != null)
            {
                var currentState = _machine.Root.Leaf();
                if (currentState != targetState)
                    _machine.Sequencer.RequestTransition(currentState, targetState);
            }
        }

        private PlayerState MapStateToEnum(State state)
        {
            return state switch
            {
                IdleState _ => PlayerState.Idle,
                WalkState _ => PlayerState.Walking,
                RunState _ => PlayerState.Running,
                AirborneState _ => PlayerState.Airborne,
                _ => PlayerState.Idle
            };
        }

        private State GetHsmStateFromEnum(PlayerState state)
        {
            if (_machine.Root is PlayerRoot root)
            {
                return state switch
                {
                    PlayerState.Idle or PlayerState.Walking or PlayerState.Running => root.Grounded,
                    PlayerState.Airborne or PlayerState.Jumping => root.Airborne,
                    _ => null
                };
            }
            return null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_machine.Sequencer != null)
                _machine.Sequencer.OnStateChange -= OnHsmStateChange;
            _disposed = true;
        }
    }
}   