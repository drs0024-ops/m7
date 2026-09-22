using UnityEngine;

namespace Game.Core.Interfaces
{

    /// <summary>
    /// Explicit (Transform IPlayerStateDriver.Transform => transform;) is the cleaner choice here — 
    /// it doesn't pollute the public API with a Transform property that could be confused with transform. 
    /// Callers still access it through the interface:
    /// IPlayerStateDriver driver = _spawner.GetPlayer();
    ///Vector3 pos = driver.Transform.position; // ✅
    /// </summary> <summary>
    /// 
    /// </summary>
    public interface IPlayerStateDriver
    {
        Transform Transform { get; }
        bool IsDead { get; }
        float CurrentHealth { get; }
    }
}