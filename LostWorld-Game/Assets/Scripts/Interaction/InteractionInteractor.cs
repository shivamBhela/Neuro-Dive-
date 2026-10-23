using System.Collections.Generic;
using UnityEngine;

namespace LostWorlds.Person2.Interaction
{
    /// <summary>Range and target selection only. Owns no input, movement, or player state.</summary>
    public sealed class InteractionInteractor : MonoBehaviour
    {
        [Tooltip("Future player/reference transform. Defaults to this transform; never moves it.")]
        [SerializeField] Transform reference;
        [Tooltip("Destination for grabbed objects without their own target override.")]
        [SerializeField] Transform holdPoint;
        [Min(0)] [SerializeField] float range = 3.5f;
        [SerializeField] LayerMask interactionLayers = ~0;
        [Min(0)] [SerializeField] float successCooldown = .25f;
        [SerializeField] bool debugFeedback;
        [SerializeField] bool drawRange = true;
        float lastGrab = float.NegativeInfinity;
        float lastActivate = float.NegativeInfinity;
        public Transform Reference { get => reference; set => reference = value; }
        public Transform HoldPoint { get => holdPoint; set => holdPoint = value; }
        public float Range { get => range; set => range = Mathf.Max(0, value); }
        public Vector3 Origin => reference != null ? reference.position : transform.position;
        public GrabInteractable HeldObject { get; private set; }
        public bool TryGrab() => TryInteract(InteractionCommand.Grab);
        public bool TryActivate() => TryInteract(InteractionCommand.Activate);

        public bool IsInRange(IInteractable target)
            => isActiveAndEnabled && target != null && (target.ClosestInteractionPoint(Origin) - Origin).sqrMagnitude <= range * range;

        public bool TryInteract(InteractionCommand command)
        {
            if (!isActiveAndEnabled) return false;
            float last = command == InteractionCommand.Grab ? lastGrab : lastActivate;
            if (Time.unscaledTime - last < successCooldown) return false;
            InteractableBase best = null;
            float bestDistance = float.PositiveInfinity;
            var seen = new HashSet<InteractableBase>();
            // Query on a command, not every frame. No fixed-size buffer can silently omit targets.
            foreach (var collider in Physics.OverlapSphere(Origin, range, interactionLayers, QueryTriggerInteraction.Collide))
            {
                var candidate = collider.GetComponentInParent<InteractableBase>();
                if (candidate == null || !seen.Add(candidate) || candidate.Command != command || !candidate.CanInteract(this)) continue;
                float distance = (candidate.ClosestInteractionPoint(Origin) - Origin).sqrMagnitude;
                if (distance > range * range) continue;
                if (best == null || distance < bestDistance)
                { best = candidate; bestDistance = distance; }
            }
            bool succeeded = best != null && best.TryInteract(this);
            if (succeeded)
            {
                if (command == InteractionCommand.Grab) lastGrab = Time.unscaledTime; else lastActivate = Time.unscaledTime;
            }
            if (debugFeedback) Debug.Log($"[Person2 interaction] {command}: {(succeeded ? best.name : "no eligible target in range")}", this);
            return succeeded;
        }

        internal void RegisterHeldObject(GrabInteractable item) => HeldObject = item;
        void OnValidate() { range = Mathf.Max(0, range); successCooldown = Mathf.Max(0, successCooldown); }
        void OnDrawGizmosSelected()
        {
            if (!drawRange) return;
            Gizmos.color = new Color(.2f,.85f,.75f,.7f); Gizmos.DrawWireSphere(Origin, range);
            if (holdPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireCube(holdPoint.position, Vector3.one * .35f); }
        }
    }
}
