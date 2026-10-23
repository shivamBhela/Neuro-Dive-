using UnityEngine;
using UnityEngine.UI;
namespace LostWorlds.Person2.Hardware
{
    public sealed class CommandDebugFeedback : MonoBehaviour
    {
        [SerializeField] CommandChannel channel;
        [SerializeField] Text label;
        [SerializeField] bool logCommands=true;
        public string DisplayText=>label!=null?label.text:"";
        public void Bind(CommandChannel source,Text target)
        {if(channel!=null)channel.CommandSent-=Received;channel=source;label=target;if(isActiveAndEnabled&&channel!=null)channel.CommandSent+=Received;Refresh();}
        void OnEnable(){if(channel!=null)channel.CommandSent+=Received;Refresh();}
        void OnDisable(){if(channel!=null)channel.CommandSent-=Received;}
        void Received(HardwareCommand command){Refresh();if(logCommands)Debug.Log("[P9 simulator] SENT "+command+" (delivery does not imply gameplay acceptance)",this);}
        void Refresh(){if(label!=null)label.text=channel==null||!channel.Latest.HasValue?"SIMULATOR • Awaiting command":"SENT: "+channel.Latest.Value+(channel.Latest==HardwareCommand.JUMP?" • event only, no movement":" • #"+channel.SentCount);}
    }
}
