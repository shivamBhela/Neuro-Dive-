using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using LostWorlds.Person2.UI;
namespace LostWorlds.Person2.Hardware
{
    // Station selection only. No movement, scene progression or final GameInputManager.
    [DefaultExecutionOrder(-200)]
    public sealed class P9SimulatorDemo : MonoBehaviour
    {
        [SerializeField] P7HudDemoControls stations;
        [SerializeField] CommandChannel channel;
        [SerializeField] InteractionCommandReceiver interactions;
        [SerializeField] RouteCommandReceiver routes;
        [SerializeField] Text hints;
        int mode=1;
        public P7HudDemoControls Stations=>stations;
        public CommandChannel Channel=>channel;
        public int Mode=>mode;
        void Start()=>ShowView(1);
        void Update()=>PollKeyboard();
        public void PollKeyboard()
        {
            if(!isActiveAndEnabled||Keyboard.current==null)return;var k=Keyboard.current;
            if(k.f1Key.wasPressedThisFrame)ShowView(1);else if(k.f3Key.wasPressedThisFrame)ShowView(3);
        }
        public void ShowView(int value)
        {
            if(value!=1&&value!=3)return;
            mode=value;stations.enabled=false;stations.Interactions.enabled=false;stations.RouteInput.enabled=false;
            stations.ShowView(value);
            routes.Bind(channel,value==3?stations.RouteInput.Route:null,value==3?stations.RouteInput.Reference:null);
            interactions.Bind(channel,value==1?stations.Interactions.GrabInteractor:null,value==1?stations.Interactions.ActivateInteractor:null);
            if(hints!=null)hints.text="SIMULATOR   F1 Interactions / F3 Routes\nA/D or arrows: LEFT/RIGHT   Space: JUMP   G/R: GRAB   E: ACTIVATE";
        }
    }
}
