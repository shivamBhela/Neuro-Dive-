using UnityEngine;
using UnityEngine.UI;
namespace LostWorlds.Person2.Hardware
{
    public sealed class HardwareAdapterStatus : MonoBehaviour
    {
        [SerializeField] ProcessedHardwareAdapter adapter;
        [SerializeField] Text label;
        public string DisplayText=>label!=null?label.text:"";
        public void Bind(ProcessedHardwareAdapter source,Text target){if(adapter!=null)adapter.Changed-=Refresh;adapter=source;label=target;if(isActiveAndEnabled&&adapter!=null)adapter.Changed+=Refresh;Refresh();}
        void OnEnable(){if(adapter!=null)adapter.Changed+=Refresh;Refresh();}
        void OnDisable(){if(adapter!=null)adapter.Changed-=Refresh;}
        void Refresh()
        {
            if(label==null||adapter==null)return;
            string connection=adapter.Connected?"Connected":"Disconnected";
            label.text="HARDWARE  "+connection+"\nLatest: "+(adapter.LatestReceived?.ToString()??"none")+"   Rejected: "+adapter.Rejected+"   Dropped: "+adapter.Dropped+"\n"+(adapter.Error.Length>0?adapter.Error:adapter.LastResult);
        }
    }
}
