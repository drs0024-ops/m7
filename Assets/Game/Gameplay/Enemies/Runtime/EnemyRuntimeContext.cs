using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    public sealed class EnemyRuntimeContext
    {
        private readonly Dictionary<string, float> _timers = new(StringComparer.Ordinal);

        public EnemyInstanceId InstanceId { get; }
        public EnemyDefinition Definition { get; }
        public string ActiveStateId { get; private set; }
        public float CurrentHealth { get; private set; }
        public int? TargetEntityId { get; private set; }
        public Vector2 LastKnownTargetPosition { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        public EnemyRuntimeContext(EnemyInstanceId instanceId, EnemyDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (!definition.TryValidate(out string error))
                throw new ArgumentException($"Invalid enemy definition: {error}", nameof(definition));

            InstanceId = instanceId;
            Definition = definition;
            CurrentHealth = definition.Stats.MaxHealth;
            ActiveStateId = definition.StateGraph.GetInitialLeafStateId();
        }

        public void EnterState(string stateId)
        {
            if (!Definition.StateGraph.ContainsState(stateId))
                throw new ArgumentException($"State '{stateId}' is not in '{Definition.TypeId}'.", nameof(stateId));

            ActiveStateId = stateId;
        }

        public void SetTarget(int targetEntityId, Vector2 lastKnownPosition)
        {
            TargetEntityId = targetEntityId;
            LastKnownTargetPosition = lastKnownPosition;
        }

        public void ClearTarget() => TargetEntityId = null;

        public void SetTimer(string timerId, float remainingSeconds)
        {
            if (string.IsNullOrWhiteSpace(timerId))
                throw new ArgumentException("Timer ID is required.", nameof(timerId));
            if (remainingSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(remainingSeconds));

            _timers[timerId] = remainingSeconds;
        }

        public float GetTimer(string timerId) =>
            _timers.TryGetValue(timerId, out float remainingSeconds) ? remainingSeconds : 0f;

        public bool ApplyDamage(float amount)
        {
            if (amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount));

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            return IsDead;
        }
    }
}