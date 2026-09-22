// Owns the current GamePhase and publishes GamePhaseChangedMessage on transition.
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public class GamePhaseController : IStartable
    {
        private IPublisher<GamePhaseChangedMessage> _publisher;
        private GamePhase _currentPhase = GamePhase.None;

        public GamePhase CurrentPhase => _currentPhase;
        
        public bool IsInputEnabled =>
            !CurrentPhase.HasFlag(GamePhase.Cinematic)
            && !CurrentPhase.HasFlag(GamePhase.Loading);

        [Inject]
        public GamePhaseController() { }

        void IStartable.Start()
        {
            _publisher = GlobalMessagePipe.GetPublisher<GamePhaseChangedMessage>();
        }

        public void SetPhase(GamePhase newPhase)
        {
            if (_currentPhase == newPhase) return;

            _currentPhase = newPhase;
            _publisher.Publish(new GamePhaseChangedMessage(newPhase));
        }

        public void AddPhase(GamePhase phase)
        {
            GamePhase newPhase = _currentPhase | phase;
            if (newPhase == _currentPhase) return;

            _currentPhase = newPhase;
            _publisher.Publish(new GamePhaseChangedMessage(newPhase));
        }

        public void RemovePhase(GamePhase phase)
        {
            GamePhase newPhase = _currentPhase & ~phase;
            if (newPhase == _currentPhase) return;

            _currentPhase = newPhase;
            _publisher.Publish(new GamePhaseChangedMessage(newPhase));
        }

        public bool HasPhase(GamePhase phase) => (_currentPhase & phase) != 0;
    }
}   