using UnityEngine;

namespace LostWorlds.Person2.Interaction
{
    public enum InteractionCommand { Grab, Activate }

    public interface IInteractable
    {
        InteractionCommand Command { get; }
        bool CanInteract(InteractionInteractor interactor);
        Vector3 ClosestInteractionPoint(Vector3 origin);
        bool TryInteract(InteractionInteractor interactor);
    }
}
