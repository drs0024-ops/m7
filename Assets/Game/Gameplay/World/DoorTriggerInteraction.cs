using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Enums;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    public class DoorTriggerInteraction : TriggerInteractionBase, IStartable
    {
        [SerializeField] private DoorToSpawnAt _doorToSpawnTo;
        [SerializeField] private SceneField _sceneToLoad;
        public DoorToSpawnAt CurrentDoorPosition;

        private IPublisher<DoorActivated> _doorActivatedPublisher;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        public DoorTriggerInteraction() { }

        void IStartable.Start()
        {
            _doorActivatedPublisher = GlobalMessagePipe.GetPublisher<DoorActivated>();
        }

        public override void Interact()
        {
            _doorActivatedPublisher.Publish(new DoorActivated(
                _sceneToLoad,
                _doorToSpawnTo,
                PlayerEnteredFromRight));
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var d in _subscriptions)
                d?.Dispose();
            _subscriptions.Clear();
        }

        private void OnDestroy() => Dispose();
    }
}   