// Tracks orb totals. Subscribes OrbPickedUp, publishes OrbCollectedMessage with NewTotal. ISaveable.
using System;
using System.Collections.Generic;
using Game.Core.Enums;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    public class OrbCounter : IStartable, IDisposable, ISaveable
    {
        private ISubscriber<OrbPickedUp> _pickedUpSub;
        private IPublisher<OrbCollectedMessage> _collectedPublisher;
        private readonly ISaveableRegistry _registry;
        private readonly List<IDisposable> _subscriptions = new();

        private int _total;
        private int _humanoidCount;
        private int _nonHumanoidCount;
        private bool _disposed;

        public int Total => _total;
        public int HumanoidCount => _humanoidCount;
        public int NonHumanoidCount => _nonHumanoidCount;

        public string SaveId => "OrbCounter";

        [Inject]
        public OrbCounter(ISaveableRegistry registry)
        {
            _registry = registry;
        }

        void IStartable.Start()
        {
            _registry.Register(this);

            _pickedUpSub = GlobalMessagePipe.GetSubscriber<OrbPickedUp>();
            _collectedPublisher = GlobalMessagePipe.GetPublisher<OrbCollectedMessage>();

            _subscriptions.Add(_pickedUpSub.Subscribe(OnOrbPickedUp));
        }

        private void OnOrbPickedUp(OrbPickedUp msg)
        {
            if (_disposed) return;

            _total++;
            if (msg.Type == OrbType.Humanoid) _humanoidCount++;
            else _nonHumanoidCount++;

            _collectedPublisher.Publish(new OrbCollectedMessage(msg.Type, _total));
        }

        #region ISaveable

        public ISaveData GetSaveData() => new OrbCounterSaveData
        {
            SaveId = "OrbCounter",
            Total = _total,
            HumanoidCount = _humanoidCount,
            NonHumanoidCount = _nonHumanoidCount
        };

        public void LoadFromData(ISaveData data)
        {
            if (data is OrbCounterSaveData d)
            {
                _total = d.Total;
                _humanoidCount = d.HumanoidCount;
                _nonHumanoidCount = d.NonHumanoidCount;
            }
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _registry.Unregister(this);
            foreach (var d in _subscriptions) d?.Dispose();
            _subscriptions.Clear();
        }
    }

    [Serializable]
    public class OrbCounterSaveData : ISaveData
    {
        public string SaveId = "OrbCounter";
        public int Total;
        public int HumanoidCount;
        public int NonHumanoidCount;

        string ISaveData.SaveId => SaveId;
    }
}   