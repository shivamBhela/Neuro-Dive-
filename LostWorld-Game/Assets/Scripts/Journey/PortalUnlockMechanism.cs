using System.Collections;
using UnityEngine;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.Polish;

namespace LostWorlds.Person2.Journey
{
    /// <summary>Reusable ACTIVATE receiver. Owns presentation/unlock state, never scene loading or input.</summary>
    public sealed class PortalUnlockMechanism : InteractableBase,IRealmExitGate
    {
        [SerializeField] RealmExitPortal portal;
        [SerializeField] GameObject[] revealObjects=new GameObject[0];
        [SerializeField] Transform handle;
        [SerializeField] Vector3 activatedEuler=new Vector3(-45,0,0);
        [SerializeField,Min(.1f)] float revealSeconds=.8f;
        [SerializeField] TextMesh prompt;
        [SerializeField] AudioSource revealAudio;
        [SerializeField] RealmPolish polish;
        [SerializeField] InteractionInteractor testInteractor;
        [Tooltip("Optional strict distance to the authored interaction point; zero preserves legacy realm behavior.")]
        [SerializeField,Min(0)] float activationRadius;
        [Tooltip("Optional gateway-local zone. Its width covers running positions; activationRadius is a short margin around it.")]
        [SerializeField] BoxCollider activationZone;
        [SerializeField] Renderer[] trophyRenderers=new Renderer[0];
        MaterialPropertyBlock trophyProperties;
        bool IsFinalConsole=>portal!=null&&portal.Realm!=null&&portal.Realm.CompletionOnActivation;
        bool IRealmExitGate.CanExit=>IsFinalConsole?RevealComplete:CanExit;
        Quaternion restingRotation;Vector3 portalScale;Coroutine reveal;
        public bool CanExit {get;private set;}
        public bool RevealComplete {get;private set;}
        public RealmExitPortal Portal=>portal;
        public override InteractionCommand Command=>InteractionCommand.Activate;
        void Awake()
        {
            restingRotation=handle!=null?handle.localRotation:Quaternion.identity;
            if(portal!=null){portalScale=portal.transform.localScale;portal.gameObject.SetActive(false);}
            foreach(var visual in revealObjects)if(visual!=null)visual.SetActive(false);
            if(prompt!=null)prompt.text=IsFinalConsole?"Press E to Activate Trophy":"Press E to Activate Portal";
            if(IsFinalConsole)Illuminate(0);
        }
        public override bool CanInteract(InteractionInteractor interactor)
        {
            var session=IntegrationSession.Instance;
            return base.CanInteract(interactor)&&WithinActivationRange(interactor.Origin)&&portal!=null&&!CanExit&&session!=null&&session.CommandsReady&&session.CurrentRealm==portal.Realm;
        }
        public bool WithinActivationRange(Vector3 origin)=>activationRadius<=0||(ClosestInteractionPoint(origin)-origin).sqrMagnitude<=activationRadius*activationRadius;
        public override Vector3 ClosestInteractionPoint(Vector3 origin)=>activationZone!=null&&activationZone.enabled?activationZone.ClosestPoint(origin):base.ClosestInteractionPoint(origin);
        protected override bool ApplyInteraction(InteractionInteractor interactor)
        {
            if(portal==null)return false;
            // Latches once on accepted ACTIVATE. All repeated requests still pass through InteractableBase.
            CanExit=true;
            if(!IsFinalConsole){portal.gameObject.SetActive(true);foreach(var visual in revealObjects)if(visual!=null)visual.SetActive(true);}
            if(prompt!=null)prompt.text=IsFinalConsole?"Journey complete — well done!":"Portal ready — proceed";
            if(revealAudio!=null&&revealAudio.clip!=null&&(polish==null||!polish.Muted))revealAudio.Play();
            if(!IsFinalConsole&&polish!=null&&polish.ReducedMotion)FinishVisuals();else reveal=StartCoroutine(Reveal());
            return true;
        }
        IEnumerator Reveal()
        {
            // Defer completion until InteractableBase has latched success, including Reduced Motion.
            if(IsFinalConsole)yield return null;
            float elapsed=0;
            while(elapsed<revealSeconds)
            {
                if(polish!=null&&polish.ReducedMotion)break;
                elapsed+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/revealSeconds));
                if(handle!=null)handle.localRotation=Quaternion.Slerp(restingRotation,restingRotation*Quaternion.Euler(activatedEuler),t);
                if(IsFinalConsole)Illuminate(t);else if(portal!=null)portal.transform.localScale=portalScale*Mathf.Lerp(.15f,1,t);
                yield return null;
            }
            FinishVisuals();reveal=null;
            if(IsFinalConsole){GetComponent<PortalAutoRunPause>()?.SampleNow();IntegrationSession.Instance?.TryCompleteActivation(portal.Realm);}
        }
        void FinishVisuals(){if(handle!=null)handle.localRotation=restingRotation*Quaternion.Euler(activatedEuler);if(portal!=null&&CanExit&&!IsFinalConsole){portal.gameObject.SetActive(true);portal.transform.localScale=portalScale;}RevealComplete=CanExit;if(IsFinalConsole)Illuminate(1);}
        void Illuminate(float amount)
        {
            if(trophyProperties==null)trophyProperties=new MaterialPropertyBlock();
            foreach(var renderer in trophyRenderers)if(renderer!=null){renderer.GetPropertyBlock(trophyProperties);trophyProperties.SetColor("_EmissionColor",new Color(.5f,.28f,.035f)*amount);renderer.SetPropertyBlock(trophyProperties);}
        }
        void Update(){if(revealAudio!=null&&polish!=null)revealAudio.mute=polish.Muted;if(prompt!=null&&activationRadius>0){var s=IntegrationSession.Instance;prompt.gameObject.SetActive(CanExit||(s!=null&&s.PlayerReference!=null&&WithinActivationRange(s.PlayerReference.position)));}}
        void OnDisable(){if(reveal!=null){StopCoroutine(reveal);reveal=null;}if(CanExit)FinishVisuals();if(revealAudio!=null)revealAudio.Stop();}
        /// <summary>Controller-free test only. Uses the same interaction/range/one-shot path.</summary>
        public bool TestActivate()=>Application.isPlaying&&testInteractor!=null&&TryInteract(testInteractor);
    }
}
