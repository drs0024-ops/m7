using System;
using UnityEngine;

namespace Game.Gameplay.Enemies.Runtime
{
    public enum EnemyModuleKind
    {
        Perception,
        Decision,
        Execution,
        Presentation
    }

    public interface IEnemyModuleRuntime : IDisposable
    {
    }

    public interface IEnemyExecutionModuleRuntime : IEnemyModuleRuntime
    {
        EnemyExecutionIntent? OnContextChanged(EnemyExecutionContext context);
        EnemyExecutionIntent? Tick(EnemyExecutionContext context, float deltaTime);
    }

    public abstract class EnemyModuleDefinition : ScriptableObject
    {
        public abstract EnemyModuleKind Kind { get; }
        public abstract IEnemyModuleRuntime CreateRuntime();
    }
}