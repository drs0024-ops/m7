using Game.Core.Messages;
using MessagePipe;
using Game.Core.StateMachine;

namespace Game.Gameplay.Player
{
    public class PlayerRoot : PlayerBaseState
    {
        public readonly GroundedState Grounded;
        public readonly AirborneState Airborne;

        public PlayerRoot(PlayerController pc, InputManager input,
            IPublisher<PlayerLanded> landedPublisher,
            IPublisher<PlayerJumped> jumpedPublisher,
            IPublisher<PlayerDoubleJumped> doubleJumpedPublisher)
            : base(null, pc, input)
        {
            Grounded = new GroundedState(this, pc, input, landedPublisher);
            Airborne = new AirborneState(this, pc, input, jumpedPublisher, doubleJumpedPublisher);
        }

        protected override State GetInitialState() => Grounded;

        public void EvaluateInitial()
        {
            Machine.Sequencer.RequestTransition(this, CheckGrounded() ? Grounded : Airborne);
        }
    }
}   