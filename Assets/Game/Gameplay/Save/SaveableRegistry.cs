// Game.Gameplay.Save
using System.Collections.Generic;
using Game.Core.Interfaces;

public class SaveableRegistry : ISaveableRegistry
{
    private readonly List<ISaveable> _savables = new();
    public void Register(ISaveable s) { if (!_savables.Contains(s)) _savables.Add(s); }
    public void Unregister(ISaveable s) => _savables.Remove(s);
    public IReadOnlyList<ISaveable> GetAll() => _savables.ToArray();   
}   