using UnityEngine;

namespace LostWorlds.Person2.Interaction
{
    public sealed class GrabInteractable : InteractableBase
    {
        [Tooltip("Optional fixed placement target; otherwise the interactor's hold point is used.")]
        [SerializeField] Transform targetPoint;
        [SerializeField] Vector3 localHoldOffset;
        [SerializeField] Vector3 localHoldEuler;
        public Transform TargetPoint { get => targetPoint; set => targetPoint = value; }
        public override InteractionCommand Command => InteractionCommand.Grab;

        public override bool CanInteract(InteractionInteractor interactor)
        {
            if (!base.CanInteract(interactor) || interactor.HeldObject != null) return false;
            var destination = targetPoint != null ? targetPoint : interactor.HoldPoint;
            return destination != null && destination != transform && !destination.IsChildOf(transform);
        }

        protected override bool ApplyInteraction(InteractionInteractor interactor)
        {
            var destination = targetPoint != null ? targetPoint : interactor.HoldPoint;
            foreach (var body in GetComponentsInChildren<Rigidbody>())
            {
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.useGravity = false; body.isKinematic = true;
            }
            // A held object must not push the future player or keep competing for selection.
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            transform.SetParent(destination, true);
            transform.localPosition = localHoldOffset;
            transform.localRotation = Quaternion.Euler(localHoldEuler);
            interactor.RegisterHeldObject(this);
            return true;
        }
    }
}
