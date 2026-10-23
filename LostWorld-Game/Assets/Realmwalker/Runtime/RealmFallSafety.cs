using UnityEngine;
using LostWorlds.Person2.Integration;

namespace Realmwalker
{
    /// <summary>Scene-specific safety envelope; only the currently bound physical runner is monitored.</summary>
    public sealed class RealmFallSafety : MonoBehaviour
    {
        public Bounds safeBounds=new Bounds(new Vector3(0,15,45),new Vector3(70,40,115));
        public BoxCollider[] hazards=new BoxCollider[0];
        RealmBindings realm;
        void Awake()=>realm=GetComponent<RealmBindings>();
        void LateUpdate()
        {
            var s=IntegrationSession.Instance;
            if(s==null||s.CurrentRealm!=realm||!s.CommandsReady||s.PlayerReference==null)return;
            var runner=s.PlayerReference.GetComponent<RealmwalkerRunner>();
            if(runner==null||!runner.isActiveAndEnabled)return;
            var p=runner.transform.position;
            bool danger=!safeBounds.Contains(p);
            // Feet must enter a hazard below its walking surface. Normal jumps never qualify.
            foreach(var hazard in hazards)if(hazard!=null&&hazard.enabled&&hazard.gameObject.activeInHierarchy&&hazard.bounds.Contains(p)){danger=true;break;}
            if(danger)s.TryRecoverRealm(realm);
        }
    }
}
