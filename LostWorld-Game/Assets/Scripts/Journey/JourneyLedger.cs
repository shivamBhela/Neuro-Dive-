using System;
using System.Collections.Generic;
using UnityEngine;
using LostWorlds.Person2.Integration;
namespace LostWorlds.Person2.Journey
{
    /// <summary>Optional session tally, independent from completion. Lives on P11's persistent root.</summary>
    public sealed class JourneyLedger : MonoBehaviour
    {
        readonly HashSet<string> collected=new HashSet<string>(StringComparer.Ordinal);
        IntegrationSession session;
        void OnEnable(){session=GetComponent<IntegrationSession>();if(session!=null)session.ReplayStarted+=ResetForReplay;}
        void OnDisable(){if(session!=null)session.ReplayStarted-=ResetForReplay;}
        void ResetForReplay()
        {
            collected.Clear();notifying=true;
            try{if(CountChanged!=null)foreach(Action<int> action in CountChanged.GetInvocationList())try{action(0);}catch(Exception e){Debug.LogException(e,this);}}
            finally{notifying=false;}
        }
        bool notifying;
        public int Count=>collected.Count;
        public event Action<int> CountChanged;
        public event Action<string> TokenCollected;
        public bool Contains(string id)=>!string.IsNullOrWhiteSpace(id)&&collected.Contains(id);
        public static JourneyLedger Ensure(IntegrationSession session)
        {
            if(session==null||!Application.isPlaying)return null;
            var ledger=session.GetComponent<JourneyLedger>();if(ledger==null)ledger=session.gameObject.AddComponent<JourneyLedger>();
            JourneyTokenHud.Ensure(session,ledger);return ledger;
        }
        // Trusted registry API. World tokens validate realm and proximity before calling this.
        public bool TryCollect(string id)
        {
            if(!Application.isPlaying||!isActiveAndEnabled||notifying||string.IsNullOrWhiteSpace(id)||!collected.Add(id))return false;
            notifying=true;try {
            if(TokenCollected!=null)foreach(Action<string> action in TokenCollected.GetInvocationList())try{action(id);}catch(Exception e){Debug.LogException(e,this);}
            if(CountChanged!=null)foreach(Action<int> action in CountChanged.GetInvocationList())try{action(Count);}catch(Exception e){Debug.LogException(e,this);}
            }finally{notifying=false;}return true;
        }
    }
}
