using UnityEngine;
using UnityEngine.InputSystem;

namespace LostWorlds.Person2.World
{
    /// <summary>Temporary test input. Never writes a participant transform or moves a player.</summary>
    public sealed class P6RouteTestInput : MonoBehaviour
    {
        [SerializeField] bool testInputEnabled;
        [SerializeField] RouteChoiceZone route;
        [SerializeField] Transform reference;
        public bool TestInputEnabled { get => testInputEnabled; set => testInputEnabled = value; }
        public RouteChoiceZone Route => route;
        public Transform Reference { get => reference; set => reference = value; }
        void Update() => PollKeyboard();
        public void PollKeyboard()
        {
            if (!isActiveAndEnabled || !testInputEnabled || route == null || Keyboard.current == null) return;
            var keys = Keyboard.current;
            bool leftHeld = keys.aKey.isPressed || keys.leftArrowKey.isPressed;
            bool rightHeld = keys.dKey.isPressed || keys.rightArrowKey.isPressed;
            // Ambiguous opposing input has no priority; release and choose either side.
            if (leftHeld == rightHeld) return;
            if (leftHeld && (keys.aKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame))
                route.TryChoose(RouteChoice.Left, reference);
            else if (rightHeld && (keys.dKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame))
                route.TryChoose(RouteChoice.Right, reference);
        }
    }
}
