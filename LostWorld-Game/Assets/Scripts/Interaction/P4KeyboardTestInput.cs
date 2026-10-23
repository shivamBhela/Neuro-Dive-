using UnityEngine;
using UnityEngine.InputSystem;

namespace LostWorlds.Person2.Interaction
{
    /// <summary>Temporary P4 keyboard test adapter. Remove/disable when integrating real input.</summary>
    public sealed class P4KeyboardTestInput : MonoBehaviour
    {
        [SerializeField] bool testInputEnabled = true;
        [Tooltip("Stationary GRAB reference for this demo; later use the same interactor for both commands.")]
        [SerializeField] InteractionInteractor grabInteractor;
        [SerializeField] InteractionInteractor activateInteractor;
        public InteractionInteractor GrabInteractor { get => grabInteractor; set => grabInteractor = value; }
        public InteractionInteractor ActivateInteractor { get => activateInteractor; set => activateInteractor = value; }
        public bool TestInputEnabled { get => testInputEnabled; set => testInputEnabled = value; }
        void Update() => PollKeyboard();

        // Public only to allow deterministic validation of this temporary adapter.
        public void PollKeyboard()
        {
            if (!isActiveAndEnabled || !testInputEnabled || Keyboard.current == null) return;
            if (Keyboard.current.gKey.wasPressedThisFrame && grabInteractor != null) grabInteractor.TryGrab();
            if (Keyboard.current.eKey.wasPressedThisFrame && activateInteractor != null) activateInteractor.TryActivate();
        }
    }
}
