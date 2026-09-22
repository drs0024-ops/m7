using System;
using System.Collections.Generic;
/// <summary>
/// Base class for all states in the HSM.
/// Manages lifecycle hooks, hierarchy traversal, and activity attachment.
/// </summary>


namespace Game.Core.StateMachine
{
    public class State
    {
        public StateMachine Machine { get; internal set; }
        public State Parent { get; }
        public string Name => GetType().Name;

        private readonly List<State> _children = new List<State>(4);
        private readonly List<IActivity> _activities = new List<IActivity>(4);
        private readonly List<Action> _enterActions = new List<Action>(4);
        private readonly List<Action> _exitActions = new List<Action>(4);

        public IReadOnlyList<State> Children => _children;
        public IReadOnlyList<IActivity> Activities => _activities;
        public IReadOnlyList<Action> EnterActions => _enterActions;
        public IReadOnlyList<Action> ExitActions => _exitActions;

        public State ActiveChild { get; internal set; }
        
        public State() { }
        public State(State parent)
        {
            Parent = parent;
            if (parent != null) parent._children.Add(this);
        }

        public virtual void Enter()
        {
            for (int i = 0; i < _enterActions.Count; i++)
                _enterActions[i]();
        }

        public virtual void Exit()
        {
            for (int i = 0; i < _exitActions.Count; i++)
                _exitActions[i]();
        }

        public virtual void Update(float deltaTime)
        {
            var leaf = Leaf();
            if (leaf != null) leaf.OnUpdate(deltaTime);
        }

        public virtual void FixedUpdate(float deltaTime)
        {
            var leaf = Leaf();
            if (leaf != null) leaf.OnFixedUpdate(deltaTime);
        }

        internal virtual void OnUpdate(float deltaTime) { }
        internal virtual void OnFixedUpdate(float deltaTime) { }

        public State AddActivity(IActivity activity)
        {
            _activities.Add(activity);
            return this;
        }

        public State OnEnter(System.Action action)
        {
            _enterActions.Add(action);
            return this;
        }

        public State OnExit(System.Action action)
        {
            _exitActions.Add(action);
            return this;
        }

        public State Leaf()
        {
            if (_children.Count == 0) return this;
            return _children[0].Leaf();
        }
        internal void PropagateMachine(StateMachine machine)
        {
            Machine = machine;
            for (int i = 0; i < _children.Count; i++)
                _children[i].PropagateMachine(machine);
        }

    }

}  