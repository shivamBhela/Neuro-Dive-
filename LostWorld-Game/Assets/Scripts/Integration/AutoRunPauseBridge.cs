using System;
using System.Collections.Generic;
using UnityEngine;
namespace LostWorlds.Person2.Integration
{
    /// <summary>Person 1 implements this on an adapter; never changes locomotion itself.</summary>
    public interface IAutoRunPauseReceiver { void SetAutoRunPaused(bool paused); }

    public sealed class AutoRunPauseBridge : MonoBehaviour
    {
        readonly HashSet<UnityEngine.Object> requests=new HashSet<UnityEngine.Object>();
        IAutoRunPauseReceiver receiver;
        public bool IsPauseRequested=>requests.Count>0;
        public event Action<bool> PauseChanged;
        public void BindReceiver(IAutoRunPauseReceiver target)
        {
            if(ReferenceEquals(receiver,target))return;
            if(IsPauseRequested)Deliver(receiver,false);
            receiver=target;
            Deliver(receiver,IsPauseRequested);
        }
        public void RequestPause(UnityEngine.Object owner)
        {if(owner==null)return;bool before=IsPauseRequested;requests.Add(owner);if(before!=IsPauseRequested)Publish();}
        public void ReleasePause(UnityEngine.Object owner)
        {bool before=IsPauseRequested;requests.Remove(owner);if(before!=IsPauseRequested)Publish();}
        public void ClearRequests(){if(!IsPauseRequested)return;requests.Clear();Publish();}
        void OnDestroy(){if(IsPauseRequested)Deliver(receiver,false);}
        void Publish()
        {
            Deliver(receiver,IsPauseRequested);
            if(PauseChanged!=null)foreach(Action<bool> action in PauseChanged.GetInvocationList())
                try{action(IsPauseRequested);}catch(Exception e){Debug.LogException(e,this);}
        }
        void Deliver(IAutoRunPauseReceiver target,bool paused)
        {if(target==null||target is UnityEngine.Object obj&&obj==null)return;try{target.SetAutoRunPaused(paused);}catch(Exception e){Debug.LogException(e,this);}}
    }
}
