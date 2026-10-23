using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Hardware;
using Object = UnityEngine.Object;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Realmwalker.Authoring
{
    [InitializeOnLoad]
    public static class RealmwalkerR5Validation
    {
        static IEnumerator routine; static double deadline; static int checks; static bool errors;
        static Keyboard journeyKeyboard;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
        static RealmwalkerR5Validation() { EditorApplication.playModeStateChanged += State; }
        [MenuItem("Tools/Lost Worlds/R5/Play Full Journey")]
        public static void Play() => RealmwalkerR2.Play();
        [MenuItem("Tools/Lost Worlds/R5/Validate All-Realm Journey")]
        public static void Validate() { if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]); SessionState.SetBool("R5.pending", true); EditorApplication.isPlaying = true; }
        public static void Batch() => EditorApplication.delayCall += Validate;
        [MenuItem("Tools/Lost Worlds/Main Menu/Validate Menu and Journey")]
        public static void MenuTests()
        {if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(MainMenu.ScenePath);SessionState.SetBool("Menu.pending",true);SessionState.SetBool("R5.pending",true);EditorApplication.isPlaying=true;}
        public static void MenuBatch()=>EditorApplication.delayCall+=MenuTests;
        public static void WJourneyBatch(){SessionState.SetBool("R5.WJourney",true);MenuBatch();}
        static void State(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("R5.pending", false))
            { SessionState.SetBool("R5.pending", false); errors = false; checks = 0; deadline = EditorApplication.timeSinceStartup + 300; routine = Run(); Application.logMessageReceived += Log; EditorApplication.update += Tick; }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("R5.done", false))
            { SessionState.SetBool("R5.done", false); if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool("R5.pass", false) ? 0 : 1); }
        }
        static void Log(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning || type == LogType.Assert) errors = true; }
        static void Check(bool pass, string message) { if (!pass) throw new Exception("R5 FAIL: " + message); checks++; Debug.Log("R5 PASS: " + message); }
        static void Tick() { try { if (EditorApplication.timeSinceStartup > deadline) throw new Exception("R5 timeout"); if (routine.MoveNext()) return; Check(!errors, "No runtime errors/warnings"); End(true); } catch (Exception e) { Debug.LogException(e); End(false); } }
        static void End(bool pass) { if(journeyKeyboard!=null){InputSystem.RemoveDevice(journeyKeyboard);journeyKeyboard=null;InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;}EditorApplication.update -= Tick; Application.logMessageReceived -= Log; SessionState.SetBool("R5.done", true); SessionState.SetBool("R5.pass", pass); Debug.Log("R5 RESULT " + checks + " checks passed=" + pass); EditorApplication.isPlaying = false; }
        static IEnumerator Wait(float seconds) { float end = Time.time + seconds; while (Time.time < end) yield return null; }

        static IEnumerator Run()
        {
            if(SessionState.GetBool("Menu.pending",false))
            {
                SessionState.SetBool("Menu.pending",false);
                var menu=Object.FindAnyObjectByType<MainMenu>();
                var settle=Wait(.15f);while(settle.MoveNext())yield return null;
                var polish=menu.GetComponent<MainMenuPolish>();
                Check(polish!=null&&polish.FadeAlpha==0,"Menu polish starts clear");
                Check(menu.startButton.transform.localScale.x>1&&menu.startButton.transform.localScale.x<=1.026f,"Keyboard selection has restrained scale feedback");
                MainMenuBuilder.CapturePolishedMenu();
                // Synchronous screenshot readback can stall one Editor frame; settle it before timing the fade.
                settle=Wait(.2f);while(settle.MoveNext())yield return null;
                Check(menu!=null&&IntegrationSession.Instance==null&&Object.FindAnyObjectByType<RealmwalkerRunner>()==null,"Menu startup has no player or session");
                menu.howButton.onClick.Invoke();Check(menu.instructions.activeSelf&&!menu.home.activeSelf,"HOW TO PLAY opens instructions");
                menu.backButton.onClick.Invoke();Check(menu.home.activeSelf&&!menu.instructions.activeSelf,"BACK restores menu");
                menu.startButton.onClick.Invoke();menu.StartJourney();Check(menu.Starting&&!menu.startButton.interactable,"START is duplicate protected");
                settle=Wait(.25f);while(settle.MoveNext())yield return null;
                Check(menu!=null&&polish.FadeAlpha>0&&polish.FadeAlpha<1&&IntegrationSession.Instance==null,"Smooth fade precedes gameplay initialization");
                while(IntegrationSession.Instance==null||IntegrationSession.Instance.CurrentRealm==null)yield return null;
                Check(IntegrationSession.Instance.CurrentRealm.RealmIndex==0&&JourneyLedger.Ensure(IntegrationSession.Instance).Count==0,"START creates fresh Jungle with zero tokens");
                var effort=IntegrationSession.Instance.GetComponentInChildren<LostWorlds.Person2.UI.EffortModel>(true);
                Check(effort!=null&&effort.Cap==55&&effort.Value==0,"Beginner effort cap and fresh reading retained");
            }
            while (IntegrationSession.Instance == null || IntegrationSession.Instance.CurrentRealm == null) yield return null;
            var session = IntegrationSession.Instance;
            session.SetInputMode(GameplayInputMode.Simulator);
            if(SessionState.GetBool("R5.WJourney",false))
            {
                SessionState.SetBool("R5.WJourney",false);session.SetInputMode(GameplayInputMode.Keyboard);
                oldBackground=InputSystem.settings.backgroundBehavior;oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                journeyKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(journeyKeyboard,new KeyboardState());InputSystem.Update();
                yield return null;yield return null;
                // Allow the movement host to observe release before holding W through all four realms.
                _=session.ForwardRequested;
                InputSystem.QueueStateEvent(journeyKeyboard,new KeyboardState(Key.W));InputSystem.Update();
            }
            if(SessionState.GetBool("R6.stability",false))
            {
                SessionState.SetBool("R6.stability",false);
                for(int cycle=0;cycle<3;cycle++)
                {
                    yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(IntegrationSession.RealmScenes[1]);
                    while(session.CurrentRealm==null || session.CurrentRealm.RealmIndex!=1 || session.PlayerReference==null)yield return null;
                    var pause=Wait(.25f);while(pause.MoveNext())yield return null;
                    var ruins=session.CurrentRealm;
                    var gate=ruins.transform.root.GetComponentInChildren<PortalUnlockMechanism>();
                    Check(gate.TestActivate(),"Stress: existing test ACTIVATE accepted");
                    while(!gate.CanExit)yield return null;
                    Check(session.TryAdvance(ruins,ruins.Exit),"Stress: production Ruins to Cave transition accepted");
                    while(session.IsTransitioning || session.CurrentRealm==null || session.CurrentRealm.RealmIndex!=2 || session.PlayerReference==null)
                    { CheckAudio(); yield return null; }
                    pause=Wait(.3f);while(pause.MoveNext()){CheckAudio();yield return null;}
                    Check(Object.FindObjectsByType<IntegrationSession>().Length==1,"Stress: cycle "+(cycle+1)+" completed with one session");
                }
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(IntegrationSession.RealmScenes[0]);
                while(session.CurrentRealm==null || session.CurrentRealm.RealmIndex!=0 || session.PlayerReference==null)yield return null;
            }
            int expected = 0;
            for (int index = 0; index < 4; index++)
            {
                while (session.CurrentRealm == null || session.CurrentRealm.RealmIndex != index || session.PlayerReference == null) yield return null;
                var realm = session.CurrentRealm; var root = realm.transform.root;
                var runner = session.PlayerReference.GetComponent<RealmwalkerRunner>();
                var wait = Wait(.35f); while (wait.MoveNext()) yield return null;
                Check(runner != null && runner.Grounded && !runner.Fallen, "Realm " + index + " spawns grounded and binds R2");
                Check(root.GetComponentsInChildren<RealmwalkerRunner>().Length == 1 && runner.GetComponent<Animator>().avatar.isHuman, "One runner with original Humanoid");
                Check(runner.GetComponent<RealmwalkerFollowCamera>().View != null && Camera.allCameras.Count(c => c.gameObject.scene == realm.gameObject.scene) == 1, "Realm follow camera, no duplicate view");
                var start = runner.transform.position;
                if(realm.Branch!=null)
                {
                    var zone=realm.Branch.Zone;var box=zone.ChoiceZone;var oldCenter=box.center;var oldSize=box.size;
                    // Exercise the authored route callbacks on clear ground without editing the scene.
                    box.center=box.transform.InverseTransformPoint(runner.transform.position);box.size=new Vector3(20,10,20);Physics.SyncTransforms();
                    int feedback=session.Hud.FeedbackCount;float z=runner.transform.position.z;float began=Time.time;
                    session.Channel.Send(HardwareCommand.RIGHT);
                    wait=Wait(.45f);while(wait.MoveNext())yield return null;
                    Check(zone.Selection==LostWorlds.Person2.World.RouteChoice.Right&&runner.Lane==1,"Route choice also reaches runner steering");
                    Check(!runner.Paused&&Mathf.Abs((runner.transform.position.z-z)/(Time.time-began)-3.1f)<.15f,"Route selection preserves 3.1 m/s forward travel");
                    Check(session.Hud.RouteText==""&&session.Hud.FeedbackCount==feedback,"Route selection creates no confirmation popup");
                    session.Channel.Send(HardwareCommand.LEFT);wait=Wait(.4f);while(wait.MoveNext())yield return null;
                    Check(runner.Lane==0&&zone.Selection==LostWorlds.Person2.World.RouteChoice.Right,"Later steering works without changing the accepted branch");
                    box.center=oldCenter;box.size=oldSize;zone.BeginNewEncounter();runner.ResetMotion(start);
                    wait=Wait(.35f);while(wait.MoveNext())yield return null;
                }
                Check(Object.FindObjectsByType<IntegrationSession>().Length == 1 && Object.FindObjectsByType<JourneyLedger>().Length == 1, "Single persistent session and token ledger");
                Check(Object.FindObjectsByType<HardwareCommandSimulator>().Count(s => s.isActiveAndEnabled) == 1 && Object.FindObjectsByType<ProcessedHardwareAdapter>().Count(s => s.isActiveAndEnabled) == 1, "Single active keyboard source and hardware adapter");
                session.Channel.Send(HardwareCommand.LEFT);
                wait = Wait(.4f); while (wait.MoveNext()) yield return null;
                Check(runner.Lane == -1 && Mathf.Abs(runner.transform.position.x + 2) < .15f, "LEFT movement reaches left position");
                session.Channel.Send(HardwareCommand.LEFT);
                wait = Wait(.25f); while (wait.MoveNext()) yield return null;
                Check(runner.Lane == -1, "Outer position clamped");
                session.GetComponent<ProcessedHardwareAdapter>().ReceiveMessage("{\"command\":\"RIGHT\"}");
                wait = Wait(.4f); while (wait.MoveNext()) yield return null;
                Check(runner.Lane == 0 && Mathf.Abs(runner.transform.position.x) < .15f, "P10 RIGHT returns to center");
                System.IO.Directory.CreateDirectory("Assets/Realmwalker/Validation/R6");
                ScreenCapture.CaptureScreenshot("Assets/Realmwalker/Validation/R6/Realm" + index + ".png");
                var world = root.GetComponentInChildren<JourneyRealm>();
                var tokens = world.Tokens;
                Check(tokens.Length == (index == 0 || index == 3 ? 10 : 8), "Preserved token count in realm " + index);
                runner.SetAutoRunPaused(true);
                foreach (var token in tokens)
                {
                    runner.ResetMotion(token.Center + new Vector3(token.Center.x > 0 ? -2 : 2, -.925f, 0)); Physics.SyncTransforms();
                    Check(!token.TryCollect(runner.transform), "Neighbor position rejected: " + token.TokenId);
                    runner.ResetMotion(token.Center - Vector3.up * 3); Physics.SyncTransforms();
                    Check(!token.TryCollect(runner.transform), "Vertical separation rejected: " + token.TokenId);
                    runner.ResetMotion(token.Center - Vector3.up * .925f); Physics.SyncTransforms();
                    float end = Time.time + .08f; while (Time.time < end) yield return null;
                    Check(token.IsCollected && !token.TryCollect(runner.transform), "Physical overlap and duplicate guard: " + token.TokenId);
                }
                expected += tokens.Length; Check(world.Ledger.Count == expected, "Persistent token tally " + expected + "/36");
                var gate = root.GetComponentInChildren<PortalUnlockMechanism>();
                if (index < 3)
                {
                    var zone = gate.GetComponent<BoxCollider>();
                    foreach (float x in new[] { -2f, 0f, 2f })
                    {
                        runner.ResetMotion(new Vector3(x, realm.Exit.position.y, zone.bounds.min.z - 1)); Physics.SyncTransforms();
                        Check(!gate.CanInteract(session.Interactor), "Gateway rejects distant approach, position " + x);
                        session.GetComponent<ProcessedHardwareAdapter>().ReceiveMessage("{\"command\":\"ACTIVATE\"}");
                        Check(!gate.CanExit && !session.TryAdvance(realm, realm.Exit), "Distant ACTIVATE and premature exit blocked");
                        runner.ResetMotion(new Vector3(x, realm.Exit.position.y, zone.bounds.center.z)); Physics.SyncTransforms();
                        Check(gate.CanInteract(session.Interactor) && session.Interactor.IsInRange(gate), "Close gateway accepts position " + x);
                        gate.GetComponent<PortalAutoRunPause>().SampleNow(); Check(gate.GetComponent<PortalAutoRunPause>().Waiting, "Reachable auto-pause for position " + x);
                        gate.GetComponent<PortalAutoRunPause>().enabled = false; gate.GetComponent<PortalAutoRunPause>().enabled = true;
                    }
                }
                else Check(gate == null && !realm.CompletionOnActivation, "Hidden City has no ACTIVATE requirement");
                runner.ResetMotion(start); runner.SetAutoRunPaused(false);
                // Real continuous travel: commands jump authored obstacles/gap; no movement/transition bypass.
                var obstacleRoot = root.GetComponentsInChildren<Transform>().First(t => t.name == "P85_EnvironmentalObstacles");
                var obstacles = obstacleRoot.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger && c.enabled).ToArray();
                int jumps = runner.AcceptedJumps; bool activated = false;
                float travelDeadline = Time.time + 65;
                while (session.CurrentRealm == realm && !session.IsComplete && !session.IsTransitioning)
                {
                    CheckFrame(runner);
                    if (Time.time > travelDeadline) throw new Exception("Traversal stalled in realm " + index + " at " + runner.transform.position);
                    if (runner.Grounded && !runner.Paused)
                    {
                        float z = runner.transform.position.z;
                        bool obstacle = obstacles.Any(c => c.bounds.min.z - z < .65f && c.bounds.max.z > z && c.bounds.min.x < .32f && c.bounds.max.x > -.32f);
                        bool gap = index == 0 && z > 71.02f && z < 71.4f || root.GetComponentsInChildren<RealmJumpChallenge>().Any(g => z > g.near - .65f && z < g.near);
                        if (obstacle || gap) session.Channel.Send(HardwareCommand.JUMP);
                    }
                    if (gate != null && runner.Paused && !activated)
                    {
                        Check(gate.CanInteract(session.Interactor), "Pause always inside gateway range");
                        session.GetComponent<ProcessedHardwareAdapter>().ReceiveMessage("{\"command\":\"ACTIVATE\"}");
                        Check(gate.CanExit && !gate.TryInteract(session.Interactor), "P10 unlocks once in realm " + index); activated = true;
                    }
                    yield return null;
                }
                Check(index == 3 ? session.IsComplete : session.IsTransitioning || session.CurrentRealm.RealmIndex == index + 1, "Physical traversal reaches realm " + index + " destination");
                Debug.Log("R5 TRAVEL realm=" + index + " jumps=" + (runner == null ? "runner unloaded" : (runner.AcceptedJumps-jumps).ToString()));
            }
            var finale = Object.FindAnyObjectByType<JourneyFinale>();
            var delay = Wait(.3f); while (delay.MoveNext()) yield return null;
            Check(finale.IsVisible && finale.AwardCount == 1 && finale.FinalTokenCount == 36 && finale.EarnedStars == 3, "Automatic trophy completion, 36-token summary and three stars");
            ScreenCapture.CaptureScreenshot("Assets/Realmwalker/Validation/R6/Finale.png");
            delay = Wait(.1f); while (delay.MoveNext()) yield return null;
            Check(session.PlayerReference.GetComponent<RealmwalkerRunner>().Paused, "Completion stops runner");
            finale.FinishButton.onClick.Invoke(); Check(session.IsSessionFinished && !finale.FinishButton.interactable, "Finish Session works");
            if(journeyKeyboard!=null){InputSystem.QueueStateEvent(journeyKeyboard,new KeyboardState());InputSystem.Update();}
            finale.ReplayButton.onClick.Invoke();
            while (session.IsTransitioning || session.CurrentRealm.RealmIndex != 0 || session.PlayerReference == null) yield return null;
            Check(!session.IsComplete && JourneyLedger.Ensure(session).Count == 0, "Replay resets session/tokens and binds Jungle runner");
            if(journeyKeyboard!=null){float z=session.PlayerReference.position.z;delay=Wait(.4f);while(delay.MoveNext())yield return null;Check(Mathf.Abs(session.PlayerReference.position.z-z)<.001f,"Replay in keyboard mode waits for W");}
        }
        static void CheckFrame(RealmwalkerRunner runner)
        {
            CheckAudio();
            if (runner == null || runner.Fallen) throw new Exception("Runner fell or disappeared during traversal");
            var hips = runner.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
            if (hips.localPosition.y < .8f) throw new Exception("Crouching regression");
        }
        static void CheckAudio()
        {
            if(Object.FindObjectsByType<AudioListener>().Count(a=>a.isActiveAndEnabled)!=1)
                throw new Exception("Expected exactly one active audio listener, including loading");
        }
    }
}

