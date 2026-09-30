using System;
using MessagePipe;
using UnityEngine;
using Game.Core.Messages;
using VContainer;

namespace Game.Gameplay.World
{
    public class BombSpawner : IDisposable
    {
        private readonly GameObject _bombPrefab;
        private readonly ISubscriber<BombPlaced> _bombPlacedSub;
        private IDisposable _sub;
        private readonly IObjectResolver _resolver;

        public BombSpawner(GameObject bombPrefab, ISubscriber<BombPlaced> bombPlacedSub)
        {
            _bombPrefab = _resolver.Resolve<GameObject>("BombPrefab");;
            _bombPlacedSub = bombPlacedSub;
            _sub = bombPlacedSub.Subscribe(OnBombPlaced);
        }

        private void OnBombPlaced(BombPlaced msg)
        {
            // TODO: Implement bomb instantiation
            // var go = Instantiate(_bombPrefab, msg.Position, Quaternion.identity);
        }

        public void Dispose()
        {
            _sub?.Dispose();
        }
    }
}   