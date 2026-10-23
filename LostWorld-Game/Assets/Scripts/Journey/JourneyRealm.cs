using UnityEngine;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Polish;
namespace LostWorlds.Person2.Journey
{
    public sealed class JourneyRealm : MonoBehaviour
    {
        [SerializeField] RealmBindings realm;
        [SerializeField] JourneyToken[] tokens=new JourneyToken[0];
        [SerializeField] RealmExitPortal portal;
        [SerializeField] RealmPolish polish;
        Transform previousReference;Vector3 previousPosition;bool initialized;
        public JourneyToken[] Tokens=>(JourneyToken[])tokens.Clone();
        public RealmExitPortal Portal=>portal;
        public JourneyLedger Ledger {get;private set;}
        public bool Ready {get{var s=IntegrationSession.Instance;return isActiveAndEnabled&&s!=null&&s.CommandsReady&&s.CurrentRealm==realm;}}
        void OnEnable(){initialized=false;previousReference=null;}
        void Update()
        {
            var s=IntegrationSession.Instance;
            if(s==null||s.CurrentRealm!=realm){previousReference=null;return;}
            if(!initialized){Ledger=JourneyLedger.Ensure(s);foreach(var token in tokens)token.Refresh(Ledger);initialized=true;}
            if(!Ready||s.PlayerReference==null||!s.PlayerReference.gameObject.activeInHierarchy){previousReference=null;return;}
            var reference=s.PlayerReference;var position=reference.position;
            // A new binding or large teleport samples only its endpoint. Normal passing sweeps the collection zone.
            var from=previousReference==reference&&(position-previousPosition).sqrMagnitude<=64?previousPosition:position;
            // A bound physical player must overlap with its actual 3D pickup shape. Swept point tests
            // remain available for controller-free reference demos, never as a feet-based magnetic pickup.
            bool physical=reference.TryGetComponent<Collider>(out _);
            foreach(var token in tokens)if(physical)token.TryCollect(reference);else token.TryPass(from,position);
            previousReference=reference;previousPosition=position;
        }
        public void Feedback(Vector3 position)
        {
            if(polish==null||!polish.isActiveAndEnabled||polish.ReducedMotion||polish.SuccessParticles==null)return;
            for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;var emit=new ParticleSystem.EmitParams{position=position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.25f,velocity=Vector3.up*.1f};polish.SuccessParticles.Emit(emit,1);}
        }
    }
}
