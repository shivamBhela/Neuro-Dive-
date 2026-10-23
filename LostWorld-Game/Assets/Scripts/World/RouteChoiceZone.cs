using System;
using UnityEngine;
using UnityEngine.Events;

namespace LostWorlds.Person2.World
{
    public enum RouteChoice { None, Left, Right }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class RouteChoiceZone : MonoBehaviour
    {
        [SerializeField] BoxCollider choiceZone;
        [SerializeField] bool debugFeedback = true;
        [SerializeField] bool drawZone = true;
        [SerializeField] UnityEvent<RouteChoice> onSelected = new UnityEvent<RouteChoice>();
        [SerializeField] UnityEvent onEncounterBegan = new UnityEvent();
        static int session;
        int stateSession = -1;
        RouteChoice selection;
        int encounterNumber = 1;
        bool dispatching;

        public event Action<RouteChoiceZone, RouteChoice> SelectionAccepted;
        public event Action<RouteChoiceZone> EncounterBegan;
        public BoxCollider ChoiceZone => choiceZone;
        public RouteChoice Selection { get { EnsureSession(); return selection; } }
        public int EncounterNumber { get { EnsureSession(); return encounterNumber; } }
        public bool HasChoice => Selection != RouteChoice.None;
        public UnityEvent<RouteChoice> OnSelected => onSelected;
        public UnityEvent OnEncounterBegan => onEncounterBegan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void NewSession() => session++;
        void EnsureSession()
        {
            if (stateSession == session) return;
            stateSession = session; selection = RouteChoice.None; encounterNumber = 1; dispatching = false;
        }
        void Reset()
        {
            choiceZone = GetComponent<BoxCollider>();
            choiceZone.isTrigger = true;
        }
        // Geometric reference containment needs no Player tag, collider or Rigidbody.
        // Local box coordinates preserve correct boundaries when the zone is rotated/scaled.
        public bool Contains(Transform reference)
        {
            if (!isActiveAndEnabled || reference == null || !reference.gameObject.activeInHierarchy ||
                choiceZone == null || !choiceZone.enabled || !choiceZone.isTrigger ||
                !choiceZone.gameObject.activeInHierarchy) return false;
            var local = choiceZone.transform.InverseTransformPoint(reference.position) - choiceZone.center;
            var half = choiceZone.size * .5f;
            return Mathf.Abs(local.x) <= half.x + .001f &&
                   Mathf.Abs(local.y) <= half.y + .001f &&
                   Mathf.Abs(local.z) <= half.z + .001f;
        }
        public bool TryChoose(RouteChoice choice, Transform reference)
        {
            EnsureSession();
            if (!Application.isPlaying || dispatching || HasChoice ||
                (choice != RouteChoice.Left && choice != RouteChoice.Right) || !Contains(reference)) return false;
            selection = choice; // Commit before callbacks so repeated/reentrant commands cannot win.
            dispatching = true;
            try
            {
                Notify(SelectionAccepted, listener => ((Action<RouteChoiceZone, RouteChoice>)listener)(this, choice));
                try { onSelected.Invoke(choice); } catch (Exception error) { Debug.LogException(error, this); }
            }
            finally { dispatching = false; }
            if (debugFeedback) Debug.Log($"[P6 Route] {name}: {choice.ToString().ToUpperInvariant()} accepted for encounter {EncounterNumber}. Selection only; no movement.", this);
            return true;
        }
        // Explicit lifecycle hook for a future encounter owner. Exiting/re-entering or
        // toggling the component never rearms the same encounter.
        public bool BeginNewEncounter()
        {
            EnsureSession();
            if (!Application.isPlaying || !isActiveAndEnabled || dispatching) return false;
            selection = RouteChoice.None; encounterNumber++;
            dispatching = true;
            try
            {
                Notify(EncounterBegan, listener => ((Action<RouteChoiceZone>)listener)(this));
                try { onEncounterBegan.Invoke(); } catch (Exception error) { Debug.LogException(error, this); }
            }
            finally { dispatching = false; }
            return true;
        }
        static void Notify(Delegate callbacks, Action<Delegate> invoke)
        {
            if (callbacks == null) return;
            foreach (var listener in callbacks.GetInvocationList())
                try { invoke(listener); } catch (Exception error) { Debug.LogException(error); }
        }
        void OnDrawGizmosSelected()
        {
            if (!drawZone || choiceZone == null) return;
            var previous = Gizmos.matrix;
            Gizmos.matrix = choiceZone.transform.localToWorldMatrix; Gizmos.color = new Color(.3f, .8f, .9f, .8f);
            Gizmos.DrawWireCube(choiceZone.center, choiceZone.size); Gizmos.matrix = previous;
        }
    }
}
