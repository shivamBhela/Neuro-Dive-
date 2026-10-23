using System;
using System.IO;
using LostWorlds.Person2.UI;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    public static class P7HudBuilder
    {
        public const string ScenePath = "Assets/Scenes/Test/P7_HudDemo.unity";
        public const string HudPath = "Assets/Prefabs/UI/P7/Person2Hud.prefab";
        static readonly Color Paper = new Color(.96f,.95f,.90f), Muted = new Color(.73f,.81f,.78f);
        static Font font;
        [MenuItem("Tools/Lost Worlds/P7/Setup HUD Demo")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save open scenes before setup.");
            if (File.Exists(ScenePath) && File.Exists(HudPath)) { Debug.Log("P7 existing HUD/demo preserved."); return; }
            Folder("Assets/Prefabs/UI/P7");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var type in new[] { "Person2Hud", "HudWorldContext", "P7HudDemoControls" })
            {
                var p = "Assets/Scripts/UI/" + type + ".cs";
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadAssetAtPath<MonoScript>(p)?.GetClass() == null) throw new InvalidOperationException("Script import incomplete: " + type);
            }
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, OpenSceneModeToNew());
            try
            {
                SceneManager.SetActiveScene(scene);
                MakeHud();
                if (File.Exists(ScenePath)) return;
                var demo = new GameObject("P7_HudDemo").transform;
                var driver = demo.gameObject.AddComponent<P7HudDemoControls>();
                var hud = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath), demo)).GetComponent<Person2Hud>();
                var settings = new SerializedObject(driver);
                settings.FindProperty("context").objectReferenceValue = hud.GetComponent<HudWorldContext>();
                var hints = Text("DemoControls", hud.transform, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(0,-32), new Vector2(860,76), "", 20, Muted);
                hints.alignment = TextAnchor.UpperCenter;
                settings.FindProperty("instructions").objectReferenceValue = hints;
                var station = Group("Interactions_TEST_ONLY", demo);
                Floor(station, new Vector3(0,-.2f,1), new Vector3(20,.4f,16));
                var grab = Instance("Assets/Prefabs/Interactions/P4/GrabDemoObject.prefab", station); grab.localPosition = new Vector3(-3,1.1f,0);
                var activate = Instance("Assets/Prefabs/Interactions/P4/ActivateBeacon.prefab", station); activate.localPosition = new Vector3(4,0,0);
                var hold = Group("HoldPoint", station); hold.localPosition = new Vector3(0,1.1f,0);
                var grabActor = Actor(station, new Vector3(-3,1.1f,-2)); grabActor.HoldPoint = hold;
                var activateActor = Actor(station, new Vector3(4,1.1f,-2));
                var p4 = station.gameObject.AddComponent<P4KeyboardTestInput>(); p4.GrabInteractor = grabActor; p4.ActivateInteractor = activateActor;
                settings.FindProperty("interactions").objectReferenceValue = p4;
                settings.FindProperty("grab").objectReferenceValue = grab.GetComponent<GrabInteractable>();
                settings.FindProperty("activate").objectReferenceValue = activate.GetComponent<ActivateInteractable>();
                var camera = Camera(station, new Vector3(4,6,-12), new Vector3(.5f,1,0), 7.3f);
                camera.gameObject.AddComponent<AudioListener>();
                settings.FindProperty("interactionCamera").objectReferenceValue = camera;
                var route = Instance(P6RouteBuilder.PrefabPath, demo); route.position = Vector3.right * 180;
                Floor(route, new Vector3(0,-.2f,10), new Vector3(44,.4f,65));
                settings.FindProperty("routeInput").objectReferenceValue = route.GetComponent<P6RouteTestInput>();
                settings.FindProperty("routeCamera").objectReferenceValue = route.GetComponentInChildren<Camera>(true);
                settings.ApplyModifiedPropertiesWithoutUndo();
                var light = Group("TestSun", demo).gameObject.AddComponent<Light>(); light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(45,-25,0); light.color = new Color(1,.94f,.84f); light.intensity = 1.3f;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.53f,.61f,.60f);
                RenderSettings.fog = false;
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("P7 demo save failed.");
                Debug.Log("P7 HUD prefab and separate demo scene saved. No environment scene modified.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
        static NewSceneMode OpenSceneModeToNew() => NewSceneMode.Additive;
        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static Transform Group(string name, Transform parent)
        { var t = new GameObject(name).transform; if (parent != null) t.SetParent(parent,false); return t; }
        static Transform Instance(string path, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (prefab == null) throw new InvalidOperationException("Missing existing prefab: " + path);
            return ((GameObject)PrefabUtility.InstantiatePrefab(prefab,parent)).transform;
        }
        static InteractionInteractor Actor(Transform parent, Vector3 position)
        { var t = Group("StationaryReference_TEST_ONLY",parent); t.localPosition = position; var actor = t.gameObject.AddComponent<InteractionInteractor>(); actor.Range = 3.5f; return actor; }
        static void Floor(Transform parent, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "TestFloor"; go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/WorldExpansion/Slate.mat");
        }
        static Camera Camera(Transform parent, Vector3 position, Vector3 look, float size)
        {
            var c = Group("TestCamera",parent).gameObject.AddComponent<Camera>(); c.transform.localPosition = position; c.transform.LookAt(parent.TransformPoint(look));
            c.orthographic = true; c.orthographicSize = size; c.nearClipPlane = .1f; c.farClipPlane = 100;
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(.28f,.36f,.36f); return c;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        static Text Text(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, string value, int fontSize, Color color)
        {
            var t = Rect(name,parent,anchor,pivot,position,size).gameObject.AddComponent<Text>();
            t.font = font; t.text = value; t.fontSize = fontSize; t.color = color; t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false; t.supportRichText = false; return t;
        }
        static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var r = Rect(name,parent,anchor,pivot,position,size); var image = r.gameObject.AddComponent<Image>();
            image.color = new Color(.055f,.12f,.12f,.96f); image.raycastTarget = false; return r;
        }
        static void MakeHud()
        {
            if (File.Exists(HudPath)) return;
            var root = new GameObject("Person2Hud",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
            try
            {
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100; canvas.pixelPerfect = true;
                var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600,900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
                root.GetComponent<CanvasGroup>().blocksRaycasts = false; root.GetComponent<CanvasGroup>().interactable = false;
                var source = root.AddComponent<HudWorldContext>(); var hud = root.AddComponent<Person2Hud>(); var serial = new SerializedObject(hud);
                serial.FindProperty("context").objectReferenceValue = source;
                Text("Title",root.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(32,-32),new Vector2(280,46),"LOST WORLDS",28,Paper);
                var interaction = Panel("InteractionPrompt",root.transform,Vector2.zero,Vector2.zero,new Vector2(32,32),new Vector2(470,154));
                Text("Heading",interaction,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-15),new Vector2(422,30),"INTERACTION",22,Muted);
                serial.FindProperty("interactionText").objectReferenceValue = Text("Prompt",interaction,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-51),new Vector2(422,86),"",28,Paper);
                serial.FindProperty("interactionPanel").objectReferenceValue = interaction.gameObject;
                var route = Panel("RoutePrompt",root.transform,new Vector2(1,0),new Vector2(1,0),new Vector2(-32,32),new Vector2(470,154));
                Text("Heading",route,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-15),new Vector2(422,30),"YOUR PATH",22,Muted);
                serial.FindProperty("routeText").objectReferenceValue = Text("Prompt",route,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-51),new Vector2(422,86),"",28,Paper);
                serial.FindProperty("routePanel").objectReferenceValue = route.gameObject;
                var feedback = Panel("PositiveFeedback",root.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,216),new Vector2(760,74));
                var label = Text("Message",feedback,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(720,60),"",26,Paper);
                label.alignment = TextAnchor.MiddleCenter;
                serial.FindProperty("feedbackText").objectReferenceValue = label; serial.FindProperty("feedbackPanel").objectReferenceValue = feedback.gameObject;
                serial.ApplyModifiedPropertiesWithoutUndo(); interaction.gameObject.SetActive(false); route.gameObject.SetActive(false); feedback.gameObject.SetActive(false);
                if (PrefabUtility.SaveAsPrefabAsset(root,HudPath) == null) throw new IOException("HUD prefab save failed.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        [MenuItem("Tools/Lost Worlds/P7/Play HUD Demo")]
        public static void Play()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Setup();
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single); EditorApplication.isPlaying = true;
        }
    }
}

