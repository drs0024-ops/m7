using System.Collections.Generic;
using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    public sealed class DamagableRegistry : IDamagableRegistry
    {
        private readonly Dictionary<int, IDamagable> _targets = new();

        public void Register(int entityId, IDamagable target)
        {
            if (target == null)
                throw new System.ArgumentNullException(nameof(target));

            _targets[entityId] = target;
        }

        public bool Unregister(int entityId) => _targets.Remove(entityId);

        public bool TryGet(int entityId, out IDamagable target)
        {
            if (!_targets.TryGetValue(entityId, out target))
                return false;

            if (target is Object unityObject && unityObject == null)
            {
                _targets.Remove(entityId);
                target = null;
                return false;
            }

            return true;
        }
    }
}