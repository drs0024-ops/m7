using System;
using System.Collections.Generic;



/// <summary>
/// Executes a list of actions sequentially on the Main Thread.
/// Zero allocation after initialization.
/// </summary>
namespace Game.Core.StateMachine.Phases
{
    public class SequentialPhase : ISequence
    {
        private readonly List<Action> _steps;
        private int _index = -1;
        public bool IsDone { get; private set; }

        public SequentialPhase(List<Action> steps)
        {
            _steps = steps;
        }

        public void Start() => Next();

        private void Next()
        {
            _index++;
            if (_index >= _steps.Count)
            {
                IsDone = true;
                return;
            }
            _steps[_index]();
            Next();
        }

        public bool Update() => IsDone;
    }
}
