using UnityEngine;

namespace LostWorlds.Person2.Interaction
{
    public sealed class ActivateInteractable : InteractableBase
    {
        [Tooltip("Optional visual object to enable. Keep it separate from this object's parent hierarchy.")]
        [SerializeField] GameObject activatedVisual;
        public GameObject ActivatedVisual { get => activatedVisual; set => activatedVisual = value; }
        public override InteractionCommand Command => InteractionCommand.Activate;
        protected override bool ApplyInteraction(InteractionInteractor interactor)
        {
            if (activatedVisual != null) activatedVisual.SetActive(true);
            return true;
        }
    }
}
