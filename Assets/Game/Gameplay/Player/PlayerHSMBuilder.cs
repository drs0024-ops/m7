using Game.Core.Messages;
using Game.Core.StateMachine;
using MessagePipe;

namespace Game.Gameplay.Player
{
    public class PlayerHSMBuilder
    {
        public (PlayerController Controller, StateMachine Machine) Build(
            PlayerContext context,
            PlayerDependencies deps,
            InputManager inputManager,
            IPublisher<PlayerLanded> landedPublisher,
            IPublisher<PlayerJumped> jumpedPublisher,
            IPublisher<PlayerDoubleJumped> doubleJumpedPublisher,
            ISubscriber<BouncePlatformHit> bounceHitSubscriber)
        {
            var controller = new PlayerController(context, deps, inputManager,
                bounceHitSubscriber);

            var rootState = new PlayerRoot(controller, inputManager,
                landedPublisher, jumpedPublisher, doubleJumpedPublisher);

            var machine = new StateMachineBuilder(rootState).Build();
            controller.SetMachine(machine);
            return (controller, machine);
        }
    }
}   