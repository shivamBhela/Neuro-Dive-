using UnityEngine;
using LostWorlds.Person2.Integration;
namespace LostWorlds.Person2.Journey
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RealmExitPortal : MonoBehaviour
    {
        [SerializeField] RealmBindings realm;
        public RealmBindings Realm=>realm;
        // P11 remains the only progression/duplicate guard. Its automatic reference polling remains active.
        public bool TryEnter(Transform reference)
        {var s=IntegrationSession.Instance;return Application.isPlaying&&isActiveAndEnabled&&realm!=null&&s!=null&&s.TryAdvance(realm,reference);}
        public bool TestEnter()=>realm!=null&&TryEnter(realm.Exit);
        void OnTriggerEnter(Collider other)
        {
            var s=IntegrationSession.Instance;var p=s!=null?s.PlayerReference:null;
            if(p!=null&&(other.transform==p||other.transform.IsChildOf(p)||p.IsChildOf(other.transform)))TryEnter(p);
        }
    }
}
