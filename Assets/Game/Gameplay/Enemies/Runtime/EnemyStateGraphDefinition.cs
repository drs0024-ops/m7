using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    [Serializable]
    public sealed class EnemyTransitionDefinition
    {
        [SerializeField] private string _sourceStateId;
        [SerializeField] private string _targetStateId;
        [SerializeField] private EnemyConditionType _condition;
        [SerializeField] private float _threshold;

        public string SourceStateId => _sourceStateId;
        public string TargetStateId => _targetStateId;
        public EnemyConditionType Condition => _condition;
        public float Threshold => _threshold;
    }

    [CreateAssetMenu(fileName = "EnemyStateGraph", menuName = "Game/Enemies/State Graph")]
    public sealed class EnemyStateGraphDefinition : ScriptableObject
    {
        [SerializeField] private string _initialStateId;
        [SerializeField] private EnemyStateNodeDefinition[] _states = Array.Empty<EnemyStateNodeDefinition>();
        [SerializeField] private EnemyTransitionDefinition[] _transitions = Array.Empty<EnemyTransitionDefinition>();

        public string InitialStateId => _initialStateId;
        public IReadOnlyList<EnemyStateNodeDefinition> States => _states;
        public IReadOnlyList<EnemyTransitionDefinition> Transitions => _transitions;

        public bool ContainsState(string stateId)
        {
            if (string.IsNullOrWhiteSpace(stateId)) return false;
            for (int i = 0; i < _states.Length; i++)
                if (_states[i] != null && _states[i].Id == stateId)
                    return true;
            return false;
        }

        public string GetInitialLeafStateId()
        {
            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);

            EnemyStateNodeDefinition current = FindState(_initialStateId);
            while (!string.IsNullOrEmpty(current.InitialChildId))
                current = FindState(current.InitialChildId);
            return current.Id;
        }

        public bool TryValidate(out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(_initialStateId))
                return Fail("Initial state ID is required.", out error);
            if (_states == null || _states.Length == 0)
                return Fail("At least one state is required.", out error);

            var statesById = new Dictionary<string, EnemyStateNodeDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < _states.Length; i++)
            {
                EnemyStateNodeDefinition state = _states[i];
                if (state == null || string.IsNullOrWhiteSpace(state.Id))
                    return Fail($"State at index {i} has no ID.", out error);
                if (!statesById.TryAdd(state.Id, state))
                    return Fail($"Duplicate state ID '{state.Id}'.", out error);
            }

            if (!statesById.TryGetValue(_initialStateId, out EnemyStateNodeDefinition initialState))
                return Fail($"Initial state '{_initialStateId}' does not exist.", out error);
            if (!string.IsNullOrEmpty(initialState.ParentId))
                return Fail("Initial state must be a root state.", out error);

            foreach (EnemyStateNodeDefinition state in _states)
            {
                if (!string.IsNullOrEmpty(state.ParentId) && !statesById.ContainsKey(state.ParentId))
                    return Fail($"Parent state '{state.ParentId}' for '{state.Id}' does not exist.", out error);

                if (!string.IsNullOrEmpty(state.InitialChildId)
                    && (!statesById.TryGetValue(state.InitialChildId, out EnemyStateNodeDefinition child)
                        || child.ParentId != state.Id))
                    return Fail($"Initial child '{state.InitialChildId}' is not a child of '{state.Id}'.", out error);

                var visited = new HashSet<string>(StringComparer.Ordinal) { state.Id };
                string parentId = state.ParentId;
                while (!string.IsNullOrEmpty(parentId))
                {
                    if (!visited.Add(parentId))
                        return Fail($"State hierarchy contains a cycle at '{parentId}'.", out error);
                    parentId = statesById[parentId].ParentId;
                }
            }

            if (_transitions == null)
                return Fail("Transition list cannot be null.", out error);

            for (int i = 0; i < _transitions.Length; i++)
            {
                EnemyTransitionDefinition transition = _transitions[i];
                if (transition == null
                    || !statesById.ContainsKey(transition.SourceStateId)
                    || !statesById.ContainsKey(transition.TargetStateId))
                    return Fail($"Transition at index {i} references a missing state.", out error);
            }

            return true;
        }

        private EnemyStateNodeDefinition FindState(string id)
        {
            for (int i = 0; i < _states.Length; i++)
                if (_states[i].Id == id)
                    return _states[i];
            throw new InvalidOperationException($"State '{id}' was not found after graph validation.");
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}