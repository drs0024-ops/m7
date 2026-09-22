using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay.World
{
    [CreateAssetMenu(menuName = "Collectable/Currency", fileName = "new Coin Collectable")]
    public class CollectableCurrencySO : CollectableSOBase
    {
        [SerializeField] private int _currencyAmount = 1;

        private IPublisher<CurrencyCollected> _currencyPublisher;

        private IPublisher<CurrencyCollected> Publisher =>
            _currencyPublisher ??= GlobalMessagePipe.GetPublisher<CurrencyCollected>();

        public override void Collect(GameObject objectThatCollected)
        {
            Publisher.Publish(new CurrencyCollected(_currencyAmount, objectThatCollected.transform, CollectionClip));
        }
    }
}   