using System;
using UnityEngine;
namespace LostWorlds.Person2.Hardware
{
    public enum HardwareCommand { LEFT, RIGHT, JUMP, GRAB, ACTIVATE }
    public interface ICommandSource { event Action<HardwareCommand> CommandSent; }
    public interface ICommandSink { bool Send(HardwareCommand command); }

    /// <summary>Shared main-thread boundary for clean commands. No device, signal or gameplay logic.</summary>
    public sealed class CommandChannel : MonoBehaviour, ICommandSource, ICommandSink
    {
        public event Action<HardwareCommand> CommandSent;
        public HardwareCommand? Latest { get { EnsureSession(); return latest; } }
        public int SentCount { get { EnsureSession(); return sentCount; } }
        HardwareCommand? latest;
        int sentCount, epoch=-1;
        static int session;
        bool notifying;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void NewSession()=>session++;
        void EnsureSession(){if(epoch==session)return;epoch=session;latest=null;sentCount=0;notifying=false;}
        public bool Send(HardwareCommand command)
        {
            EnsureSession();
            if(!Application.isPlaying || !isActiveAndEnabled || notifying || !Enum.IsDefined(typeof(HardwareCommand),command))return false;
            latest=command;sentCount++;notifying=true;
            try
            {
                if(CommandSent!=null)foreach(Action<HardwareCommand> listener in CommandSent.GetInvocationList())
                    try{listener(command);}catch(Exception error){Debug.LogException(error,this);}
            }
            finally{notifying=false;}
            return true; // Published, not necessarily accepted by any gameplay receiver.
        }
    }
}
