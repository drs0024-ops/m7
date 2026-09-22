// Game.Core.Interfaces
using System.Collections.Generic;
using Game.Core.Interfaces;

public interface ISaveableRegistry
{
    void Register(ISaveable saveable);
    void Unregister(ISaveable saveable);
    IReadOnlyList<ISaveable> GetAll();
}   