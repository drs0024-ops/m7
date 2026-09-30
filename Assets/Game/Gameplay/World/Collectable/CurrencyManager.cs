using System;
using System.Collections.Generic;
using Game.Core.Messages;
using MessagePipe;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    /// <summary>
    /// Tracks the player's current currency balance. Subscribes to collection events
    /// and publishes balance changes.
    /// </summary>
    public class CurrencyManager : IStartable, IDisposable
    {
        #region Dependencies

        private readonly ISubscriber<CurrencyCollected> _collectedSubscriber;
        private readonly IPublisher<CurrencyChanged> _currencyChangedPublisher;

        #endregion

        #region State

        private readonly List<IDisposable> _subscriptions = new(1);
        private bool _disposed;

        #endregion

        #region Public API

        public int CurrentCurrency { get; private set; }

        public CurrencyManager(
            ISubscriber<CurrencyCollected> collectedSubscriber,
            IPublisher<CurrencyChanged> currencyChangedPublisher)
        {
            _collectedSubscriber = collectedSubscriber;
            _currencyChangedPublisher = currencyChangedPublisher;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }

        #endregion

        #region IStartable

        void IStartable.Start()
        {
            _subscriptions.Add(_collectedSubscriber.Subscribe(OnCurrencyCollected));
        }

        #endregion

        #region Message Handlers

        private void OnCurrencyCollected(CurrencyCollected message)
        {
            CurrentCurrency += message.Amount;
            _currencyChangedPublisher.Publish(new CurrencyChanged(CurrentCurrency));
        }

        #endregion
    }
}   