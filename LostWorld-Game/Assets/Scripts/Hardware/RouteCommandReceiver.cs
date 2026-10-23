using UnityEngine;
using LostWorlds.Person2.World;
namespace LostWorlds.Person2.Hardware
{
    public sealed class RouteCommandReceiver : MonoBehaviour
    {
        [SerializeField] CommandChannel channel;
        [SerializeField] RouteChoiceZone route;
        [SerializeField] Transform reference;
        public void Bind(CommandChannel source,RouteChoiceZone target,Transform origin)
        {if(channel!=null)channel.CommandSent-=Receive;channel=source;route=target;reference=origin;if(isActiveAndEnabled&&channel!=null)channel.CommandSent+=Receive;}
        void OnEnable(){if(channel!=null)channel.CommandSent+=Receive;}
        void OnDisable(){if(channel!=null)channel.CommandSent-=Receive;}
        void Receive(HardwareCommand command)
        { if(route!=null&&(command==HardwareCommand.LEFT||command==HardwareCommand.RIGHT))route.TryChoose(command==HardwareCommand.LEFT?RouteChoice.Left:RouteChoice.Right,reference); }
    }
}
