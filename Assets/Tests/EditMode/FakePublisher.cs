using System.Collections.Generic;
using MessagePipe;

namespace Game.Tests
{
    public class FakePublisher<T> : IPublisher<T> where T : struct
    {
        public readonly List<T> Messages = new();

        public void Publish(T message) => Messages.Add(message);

        public void Publish(T message, System.Action onError) => Messages.Add(message);

        public void Publish(T message, System.Action<T, System.Action> onMessage) => Messages.Add(message);
    }
}   