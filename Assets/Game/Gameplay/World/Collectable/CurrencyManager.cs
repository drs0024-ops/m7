using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    public class CurrencyManager : MonoBehaviour, IStartable, IDisposable
    {
        private readonly ISubscriber<CurrencyCollected> _collectedSubscriber;
        private readonly IPublisher<CurrencyChanged> _currencyChangedPublisher;
        private readonly List<IDisposable> _subscriptions = new();

        public int CurrentCurrency { get; private set; }

        public CurrencyManager()
        {
            _collectedSubscriber = GlobalMessagePipe.GetSubscriber<CurrencyCollected>();
            _currencyChangedPublisher = GlobalMessagePipe.GetPublisher<CurrencyChanged>();
        }

        void IStartable.Start()
        {
            _subscriptions.Add(_collectedSubscriber.Subscribe(OnCurrencyCollected));
        }

        private void OnCurrencyCollected(CurrencyCollected message)
        {
            CurrentCurrency += message.Amount;
            _currencyChangedPublisher.Publish(new CurrencyChanged(CurrentCurrency));
        }

        public void Dispose()
        {
            foreach (var d in _subscriptions)
                d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            if (_subscriptions.Count != 0)
                Dispose();
        }
    }
}   