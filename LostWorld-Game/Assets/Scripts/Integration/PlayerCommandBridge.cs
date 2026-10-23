using System;
using UnityEngine;
using UnityEngine.Events;
using LostWorlds.Person2.Hardware;
namespace LostWorlds.Person2.Integration
{
    public sealed class PlayerCommandBridge : MonoBehaviour
    {
        [SerializeField] IntegrationSession session;
        [SerializeField] CommandChannel channel;
        [SerializeField] UnityEvent onLeft=new UnityEvent(),onRight=new UnityEvent(),onJump=new UnityEvent();
        public event Action<HardwareCommand> PlayerCommandRequested;
        void OnEnable(){if(channel!=null)channel.CommandSent+=Receive;}
        void OnDisable(){if(channel!=null)channel.CommandSent-=Receive;}
        void Receive(HardwareCommand command)
        {
            if(session==null||!session.CommandsReady)return;
            if(command!=HardwareCommand.LEFT&&command!=HardwareCommand.RIGHT&&command!=HardwareCommand.JUMP)return;
            // Route selection records the branch, but must not swallow runner steering.
            if(command!=HardwareCommand.JUMP)session.ConsumeRouteDirection(command);
            if(PlayerCommandRequested!=null)foreach(Action<HardwareCommand> callback in PlayerCommandRequested.GetInvocationList())try{callback(command);}catch(Exception ex){Debug.LogException(ex,this);}
            try{if(command==HardwareCommand.LEFT)onLeft.Invoke();else if(command==HardwareCommand.RIGHT)onRight.Invoke();else onJump.Invoke();}catch(Exception ex){Debug.LogException(ex,this);}
        }
    }
}
