using UnityEngine;
using UnityEngine.InputSystem;
namespace LostWorlds.Person2.Hardware
{
    /// <summary>Temporary keyboard source. Replace this producer, retaining the channel and receivers.</summary>
    public sealed class HardwareCommandSimulator : MonoBehaviour
    {
        [SerializeField] CommandChannel channel;
        public CommandChannel Channel => channel;
        // Optional host policy; keeps this producer independent of the integration assembly.
        public System.Func<bool> KeyboardPermission {get;set;}
        public static bool ForwardHeld=>Keyboard.current?.wKey.isPressed??false;
        public static bool AnyGameplayKeyHeld
        {
            get{var k=Keyboard.current;return k!=null&&(k.wKey.isPressed||k.aKey.isPressed||k.dKey.isPressed||k.leftArrowKey.isPressed||k.rightArrowKey.isPressed||k.spaceKey.isPressed||k.eKey.isPressed||k.gKey.isPressed||k.rKey.isPressed);}
        }
        public void Bind(CommandChannel target)=>channel=target;
        void Update()=>PollKeyboard();
        public void PollKeyboard()
        {
            if(!isActiveAndEnabled || channel==null || Keyboard.current==null)return;
            if(KeyboardPermission!=null&&!KeyboardPermission())return;
            var k=Keyboard.current;
            bool left=k.aKey.isPressed||k.leftArrowKey.isPressed,right=k.dKey.isPressed||k.rightArrowKey.isPressed;
            if(left!=right)
            {
                if(left&&(k.aKey.wasPressedThisFrame||k.leftArrowKey.wasPressedThisFrame))channel.Send(HardwareCommand.LEFT);
                if(right&&(k.dKey.wasPressedThisFrame||k.rightArrowKey.wasPressedThisFrame))channel.Send(HardwareCommand.RIGHT);
            }
            if(k.spaceKey.wasPressedThisFrame)channel.Send(HardwareCommand.JUMP);
            if(k.gKey.wasPressedThisFrame||k.rKey.wasPressedThisFrame)channel.Send(HardwareCommand.GRAB);
            if(k.eKey.wasPressedThisFrame)channel.Send(HardwareCommand.ACTIVATE);
        }
    }
}
