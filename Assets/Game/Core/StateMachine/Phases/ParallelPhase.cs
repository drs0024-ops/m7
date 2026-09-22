using System;
using System.Collections.Generic;

/// <summary>
/// Executes a list of actions in parallel (same frame).
/// Useful for firing multiple events simultaneously.
/// </summary>

namespace Game.Core.StateMachine.Phases
{
    public class ParallelPhase : ISequence
    {
        private readonly List<Action> _steps;
        public bool IsDone { get; private set; }

        public ParallelPhase(List<Action> steps)
        {
            _steps = steps;
        }

        public void Start()
        {
            if (_steps == null || _steps.Count == 0)
            {
                IsDone = true;
                return;
            }
            for (int i = 0; i < _steps.Count; i++)
                _steps[i]();
            IsDone = true;
        }

        public bool Update() => IsDone;
    }
}   