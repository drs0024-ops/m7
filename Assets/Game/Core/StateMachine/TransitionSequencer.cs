using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.StateMachine.Phases;

/// <summary>
/// Manages asynchronous-like state transitions using synchronous steps.
/// Handles deactivation, hierarchy switch, and activation without GC allocation.
/// </summary>
namespace Game.Core.StateMachine
{
    public class TransitionSequencer
    {
        public readonly StateMachine Machine;
        public event Action<State> OnStateChange;

        private ISequence _currentPhase;
        private Action _nextPhase;
        private (State from, State to)? _pending;
        private State _lastTo;

        // Instance buffers — safe for multiple concurrent machines
        private readonly List<State> _exitBuffer = new List<State>(16);
        private readonly List<State> _enterBuffer = new List<State>(16);
        private readonly List<Action> _stepBuffer = new List<Action>(16);
        private readonly Stack<State> _reverseBuffer = new Stack<State>(16);

        public bool IsTransitioning => _currentPhase != null;

        public TransitionSequencer(StateMachine machine)
        {
            Machine = machine;
        }

        public void RequestTransition(State from, State to)
        {
            if (to == null || from == to) return;

            if (_currentPhase != null)
            {
                _pending = (from, to);
                return;
            }

            BeginTransition(from, to);
        }

        private void BeginTransition(State from, State to)
        {
            _lastTo = to;
            var lca = HsmUtility.Lca(from, to);

            _exitBuffer.Clear();
            _enterBuffer.Clear();

            for (var s = from; s != null && s != lca; s = s.Parent)
                _exitBuffer.Add(s);

            _reverseBuffer.Clear();
            for (var s = to; s != lca; s = s.Parent)
                _reverseBuffer.Push(s);
            while (_reverseBuffer.Count > 0)
                _enterBuffer.Add(_reverseBuffer.Pop());

            // Phase 1: Deactivate old activities
            _stepBuffer.Clear();
            for (int i = 0; i < _exitBuffer.Count; i++)
            {
                var activities = _exitBuffer[i].Activities;
                for (int j = 0; j < activities.Count; j++)
                {
                    var act = activities[j];
                    _stepBuffer.Add(() => act.Deactivate());
                }
            }

            _currentPhase = new SequentialPhase(_stepBuffer);
            _currentPhase.Start();

            _nextPhase = () =>
            {
                Machine.ChangeState(from, to);

                // Phase 2: Activate new activities
                _stepBuffer.Clear();
                for (int i = 0; i < _enterBuffer.Count; i++)
                {
                    var activities = _enterBuffer[i].Activities;
                    for (int j = 0; j < activities.Count; j++)
                    {
                        var act = activities[j];
                        _stepBuffer.Add(() => act.Activate());
                    }
                }

                _currentPhase = new SequentialPhase(_stepBuffer);
                _currentPhase.Start();
                _nextPhase = null;
            };
        }

        private void EndTransition()
        {
            _currentPhase = null;
            OnStateChange?.Invoke(_lastTo);

            if (_pending.HasValue)
            {
                var p = _pending.Value;
                _pending = null;
                BeginTransition(p.from, p.to);
            }
        }

        public void Tick(float deltaTime)
        {
            if (_currentPhase != null)
            {
                if (_currentPhase.Update())
                {
                    if (_nextPhase != null)
                    {
                        var n = _nextPhase;
                        _nextPhase = null;
                        n();
                    }
                    else
                    {
                        EndTransition();
                    }
                }
                return;
            }

            Machine.InternalTick(deltaTime);
        }

        public void TickFixedUpdate(float deltaTime)
        {
            if (_currentPhase != null) return;
            Machine.InternalTickFixedUpdate(deltaTime);
        }
    }
}