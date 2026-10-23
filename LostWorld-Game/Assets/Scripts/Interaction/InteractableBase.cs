using UnityEngine;
using UnityEngine.Events;

namespace LostWorlds.Person2.Interaction
{
    /// <summary>One-shot world interaction; add an enabled collider on this object or a child.</summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [Tooltip("Optional range reference. Leave empty to use the nearest enabled collider surface.")]
        [SerializeField] Transform interactionPoint;
        [SerializeField] Renderer[] feedbackRenderers = new Renderer[0];
        [SerializeField] Material successMaterial;
        [SerializeField] UnityEvent onSucceeded = new UnityEvent();
        public abstract InteractionCommand Command { get; }
        public bool HasInteracted { get; private set; }
        public UnityEvent OnSucceeded => onSucceeded;

        public virtual bool CanInteract(InteractionInteractor interactor)
            => isActiveAndEnabled && !HasInteracted && interactor != null && interactor.isActiveAndEnabled;

        public virtual Vector3 ClosestInteractionPoint(Vector3 origin)
        {
            if (interactionPoint != null) return interactionPoint.position;
            Vector3 closest = transform.position;
            float distance = float.PositiveInfinity;
            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                // Non-convex mesh colliders do not support Collider.ClosestPoint.
                var p = collider is MeshCollider mesh && !mesh.convex
                    ? collider.bounds.ClosestPoint(origin) : collider.ClosestPoint(origin);
                float d = (p - origin).sqrMagnitude;
                if (d < distance) { closest = p; distance = d; }
            }
            return closest;
        }

        // Gate every call, including calls made directly by a future input adapter.
        public bool TryInteract(InteractionInteractor interactor)
        {
            if (!CanInteract(interactor) || !interactor.IsInRange(this)) return false;
            if (!ApplyInteraction(interactor)) return false;
            HasInteracted = true;
            if (successMaterial != null)
                foreach (var renderer in feedbackRenderers)
                    if (renderer != null) renderer.sharedMaterial = successMaterial;
            onSucceeded.Invoke();
            return true;
        }

        protected abstract bool ApplyInteraction(InteractionInteractor interactor);
    }
}
