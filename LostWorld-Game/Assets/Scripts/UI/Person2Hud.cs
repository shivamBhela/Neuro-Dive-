using System.Collections;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.World;
using UnityEngine;
using UnityEngine.UI;

namespace LostWorlds.Person2.UI
{
    public sealed class Person2Hud : MonoBehaviour
    {
        [SerializeField] HudWorldContext context;
        [SerializeField] Text interactionText, routeText, feedbackText;
        [SerializeField] GameObject interactionPanel, routePanel, feedbackPanel;
        [Min(1)] [SerializeField] float feedbackSeconds = 4.5f;
        InteractableBase boundGrab, boundActivate;
        RouteChoiceZone boundRoute;
        Coroutine feedbackTimer;
        public string InteractionText => interactionText != null ? interactionText.text : "";
        public string RouteText => routeText != null ? routeText.text : "";
        public string FeedbackText => feedbackText != null ? feedbackText.text : "";
        public int FeedbackCount { get; private set; }
        public HudWorldContext Context => context;

        void OnEnable()
        {
            if (context != null) { context.BindingsChanged += Rebind; context.AvailabilityChanged += RefreshPrompts; }
            Rebind();
            if (feedbackPanel != null) feedbackPanel.SetActive(false);
        }
        void OnDisable()
        {
            if (context != null) { context.BindingsChanged -= Rebind; context.AvailabilityChanged -= RefreshPrompts; }
            Unbind();
            if (feedbackTimer != null) StopCoroutine(feedbackTimer);
            feedbackTimer = null;
            if (feedbackPanel != null) feedbackPanel.SetActive(false);
        }
        void Unbind()
        {
            if (boundGrab != null) boundGrab.OnSucceeded.RemoveListener(GrabSucceeded);
            if (boundActivate != null) boundActivate.OnSucceeded.RemoveListener(ActivateSucceeded);
            if (boundRoute != null) { boundRoute.SelectionAccepted -= RouteSelected; boundRoute.EncounterBegan -= RouteReset; }
            boundGrab = null; boundActivate = null; boundRoute = null;
        }
        void Rebind()
        {
            Unbind();
            if (context != null)
            {
                boundGrab = context.GrabTarget; boundActivate = context.ActivateTarget; boundRoute = context.Route;
                if (boundGrab != null) boundGrab.OnSucceeded.AddListener(GrabSucceeded);
                if (boundActivate != null) boundActivate.OnSucceeded.AddListener(ActivateSucceeded);
                if (boundRoute != null) { boundRoute.SelectionAccepted += RouteSelected; boundRoute.EncounterBegan += RouteReset; }
            }
            RefreshPrompts();
        }
        static void Set(Text text, string value) { if (text != null && text.text != value) text.text = value; }
        void GrabSucceeded()
        {
            ShowFeedback("Object placed. Nicely done.");
            RefreshPrompts();
        }
        void ActivateSucceeded() { ShowFeedback("Activated. Nicely done."); RefreshPrompts(); }
        void RouteSelected(RouteChoiceZone zone, RouteChoice choice)
        { RefreshPrompts(); }
        void RouteReset(RouteChoiceZone zone) => RefreshPrompts();
        public void RefreshPrompts()
        {
            bool interactions = context != null && (context.GrabTarget != null || context.ActivateTarget != null);
            if (interactionPanel != null) interactionPanel.SetActive(interactions);
            string prompts = "";
            if (interactions)
            {
                if (context.GrabTarget != null) prompts = context.GrabComplete ? "GRAB complete" :
                    context.GrabAvailable ? context.GrabHint : "Nothing within reach";
                if (context.ActivateTarget != null)
                {
                    if (prompts.Length > 0) prompts += "\n";
                    prompts += context.ActivateComplete ? "ACTIVATE complete" : context.ActivateAvailable ? context.ActivateHint : "ACTIVATE unavailable";
                }
            }
            Set(interactionText, prompts);
            bool hasRoute = boundRoute != null && !boundRoute.HasChoice;
            if (routePanel != null) routePanel.SetActive(hasRoute);
            Set(routeText, !hasRoute ? "" :
                context != null && context.InRouteZone ? "LEFT  /  RIGHT\nChoose either path" : "At the junction, choose a path");
        }
        public void ShowFeedback(string message)
        {
            FeedbackCount++; Set(feedbackText, message);
            if (feedbackPanel == null) return;
            feedbackPanel.SetActive(true);
            if (feedbackTimer != null) StopCoroutine(feedbackTimer);
            feedbackTimer = StartCoroutine(ClearFeedback());
        }
        IEnumerator ClearFeedback()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(1, feedbackSeconds));
            if (feedbackPanel != null) feedbackPanel.SetActive(false);
            feedbackTimer = null;
        }
    }
}
