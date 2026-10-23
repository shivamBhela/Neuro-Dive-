using System.Linq;
using UnityEngine;
namespace LostWorlds.Person2.Integration
{
    public sealed class RealmBindings : MonoBehaviour
    {
        [SerializeField,Range(0,3)] int realmIndex;
        [SerializeField] IntegrationSession sessionPrefab;
        [SerializeField] Transform entrance,exit;
        [SerializeField] Transform[] mainPath=new Transform[0];
        [SerializeField] BranchPathBinding branch;
        [Tooltip("Optional Person-2 exit condition implementing IRealmExitGate. Empty preserves normal portals.")]
        [SerializeField] MonoBehaviour exitGate;
        [SerializeField] bool completionOnActivation;
        public bool CompletionOnActivation=>completionOnActivation&&realmIndex==3;
        public bool ExitUnlocked=>exitGate==null||exitGate is IRealmExitGate gate&&gate.CanExit;
        [SerializeField] Vector3 exitHalfExtents=new Vector3(4,3,2);
        public int RealmIndex=>realmIndex;
        public Transform Entrance=>entrance;
        public Transform Exit=>exit;
        public BranchPathBinding Branch=>branch;
        public Pose ArrivalPose=>new Pose(entrance.position,entrance.rotation);
        public Vector3[] GetMainPath()=>mainPath.Where(t=>t!=null).Select(t=>t.position).ToArray();
        public bool IsAtExit(Transform reference)
        {
            if(!isActiveAndEnabled||reference==null||!reference.gameObject.activeInHierarchy||exit==null)return false;
            var p=exit.InverseTransformPoint(reference.position);
            return Mathf.Abs(p.x)<=exitHalfExtents.x&&Mathf.Abs(p.y)<=exitHalfExtents.y&&Mathf.Abs(p.z)<=exitHalfExtents.z;
        }
        void Start()
        {
            // Additive test scenes must not replace the currently active realm.
            if(gameObject.scene!=UnityEngine.SceneManagement.SceneManager.GetActiveScene())return;
            var session=IntegrationSession.Instance;
            if(session==null&&sessionPrefab!=null)session=Instantiate(sessionPrefab);
            if(session!=null&&!session.IsTransitioning)session.EnterRealm(this);
        }
        void OnDrawGizmosSelected(){if(exit==null)return;var old=Gizmos.matrix;Gizmos.matrix=exit.localToWorldMatrix;Gizmos.color=Color.cyan;Gizmos.DrawWireCube(Vector3.zero,exitHalfExtents*2);Gizmos.matrix=old;}
    }
}
