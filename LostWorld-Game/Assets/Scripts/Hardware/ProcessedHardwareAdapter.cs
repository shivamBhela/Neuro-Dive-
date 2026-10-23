using System;
using UnityEngine;
namespace LostWorlds.Person2.Hardware
{
    public sealed class ProcessedHardwareAdapter : MonoBehaviour
    {
        [SerializeField] CommandChannel channel;
        [SerializeField] LostWorlds.Person2.UI.EffortModel effortModel;
        public void BindEffort(LostWorlds.Person2.UI.EffortModel target)=>effortModel=target;
        [SerializeField] string bindAddress="127.0.0.1";
        [SerializeField,Range(1024,65535)] int port=55010;
        IProcessedMessageTransport transport;
        string startupError="",snapshotError;
        bool hasSnapshot,snapshotConnected,snapshotListening;
        int snapshotDropped;
        bool recoveryBlocked;
        public void SetRecoveryBlocked(bool blocked)
        {
            // Establish a fresh receive boundary on both sides of a realm retry.
            (transport as TcpCommandTransport)?.DiscardPending();
            recoveryBlocked=blocked;
        }
        public event Action Changed;
        public string Address=>bindAddress;
        public int Port=>port;
        public bool Connected=>transport!=null&&transport.Connected;
        public bool Listening=>transport!=null&&transport.Listening;
        public string Error=>string.IsNullOrEmpty(startupError)?transport?.Error??"":startupError;
        public HardwareCommand? LatestReceived {get;private set;}
        public int Forwarded {get;private set;}
        public int Rejected {get;private set;}
        public int Dropped=>transport?.Dropped??0;
        public string LastResult {get;private set;}="Awaiting message";
        public void Bind(CommandChannel target)=>channel=target;
        public void ConfigureEndpoint(string address,int value)
        {if(transport!=null)throw new InvalidOperationException("Stop adapter before changing endpoint.");bindAddress=address;port=value;}
        void OnEnable(){if(Application.isPlaying)StartAdapter();}
        void OnDisable()=>StopAdapter();
        void OnDestroy()=>StopAdapter();
        void OnApplicationQuit()=>StopAdapter();
        public void StartAdapter()
        {
            if(!Application.isPlaying||!isActiveAndEnabled||transport!=null)return;
            startupError="";LatestReceived=null;Forwarded=Rejected=0;LastResult="Awaiting message";
            try{transport=new TcpCommandTransport(bindAddress,port);}catch(Exception ex){startupError="Cannot listen: "+ex.Message;}
            Notify();
        }
        public void StopAdapter(){transport?.Dispose();transport=null;Notify();}
        void Update()=>Pump();
        // Main-thread consumer. Another transport can use this same parser/channel boundary later.
        public void Pump()
        {
            if(!isActiveAndEnabled||!Application.isPlaying)return;
            for(int i=0;i<16&&transport!=null&&transport.TryRead(out var message);i++)ReceiveMessage(message);
            // Compare values before formatting UI text; idle pumping needs no status-string allocation.
            bool connected=Connected,listening=Listening;string error=Error;int dropped=Dropped;
            if(!hasSnapshot||connected!=snapshotConnected||listening!=snapshotListening||error!=snapshotError||dropped!=snapshotDropped)
            {hasSnapshot=true;snapshotConnected=connected;snapshotListening=listening;snapshotError=error;snapshotDropped=dropped;Notify();}
        }
        public bool ReceiveMessage(string message)
        {
            if(!isActiveAndEnabled||!Application.isPlaying)return false;
            if(ProcessedCommandParser.TryParseEffort(message,out var effort))
            {
                if(effortModel==null)effortModel=GetComponentInChildren<LostWorlds.Person2.UI.EffortModel>(true);
                if(effortModel==null){Rejected++;LastResult="Effort target unavailable";Notify();return false;}
                effortModel.SetEffort(effort);
                LastResult="Processed effort received";Notify();return true;
            }
            if(!ProcessedCommandParser.TryParse(message,out var command)){Rejected++;LastResult="Rejected malformed/unknown command";Notify();return false;}
            if(recoveryBlocked){LastResult="Command ignored during recovery";return false;}
            LatestReceived=command;
            bool sent=channel!=null&&channel.Send(command);
            if(sent){Forwarded++;LastResult="Forwarded to P9";}else{Rejected++;LastResult="Channel unavailable; not forwarded";}
            Notify();return sent;
        }
        void Notify()
        {if(Changed!=null)foreach(Action listener in Changed.GetInvocationList())try{listener();}catch(Exception ex){Debug.LogException(ex,this);}}
    }
}
