using System;
using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Game/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _typeId;
        [SerializeField] private EnemyStats _stats;
        [SerializeField] private EnemyStateGraphDefinition _stateGraph;
        [SerializeField] private EnemyModuleDefinition[] _modules = Array.Empty<EnemyModuleDefinition>();

        public string TypeId => _typeId;
        public EnemyStats Stats => _stats;
        public EnemyStateGraphDefinition StateGraph => _stateGraph;
        public EnemyModuleDefinition[] Modules => _modules;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(_typeId))
                return Fail("Enemy type ID is required.", out error);
            if (_stats == null)
                return Fail("Enemy stats are required.", out error);
            if (!_stats.TryValidate(out error))
                return false;
            if (_stateGraph == null)
                return Fail("Enemy state graph is required.", out error);
            if (!_stateGraph.TryValidate(out error))
                return false;
            if (_modules == null)
                return Fail("Module definitions cannot be null.", out error);

            for (int i = 0; i < _modules.Length; i++)
                if (_modules[i] == null)
                    return Fail($"Module definition at index {i} is null.", out error);

            error = null;
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}