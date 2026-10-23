using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.Journey;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace Realmwalker.Authoring
{
    [InitializeOnLoad]
    public static class FallRecoveryValidation
    {
        static IEnumerator routine;
        static double deadline;
        static bool errors;
        static int checks;
        static Keyboard keyboard;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
        static FallRecoveryValidation(){EditorApplication.playModeStateChanged+=State;}
        public static void Batch()=>EditorApplication.delayCall+=Tests;
        [MenuItem("Tools/Lost Worlds/Jump Recovery/Run Focused Play Mode Checks")]
        public static void Tests(){if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);SessionState.SetBool("FallQA.test",true);EditorApplication.isPlaying=true;}
        public static void MeasureBatch()
        {
            for(int i=1;i<4;i++)
            {
                var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);
                Physics.SyncTransforms();
                foreach(float z in new[]{18f,24,34,44,56,66})
                {
                    var hits=Physics.RaycastAll(new Vector3(0,10,z),Vector3.down,30,~0,QueryTriggerInteraction.Ignore);
                    foreach(var h in hits)Debug.Log($"SURFACE {i} z={z}: {Path(h.transform)} y={h.point.y:F3} {h.collider.GetType().Name} bounds={h.collider.bounds} mesh={h.collider.GetComponent<MeshFilter>()?.sharedMesh?.name}");
                }
            }
            EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);
            SessionState.SetBool("FallQA.measure",true);EditorApplication.isPlaying=true;
        }
        static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&(SessionState.GetBool("FallQA.measure",false)||SessionState.GetBool("FallQA.test",false)))
            {bool measure=SessionState.GetBool("FallQA.measure",false);SessionState.SetBool("FallQA.measure",false);SessionState.SetBool("FallQA.test",false);checks=0;errors=false;Application.logMessageReceived+=Log;routine=measure?Measure():Run();deadline=EditorApplication.timeSinceStartup+240;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("FallQA.done",false))
            {SessionState.SetBool("FallQA.done",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool("FallQA.pass",false)?0:1);}
        }
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Warning)errors=true;}
        static void Tick()
        {
            try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Fall QA timeout");if(routine.MoveNext())return;if(errors)throw new Exception("Runtime warning/error");End(true);}
            catch(Exception e){Debug.LogException(e);End(false);}
        }
        static void End(bool pass){Time.captureDeltaTime=0;if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;}EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SessionState.SetBool("FallQA.pass",pass);SessionState.SetBool("FallQA.done",true);Debug.Log("FALL QA checks="+checks+" passed="+pass);EditorApplication.isPlaying=false;}
        static void Check(bool pass,string text){if(!pass)throw new Exception("FALL QA FAIL: "+text);checks++;Debug.Log("FALL QA PASS: "+text);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
        static IEnumerator Measure()
        {
            while(IntegrationSession.Instance?.PlayerReference==null)yield return null;
            var s=IntegrationSession.Instance;var runner=s.PlayerReference.GetComponent<RealmwalkerRunner>();
            s.SetInputMode(GameplayInputMode.Simulator);
            foreach(float frame in new[]{1f/60,1f/30})
            {
                Time.captureDeltaTime=frame;
                runner.ResetMotion(new Vector3(0,.08f,5));var w=Wait(.6f);while(w.MoveNext())yield return null;
                float y=runner.transform.position.y,z=runner.transform.position.z,time=Time.time,peak=y;
                int before=runner.AcceptedJumps;s.GetComponent<ProcessedHardwareAdapter>().ReceiveMessage("{\"command\":\"JUMP\"}");
                while(runner.AcceptedJumps==before)yield return null;
                while(!runner.Grounded){peak=Mathf.Max(peak,runner.transform.position.y);yield return null;}
                Debug.Log($"JUMP MEASURE fps={1/frame:F0} height={peak-y:F3} airtime={Time.time-time:F3} distance={runner.transform.position.z-z:F3} start={z:F3} landed={runner.Grounded}");
            }
            Time.captureDeltaTime=0;
        }
        static IEnumerator Run()
        {
            Time.captureDeltaTime=1f/60;
            while(IntegrationSession.Instance?.PlayerReference==null)yield return null;
            var s=IntegrationSession.Instance;
            oldBackground=InputSystem.settings.backgroundBehavior;oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();
            var keyboardTests=KeyboardChecks(s);while(keyboardTests.MoveNext())yield return null;
            s.SetInputMode(GameplayInputMode.Simulator);
            for(int index=0;index<4;index++)
            {
                if(index>0){var operation=SceneManager.LoadSceneAsync(IntegrationSession.RealmScenes[index]);while(!operation.isDone)yield return null;}
                while(s.CurrentRealm==null||s.CurrentRealm.RealmIndex!=index||s.PlayerReference==null)yield return null;
                var r=s.PlayerReference.GetComponent<RealmwalkerRunner>();var realm=s.CurrentRealm;
                var gaps=realm.transform.root.GetComponentsInChildren<RealmJumpChallenge>();
                var near=gaps.Select(g=>g.near).ToArray();var far=gaps.Select(g=>g.far).ToArray();
                if(index==0){near=new[]{71.4f};far=new[]{72.6f};}
                float floor=index==0?2.1f:0;
                for(int gap=0;gap<near.Length;gap++)foreach(float lane in new[]{-2f,0,2f})foreach(float delay in new[]{.1f,.3f})
                {
                    bool keys=lane==0&&delay==.1f;
                    s.SetInputMode(keys?GameplayInputMode.Keyboard:GameplayInputMode.Simulator);Keys();yield return null;yield return null;
                    if(keys)Keys(Key.W);
                    r.ResetMotion(new Vector3(lane,floor+.03f,near[gap]-2.7f));r.SetAutoRunPaused(false);
                    var w=Wait(.6f);while(w.MoveNext())yield return null;
                    Check(r.Grounded&&!s.IsRecovering,"Grounded approach realm "+index+" gap "+gap+" lane "+lane);
                    w=Wait(delay);while(w.MoveNext())yield return null;
                    int jumps=r.AcceptedJumps;
                    if(keys){Keys(Key.W,Key.Space);s.GetComponent<HardwareCommandSimulator>().PollKeyboard();Keys(Key.W);}else Check(s.GetComponent<ProcessedHardwareAdapter>().ReceiveMessage("{\"command\":\"JUMP\"}"),"P10 delayed JUMP accepted");
                    w=Wait(1.05f);while(w.MoveNext())yield return null;
                    Check(r!=null&&!s.IsRecovering&&r.Grounded&&r.transform.position.z>far[gap]&&r.AcceptedJumps==jumps+1,"Physical gap crossing and landing: realm "+index+" lane "+lane+" provisional delay "+delay);
                }
                s.SetInputMode(GameplayInputMode.Simulator);Keys();
                // Keep a real collected ID, then miss the actual gap: retry must not duplicate it.
                var world=realm.transform.root.GetComponentInChildren<JourneyRealm>();var token=world.Tokens[0];token.TestCollect();string id=token.TokenId;
                var ledger=JourneyLedger.Ensure(s);int count=ledger.Count;
                r.ResetMotion(new Vector3(0,floor+.01f,(near[0]+far[0])*.5f));r.SetAutoRunPaused(true);
                float end=Time.time+2;while(!s.IsRecovering&&Time.time<end)yield return null;
                Check(s.IsRecovering&&!s.CommandsReady&&r.Paused,"Missed gap begins safe recovery in realm "+index);
                Check(s.Hud.FeedbackText=="Let's try that path again!","Friendly recovery feedback");
                Check(!s.TryRecoverRealm(realm)&&!s.TryAdvance(realm,r.transform),"Duplicate recovery and portal transition blocked");
                int accepted=r.AcceptedJumps;s.Channel.Send(HardwareCommand.JUMP);s.Channel.Send(HardwareCommand.LEFT);
                Check(!s.GetComponent<ProcessedHardwareAdapter>().ReceiveMessage("{\"command\":\"ACTIVATE\"}")&&r.AcceptedJumps==accepted,"Commands ignored during recovery");
                float began=Time.unscaledTime;
                while(s.IsRecovering||s.PlayerReference==null)yield return null;
                Check(Time.unscaledTime-began>=1.4f&&s.CurrentRealm.RealmIndex==index&&s.CurrentRealm!=realm,"Delayed current-realm reload only");
                r=s.PlayerReference.GetComponent<RealmwalkerRunner>();
                Check(r.transform.position.z<5&&Mathf.Abs(r.transform.position.x)<.01f,"Authored safe starting position restored");
                var settle=Wait(.6f);while(settle.MoveNext())yield return null;
                var refreshed=s.CurrentRealm.transform.root.GetComponentInChildren<JourneyRealm>().Tokens.Single(t=>t.TokenId==id);
                Check(ledger.Count==count&&refreshed.IsCollected&&!refreshed.TestCollect(),"Collected IDs persist and duplicate reward is blocked");
                Check(r.Grounded&&!r.Paused&&r.AcceptedJumps==0,"Safe grounded restart without stale jump");
                var gate=s.CurrentRealm.transform.root.GetComponentInChildren<PortalUnlockMechanism>();
                Check(index==3?gate==null:gate!=null&&!gate.CanExit,"Current-realm interaction state resets without adding final gate");
                float z=r.transform.position.z;settle=Wait(.5f);while(settle.MoveNext())yield return null;
                Check(Mathf.Abs((r.transform.position.z-z)/.5f-3.1f)<.15f,"Retry resumes 3.1 m/s");
                Check(Object.FindObjectsByType<IntegrationSession>().Length==1&&Object.FindObjectsByType<JourneyLedger>().Length==1,"No duplicate session/ledger after recovery");
                Check(Object.FindObjectsByType<AudioListener>().Count(a=>a.isActiveAndEnabled)==1,"One active audio listener after recovery");
                r.ResetMotion(new Vector3(0,floor+.03f,near[0]-2));
                end=Time.time+4;while(!s.IsRecovering&&Time.time<end)yield return null;
                Check(s.IsRecovering,"Running without JUMP actually falls; gap cannot be auto-stepped in realm "+index);
                while(s.IsRecovering||s.PlayerReference==null)yield return null;
            }
            // Holding W across recovery is stale intent: require release and a fresh press.
            s.SetInputMode(GameplayInputMode.Keyboard);Keys();yield return null;yield return null;Keys(Key.W);
            var retryRunner=s.PlayerReference.GetComponent<RealmwalkerRunner>();retryRunner.ResetMotion(new Vector3(0,-5.1f,20));
            while(!s.IsRecovering)yield return null;while(s.IsRecovering||s.PlayerReference==null)yield return null;
            retryRunner=s.PlayerReference.GetComponent<RealmwalkerRunner>();float retryZ=retryRunner.transform.position.z;
            var hold=Wait(.5f);while(hold.MoveNext())yield return null;
            Check(Mathf.Abs(retryRunner.transform.position.z-retryZ)<.001f,"Held W from before recovery cannot restart movement");
            Keys();yield return null;yield return null;Keys(Key.W);hold=Wait(.5f);while(hold.MoveNext())yield return null;
            Check(retryRunner.transform.position.z>retryZ+.5f,"Fresh W press resumes after recovery");
            Keys();s.SetInputMode(GameplayInputMode.Simulator);
            // Safety net covers beyond-world and below-world positions without tagging decorative water.
            foreach(var p in new[]{new Vector3(100,0,20),new Vector3(0,-5.1f,20)})
            {
                var r=s.PlayerReference.GetComponent<RealmwalkerRunner>();r.ResetMotion(p);yield return null;yield return null;
                Check(s.IsRecovering,"Out-of-bounds/below-world recovery");while(s.IsRecovering||s.PlayerReference==null)yield return null;
            }
            // Loopback exercises the real transport queue and partial-frame discard, without physical hardware claims.
            Time.captureDeltaTime=0;
            var tcp=s.GetComponent<ProcessedHardwareAdapter>();
            using(var client=new System.Net.Sockets.TcpClient())
            {
                client.Connect(tcp.Address,tcp.Port);var stream=client.GetStream();
                var bytes=System.Text.Encoding.ASCII.GetBytes("{\"command\":\"JU");stream.Write(bytes,0,bytes.Length);
                var wait=Wait(.15f);while(wait.MoveNext())yield return null;
                tcp.SetRecoveryBlocked(true);tcp.SetRecoveryBlocked(false);
                bytes=System.Text.Encoding.ASCII.GetBytes("MP\"}\n");stream.Write(bytes,0,bytes.Length);
                int before=tcp.Forwarded;wait=Wait(.15f);while(wait.MoveNext())yield return null;
                Check(tcp.Forwarded==before,"Old partial TCP frame is discarded across recovery boundary");
                bytes=System.Text.Encoding.ASCII.GetBytes("{\"command\":\"JUMP\"}\n");stream.Write(bytes,0,bytes.Length);
                wait=Wait(.15f);while(wait.MoveNext())yield return null;
                Check(tcp.Forwarded==before+1,"Fresh TCP commands still forward after recovery");
            }
        }
        static void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
        static IEnumerator KeyboardChecks(IntegrationSession s)
        {
            var r=s.PlayerReference.GetComponent<RealmwalkerRunner>();
            Check(s.InputMode==GameplayInputMode.Keyboard,"Fresh session defaults to W-controlled keyboard mode");
            Keys();r.ResetMotion(new Vector3(0,.08f,4));var w=Wait(.4f);while(w.MoveNext())yield return null;
            Check(Mathf.Abs(r.transform.position.z-4)<.001f,"No automatic forward movement without W");
            Keys(Key.W);w=Wait(.5f);while(w.MoveNext())yield return null;float z=r.transform.position.z;
            w=Wait(.5f);while(w.MoveNext())yield return null;
            Check(Mathf.Abs((r.transform.position.z-z)/.5f-3.1f)<.12f&&r.GetComponent<RealmwalkerAnimation>().CurrentState=="Run","Held W reaches 3.1 m/s and Run with original acceleration");
            Keys();yield return null;z=r.transform.position.z;w=Wait(.25f);while(w.MoveNext())yield return null;
            Check(Mathf.Abs(r.transform.position.z-z)<.001f&&r.GetComponent<RealmwalkerAnimation>().CurrentState=="Idle","W release stops immediately and selects Idle");
            Keys(Key.A);s.GetComponent<HardwareCommandSimulator>().PollKeyboard();Keys();w=Wait(.4f);while(w.MoveNext())yield return null;
            Check(r.Lane==-1&&Mathf.Abs(r.transform.position.z-z)<.001f,"LEFT steering works while forward movement is stopped");
            int before=r.AcceptedJumps;Keys(Key.Space);s.GetComponent<HardwareCommandSimulator>().PollKeyboard();Keys();w=Wait(1);while(w.MoveNext())yield return null;
            Check(r.AcceptedJumps==before+1&&r.Grounded&&Mathf.Abs(r.transform.position.z-z)<.001f,"Stationary JUMP has no forward drift and lands");
            Keys(Key.W);w=Wait(.5f);while(w.MoveNext())yield return null;Keys(Key.W,Key.Space);s.GetComponent<HardwareCommandSimulator>().PollKeyboard();Keys(Key.W);w=Wait(.15f);while(w.MoveNext())yield return null;
            Keys();yield return null;z=r.transform.position.z;Check(!r.Grounded,"Moving jump is airborne before W release");w=Wait(.8f);while(w.MoveNext())yield return null;
            Check(r.Grounded&&Mathf.Abs(r.transform.position.z-z)<.001f,"W release in air stops forward travel while gravity and landing continue");
            s.SetInputMode(GameplayInputMode.Hardware);r.ResetMotion(new Vector3(0,.08f,4));w=Wait(.4f);while(w.MoveNext())yield return null;
            z=r.transform.position.z;before=r.AcceptedJumps;Keys(Key.A,Key.Space,Key.E);s.GetComponent<HardwareCommandSimulator>().PollKeyboard();Keys();w=Wait(.4f);while(w.MoveNext())yield return null;
            Check(r.Lane==0&&r.AcceptedJumps==before&&r.transform.position.z>z+1,"Hardware auto-run ignores keyboard steering/jump/ACTIVATE");
            Check(s.GetComponent<ProcessedHardwareAdapter>().ReceiveMessage("{\"command\":\"RIGHT\"}"),"Hardware source still accepts processed commands");w=Wait(.4f);while(w.MoveNext())yield return null;
            Check(r.Lane==1,"Hardware RIGHT remains functional without W");
        }
    }
}
