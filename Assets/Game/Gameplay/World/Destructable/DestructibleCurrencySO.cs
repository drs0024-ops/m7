using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay.World
{
    [CreateAssetMenu(menuName = "Destructible/Points", fileName = "new Destructible Points")]
    public class DestructibleCurrencySO : DestructableSOBase
    {
        [SerializeField] private int _pointAmount = 1;

        private readonly IPublisher<CurrencyCollected> _currencyPublisher;

        public DestructibleCurrencySO()
        {
            _currencyPublisher = GlobalMessagePipe.GetPublisher<CurrencyCollected>();
        }

        public override void Destroy(GameObject objectThatDestroyed)
        {
            _currencyPublisher.Publish(new CurrencyCollected(_pointAmount, objectThatDestroyed.transform, DestroyClip));
        }
    }
}   