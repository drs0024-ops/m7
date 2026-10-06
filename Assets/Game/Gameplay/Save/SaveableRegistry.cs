// Game.Gameplay.Save
using System.Collections.Generic;
using Game.Core.Interfaces;

public class SaveableRegistry : ISaveableRegistry
{
    private readonly List<ISaveable> _savables = new();

    public event System.Action<ISaveable> SaveableRegistered;

    public void Register(ISaveable s)
    {
        if (s == null || _savables.Contains(s)) return;
        _savables.Add(s);
        SaveableRegistered?.Invoke(s);
    }

    public void Unregister(ISaveable s) => _savables.Remove(s);
    public IReadOnlyList<ISaveable> GetAll() => _savables.ToArray();   
}   