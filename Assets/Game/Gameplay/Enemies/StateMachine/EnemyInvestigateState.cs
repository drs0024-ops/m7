using Game.Core.StateMachine;

namespace Game.Gameplay.Enemies
{
    /// <summary>Moves an enemy toward an active invisibility broadcast.</summary>
    public sealed class EnemyInvestigateState : EnemyBaseState
    {
        /// <summary>Creates an investigation state for its enemy FSM.</summary>
        public EnemyInvestigateState(State parent, Enemy enemy) : base(parent, enemy) { }

        protected override void OnEnter() => enemy.SetAnimation("investigate");

        protected override void OnFixedUpdate(float deltaTime) =>
            enemy.MoveToInvestigationTarget(deltaTime);

        protected override State GetTransition() =>
            enemy.IsInvestigating ? null : ((EnemyGroundedState)Parent).Idle;
    }
}