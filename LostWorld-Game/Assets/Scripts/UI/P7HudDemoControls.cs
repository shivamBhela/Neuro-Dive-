using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LostWorlds.Person2.UI
{
    // Demo-only camera/context selection. Uses original P4/P6 input adapters.
    [DefaultExecutionOrder(-100)]
    public sealed class P7HudDemoControls : MonoBehaviour
    {
        [SerializeField] HudWorldContext context;
        [SerializeField] P4KeyboardTestInput interactions;
        [SerializeField] GrabInteractable grab;
        [SerializeField] ActivateInteractable activate;
        [SerializeField] Camera interactionCamera;
        [SerializeField] P6RouteTestInput routeInput;
        [SerializeField] Camera routeCamera;
        [SerializeField] Text instructions;
        int mode;
        public P4KeyboardTestInput Interactions => interactions;
        public P6RouteTestInput RouteInput => routeInput;
        public int Mode => mode;
        public Camera ActiveCamera
        {
            get
            {
                if (mode == 1) return interactionCamera;
                if (mode == 3) return routeCamera;
                return interactionCamera;
            }
        }
        void Start() { if (mode == 0) ShowView(1); }
        void Update() => PollKeyboard();
        public void PollKeyboard()
        {
            var keyboard = Keyboard.current; if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame) ShowView(1);
            else if (keyboard.f3Key.wasPressedThisFrame) ShowView(3);
        }
        public void ShowView(int value)
        {
            if (value != 1 && value != 3) return;
            mode = value;
            interactions.TestInputEnabled = value == 1; interactionCamera.enabled = value == 1;
            routeInput.TestInputEnabled = value == 3; routeCamera.enabled = value == 3;
            if (value == 1)
                context.SetContext(grab, interactions.GrabInteractor, activate, interactions.ActivateInteractor, null, null, "G   GRAB", "E   ACTIVATE");
            else if (value == 3)
                context.SetContext(null, null, null, null, routeInput.Route, routeInput.Reference);
            if (instructions != null) instructions.text = "HUD DEMO   F1  Interactions   /   F3  Routes\n" +
                (value == 1 ? "G  Grab   /   E  Activate" : "A / Left Arrow  LEFT   /   D / Right Arrow  RIGHT");
        }
    }
}
