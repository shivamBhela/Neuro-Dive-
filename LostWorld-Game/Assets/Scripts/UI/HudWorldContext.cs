using System;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.World;
using UnityEngine;

namespace LostWorlds.Person2.UI
{
    // Read-only availability adapter: existing P4/P6 have no proximity-change events.
    // All gameplay state changes continue to come from their original callbacks.
    public sealed class HudWorldContext : MonoBehaviour
    {
        [SerializeField] InteractableBase grabTarget, activateTarget;
        [SerializeField] InteractionInteractor grabReference, activateReference;
        [SerializeField] RouteChoiceZone route;
        [SerializeField] Transform routeReference;
        [SerializeField] string grabHint = "GRAB", activateHint = "ACTIVATE";
        [Min(.05f)] [SerializeField] float sampleInterval = .15f;
        float nextSample;
        int previousState = -1;
        public event Action BindingsChanged;
        public event Action AvailabilityChanged;
        public InteractableBase GrabTarget => grabTarget;
        public InteractableBase ActivateTarget => activateTarget;
        public RouteChoiceZone Route => route;
        public string GrabHint => grabHint;
        public string ActivateHint => activateHint;
        public bool GrabComplete => grabTarget != null && grabTarget.HasInteracted;
        public bool ActivateComplete => activateTarget != null && activateTarget.HasInteracted;
        public bool GrabAvailable => Available(grabTarget, grabReference);
        public bool ActivateAvailable => Available(activateTarget, activateReference);
        public bool InRouteZone => route != null && route.Contains(routeReference);
        static bool Available(InteractableBase target, InteractionInteractor reference)
            => target != null && reference != null && target.CanInteract(reference) && reference.IsInRange(target);
        public void SetContext(InteractableBase grab, InteractionInteractor grabActor, InteractableBase activate,
            InteractionInteractor activateActor, RouteChoiceZone choice, Transform choiceReference,
            string grabLabel = "GRAB", string activateLabel = "ACTIVATE")
        {
            grabTarget = grab; grabReference = grabActor; activateTarget = activate; activateReference = activateActor;
            route = choice; routeReference = choiceReference; grabHint = grabLabel; activateHint = activateLabel;
            BindingsChanged?.Invoke(); SampleNow();
        }
        void Update()
        {
            if (Time.unscaledTime < nextSample) return;
            nextSample = Time.unscaledTime + Mathf.Max(.05f, sampleInterval);
            SampleNow();
        }
        public void SampleNow()
        {
            int state = (GrabAvailable ? 1 : 0) | (ActivateAvailable ? 2 : 0) | (InRouteZone ? 4 : 0) |
                        (GrabComplete ? 8 : 0) | (ActivateComplete ? 16 : 0);
            if (state == previousState) return;
            previousState = state; AvailabilityChanged?.Invoke();
        }
    }
}
