using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Polish;
using LostWorlds.Person2.UI;
using LostWorlds.Person2.World;
using Object = UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    // Editor-only QA. Creates temporary references, never a controller or saved gameplay object.
    [InitializeOnLoad]
    public static class P13FinalQA
    {
        const string Pending="P13.pending", Done="P13.done", Passed="P13.passed";
        static IEnumerator routine;
        static double deadline;
        static int checks;
        static readonly List<string> report=new List<string>();
        static SceneSetup[] previous;
        static bool consoleFailed;
        static P13FinalQA(){EditorApplication.playModeStateChanged+=State;}

        [MenuItem("Tools/Lost Worlds/P13/Play Final Test")]
        public static void Play()=>P125JourneyBuilder.Play();

        [MenuItem("Tools/Lost Worlds/P13/Run Final QA")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Begin();
        }
        public static void RunBatch()=>EditorApplication.delayCall+=Begin;
        static void Begin()
        {
            checks=0;report.Clear();consoleFailed=false;previous=EditorSceneManager.GetSceneManagerSetup();
            // A fresh batch Editor may start with no loaded scene; validators restore their input scene setup.
            if(!previous.Any(s=>s.isLoaded&&s.isActive))
            {EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);previous=EditorSceneManager.GetSceneManagerSetup();}
            Application.logMessageReceived+=ConsoleMessage;
            try
            {
                Audit();EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);
                SessionState.SetBool(Passed,false);SessionState.SetBool(Done,false);SessionState.SetBool(Pending,true);
                EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);Finish(false);}
        }
        static void ConsoleMessage(string message,string trace,LogType type)
        {
            if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert||type==LogType.Warning)
            {consoleFailed=true;report.Add("CONSOLE "+type+": "+message);}
        }
        static void Check(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException("P13 FAIL: "+message);
            checks++;report.Add("PASS "+message);
        }
        static void Audit()
        {
            RelicRemovalValidation.Validate();P85ObstacleBuilder.Validate();P12PolishBuilder.Validate();P125JourneyBuilder.Validate();
            foreach(string path in IntegrationSession.RealmScenes)
            {
                Check(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==path),"Build Settings: "+path);
                var scene=EditorSceneManager.OpenScene(path);var root=scene.GetRootGameObjects().Single();
                foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)!=0)throw new Exception("Missing script: "+transform.name);
                Check(true,"No missing scripts: "+scene.name);
                var renderers=root.GetComponentsInChildren<Renderer>(true);
                foreach(var renderer in renderers)
                    foreach(var material in renderer.sharedMaterials)
                        if(material==null||material.shader==null||!material.shader.isSupported||ShaderUtil.ShaderHasError(material.shader))throw new Exception("Missing/unsupported material shader: "+renderer.name);
                Check(true,"Materials/shaders valid: "+scene.name);
                var particles=root.GetComponentsInChildren<ParticleSystem>(true);
                int budget=particles.Sum(p=>p.main.maxParticles);
                bool finalRealm=scene.path==IntegrationSession.RealmScenes[3];Check(particles.Length==(finalRealm?3:4)&&budget==(finalRealm?64:76),"Realm particle budget excludes retired final portal: "+scene.name);
                var portal=root.GetComponentInChildren<RealmExitPortal>(true);
                Check(portal.GetComponentsInChildren<Collider>(true).All(c=>c.isTrigger),"Portal has no blocking colliders: "+scene.name);
                Check(portal.GetComponentsInChildren<Light>(true).Length==0,"Portal adds no dynamic lights: "+scene.name);
                report.Add("INVENTORY "+scene.name+": renderers="+renderers.Length+", colliders="+root.GetComponentsInChildren<Collider>(true).Length+", lights="+root.GetComponentsInChildren<Light>(true).Length+", particle cap="+budget);
            }
        }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))
            {SessionState.SetBool(Pending,false);routine=Journey();deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Done,false))
            {SessionState.SetBool(Done,false);EditorApplication.delayCall+=Restore;}
        }
        static void Tick()
        {
            try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Final QA timeout");if(routine.MoveNext())return;Check(!consoleFailed,"No Console warnings/errors during QA");Finish(true);}
            catch(Exception e){Debug.LogException(e);Finish(false);}
        }
        static void Finish(bool success)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=ConsoleMessage;
            SessionState.SetBool(Passed,success);report.Add("P13 RESULT: "+checks+" checks; passed="+success);
            Directory.CreateDirectory("Documentation/P13");File.WriteAllLines("Documentation/P13/FINAL_QA.txt",report);
            Debug.Log(report.Last());
            if(EditorApplication.isPlaying){SessionState.SetBool(Done,true);EditorApplication.isPlaying=false;}else Restore();
        }
        static void Restore()
        {
            try{if(previous!=null)EditorSceneManager.RestoreSceneManagerSetup(previous);}
            finally{if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool(Passed,false)?0:1);}
        }
        static JourneyRealm World(IntegrationSession session)=>session.CurrentRealm.transform.root.GetComponentInChildren<JourneyRealm>();
        static IEnumerator Journey()
        {
            while(IntegrationSession.Instance==null||IntegrationSession.Instance.CurrentRealm==null||World(IntegrationSession.Instance).Ledger==null)yield return null;
            var session=IntegrationSession.Instance;var ledger=World(session).Ledger;
            var gate=session.CurrentRealm.transform.root.GetComponentInChildren<PortalUnlockMechanism>();
            Check(gate!=null&&!gate.CanExit&&!gate.Portal.gameObject.activeInHierarchy,"Jungle portal starts invisible and locked");
            Check(!gate.Portal.TestEnter()&&!session.TryAdvance(session.CurrentRealm,session.CurrentRealm.Exit),"Portal and direct P11 calls reject locked exit");
            var gateProbe=new GameObject("PortalUnlock_QA_REFERENCE").transform;gateProbe.position=session.CurrentRealm.Exit.position;session.BindPlayer(gateProbe);
            yield return null;yield return null;Check(!session.IsTransitioning,"Automatic P11 exit polling cannot bypass lever lock");
            gateProbe.position=Vector3.one*1000;Physics.SyncTransforms();session.Channel.Send(HardwareCommand.ACTIVATE);Check(!gate.CanExit,"Distant ACTIVATE safely rejected; retry remains available");
            gateProbe.position=gate.transform.position;Physics.SyncTransforms();int unlockEvents=0;gate.OnSucceeded.AddListener(()=>unlockEvents++);
            session.Channel.Send(HardwareCommand.ACTIVATE);Check(gate.CanExit&&gate.HasInteracted&&gate.Portal.gameObject.activeInHierarchy,"Processed ACTIVATE unlocks and reveals existing portal");
            Check(!gate.TestActivate()&&unlockEvents==1,"Repeated activation cannot duplicate success");session.BindPlayer(null);Object.Destroy(gateProbe.gameObject);
            float revealUntil=Time.unscaledTime+1;while(Time.unscaledTime<revealUntil)yield return null;
            // Reproduce the lost nonserialized cache seen after a Play-mode script reload.
            var portalEffect=World(session).Portal.GetComponentInChildren<PortalAtmosphere>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var cache=typeof(PortalAtmosphere).GetField("properties",flags);
            cache.SetValue(portalEffect,null);
            typeof(PortalAtmosphere).GetField("polish",flags).SetValue(portalEffect,null);
            portalEffect.SendMessage("Update");
            Check(cache.GetValue(portalEffect)!=null,"Portal recovers lost runtime cache without Awake");
            portalEffect.enabled=false;cache.SetValue(portalEffect,null);portalEffect.enabled=true;
            Check(cache.GetValue(portalEffect)!=null,"Portal restores runtime cache on re-enable");
            Check(ledger.Count==0,"Fresh journey starts with 0 tokens");
            int transitions=0,completion=0,tokenEvents=0,commands=0,branches=0;
            session.TransitionStarted+=()=>transitions++;session.Completed+=()=>completion++;ledger.TokenCollected+=id=>tokenEvents++;
            session.PlayerBridge.PlayerCommandRequested+=command=>commands++;session.BranchSelected+=(r,c,p)=>branches++;
            var probe=new GameObject("P13_QA_REFERENCE_NO_CONTROLLER").transform;Object.DontDestroyOnLoad(probe.gameObject);
            var effort=session.GetComponentInChildren<EffortModel>(true);Check(effort!=null,"Persistent effort model exists");
            effort.SetEffort(effort.Cap*effort.NearLimitFraction);Check(effort.State==EffortState.NearLimit,"Near Limit feedback");
            effort.SetEffort(effort.Cap);Check(effort.State==EffortState.RestNeeded&&session.CommandsReady,"Rest feedback does not block play");
            int expectedTokens=0;
            for(int index=0;index<4;index++)
            {
                while(World(session).Ledger==null)yield return null;
                var world=World(session);var realm=session.CurrentRealm;
                Check(session.Hud.transform.Find("RelicStatus")==null,"No retired counter in persistent HUD "+index);
                Check(realm.RealmIndex==index,"Correct journey realm "+index);
                Check(world.Ledger==ledger,"Same persistent token ledger "+index);
                probe.position=realm.Entrance.position;session.BindPlayer(probe,probe);Physics.SyncTransforms();
                var initial=probe.position;int before=commands;session.Channel.Send(HardwareCommand.JUMP);
                Check(commands==before+1&&probe.position==initial,"JUMP callback only, no player movement "+index);
                if(index==0)Check(realm.Branch==null&&realm.transform.root.GetComponentsInChildren<RouteChoiceZone>(true).Length==0,"Jungle has no route-choice requirement");
                if(realm.Branch!=null)
                {
                    probe.position=realm.Branch.Zone.transform.position;Physics.SyncTransforms();int selected=branches;
                    session.Channel.Send(HardwareCommand.LEFT);session.Channel.Send(HardwareCommand.RIGHT);
                    Check(branches==selected+1&&realm.Branch.Zone.Selection==RouteChoice.Left,"One LEFT route accepted "+index);
                    realm.Branch.Zone.BeginNewEncounter();session.Channel.Send(HardwareCommand.RIGHT);session.Channel.Send(HardwareCommand.RIGHT);
                    Check(branches==selected+2&&realm.Branch.Zone.Selection==RouteChoice.Right,"RIGHT accepted once in fresh encounter "+index);
                }
                foreach(var target in realm.transform.root.GetComponentsInChildren<InteractableBase>().ToArray())
                {
                    if(realm.CompletionOnActivation&&target is PortalUnlockMechanism)continue;
                    if(target is PortalUnlockMechanism&&target.HasInteracted){Check(((PortalUnlockMechanism)target).CanExit,"Unlock remains latched for the realm session");continue;}
                    float wait=Time.unscaledTime+.3f;while(Time.unscaledTime<wait)yield return null;
                    probe.position=target.transform.position;Physics.SyncTransforms();session.RefreshContext(true);
                    session.Channel.Send(target.Command==InteractionCommand.Grab?HardwareCommand.GRAB:HardwareCommand.ACTIVATE);
                    Check(target.HasInteracted,"P4 channel interaction: "+target.name);Check(!target.TryInteract(session.Interactor),"Repeated interaction rejected");
                }
                // All 36 are deliberately covered, including both optional branches; this test does not move a player.
                session.BindPlayer(null);
                foreach(var token in world.Tokens){token.TestCollect();Check(token.IsCollected&&!token.TestCollect(),"Token collected once: "+token.TokenId);}
                expectedTokens+=world.Tokens.Length;
                Check(ledger.Count==expectedTokens&&tokenEvents==expectedTokens,"Exactly one event per persistent token "+index);
                Check(session.Hud.GetComponentInChildren<JourneyTokenHud>().DisplayText=="Journey Tokens: "+expectedTokens,"Token HUD tally "+index);
                var polish=realm.transform.root.GetComponentInChildren<RealmPolish>();polish.ReducedMotion=true;yield return null;yield return null;
                Check(realm.transform.root.GetComponentsInChildren<ParticleSystem>().All(p=>!p.isPlaying&&p.particleCount==0),"Reduced Motion clears all realm/portal particles "+index);polish.ReducedMotion=false;
                Check(!world.Portal.TryEnter(realm.Entrance),"Portal rejects outside reference "+index);
                if(realm.CompletionOnActivation){Check(world.transform.root.GetComponentInChildren<PortalUnlockMechanism>().TestActivate(),"Final console accepts ACTIVATE");while(!session.IsComplete)yield return null;}else Check(world.Portal.TestEnter(),"Existing portal advances "+index);Check(!world.Portal.TestEnter(),"Repeated portal ignored "+index);
                if(index<3){while(session.IsTransitioning)yield return null;Check(IntegrationSession.Instance==session&&session.PlayerReference==null,"Session persists; arrival awaits Person 1 binding");}
            }
            Check(session.IsComplete&&completion==1&&transitions==3,"Four realms to one Journey Complete");
            Check(ledger.Count==36,"Full journey retains 36 tokens");
            Check(Object.FindObjectsByType<IntegrationSession>().Length==1,"Exactly one persistent session");
            Check(effort.State==EffortState.RestNeeded,"Rest state survives without penalizing completion");effort.ResetEffort();Check(effort.State==EffortState.Normal,"Effort reset remains available");
            Object.Destroy(probe.gameObject);
        }
    }
}
