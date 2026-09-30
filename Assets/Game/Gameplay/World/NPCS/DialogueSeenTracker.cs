using System;
using System.Collections.Generic;
using Game.Core.Data;
using Game.Core.Interfaces;
using Game.Core.Messages;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.World
{
    /// <summary>
    /// Tracks which dialogues the player has seen. Integrates with save system via ISaveable.
    /// </summary>
    public class DialogueSeenTracker : IInitializable, IDisposable, ISaveable
    {
        private const string SAVE_ID = "SeenDialogues";

        #region Dependencies

        private readonly ISaveableRegistry _saveableRegistry;
        private readonly ISubscriber<DialogueStarted> _startedSub;

        #endregion

        #region State

        private readonly List<IDisposable> _disposables = new(1);
        private readonly HashSet<string> _seenIds = new();
        private bool _isDisposed;

        #endregion

        #region Public API

        public string SaveId => SAVE_ID;

        public DialogueSeenTracker(
            ISaveableRegistry saveableRegistry,
            ISubscriber<DialogueStarted> startedSub)
        {
            _saveableRegistry = saveableRegistry;
            _startedSub = startedSub;
        }

        public bool HasSeen(string dialogueId)
        {
            return _seenIds.Contains(dialogueId);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _saveableRegistry.Unregister(this);

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();
        }

        #endregion

        #region IInitializable

        void IInitializable.Initialize()
        {
            _saveableRegistry.Register(this);
            _disposables.Add(_startedSub.Subscribe(OnDialogueStarted));
        }

        #endregion

        #region ISaveable

        public ISaveData GetSaveData()
        {
            return new SeenDialoguesSaveData
            {
                Ids = new List<string>(_seenIds)
            };
        }

        public void LoadFromData(ISaveData data)
        {
            if (data is not SeenDialoguesSaveData s) return;

            _seenIds.Clear();
            if (s.Ids == null) return;

            for (int i = 0; i < s.Ids.Count; i++)
                _seenIds.Add(s.Ids[i]);
        }

        #endregion

        #region Message Handlers

        private void OnDialogueStarted(DialogueStarted msg)
        {
            _seenIds.Add(msg.DialogueID);
        }

        #endregion
    }
}   