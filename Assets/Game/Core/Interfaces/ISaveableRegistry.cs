// Game.Core.Interfaces
using System;
using System.Collections.Generic;
using Game.Core.Interfaces;

public interface ISaveableRegistry
{
    event Action<ISaveable> SaveableRegistered;
    void Register(ISaveable saveable);
    void Unregister(ISaveable saveable);
    IReadOnlyList<ISaveable> GetAll();
}   