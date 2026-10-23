using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.Polish;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
    [InitializeOnLoad] public static class PortalUnlockValidation
    {
        static int RealmIndex=>SessionState.GetInt("PortalUnlock.realm",0);
        static IEnumerator routine;static double deadline;static int checks;static bool errors;
        sealed class PauseReceiver : IAutoRunPauseReceiver
        {
            public bool Paused;public int Pauses,Resumes;
            public void SetAutoRunPaused(bool value){if(value!=Paused){if(value)Pauses++;else Resumes++;}Paused=value;}
        }
        static PortalUnlockValidation(){EditorApplication.playModeStateChanged+=State;}
        public static void RunBatch()=>EditorApplication.delayCall+=()=>{if(Environment.GetCommandLineArgs().Contains("-portalCity"))BeginCity();else if(Environment.GetCommandLineArgs().Contains("-portalCave"))BeginCave();else if(Environment.GetCommandLineArgs().Contains("-portalRuins"))BeginRuins();else Begin();};
        [MenuItem("Tools/Lost Worlds/Hidden City/Run Finale Checks")]
        public static void BeginCity()=>StartChecks(3);
        [MenuItem("Tools/Lost Worlds/Crystal Cave/Run Portal and Pause Checks")]
        public static void BeginCave()=>StartChecks(2);
        [MenuItem("Tools/Lost Worlds/Ruins/Run Portal and Pause Checks")]
        public static void BeginRuins()=>StartChecks(1);
        [MenuItem("Tools/Lost Worlds/Jungle/Run Portal Unlock Checks")]
        public static void Begin()=>StartChecks(0);
        static void StartChecks(int realm)
        {if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;SessionState.SetInt("PortalUnlock.realm",realm);EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[realm]);SessionState.SetBool("PortalUnlock.pending",true);EditorApplication.isPlaying=true;}
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PortalUnlock.pending",false)){SessionState.SetBool("PortalUnlock.pending",false);checks=0;errors=false;Application.logMessageReceived+=Log;routine=Run();deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("PortalUnlock.done",false)){SessionState.SetBool("PortalUnlock.done",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool("PortalUnlock.pass",false)?0:1);}
        }
        static void Log(string message,string trace,LogType type){if(type==LogType.Warning||type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors=true;}
        static void Check(bool value,string message){if(!value)throw new Exception("PORTAL UNLOCK FAIL: "+message);checks++;Debug.Log("PORTAL UNLOCK PASS: "+message);}
        static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Unlock check timeout");if(routine.MoveNext())return;Check(!errors,"No runtime warnings/errors");End(true);}catch(Exception e){Debug.LogException(e);End(false);}}
        static void End(bool pass){EditorApplication.update-=Tick;Application.logMessageReceived-=Log;(routine as IDisposable)?.Dispose();SessionState.SetBool("PortalUnlock.pass",pass);SessionState.SetBool("PortalUnlock.done",true);Debug.Log($"PORTAL UNLOCK RESULT: {checks} checks; passed={pass}");EditorApplication.isPlaying=false;}
        static IEnumerator Run()
        {
            while(IntegrationSession.Instance==null||IntegrationSession.Instance.CurrentRealm==null)yield return null;
            if(RealmIndex==3){var trophyChecks=RunTrophy();while(trophyChecks.MoveNext())yield return trophyChecks.Current;yield break;}
            var s=IntegrationSession.Instance;var root=s.CurrentRealm.transform.root;var gate=root.GetComponentInChildren<PortalUnlockMechanism>();
            Check(gate!=null&&!gate.CanExit&&!gate.Portal.gameObject.activeInHierarchy,"Fresh realm starts locked/invisible");
            Check(gate.Portal.GetComponentsInChildren<Renderer>(true).All(r=>!r.gameObject.activeInHierarchy),"All portal renderers inactive");
            Check(gate.Portal.GetComponentsInChildren<Collider>(true).All(c=>!c.gameObject.activeInHierarchy),"All portal triggers inactive");
            var frame=root.GetComponentsInChildren<Transform>(true).First(t=>t.name==(RealmIndex==0?"JungleExit_ThresholdOnly":RealmIndex==1?"CaveThreshold":RealmIndex==2?"CityLightPortal":"SanctuaryColonnade"));
            Check(frame.gameObject.activeInHierarchy==(RealmIndex!=3),"Portal-realm stone gateway visible at startup; Hidden City policy preserved");
            Check(!gate.Portal.TestEnter()&&!s.TryAdvance(s.CurrentRealm,s.CurrentRealm.Exit),"Controller-free and P11 locked entry rejected");
            Capture(root,gate,"Locked");
            if(RealmIndex==3){var trophy=root.Find("FinalCityArchitecture/GoldenJourneyTrophy_DisplayOnly");Check(trophy!=null&&trophy.gameObject.activeInHierarchy&&trophy.position==gate.Portal.transform.position,"Existing trophy visible at exact former portal position");Check(!s.TryCompleteActivation(s.CurrentRealm),"Direct completion API rejects unactivated console");Check(root.GetComponent<JourneyFinale>().AwardCount==0&&!s.ReplayJourney()&&!s.FinishJourney(),"No premature award, replay or finish");foreach(var token in root.GetComponentInChildren<JourneyRealm>().Tokens.Take(2))token.TestCollect();}
            var simulator=s.GetComponentInChildren<HardwareCommandSimulator>(true);Check(simulator!=null,"Existing keyboard simulator retained");
            var probe=new GameObject("UnlockTest_REFERENCE_ONLY").transform;Keyboard keyboard=null;var settings=InputSystem.settings;var background=settings.backgroundBehavior;var editor=settings.editorInputBehaviorInPlayMode;
            try
            {
                probe.position=gate.transform.position+new Vector3(-5,1,-5);s.BindPlayer(probe);Physics.SyncTransforms();Check(s.Interactor.IsInRange(gate),"Forgiving range includes early main-road approach");
                var receiver=new PauseReceiver();s.AutoRunPause.BindReceiver(receiver);
                var pause=gate.GetComponent<PortalAutoRunPause>();Check(pause!=null,"Jungle has reusable lever pause zone");pause.SampleNow();
                Check(pause.Waiting&&receiver.Paused&&receiver.Pauses==1,"Approach requests one auto-run pause through receiver hook");
                float waitUntil=Time.unscaledTime+2;while(Time.unscaledTime<waitUntil){pause.SampleNow();if(RealmIndex!=3&&!frame.gameObject.activeInHierarchy)throw new Exception("Gateway disappeared while locked");yield return null;}
                Check(receiver.Paused&&receiver.Pauses==1&&!gate.CanExit,"Waiting neither times out nor unlocks nor repeats pause");
                probe.position=gate.transform.position+new Vector3(-5,1,-15);pause.SampleNow();Check(receiver.Paused,"Outstanding pause remains latched outside zone");probe.position=gate.transform.position+new Vector3(-5,1,-5);
                var secondOwner=new GameObject("OtherPauseOwner_TEST");s.AutoRunPause.RequestPause(secondOwner);s.AutoRunPause.ReleasePause(secondOwner);Object.Destroy(secondOwner);
                Check(receiver.Paused&&receiver.Resumes==0,"Other pause owners cannot prematurely resume the lever wait");
                int accepted=0;gate.OnSucceeded.AddListener(()=>accepted++);
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                var fallback=gate.GetComponent<PortalUnlockKeyboardInput>();Check(fallback!=null&&!simulator.enabled,"Local E fallback supports P11's disabled simulator default");
                keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();simulator.enabled=true;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));InputSystem.Update();fallback.PollKeyboard();Check(!gate.CanExit,"Local fallback defers to an enabled P9 simulator");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();simulator.enabled=false;
                if(Environment.GetCommandLineArgs().Contains("-portalHardware"))
                {
                    var adapter=s.GetComponentInChildren<ProcessedHardwareAdapter>(true);
                    Check(adapter!=null,"Existing P10 adapter retained");
                    bool wasEnabled=adapter.enabled;
                    try{adapter.enabled=true;adapter.StopAdapter();Check(adapter.ReceiveMessage("{\"command\":\"ACTIVATE\"}"),"Processed ACTIVATE passes actual P10 parser and P9 channel");}
                    finally{adapter.enabled=wasEnabled;}
                }
                else{InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));InputSystem.Update();fallback.PollKeyboard();}
                Check(gate.HasInteracted&&gate.CanExit&&accepted==1,"Selected input unlocks through existing command channel");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();simulator.PollKeyboard();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));InputSystem.Update();fallback.PollKeyboard();
                Check(accepted==1&&!gate.TestActivate(),"Repeated E and test input cannot retrigger");
                float until=Time.unscaledTime+1;while(Time.unscaledTime<until){if(RealmIndex!=3&&!frame.gameObject.activeInHierarchy)throw new Exception("Gateway disappeared during activation");yield return null;}
                pause.SampleNow();Check(!pause.Waiting&&receiver.Resumes==1&&(RealmIndex==3?s.IsComplete:!receiver.Paused),"Completed lever reveal releases auto-run exactly once");
                pause.SampleNow();Check(receiver.Pauses==(RealmIndex==3?2:1)&&receiver.Resumes==1,"Unlocked zone cannot pause again");
                var handle=gate.transform.Find("LeverPivot");Check(Quaternion.Angle(Quaternion.identity,handle.localRotation)>(RealmIndex==0?40:10),"Mechanism reaches activated pose");
                Check(RealmIndex==3?!gate.Portal.gameObject.activeInHierarchy&&gate.Portal.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled):gate.Portal.gameObject.activeInHierarchy&&Vector3.Distance(gate.Portal.transform.localScale,Vector3.one)<.01f,"Gentle portal reveal finishes at original scale");
                Check(gate.GetComponentInChildren<TextMesh>().text.Contains(RealmIndex==3?"Journey complete":"Portal ready"),"Prompt confirms unlocked state");
                if(RealmIndex==2)Check(gate.transform.Find("LeverPivot/CrystalActivationFocus").GetComponent<Renderer>().sharedMaterial.name=="PedestalActivated"&&gate.transform.Find("LeverPivot/CrystalActivationFocus").GetComponent<Renderer>().sharedMaterial.IsKeywordEnabled("_EMISSION"),"Crystal pedestal illuminates on accepted activation");
                var audio=gate.GetComponent<AudioSource>();Check(audio.clip!=null&&audio.volume<=.1f&&!audio.loop,"Short low-volume reveal sound configured");
                var polish=root.GetComponentInChildren<RealmPolish>();polish.Muted=true;yield return null;yield return null;Check(audio.mute,"P12 mute respected");
                gate.enabled=false;gate.enabled=true;Check(gate.CanExit&&(RealmIndex==3?!gate.Portal.gameObject.activeInHierarchy:gate.Portal.gameObject.activeInHierarchy),"Unlocked state survives component toggle");
                Capture(root,gate,"Unlocked");if(RealmIndex>0)Capture(root,gate,"Overview");
                Check(frame.gameObject.activeInHierarchy==(RealmIndex!=3),"Frame follows realm-specific activation policy");
                if(RealmIndex==3)Check(s.IsComplete&&!s.TryAdvance(s.CurrentRealm,s.CurrentRealm.Exit)&&!s.TryCompleteActivation(s.CurrentRealm),"Console completes without portal entry; all repeat completion paths rejected");
                var previousRealm=s.CurrentRealm;int tokenCount=s.GetComponent<JourneyLedger>().Count;
                if(RealmIndex!=3)probe.position=previousRealm.Exit.position;Physics.SyncTransforms();
                if(RealmIndex==3)
                {
                    while(!s.IsComplete)yield return null;
                    yield return null;var finale=root.GetComponent<JourneyFinale>();
                    Check(finale.AwardCount==1&&finale.IsVisible&&finale.FinalTokenCount==tokenCount,"Completion awards one trophy and displays actual tokens");
                    var labels=finale.GetComponentsInChildren<UnityEngine.UI.Text>();
                    Check(labels.Any(t=>t.text=="YOU DID IT! — JOURNEY COMPLETE")&&labels.Any(t=>t.text=="Realms completed: 4/4")&&labels.Any(t=>t.text=="Journey Tokens collected: "+tokenCount+"/36"),"Completion UI contains requested title, realms and actual tally");
                    Check(!gate.Portal.TestEnter()&&finale.AwardCount==1,"Duplicate completion cannot award twice");
                    CaptureFinale(root);
                    UnityEngine.EventSystems.ExecuteEvents.Execute(finale.FinishButton.gameObject,new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left},UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);Check(s.IsSessionFinished&&!finale.FinishButton.interactable&&!s.FinishJourney(),"Finish button ends session once");
                    bool resetProtected=false;s.GetComponent<JourneyLedger>().CountChanged+=count=>{if(count==0)resetProtected=!s.GetComponent<JourneyLedger>().TryCollect("reentrant_reset_test");};UnityEngine.EventSystems.ExecuteEvents.Execute(finale.ReplayButton.gameObject,new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left},UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);Check(s.IsTransitioning&&!s.ReplayJourney(),"Replay button begins one existing-loader restart");
                    while(s.IsTransitioning)yield return null;
                    Check(s.CurrentRealm.RealmIndex==0&&!s.IsComplete&&!s.IsSessionFinished,"Replay returns to fresh Jungle");
                    Check(resetProtected&&s.GetComponent<JourneyLedger>().Count==0&&s.PlayerReference==null,"Replay resets tokens and awaits player binding");
                    Check(!s.CurrentRealm.ExitUnlocked,"Replay restores locked Jungle gate");
                    yield break;
                }
                while(s.CurrentRealm==null||s.CurrentRealm==previousRealm||s.IsTransitioning)yield return null;
                Check(s.CurrentRealm.gameObject.scene.path==IntegrationSession.RealmScenes[RealmIndex+1],"Bound reference entering unlocked exit loads next realm through P11");
                Check(s.GetComponent<JourneyLedger>().Count==tokenCount,"Journey Token count preserved through exit");
                Check(!s.CurrentRealm.ExitUnlocked,"Next realm retains its own exit policy");
            }
            finally{settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=editor;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(s!=null)s.BindPlayer(null);if(probe!=null)Object.Destroy(probe.gameObject);}
        }
        static IEnumerator RunTrophy()
        {
            var s=IntegrationSession.Instance;var root=s.CurrentRealm.transform.root;var world=root.GetComponentInChildren<JourneyRealm>();var finale=root.GetComponent<JourneyFinale>();
            var trophy=root.Find("FinalCityArchitecture/GoldenJourneyTrophy_DisplayOnly");
            Check(trophy!=null&&trophy.gameObject.activeInHierarchy&&trophy.position==s.CurrentRealm.Exit.position,"Existing golden trophy visible at former portal location");
            Check(root.GetComponentInChildren<PortalUnlockMechanism>(true)==null&&root.GetComponentInChildren<PortalAutoRunPause>(true)==null,"Obsolete console, activation and waiting pause removed");
            Check(world.Portal.name=="TrophyCompletionTrigger"&&world.Portal.GetComponentsInChildren<Renderer>(true).Length==0&&world.Portal.GetComponentsInChildren<PortalAtmosphere>(true).Length==0,"No obsolete portal visuals or effects");
            Check(world.Portal.GetComponent<BoxCollider>().isTrigger&&world.Portal.GetComponent<BoxCollider>().enabled,"Generous non-solid trophy trigger enabled");
            Check(!s.AutoRunPause.IsPauseRequested&&!s.IsComplete&&!s.ReplayJourney()&&!s.FinishJourney(),"No pre-trophy pause or premature completion");
            int[] tokens={0,17,18,29,30,36},ratings={1,1,2,2,3,3};for(int i=0;i<tokens.Length;i++)Check(JourneyFinale.StarsForTokens(tokens[i])==ratings[i],"Star boundary "+tokens[i]+" -> "+ratings[i]);
            s.Channel.Send(HardwareCommand.ACTIVATE);yield return null;Check(!s.IsComplete,"ACTIVATE alone cannot collect trophy");
            var probe=new GameObject("TrophyPhysics_TEST");var collider=probe.AddComponent<SphereCollider>();collider.radius=.25f;var body=probe.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
            var polling=typeof(IntegrationSession).GetField("automaticExitDetection",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);bool original=(bool)polling.GetValue(s);polling.SetValue(s,false);
            try
            {
                probe.transform.position=s.CurrentRealm.Exit.position+Vector3.back*8+Vector3.up;s.BindPlayer(probe.transform);Physics.SyncTransforms();yield return null;yield return null;
                Check(!s.IsComplete&&!world.Portal.TryEnter(s.CurrentRealm.Entrance),"Outside trigger cannot complete");
                probe.transform.position=s.CurrentRealm.Exit.position+Vector3.up+Vector3.right*3;Physics.SyncTransforms();float end=Time.unscaledTime+2;while(!s.IsComplete&&Time.unscaledTime<end)yield return null;
                Check(s.IsComplete&&finale.AwardCount==1&&finale.EarnedStars==1&&finale.FinalTokenCount==0,"Physical bound-player contact completes once with zero tokens");
                Check(!world.Portal.TestEnter()&&finale.AwardCount==1,"Duplicate collision/demo completion rejected");
                finale.ReplayButton.onClick.Invoke();while(s.IsTransitioning)yield return null;Check(s.CurrentRealm.RealmIndex==0&&!s.IsComplete&&s.GetComponent<JourneyLedger>().Count==0,"Replay returns to fresh Jungle and resets tokens");
            }
            finally{polling.SetValue(s,original);s.BindPlayer(null);Object.Destroy(probe);}
            var load=UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(IntegrationSession.RealmScenes[3]);while(!load.isDone)yield return null;
            while(s.CurrentRealm.RealmIndex!=3||s.CurrentRealm.transform.root.GetComponentInChildren<JourneyRealm>().Ledger==null)yield return null;
            root=s.CurrentRealm.transform.root;world=root.GetComponentInChildren<JourneyRealm>();finale=root.GetComponent<JourneyFinale>();
            for(int i=0;i<18;i++)Check(world.Ledger.TryCollect("rating-test-"+i),"Independent optional token tally "+i);
            Check(world.Portal.TestEnter(),"Reach trophy demo invokes existing P11 completion without player");yield return null;
            Check(finale.AwardCount==1&&finale.EarnedStars==2&&finale.FinalTokenCount==18&&!world.Portal.TestEnter(),"Two-star demo uses actual tally and awards once");
            var stars=finale.GetComponentsInChildren<UnityEngine.UI.Image>().Where(i=>i.name.StartsWith("VictoryStar")).OrderBy(i=>i.name).ToArray();
            Check(stars.Length==3&&stars.All(i=>i.sprite!=null)&&stars.Take(2).All(i=>i.GetComponent<UnityEngine.UI.Outline>()!=null)&&stars[2].GetComponent<UnityEngine.UI.Outline>()==null,"Three positions: two glowing earned stars and one muted star");
            var labels=finale.GetComponentsInChildren<UnityEngine.UI.Text>();Check(new[]{"CONGRATULATIONS! YOU DID IT!","JOURNEY COMPLETE","Every step is progress!","Realms completed: 4/4","Journey Tokens collected: 18/36"}.All(text=>labels.Any(t=>t.text==text)),"Victory screen contains both titles, encouragement and actual totals");
            var polish=root.GetComponentInChildren<RealmPolish>();polish.ReducedMotion=true;yield return null;yield return null;Check(stars.All(i=>i.rectTransform.localScale==Vector3.one),"Reduced Motion stops star animation");
            CaptureFinale(root);
            finale.FinishButton.onClick.Invoke();Check(s.IsSessionFinished&&!finale.FinishButton.interactable&&!s.FinishJourney(),"Finish Session works once");
            finale.ReplayButton.onClick.Invoke();while(s.IsTransitioning)yield return null;Check(s.CurrentRealm.RealmIndex==0&&!s.IsComplete&&!s.IsSessionFinished&&s.GetComponent<JourneyLedger>().Count==0,"Replay after Finish resets journey and tally");
        }
        static void CaptureFinale(Transform root)
        {
            var canvas=root.GetComponentInChildren<JourneyFinale>().GetComponentInChildren<Canvas>();
            var camera=root.GetComponentsInChildren<Camera>(true).First(c=>c.name=="PrototypeOverviewCamera_NoController");
            var mode=canvas.renderMode;var old=canvas.worldCamera;float distance=canvas.planeDistance;
            try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();Capture(root,root.GetComponentInChildren<PortalUnlockMechanism>(),"Finale");}
            finally{canvas.renderMode=mode;canvas.worldCamera=old;canvas.planeDistance=distance;}
        }
        static void Capture(Transform root,PortalUnlockMechanism gate,string name)
        {
            var camera=root.GetComponentsInChildren<Camera>(true).First(c=>c.name=="PrototypeOverviewCamera_NoController");var pos=camera.transform.position;var rot=camera.transform.rotation;bool ortho=camera.orthographic;
            var pipeline=Object.Instantiate(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);var previous=QualitySettings.renderPipeline;var active=RenderTexture.active;var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{pipeline.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;pipeline.useSRPBatcher=false;QualitySettings.renderPipeline=pipeline;if(name!="Overview"&&gate!=null){camera.orthographic=false;camera.transform.position=gate.transform.position+new Vector3(-7,4,-10);camera.transform.LookAt(gate.transform.position+new Vector3(-2,1.5f,2));}Canvas.ForceUpdateCanvases();for(int i=0;i<3;i++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();string folder=RealmIndex==0?"Documentation/PortalUnlock":RealmIndex==1?"Documentation/RuinsUpgrade":RealmIndex==2?"Documentation/CrystalCaveUpgrade":"Documentation/TrophyFinale";Directory.CreateDirectory(folder);File.WriteAllBytes(folder+"/"+name+".png",image.EncodeToPNG());}
            finally{camera.transform.SetPositionAndRotation(pos,rot);camera.orthographic=ortho;QualitySettings.renderPipeline=previous;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(pipeline);}
        }
    }
}





