using UnityEngine;
using LostWorlds.Person2.Integration;
namespace LostWorlds.Person2.Journey
{
    /// <summary>Requests a locomotion pause only; no movement, time scale or controller changes.</summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PortalUnlockMechanism),typeof(BoxCollider))]
    public sealed class PortalAutoRunPause : MonoBehaviour
    {
        PortalUnlockMechanism gate;
        BoxCollider zone;
        AutoRunPauseBridge bridge;
        public bool Waiting {get;private set;}
        void Awake(){gate=GetComponent<PortalUnlockMechanism>();zone=GetComponent<BoxCollider>();}
        void Update()=>SampleNow();
        public void SampleNow()
        {
            var session=IntegrationSession.Instance;
            if(!isActiveAndEnabled||session==null||gate==null||gate.Portal==null||!session.CommandsReady||session.CurrentRealm!=gate.Portal.Realm){Release();return;}
            if(gate.RevealComplete){Release();return;}
            if(Waiting)return; // Leaving the zone cannot cancel an outstanding pause.
            var reference=session.PlayerReference;
            if(gate.CanExit||reference==null)return;
            // Never freeze locomotion outside the same range required by ACTIVATE.
            if(!gate.WithinActivationRange(reference.position))return;
            Vector3 p=zone.transform.InverseTransformPoint(reference.position)-zone.center;
            Vector3 half=zone.size*.5f;
            if(Mathf.Abs(p.x)>half.x||Mathf.Abs(p.y)>half.y||Mathf.Abs(p.z)>half.z)return;
            bridge=session.AutoRunPause;Waiting=true;bridge.RequestPause(this);
        }
        void Release(){if(bridge!=null)bridge.ReleasePause(this);bridge=null;Waiting=false;}
        void OnDisable()=>Release();
    }
}
