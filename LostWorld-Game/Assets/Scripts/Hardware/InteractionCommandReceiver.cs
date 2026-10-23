using UnityEngine;
using LostWorlds.Person2.Interaction;
namespace LostWorlds.Person2.Hardware
{
    public sealed class InteractionCommandReceiver : MonoBehaviour
    {
        [SerializeField] CommandChannel channel;
        [SerializeField] InteractionInteractor grab,activate;
        public void Bind(CommandChannel source,InteractionInteractor grabReference,InteractionInteractor activateReference)
        { if(channel!=null)channel.CommandSent-=Receive;channel=source;grab=grabReference;activate=activateReference;if(isActiveAndEnabled&&channel!=null)channel.CommandSent+=Receive; }
        void OnEnable(){if(channel!=null)channel.CommandSent+=Receive;}
        void OnDisable(){if(channel!=null)channel.CommandSent-=Receive;}
        void Receive(HardwareCommand command)
        { if(command==HardwareCommand.GRAB&&grab!=null)grab.TryGrab();else if(command==HardwareCommand.ACTIVATE&&activate!=null)activate.TryActivate(); }
    }
}
