using System;
using System.Collections.Generic;
using MessagePipe;

namespace Game.Tests
{
    public class FakeSubscriber<T> : ISubscriber<T> where T : struct
    {
        private readonly List<IMessageHandler<T>> _handlers = new();

        public IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters)
        {
            _handlers.Add(handler);
            return new Unsubscriber(() => _handlers.Remove(handler));
        }

        /// <summary>
        /// Test helper: triggers all subscribed handlers with a message.
        /// </summary>
        public void Publish(T message)
        {
            for (int i = 0; i < _handlers.Count; i++)
                _handlers[i].Handle(message);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly Action _action;
            public Unsubscriber(Action action) => _action = action;
            public void Dispose() => _action();
        }
    }
}   