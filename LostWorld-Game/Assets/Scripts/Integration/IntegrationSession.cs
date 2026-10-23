using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.World;
using LostWorlds.Person2.UI;
namespace LostWorlds.Person2.Integration
{
    public enum GameplayInputMode { Keyboard, Simulator, Hardware }
    public sealed class IntegrationSession : MonoBehaviour
    {
        public static IntegrationSession Instance {get;private set;}
        public static readonly string[] RealmScenes={"Assets/Scenes/Person2/Jungle_Prototype.unity","Assets/Scenes/Person2/Ruins_Prototype.unity","Assets/Scenes/Person2/CrystalCave_Prototype.unity","Assets/Scenes/Person2/HiddenCity_Prototype.unity"};
        [SerializeField] CommandChannel channel;
        [SerializeField] PlayerCommandBridge bridge;
        [SerializeField] InteractionInteractor interactor;
        [SerializeField] Person2Hud hud;
        [SerializeField] Text status;
        [SerializeField] bool automaticExitDetection=true;
        [Tooltip("Keyboard: hold W to run. Simulator: original auto-run with test keys. Hardware: auto-run, keyboard movement commands ignored.")]
        [SerializeField] GameplayInputMode inputMode=GameplayInputMode.Keyboard;
        bool requireForwardRelease;
        bool requireKeyboardRelease;
        public GameplayInputMode InputMode=>inputMode;
        public void SetInputMode(GameplayInputMode value)
        {if(inputMode==value)return;inputMode=value;requireForwardRelease=value==GameplayInputMode.Keyboard;}
        bool KeyboardAllowed()
        {
            if(inputMode==GameplayInputMode.Hardware||!CommandsReady)return false;
            if(!requireKeyboardRelease)return true;
            if(!HardwareCommandSimulator.AnyGameplayKeyHeld)requireKeyboardRelease=false;
            return false;
        }
        public bool ForwardRequested
        {
            get
            {
                if(!CommandsReady)return false;
                if(inputMode!=GameplayInputMode.Keyboard)return true;
                bool held=HardwareCommandSimulator.ForwardHeld;
                if(requireForwardRelease){if(!held)requireForwardRelease=false;return false;}
                return held;
            }
        }
        Transform player,hold;
        bool integratedInput;
        float nextContext;
        InteractableBase boundGrab,boundActivate;
        RouteChoiceZone boundRoute;
        public RealmBindings CurrentRealm {get;private set;}
        public bool IsTransitioning {get;private set;}
        public bool IsRecovering {get;private set;}
        public bool IsComplete {get;private set;}
        public bool IsSessionFinished {get;private set;}
        public event Action ReplayStarted;
        public event Action SessionFinished;
        public string LastError {get;private set;}="";
        public bool CommandsReady=>isActiveAndEnabled&&CurrentRealm!=null&&!IsTransitioning&&!IsComplete&&!IsRecovering;
        public CommandChannel Channel=>channel;
        public PlayerCommandBridge PlayerBridge=>bridge;
        public InteractionInteractor Interactor=>interactor;
        public Transform PlayerReference=>player;
        public AutoRunPauseBridge AutoRunPause {get;private set;}
        public Person2Hud Hud=>hud;
        public event Action<RealmBindings> RealmReady;
        public event Action<RealmBindings,RouteChoice,Vector3[]> BranchSelected;
        public event Action TransitionStarted;
        public event Action Completed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetInstance()=>Instance=null;
        void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            var keyboardSource=GetComponent<HardwareCommandSimulator>();if(keyboardSource!=null)keyboardSource.KeyboardPermission=KeyboardAllowed;
            if(GetComponent<SessionAudioListener>()==null)gameObject.AddComponent<SessionAudioListener>();
            AutoRunPause=GetComponent<AutoRunPauseBridge>();if(AutoRunPause==null)AutoRunPause=gameObject.AddComponent<AutoRunPauseBridge>();
        }
        void OnEnable(){if(channel!=null)channel.CommandSent+=Interact;}
        void OnDisable(){if(channel!=null)channel.CommandSent-=Interact;}
        void OnDestroy(){if(CurrentRealm!=null&&CurrentRealm.Branch!=null)CurrentRealm.Branch.PathSelected-=SelectedPath;if(Instance==this)Instance=null;}
        public void EnterRealm(RealmBindings realm)
        {
            if(realm==null||realm==CurrentRealm)return;
            if(CurrentRealm!=null&&CurrentRealm.Branch!=null)CurrentRealm.Branch.PathSelected-=SelectedPath;
            CurrentRealm=realm;IsComplete=false;IsTransitioning=false;LastError="";
            if(IsRecovering){foreach(var adapter in GetComponentsInChildren<ProcessedHardwareAdapter>(true))adapter.SetRecoveryBlocked(false);IsRecovering=false;}
            AutoRunPause.ClearRequests();
            BindPlayer(null,null);
            if(realm.Branch!=null)realm.Branch.PathSelected+=SelectedPath;
            RefreshStatus();Notify(RealmReady,callback=>((Action<RealmBindings>)callback)(realm));
        }
        /// <summary>References only. Person 1 owns spawning, positioning, camera and locomotion.</summary>
        public void BindPlayer(Transform reference,Transform holdPoint=null)
        {
            player=reference;hold=holdPoint;interactor.Reference=reference;interactor.HoldPoint=holdPoint;interactor.enabled=reference!=null;
            AutoRunPause.BindReceiver(reference==null?null:reference.GetComponentsInParent<MonoBehaviour>(true).OfType<IAutoRunPauseReceiver>().FirstOrDefault());
            // Preserve standalone tests until integration is bound; suppress duplicate input in later realms too.
            if(reference!=null)integratedInput=true;
            if(integratedInput&&CurrentRealm!=null)
            {
                foreach(var root in CurrentRealm.gameObject.scene.GetRootGameObjects())
                {
                    foreach(var test in root.GetComponentsInChildren<P4KeyboardTestInput>(true))test.enabled=false;
                    foreach(var test in root.GetComponentsInChildren<P6RouteTestInput>(true))test.enabled=false;
                }
            }
            RefreshContext(true);RefreshStatus();
        }
        public bool ConsumeRouteDirection(HardwareCommand command)
        {
            var branch=CurrentRealm!=null?CurrentRealm.Branch:null;
            if(!CommandsReady||branch==null||!branch.Zone.Contains(player))return false;
            branch.Zone.TryChoose(command==HardwareCommand.LEFT?RouteChoice.Left:RouteChoice.Right,player);
            return true; // The junction owns directions even after its choice is locked.
        }
        void SelectedPath(RouteChoice choice,Vector3[] points)
        {Notify(BranchSelected,callback=>((Action<RealmBindings,RouteChoice,Vector3[]>)callback)(CurrentRealm,choice,(Vector3[])points.Clone()));}
        void Interact(HardwareCommand command)
        {
            if(!CommandsReady||player==null)return;
            RefreshContext(false);
            if(command==HardwareCommand.GRAB)interactor.TryGrab();else if(command==HardwareCommand.ACTIVATE)interactor.TryActivate();
        }
        void Update()
        {
            if(Time.unscaledTime>=nextContext){nextContext=Time.unscaledTime+.15f;RefreshContext(false);RefreshStatus();}
            if(automaticExitDetection&&CommandsReady&&player!=null)TryAdvance(CurrentRealm,player);
        }
        InteractableBase Nearest(InteractionCommand command)
        {
            if(player==null||!CommandsReady)return null;
            return Physics.OverlapSphere(interactor.Origin,interactor.Range,~0,QueryTriggerInteraction.Collide)
                .Select(c=>c.GetComponentInParent<InteractableBase>()).Where(t=>t!=null&&t.Command==command&&t.CanInteract(interactor)&&interactor.IsInRange(t))
                .Distinct().OrderBy(t=>(t.ClosestInteractionPoint(interactor.Origin)-interactor.Origin).sqrMagnitude).FirstOrDefault();
        }
        public void RefreshContext(bool force)
        {
            if(hud==null)return;
            var grab=Nearest(InteractionCommand.Grab);var activate=Nearest(InteractionCommand.Activate);
            var route=CommandsReady&&CurrentRealm.Branch!=null&&CurrentRealm.Branch.Zone.Contains(player)?CurrentRealm.Branch.Zone:null;
            if(force||grab!=boundGrab||activate!=boundActivate||route!=boundRoute)
            {boundGrab=grab;boundActivate=activate;boundRoute=route;hud.Context.SetContext(grab,interactor,activate,interactor,route,player);}
            else hud.Context.SampleNow();
        }
        /// <summary>Trusted integration/exit hook. Requires the current realm and a reference inside its exit volume.</summary>
        public bool TryAdvance(RealmBindings source,Transform reference)
        {
            if(!CommandsReady||source!=CurrentRealm||source.CompletionOnActivation||!source.ExitUnlocked||!source.IsAtExit(reference))return false;
            if(source.RealmIndex==3)
            {IsComplete=true;RefreshContext(true);RefreshStatus();Notify(Completed,callback=>((Action)callback)());return true;}
            var next=RealmScenes[source.RealmIndex+1];
            if(!Application.CanStreamedLevelBeLoaded(next)){LastError="Next realm is not in Build Settings";RefreshStatus();return false;}
            IsTransitioning=true;LastError="";RefreshContext(true);RefreshStatus();
            Notify(TransitionStarted,callback=>((Action)callback)());StartCoroutine(LoadRealm(next));return true;
        }
        /// <summary>Final-console completion. The authored gate must finish its accepted activation first.</summary>
        public bool TryCompleteActivation(RealmBindings source)
        {
            if(!CommandsReady||source!=CurrentRealm||!source.CompletionOnActivation||!source.ExitUnlocked)return false;
            IsComplete=true;RefreshContext(true);RefreshStatus();Notify(Completed,callback=>((Action)callback)());return true;
        }
        IEnumerator LoadRealm(string scenePath)
        {
            AsyncOperation operation;
            try{operation=SceneManager.LoadSceneAsync(scenePath,LoadSceneMode.Single);}
            catch(Exception ex){LastError=ex.Message;IsTransitioning=false;RefreshStatus();yield break;}
            if(operation==null){LastError="Scene loading failed";IsTransitioning=false;RefreshStatus();yield break;}
            yield return operation;
            var realm=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<RealmBindings>(true)).SingleOrDefault();
            if(realm==null){LastError="Loaded scene has no RealmBindings";IsTransitioning=false;RefreshStatus();yield break;}
            EnterRealm(realm);
        }
        /// <summary>Retry only this realm. The session ledger and earlier realm progress survive;
        /// scene-local gates and encounters reset naturally through the existing loader.</summary>
        public bool TryRecoverRealm(RealmBindings source)
        {
            if(!CommandsReady||source!=CurrentRealm||!Application.CanStreamedLevelBeLoaded(source.gameObject.scene.path))return false;
            IsRecovering=true;IsTransitioning=true;LastError="";
            requireForwardRelease=true;
            requireKeyboardRelease=true;
            AutoRunPause.RequestPause(this);
            foreach(var adapter in GetComponentsInChildren<ProcessedHardwareAdapter>(true))adapter.SetRecoveryBlocked(true);
            RefreshContext(true);RefreshStatus();hud?.ShowFeedback("Let's try that path again!");
            StartCoroutine(RecoverRealm(source.gameObject.scene.path));return true;
        }
        IEnumerator RecoverRealm(string path)
        {yield return new WaitForSecondsRealtime(1.5f);yield return LoadRealm(path);}
        /// <summary>Fresh controller-free journey through the existing scene loader.</summary>
        public bool ReplayJourney()
        {
            if(!IsComplete||IsTransitioning||!Application.CanStreamedLevelBeLoaded(RealmScenes[0]))return false;
            IsTransitioning=true;IsComplete=false;IsSessionFinished=false;
            Notify(ReplayStarted,callback=>((Action)callback)());AutoRunPause.ClearRequests();BindPlayer(null);
            StartCoroutine(LoadRealm(RealmScenes[0]));return true;
        }
        /// <summary>Ends the session without closing the Editor or application.</summary>
        public bool FinishJourney()
        {
            if(!IsComplete||IsTransitioning||IsSessionFinished)return false;
            IsSessionFinished=true;AutoRunPause.RequestPause(this);
            Notify(SessionFinished,callback=>((Action)callback)());return true;
        }
        void RefreshStatus()
        {
            if(status==null)return;
            status.text=LastError.Length>0?LastError:IsRecovering?"Let's try that path again!":IsComplete?"Journey Complete":IsTransitioning?"Entering next realm…":CurrentRealm==null?"Waiting for realm":CurrentRealm.gameObject.scene.name.Replace("_Prototype","")+" • "+(player==null?"Player not connected":"Player reference connected");
        }
        void Notify(Delegate callbacks,Action<Delegate> invoke){if(callbacks!=null)foreach(var callback in callbacks.GetInvocationList())try{invoke(callback);}catch(Exception ex){Debug.LogException(ex,this);}}
    }
}
