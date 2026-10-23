using UnityEngine;
using UnityEngine.InputSystem;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.Integration;
namespace LostWorlds.Person2.Journey
{
    /// <summary>Optional local keyboard fallback, separate from the reusable interaction receiver.</summary>
    [RequireComponent(typeof(PortalUnlockMechanism))]
    public sealed class PortalUnlockKeyboardInput : MonoBehaviour
    {
        PortalUnlockMechanism mechanism;
        void Awake()=>mechanism=GetComponent<PortalUnlockMechanism>();
        void Update()=>PollKeyboard();
        public void PollKeyboard()
        {
            if(!isActiveAndEnabled||Keyboard.current==null||!Keyboard.current.eKey.wasPressedThisFrame)return;
            var s=IntegrationSession.Instance;if(s==null||!s.CommandsReady||s.PlayerReference==null)return;
            // Never compete with P9's enabled producer or change its configured state.
            var simulator=s.GetComponentInChildren<HardwareCommandSimulator>(true);if(simulator!=null&&simulator.isActiveAndEnabled)return;
            if(mechanism==null)mechanism=GetComponent<PortalUnlockMechanism>();
            if(mechanism!=null&&mechanism.CanInteract(s.Interactor)&&s.Interactor.IsInRange(mechanism))s.Channel.Send(HardwareCommand.ACTIVATE);
        }
    }
}
