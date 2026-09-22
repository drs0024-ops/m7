using System.Collections.Generic;
    
/// <summary>
/// Core controller for the Hierarchical State Machine.
/// Manages the state tree lifecycle, delegates updates, and orchestrates transitions.
/// </summary>
namespace Game.Core.StateMachine
{
    public class StateMachine
    {
        public readonly State Root;
        public readonly TransitionSequencer Sequencer;
        private bool _started;
        private readonly Stack<State> _enterPath = new Stack<State>(16);

        public StateMachine(State root)
        {
            Root = root;
            if (Root != null) Root.PropagateMachine(this);
            Sequencer = new TransitionSequencer(this);
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            Root?.Enter();

            // Initialize ActiveChild chain
            State child = Root;
            while (child.ActiveChild == null && child.Children.Count > 0)
            {
                child.ActiveChild = child.Children[0];
                child = child.ActiveChild;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_started) Start();
            Sequencer.Tick(deltaTime);
        }

        public void TickFixedUpdate(float deltaTime)
        {
            Sequencer.TickFixedUpdate(deltaTime);
        }

        internal void InternalTick(float deltaTime) => Root?.Update(deltaTime);
        internal void InternalTickFixedUpdate(float deltaTime) => Root?.FixedUpdate(deltaTime);

        internal void ChangeState(State from, State to)
        {
            if (from == to || from == null || to == null) return;

            State lca = HsmUtility.Lca(from, to);
            if (lca == null) return;

            // Exit: bottom-up from `from` to LCA (exclusive)
            int depth = 0;
            for (State s = from; s != lca; s = s.Parent)
            {
                if (++depth > 64) return;
                s.Exit();
            }

            // Enter: top-down from LCA (exclusive) to `to`
            _enterPath.Clear();
            for (State s = to; s != lca; s = s.Parent)
                _enterPath.Push(s);

            while (_enterPath.Count > 0)
                _enterPath.Pop().Enter();

            // Update ActiveChild pointers along the new path (LCA → to)
            if (to != lca)
            {
                State current = to;
                while (current.Parent != null && current.Parent != lca)
                {
                    current.Parent.ActiveChild = current;
                    current = current.Parent;
                }
                if (current.Parent == lca)
                    lca.ActiveChild = current;
            }
        }   
    }
}
