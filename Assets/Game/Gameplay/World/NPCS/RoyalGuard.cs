using Game.Core.Data;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay.World
{
    /// <summary>
    /// Royal Guard NPC. Responds to player interact with a dialogue.
    /// </summary>
    public class RoyalGuard : NPC
    {
        [SerializeField] private DialogueDataSO _firstVisit;
        [SerializeField] private DialogueDataSO _returnVisit;
        [SerializeField] private DialogueSeenTracker _tracker;

        public override void Interact()
        {
            var dialogue = _tracker.HasSeen(_firstVisit.name) ? _returnVisit : _firstVisit;

            GlobalMessagePipe.GetPublisher<DialogueRequested>()
                .Publish(new DialogueRequested(dialogue, autoClose: false));
        }
    }   
}   