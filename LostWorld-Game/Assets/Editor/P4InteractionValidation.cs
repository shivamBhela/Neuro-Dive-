using System;
using System.IO;
using LostWorlds.Person2.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    [InitializeOnLoad]
    public static class P4InteractionValidation
    {
        const string Pending="LostWorlds.P4.ValidationPending";
        const string Finished="LostWorlds.P4.ValidationFinished";
        const string Passed="LostWorlds.P4.ValidationPassed";
        static int checks;
        static P4InteractionValidation() { EditorApplication.playModeStateChanged += StateChanged; }

        public static void BuildAndValidateBatch()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Batch entry point only.");
            EditorSceneManager.OpenScene(P4InteractionDemoBuilder.JunglePath,OpenSceneMode.Single);
            P4InteractionDemoBuilder.Setup(); Begin();
        }
        [MenuItem("Tools/Lost Worlds/P4/Run Play Mode Interaction Checks")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Stop Play mode before running checks.");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(P4InteractionDemoBuilder.JunglePath,OpenSceneMode.Single);
            P4InteractionDemoBuilder.ValidateSetup();Begin();
        }
        static void Begin()
        {SessionState.SetBool(Finished,false);SessionState.SetBool(Passed,false);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;}
        static void StateChanged(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))EditorApplication.update+=RunPendingChecks;
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Finished,false))
            {
                SessionState.SetBool(Finished,false);
                if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(SessionState.GetBool(Passed,false)?0:1);
            }
        }
        static void RunPendingChecks()
        { EditorApplication.update-=RunPendingChecks;Checks(); }
        static void Assert(bool condition,string message)
        {if(!condition)throw new InvalidOperationException("P4 check failed: "+message);checks++;Debug.Log("P4 CHECK PASS: "+message);}
        static void Key(Keyboard keyboard,P4KeyboardTestInput input,params UnityEngine.InputSystem.Key[] keys)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();input.PollKeyboard();}

        static void Checks()
        {
            SessionState.SetBool(Pending,false);checks=0;Keyboard keyboard=null;GameObject fixture=null;
            var originalInputSettings=InputSystem.settings;
            var originalBackground=originalInputSettings.backgroundBehavior;
            var originalEditorInput=originalInputSettings.editorInputBehaviorInPlayMode;
            try
            {
                var demo=P4InteractionDemoBuilder.Demo();var input=demo.GetComponent<P4KeyboardTestInput>();
                var grab=demo.GetComponentInChildren<GrabInteractable>();var activate=demo.GetComponentInChildren<ActivateInteractable>();
                int grabEvents=0,activateEvents=0;grab.OnSucceeded.AddListener(()=>grabEvents++);activate.OnSucceeded.AddListener(()=>activateEvents++);
                Physics.SyncTransforms();
                Assert(!grab.HasInteracted&&!activate.HasInteracted&&!activate.ActivatedVisual.activeSelf,"Saved demo starts ready with beacon off");
                P4InteractionDemoBuilder.Capture("Before");
                var savedOrigin=input.GrabInteractor.transform.position;input.GrabInteractor.transform.position+=Vector3.right*100;
                Assert(!input.GrabInteractor.TryGrab()&&!grab.TryInteract(input.GrabInteractor),"Out-of-range grab rejected through both APIs");
                input.GrabInteractor.transform.position=savedOrigin;
                var savedActivate=input.ActivateInteractor.transform.position;input.ActivateInteractor.transform.position+=Vector3.right*100;
                Assert(!input.ActivateInteractor.TryActivate()&&!activate.TryInteract(input.ActivateInteractor),"Out-of-range activation rejected through both APIs");
                input.ActivateInteractor.transform.position=savedActivate;
                var hold=input.GrabInteractor.HoldPoint;input.GrabInteractor.HoldPoint=null;
                Assert(!input.GrabInteractor.TryGrab(),"Missing hold point safely rejects grab");input.GrabInteractor.HoldPoint=hold;
                // Temporarily allow synthetic input in an unfocused batch Editor; restore below.
                originalInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                originalInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

                keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
                input.TestInputEnabled=false;Key(keyboard,input,UnityEngine.InputSystem.Key.G,UnityEngine.InputSystem.Key.E);
                Assert(!grab.HasInteracted&&!activate.HasInteracted,"Disabled test adapter ignores G/E");
                Key(keyboard,input);input.TestInputEnabled=true;Key(keyboard,input,UnityEngine.InputSystem.Key.G);
                Assert(grab.HasInteracted&&grab.transform.parent==hold&&Vector3.Distance(grab.transform.position,hold.position)<.001f,"G grabs object to defined hold point");
                Assert(!activate.HasInteracted,"G does not activate another command type");
                Assert(input.GrabInteractor.HeldObject==grab&&grab.GetComponent<Rigidbody>().isKinematic,"Grab registers held object and keeps physics stable");
                Assert(!grab.GetComponent<Collider>().enabled,"Held collider disabled to avoid pushing a future player");
                Assert(grab.GetComponent<Renderer>().sharedMaterial.name=="SuccessTeal","Grab gives visible success color");
                Assert(!input.GrabInteractor.TryGrab()&&grabEvents==1,"Repeated grab produces one success event only");
                hold.position+=Vector3.right;Assert(Vector3.Distance(grab.transform.position,hold.position)<.001f,"Held object follows its reference without player code");hold.position-=Vector3.right;
                Key(keyboard,input);Key(keyboard,input,UnityEngine.InputSystem.Key.E);
                Assert(activate.HasInteracted&&activate.ActivatedVisual.activeSelf,"E activates the world beacon");
                Assert(activate.transform.Find("ActivationOrb").GetComponent<Renderer>().sharedMaterial.name=="SuccessTeal","Activate gives visible success color");
                Assert(!input.ActivateInteractor.TryActivate()&&activateEvents==1,"Repeated activation is idempotent");
                Key(keyboard,input);P4InteractionDemoBuilder.Capture("After");

                // Isolated, temporary fixture validates reuse/selection independently of Jungle.
                fixture=new GameObject("P4_ValidationFixture");fixture.transform.position=new Vector3(1000,100,1000);
                var actor=fixture.AddComponent<InteractionInteractor>();actor.Range=4;
                var fixtureHold=new GameObject("Hold").transform;fixtureHold.SetParent(fixture.transform,false);fixtureHold.localPosition=Vector3.right*5;actor.HoldPoint=fixtureHold;
                var near=GameObject.CreatePrimitive(PrimitiveType.Cube);near.transform.SetParent(fixture.transform,false);near.transform.localPosition=Vector3.forward*2;var nearGrab=near.AddComponent<GrabInteractable>();
                var far=GameObject.CreatePrimitive(PrimitiveType.Cube);far.transform.SetParent(fixture.transform,false);far.transform.localPosition=Vector3.forward*3;var farGrab=far.AddComponent<GrabInteractable>();
                near.AddComponent<SphereCollider>();int count=0;nearGrab.OnSucceeded.AddListener(()=>count++);
                Physics.SyncTransforms();Assert(!actor.TryActivate(),"Command filtering ignores grab-only targets");
                Assert(actor.TryGrab()&&nearGrab.HasInteracted&&!farGrab.HasInteracted&&count==1,"Nearest eligible target selected once despite multiple colliders");
                Assert(!farGrab.TryInteract(actor),"Occupied hold point prevents stacking multiple held objects");
                var switchObject=GameObject.CreatePrimitive(PrimitiveType.Cube);switchObject.transform.SetParent(fixture.transform,false);switchObject.transform.localPosition=Vector3.back*2;switchObject.GetComponent<Collider>().isTrigger=true;
                var switchTarget=switchObject.AddComponent<ActivateInteractable>();Physics.SyncTransforms();actor.enabled=false;
                Assert(!switchTarget.TryInteract(actor),"Disabled interactor cannot directly activate");actor.enabled=true;switchTarget.enabled=false;
                Assert(!actor.TryActivate(),"Disabled targets are ignored");switchTarget.enabled=true;
                Assert(actor.TryActivate()&&switchTarget.HasInteracted,"Trigger zones work without a player Rigidbody or controller");
                Assert(grabEvents==1&&activateEvents==1,"Demo actions fire exactly once across repeated input");
                SessionState.SetBool(Passed,true);
                Directory.CreateDirectory("Documentation/P4");
                File.WriteAllText("Documentation/P4/PLAY_MODE_VERIFICATION.txt","Unity "+Application.unityVersion+"\nP4 Play Mode checks passed: "+checks+"\nActual Input System keyboard events tested for G/E.\nRange rejection, missing hold point, disabled input/targets/interactor, nearest selection, duplicate colliders, held following, idempotence, trigger zones and success feedback passed.\nRuntime test changes were not saved to the scene.\n");
                Debug.Log("P4 PLAY MODE VALIDATION PASS: "+checks+" checks.");
            }
            catch(Exception exception){SessionState.SetBool(Passed,false);Debug.LogException(exception);}
            finally
            {
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(fixture!=null)Object.DestroyImmediate(fixture);
                originalInputSettings.backgroundBehavior=originalBackground;
                originalInputSettings.editorInputBehaviorInPlayMode=originalEditorInput;
                SessionState.SetBool(Finished,true);EditorApplication.isPlaying=false;
            }
        }
    }
}
