using System;
using UnityEngine;

namespace LostWorlds.Person2.UI
{
    public enum EffortState { Normal, NearLimit, RestNeeded }

    /// <summary>Game feedback only. Accepts a processed 0-100 value, never raw biosignals.</summary>
    public sealed class EffortModel : MonoBehaviour
    {
        [SerializeField, Range(1,100)] float cap = 55;
        [SerializeField, Range(.1f,.99f)] float nearLimitFraction = .8f;
        [SerializeField, Range(0,100)] float initialEffort;
        float value;
        bool dispatching;
        int epoch = -1;
        static int session;
        public event Action Changed;
        public event Action<EffortState> StateChanged;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void NewSession() => session++;
        void EnsureSession() { if(epoch == session) return; epoch=session; value=Mathf.Clamp(initialEffort,0,100); }
        public float Value { get { EnsureSession(); return value; } }
        public float Cap => cap;
        public float NearLimitFraction => nearLimitFraction;
        public EffortState State => Value >= cap ? EffortState.RestNeeded : (Value >= cap * nearLimitFraction || Mathf.Approximately(Value, cap * nearLimitFraction)) ? EffortState.NearLimit : EffortState.Normal;
        void OnValidate() { cap=Finite(cap)?Mathf.Clamp(cap,1,100):55; nearLimitFraction=Finite(nearLimitFraction)?Mathf.Clamp(nearLimitFraction,.1f,.99f):.8f; initialEffort=Finite(initialEffort)?Mathf.Clamp(initialEffort,0,100):0; }
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        // Call on Unity's main thread. No transport, filtering or clinical interpretation.
        public bool SetEffort(float processedValue)
        {
            if(dispatching || !Finite(processedValue)) return false;
            var next=Mathf.Clamp(processedValue,0,100); if(Value==next) return false;
            var previous=State; value=next; Publish(previous); return true;
        }
        public bool Configure(float effortCap,float nearFraction)
        {
            if(dispatching || !Finite(effortCap) || !Finite(nearFraction)) return false;
            effortCap=Mathf.Clamp(effortCap,1,100); nearFraction=Mathf.Clamp(nearFraction,.1f,.99f);
            if(cap==effortCap && nearLimitFraction==nearFraction) return false;
            var previous=State; cap=effortCap; nearLimitFraction=nearFraction; Publish(previous); return true;
        }
        public bool ResetEffort() => SetEffort(0);
        void Publish(EffortState previous)
        {
            dispatching=true;
            try
            {
                Notify(Changed, callback=>((Action)callback)());
                if(previous!=State) Notify(StateChanged, callback=>((Action<EffortState>)callback)(State));
            }
            finally { dispatching=false; }
        }
        void Notify(Delegate callbacks,Action<Delegate> invoke)
        {
            if(callbacks==null)return;
            foreach(var callback in callbacks.GetInvocationList())
                try { invoke(callback); } catch(Exception error) { Debug.LogException(error,this); }
        }
    }
}

