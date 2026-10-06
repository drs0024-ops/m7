using System;
using Game.Core.Messages;
using MessagePipe;

namespace Game.Gameplay.Enemies.Runtime
{
    public sealed class EnemyDecisionRuntime
    {
        public const string AttackCooldownTimerId = "attack";

        private readonly IPublisher<EnemyStateChanged> _stateChangedPublisher;

        public EnemyDecisionRuntime(IPublisher<EnemyStateChanged> stateChangedPublisher)
        {
            _stateChangedPublisher = stateChangedPublisher
                ?? throw new ArgumentNullException(nameof(stateChangedPublisher));
        }

        public bool TryTransition(EnemyRuntimeContext context, EnemyConditionSnapshot snapshot)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var transitions = context.Definition.StateGraph.Transitions;
            for (int i = 0; i < transitions.Count; i++)
            {
                EnemyTransitionDefinition transition = transitions[i];
                if (transition.SourceStateId != context.ActiveStateId
                    || !IsConditionMet(transition, context, snapshot))
                    continue;

                if (transition.TargetStateId == context.ActiveStateId)
                    continue;

                string previousStateId = context.ActiveStateId;
                context.EnterState(transition.TargetStateId);
                _stateChangedPublisher.Publish(new EnemyStateChanged(
                    context.InstanceId,
                    previousStateId,
                    context.ActiveStateId));
                return true;
            }

            return false;
        }

        private static bool IsConditionMet(
            EnemyTransitionDefinition transition,
            EnemyRuntimeContext context,
            EnemyConditionSnapshot snapshot)
        {
            bool hasTarget = context.TargetEntityId.HasValue;
            float attackRange = context.Definition.Stats.AttackRange;

            return transition.Condition switch
            {
                EnemyConditionType.TargetAcquired => hasTarget,
                EnemyConditionType.TargetLost => !hasTarget,
                EnemyConditionType.TargetInAttackRange => hasTarget && snapshot.TargetDistance <= attackRange,
                EnemyConditionType.TargetOutOfAttackRange => hasTarget && snapshot.TargetDistance > attackRange,
                EnemyConditionType.AttackCooldownElapsed =>
                    context.GetTimer(AttackCooldownTimerId) <= 0f,
                EnemyConditionType.HealthAtOrBelowThreshold =>
                    context.CurrentHealth <= context.Definition.Stats.MaxHealth * transition.Threshold,
                EnemyConditionType.Grounded => snapshot.IsGrounded == true,
                EnemyConditionType.Airborne => snapshot.IsGrounded == false,
                EnemyConditionType.InvestigationComplete => snapshot.InvestigationComplete,
                _ => false
            };
        }
    }
}