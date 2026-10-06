using System;
using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    [Serializable]
    public sealed class EnemyStateNodeDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private string _parentId;
        [SerializeField] private string _initialChildId;

        public string Id => _id;
        public string ParentId => _parentId;
        public string InitialChildId => _initialChildId;
    }

}