using UnityEngine;

namespace LostWorlds.Person2.World
{
    public sealed class RouteChoiceFeedback : MonoBehaviour
    {
        [SerializeField] RouteChoiceZone route;
        [SerializeField] Renderer[] leftMarkers = new Renderer[0];
        [SerializeField] Renderer[] rightMarkers = new Renderer[0];
        [SerializeField] Material readyMaterial;
        [SerializeField] Material selectedMaterial;
        [SerializeField] GameObject leftSelectionMarker;
        [SerializeField] GameObject rightSelectionMarker;

        void OnEnable()
        {
            if (route == null) return;
            route.SelectionAccepted += Selected; route.EncounterBegan += NewEncounter;
            Refresh();
        }
        void OnDisable()
        {
            if (route == null) return;
            route.SelectionAccepted -= Selected; route.EncounterBegan -= NewEncounter;
        }
        void Selected(RouteChoiceZone zone, RouteChoice choice) => Refresh();
        void NewEncounter(RouteChoiceZone zone) => Refresh();
        public void Refresh()
        {
            if (route == null) return;
            Apply(leftMarkers, route.Selection == RouteChoice.Left);
            Apply(rightMarkers, route.Selection == RouteChoice.Right);
            if (leftSelectionMarker != null) leftSelectionMarker.SetActive(route.Selection == RouteChoice.Left);
            if (rightSelectionMarker != null) rightSelectionMarker.SetActive(route.Selection == RouteChoice.Right);
        }
        void Apply(Renderer[] markers, bool selected)
        {
            var material = selected ? selectedMaterial : readyMaterial;
            if (material == null) return;
            foreach (var marker in markers) if (marker != null) marker.sharedMaterial = material;
        }
    }
}
