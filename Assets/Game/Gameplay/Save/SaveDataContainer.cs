using System;
using System.Collections.Generic;

namespace Game.Gameplay.Save
{
    [System.Serializable]
    public class SaveDataContainer
    {   
        [System.Serializable]
        public class SaveEntry
        {
            public string Key;
            public string Value;
        }

        public int version;
        public List<SaveEntry> SaveDataChunks = new List<SaveEntry>();
        [NonSerialized] private Dictionary<string, string> _chunkDict;

        public void RebuildDictionary()
        {
            _chunkDict = new Dictionary<string, string>();
            if (SaveDataChunks == null) return;

            foreach (var entry in SaveDataChunks)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                    _chunkDict[entry.Key] = entry.Value;
            }
        }

        public bool TryGetValue(string key, out string value)
        {
            value = null;
            if (_chunkDict == null) RebuildDictionary();

            if (_chunkDict != null && _chunkDict.TryGetValue(key, out var foundValue))
            {
                value = foundValue;
                return true;
            }
            return false;
        }

        public void SetValue(string key, string value)
        {
            if (_chunkDict == null) RebuildDictionary();
            _chunkDict[key] = value;

            SaveDataChunks.Clear();
            foreach (var kvp in _chunkDict)
            {
                SaveDataChunks.Add(new SaveEntry { Key = kvp.Key, Value = kvp.Value });
            }
        }
    }
}   