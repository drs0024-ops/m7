using Game.Core.StateMachine;
namespace Game.Gameplay.Enemies
{
    public abstract class EnemyBaseState : State
    {
        protected readonly Enemy enemy;

        protected EnemyBaseState(State parent, Enemy enemy)
            : base(parent)
        {
            this.enemy = enemy;
        }

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnUpdate(float deltaTime) { }
        protected virtual void OnFixedUpdate(float deltaTime) { }
        protected virtual State GetTransition() => null;
        protected virtual State GetInitialState() => null;

        public override void Enter() { base.Enter(); OnEnter(); }
        public override void Exit() { OnExit(); base.Exit(); }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            OnUpdate(deltaTime);
            var target = GetTransition();
            if (target != null)
                Machine.Sequencer.RequestTransition(Leaf(), target);
        }

        public override void FixedUpdate(float deltaTime)
        {
            base.FixedUpdate(deltaTime);
            OnFixedUpdate(deltaTime);
        }
    }
}   