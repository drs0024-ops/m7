using UnityEngine;

namespace Game.Core.Interfaces
{
    public interface IInteractable
    {
        Transform Player { get; }
        bool CanInteract { get; }
        bool PlayerEnteredFromRight { get; }
        void Interact();
    }
}   