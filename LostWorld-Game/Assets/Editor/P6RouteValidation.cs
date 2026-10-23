using System;
using System.IO;
using LostWorlds.Person2.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    [InitializeOnLoad]
    public static class P6RouteValidation
    {
        const string Mode = "LostWorlds.P6.Mode", Next = "LostWorlds.P6.Next", Passed = "LostWorlds.P6.Passed", Count = "LostWorlds.P6.Count";
        static int checks;
        static P6RouteValidation() => EditorApplication.playModeStateChanged += StateChanged;
        public static void BuildAndValidateBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
            EditorSceneManager.OpenScene(P6RouteBuilder.ScenePath, OpenSceneMode.Single);
            P6RouteBuilder.Setup(); Begin("Checks");
        }
        [MenuItem("Tools/Lost Worlds/P6/Play Standalone Route Test")]
        public static void PlayTest() { if (CanOpen()) Begin("Manual"); }
        [MenuItem("Tools/Lost Worlds/P6/Run Route Play Mode Checks")]
        public static void RunChecks() { if (CanOpen()) Begin("Checks"); }
        static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        static void Begin(string mode)
        {
            EditorSceneManager.OpenScene(P6RouteBuilder.ScenePath, OpenSceneMode.Single); P6RouteBuilder.ValidateSetup();
            SessionState.SetString(Mode, mode); SessionState.SetString(Next, ""); EditorApplication.isPlaying = true;
        }
        static void StateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetString(Mode, "") != "") EditorApplication.update += RunPending;
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetString(Next, "") != "") EditorApplication.update += AfterPlay;
        }
        static void AfterPlay()
        {
            EditorApplication.update -= AfterPlay;
            var next = SessionState.GetString(Next, ""); SessionState.SetString(Next, "");
            if (next == "Reset") { Begin("Reset"); return; }
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(Passed, false) ? 0 : 1);
        }
        static P6RouteTestInput Prepare()
        {
            var scene = SceneManager.GetSceneByPath(P6RouteBuilder.ScenePath);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            var station = P6RouteBuilder.Station();
            if(station==null){var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(P6RouteBuilder.PrefabPath),new Vector3(1000,0,0),Quaternion.identity);go.name="P6_StandaloneTest";station=go.transform;}
            station.GetComponentInChildren<Camera>(true).enabled = true;
            var input = station.GetComponent<P6RouteTestInput>(); input.TestInputEnabled = true;
            return input;
        }
        static void Assert(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("P6 CHECK FAILED: " + message); checks++; Debug.Log("P6 CHECK PASS: " + message); }
        static void Key(Keyboard keyboard, P6RouteTestInput input, params UnityEngine.InputSystem.Key[] keys)
        { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); input.PollKeyboard(); }

        static void RunPending()
        {
            EditorApplication.update -= RunPending;
            var mode = SessionState.GetString(Mode, ""); SessionState.SetString(Mode, "");
            bool success = false; Keyboard keyboard = null; GameObject fixture = null;
            var settings = InputSystem.settings; var background = settings.backgroundBehavior; var editorInput = settings.editorInputBehaviorInPlayMode;
            try
            {
                var input = Prepare(); var zone = input.Route; var station = P6RouteBuilder.Station();
                if (mode == "Manual")
                {
                    Debug.Log("[P6 TEST] Click Game. A / Left Arrow = LEFT; D / Right Arrow = RIGHT. One choice only. Stop and rerun this menu to test the other choice. Selection does not move a player.");
                    return;
                }
                if (mode == "Reset")
                {
                    checks = SessionState.GetInt(Count, 0);
                    Assert(zone.Selection == RouteChoice.None && zone.EncounterNumber == 1, "Fresh Play session starts an unselected first encounter");
                    Assert(!station.Find("LEFT_WorldMarkers/LEFT_SelectedBeacon").gameObject.activeSelf &&
                           !station.Find("RIGHT_WorldMarkers/RIGHT_SelectedBeacon").gameObject.activeSelf, "Fresh session restores neutral feedback");
                    Directory.CreateDirectory("Documentation/P6");
                    File.WriteAllText("Documentation/P6/PLAY_MODE_VERIFICATION.txt", "Unity " + Application.unityVersion +
                        "\nP6 checks passed: " + checks +
                        "\nActual Input System A/D/LeftArrow/RightArrow events passed.\nOutside/disabled/ambiguous/held/repeated/opposite/reentrant input, explicit encounter reset, scaled/rotated zone reuse, callbacks, visual feedback and fresh-session reset passed.\nNo input action moved the reference transform. Test changes were not saved.\n");
                    Debug.Log("P6 PLAY MODE VALIDATION PASS: " + checks + " checks."); success = true; return;
                }
                checks = 0;
                Assert(zone.Selection == RouteChoice.None && zone.Contains(input.Reference), "Fresh scene is unselected and reference is inside generous zone");
                Assert(input.Reference.GetComponent<Collider>() == null && input.Reference.GetComponent<Rigidbody>() == null, "Reference needs no Player controller, collider or Rigidbody");
                Assert(!zone.TryChoose(RouteChoice.None, input.Reference) && !zone.TryChoose((RouteChoice)99, input.Reference) && !zone.TryChoose(RouteChoice.Left, null), "Invalid choices and missing reference rejected");
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                int accepted = 0, unityEvents = 0, began = 0; bool reentrantBlocked = true;
                zone.SelectionAccepted += (sender, choice) =>
                {
                    accepted++;
                    reentrantBlocked &= !sender.TryChoose(choice == RouteChoice.Left ? RouteChoice.Right : RouteChoice.Left, input.Reference) && !sender.BeginNewEncounter();
                };
                zone.OnSelected.AddListener(choice => unityEvents++);
                zone.EncounterBegan += sender => began++;
                var origin = input.Reference.position;
                input.Reference.position += Vector3.right * 30;
                Key(keyboard, input, UnityEngine.InputSystem.Key.A);
                Assert(zone.Selection == RouteChoice.None && !zone.TryChoose(RouteChoice.Right, input.Reference), "Out-of-zone keyboard and direct input rejected");
                input.Reference.position = origin; Key(keyboard, input);
                input.TestInputEnabled = false; Key(keyboard, input, UnityEngine.InputSystem.Key.D);
                Assert(!zone.HasChoice, "Disabled test input ignores commands"); input.TestInputEnabled = true; Key(keyboard, input);
                zone.enabled = false;
                Assert(!zone.TryChoose(RouteChoice.Left, input.Reference), "Disabled route rejects input"); zone.enabled = true;
                zone.ChoiceZone.enabled = false;
                Assert(!zone.TryChoose(RouteChoice.Right, input.Reference), "Disabled volume rejects input"); zone.ChoiceZone.enabled = true;
                Key(keyboard, input, UnityEngine.InputSystem.Key.A, UnityEngine.InputSystem.Key.D);
                Assert(!zone.HasChoice, "Simultaneous A/D accepts neither choice"); Key(keyboard, input);
                Key(keyboard, input, UnityEngine.InputSystem.Key.LeftArrow, UnityEngine.InputSystem.Key.RightArrow);
                Assert(!zone.HasChoice, "Simultaneous arrows accept neither choice"); Key(keyboard, input);
                Capture(station.GetComponentInChildren<Camera>(true), "Before");
                var keys = new[] { UnityEngine.InputSystem.Key.A, UnityEngine.InputSystem.Key.D, UnityEngine.InputSystem.Key.LeftArrow, UnityEngine.InputSystem.Key.RightArrow };
                for (int i = 0; i < keys.Length; i++)
                {
                    if (i > 0) Assert(zone.BeginNewEncounter() && !zone.HasChoice, "Explicit new encounter clears choice " + i);
                    var expected = i % 2 == 0 ? RouteChoice.Left : RouteChoice.Right;
                    Key(keyboard, input, keys[i]);
                    Assert(zone.Selection == expected && accepted == i + 1 && unityEvents == i + 1, keys[i] + " selects " + expected + " and emits one callback per API");
                    var selected = station.Find(expected == RouteChoice.Left ? "LEFT_WorldMarkers/LEFT_SelectedBeacon" : "RIGHT_WorldMarkers/RIGHT_SelectedBeacon");
                    var other = station.Find(expected == RouteChoice.Left ? "RIGHT_WorldMarkers/RIGHT_SelectedBeacon" : "LEFT_WorldMarkers/LEFT_SelectedBeacon");
                    Assert(selected.gameObject.activeSelf && !other.gameObject.activeSelf, "Only accepted side has the steady selection beacon");
                    input.PollKeyboard(); Key(keyboard, input); Key(keyboard, input, i % 2 == 0 ? UnityEngine.InputSystem.Key.D : UnityEngine.InputSystem.Key.A);
                    Assert(zone.Selection == expected && accepted == i + 1 && unityEvents == i + 1, "Repeated/opposite input cannot change encounter " + (i + 1));
                    if (i < 2) Capture(station.GetComponentInChildren<Camera>(true), expected.ToString());
                    Key(keyboard, input);
                }
                Assert(reentrantBlocked && began == 3, "Callback reentrancy cannot change selection or reset the encounter");
                zone.enabled = false; zone.enabled = true;
                input.Reference.position += Vector3.right * 30; input.Reference.position = origin;
                Assert(zone.Selection == RouteChoice.Right && !zone.TryChoose(RouteChoice.Left, input.Reference), "Disable/re-enable and zone re-entry do not rearm a chosen encounter");
                zone.BeginNewEncounter(); Key(keyboard, input, UnityEngine.InputSystem.Key.A);
                zone.BeginNewEncounter(); Key(keyboard, input, UnityEngine.InputSystem.Key.A);
                Assert(!zone.HasChoice, "A held key cannot auto-select a new encounter");
                Key(keyboard, input); Key(keyboard, input, UnityEngine.InputSystem.Key.RightArrow);
                Assert(zone.Selection == RouteChoice.Right, "Release and fresh press selects in new encounter");
                Assert(input.Reference.position == origin, "All accepted keyboard choices leave reference position unchanged");
                fixture = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(P6RouteBuilder.PrefabPath), new Vector3(1000, 0, 0), Quaternion.Euler(0, 47, 0));
                fixture.transform.localScale = new Vector3(2, 1, .5f);
                var otherZone = fixture.GetComponent<RouteChoiceZone>(); var otherInput = fixture.GetComponent<P6RouteTestInput>();
                Assert(otherZone.Contains(otherInput.Reference), "Rotated/scaled reusable zone contains its local reference");
                otherInput.Reference.localPosition = new Vector3(7, 1, 0);
                Assert(!otherZone.Contains(otherInput.Reference) && !otherZone.TryChoose(RouteChoice.Left, otherInput.Reference), "Rotated/scaled zone rejects points outside actual box");
                otherInput.Reference.localPosition = new Vector3(0, 1, 0);
                Assert(otherZone.TryChoose(RouteChoice.Left, otherInput.Reference) && zone.Selection == RouteChoice.Right, "Independent zone accepts its own encounter without affecting Jungle");
                int samples = P6RouteBuilder.ValidatePaths();
                Directory.CreateDirectory("Documentation/P6");
                File.WriteAllText("Documentation/P6/PATH_VERIFICATION.txt", "Reusable route prefab tested in a temporary fixture. Jungle is a continuous road with no branch requirement.\n");
                SessionState.SetInt(Count, checks); success = true;
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (mode != "Manual")
                {
                    if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                    if (fixture != null) Object.DestroyImmediate(fixture);
                    settings.backgroundBehavior = background; settings.editorInputBehaviorInPlayMode = editorInput;
                    SessionState.SetBool(Passed, success); SessionState.SetString(Next, success && mode == "Checks" ? "Reset" : "Exit");
                    EditorApplication.isPlaying = false;
                }
            }
        }
        static void Capture(Camera camera, string name)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new InvalidOperationException("URP unavailable.");
            var clone = Object.Instantiate(pipeline); var previous = QualitySettings.renderPipeline;
            var active = RenderTexture.active; var target = new RenderTexture(1100, 1100, 24);
            var image = new Texture2D(1100, 1100, TextureFormat.RGB24, false);
            try
            {
                clone.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled; clone.useSRPBatcher = false; QualitySettings.renderPipeline = clone;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target, isPreview = true });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1100, 1100), 0, 0); image.Apply();
                Directory.CreateDirectory("Documentation/P6"); File.WriteAllBytes("Documentation/P6/Route_" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                QualitySettings.renderPipeline = previous; RenderTexture.active = active; target.Release();
                Object.DestroyImmediate(target); Object.DestroyImmediate(image); Object.DestroyImmediate(clone);
            }
        }
    }
}

