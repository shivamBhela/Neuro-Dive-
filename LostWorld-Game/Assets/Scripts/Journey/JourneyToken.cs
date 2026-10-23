using System;
using UnityEngine;
using LostWorlds.Person2.Integration;
namespace LostWorlds.Person2.Journey
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class JourneyToken : MonoBehaviour
    {
        [SerializeField] string tokenId;
        [SerializeField] JourneyRealm owner;
        [SerializeField] GameObject visual;
        SphereCollider zone;
        public string TokenId=>tokenId;
        public float Radius=>Zone.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y),Mathf.Abs(transform.lossyScale.z));
        public Vector3 Center=>transform.TransformPoint(Zone.center);
        SphereCollider Zone=>zone!=null?zone:zone=GetComponent<SphereCollider>();
        public bool IsCollected {get;private set;}
        public event Action<JourneyToken> Collected;
        public void Refresh(JourneyLedger ledger){IsCollected=ledger!=null&&ledger.Contains(tokenId);if(visual!=null)visual.SetActive(!IsCollected);Zone.enabled=!IsCollected;}
        public bool TryCollect(Transform reference)
        {
            if(reference==null||!reference.gameObject.activeInHierarchy)return false;
            Vector3 point=reference.position;
            if(reference.TryGetComponent<CharacterController>(out var controller))
            {
                if(!controller.enabled)return false;
                // CharacterController is a capsule, not the player's feet or a broad trigger volume.
                var scale=reference.lossyScale;
                float radius=controller.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
                float half=Mathf.Max(0,controller.height*Mathf.Abs(scale.y)*.5f-radius);
                var center=reference.TransformPoint(controller.center);var axis=reference.up;
                var closest=center+axis*Mathf.Clamp(Vector3.Dot(Center-center,axis),-half,half);
                point=closest+Vector3.ClampMagnitude(Center-closest,radius);
            }
            else if(reference.TryGetComponent<Collider>(out var collider))
            {
                if(!collider.enabled||collider.isTrigger)return false;
                point=collider is MeshCollider mesh&&!mesh.convex?collider.bounds.ClosestPoint(Center):collider.ClosestPoint(Center);
            }
            return TryPass(point,point);
        }
        public bool TestCollect()=>TryPass(Center,Center);
        public bool TryPass(Vector3 from,Vector3 to)
        {
            if(!Application.isPlaying||!isActiveAndEnabled||owner==null||!owner.Ready||IsCollected||!Zone.enabled||!Zone.isTrigger||!Finite(from)||!Finite(to))return false;
            var delta=to-from;float t=delta.sqrMagnitude>.00001f?Mathf.Clamp01(Vector3.Dot(Center-from,delta)/delta.sqrMagnitude):0;
            if((from+delta*t-Center).sqrMagnitude>Radius*Radius)return false;
            var ledger=owner.Ledger;if(ledger==null||!ledger.TryCollect(tokenId)){Refresh(ledger);return false;}
            Refresh(ledger);owner.Feedback(Center);
            if(Collected!=null)foreach(Action<JourneyToken> action in Collected.GetInvocationList())try{action(this);}catch(Exception e){Debug.LogException(e,this);}
            return true;
        }
        static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
        void OnTriggerEnter(Collider other)
        {var s=IntegrationSession.Instance;var p=s!=null?s.PlayerReference:null;if(p!=null&&(other.transform==p||other.transform.IsChildOf(p)))TryCollect(p);}
    }
}
