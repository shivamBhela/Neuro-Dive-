using System;
using System.IO;
using System.Linq;
using LostWorlds.Person2.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    public static class P6RouteBuilder
    {
        public const string ScenePath = "Assets/Scenes/Person2/Jungle_Prototype.unity";
        public const string RootName = "P6_RouteChoice";
        public const string PrefabPath = "Assets/Prefabs/World/Routes/P6RouteChoice.prefab";
        const string Art = "Assets/Art/Environments/Jungle/P6Routes";
        const string Mats = "Assets/Art/Materials/P6Routes";

        [MenuItem("Tools/Lost Worlds/P6/Setup Jungle Route Choice")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before setup.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save open scenes before setup. No changes made.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = scene.GetRootGameObjects().Single(go => go.name == "Jungle_Prototype").transform;
                if (root.Find(RootName) != null) { ValidateSetup(); Debug.Log("P6 existing setup preserved."); return; }
                Folder(Mats); Folder(Art); Folder("Assets/Prefabs/World/Routes");
                PreparePrefab();
                var anchor = root.Find("FutureGameplayPoints/RouteSplit");
                if (anchor == null) { Debug.Log("Jungle is a single road; reusable P6 test prefab remains available."); return; }
                var station = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), root)).transform;
                station.name = RootName; station.position = anchor.position - Vector3.up * .15f;
                ValidateSetup(); ValidatePaths();
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Unable to save Jungle.");
                Debug.Log("P6 route choice generated and saved. Existing paths and P4/P5 objects preserved.");
            }
            finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }
        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        static Transform Group(string name, Transform parent)
        { var t = new GameObject(name).transform; if (parent != null) t.SetParent(parent, false); return t; }
        static Material Mat(string name, Color color, float emission = 0)
        {
            var path = Mats + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path); if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) throw new InvalidOperationException("URP missing.");
            mat = new Material(shader) { name = name }; mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", .15f);
            if (emission > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * emission); }
            AssetDatabase.CreateAsset(mat, path); return mat;
        }
        static Renderer Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale; Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material; return renderer;
        }
        static Mesh ArrowMesh()
        {
            var path = Art + "/GroundArrow.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "GroundArrow" };
            mesh.vertices = new[] { new Vector3(-.3f,0,-1),new Vector3(.3f,0,-1),new Vector3(.3f,0,0),new Vector3(.8f,0,0),new Vector3(0,0,1.2f),new Vector3(-.8f,0,0),new Vector3(-.3f,0,0) };
            mesh.triangles = new[] { 0,2,1,0,6,2,5,4,3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static void PreparePrefab()
        {
            if (File.Exists(PrefabPath)) return;
            foreach (var type in new[] { "RouteChoiceZone", "RouteChoiceFeedback", "P6RouteTestInput" })
            {
                var path = "Assets/Scripts/World/" + type + ".cs";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadAssetAtPath<MonoScript>(path)?.GetClass() == null) throw new InvalidOperationException("Script import not ready: " + type);
            }
            var ready = Mat("ReadyBlue", new Color(.25f, .48f, .61f), .12f);
            var selected = Mat("SelectedTeal", new Color(.25f, 1, .67f), .5f);
            var stone = Mat("MarkerStone", new Color(.65f, .68f, .57f));
            var arrow = ArrowMesh();
            var root = Group(RootName, null);
            try
            {
                var box = root.gameObject.AddComponent<BoxCollider>(); box.isTrigger = true;
                box.center = new Vector3(0, 1.5f, 0); box.size = new Vector3(12, 4, 10);
                var zone = root.gameObject.AddComponent<RouteChoiceZone>();
                var serial = new SerializedObject(zone); serial.FindProperty("choiceZone").objectReferenceValue = box; serial.ApplyModifiedPropertiesWithoutUndo();
                var feedback = root.gameObject.AddComponent<RouteChoiceFeedback>();
                var fs = new SerializedObject(feedback);
                fs.FindProperty("route").objectReferenceValue = zone;
                fs.FindProperty("readyMaterial").objectReferenceValue = ready;
                fs.FindProperty("selectedMaterial").objectReferenceValue = selected;
                for (int side = -1; side <= 1; side += 2)
                {
                    bool left = side < 0; string label = left ? "LEFT" : "RIGHT";
                    var markers = Group(label + "_WorldMarkers", root);
                    Shape("Post", markers, PrimitiveType.Cylinder, new Vector3(side * 7, 1, 0), new Vector3(.2f, 1, .2f), stone);
                    var board = Shape(label + "_Sign", markers, PrimitiveType.Cube, new Vector3(side * 7, 2.2f, 0), new Vector3(3, .9f, .18f), ready);
                    var text = Group(label + "_WorldLabel", markers).gameObject.AddComponent<TextMesh>();
                    text.transform.localPosition = new Vector3(side * 7, 2.2f, -.12f);
                    text.text = label; text.fontSize = 64; text.characterSize = .25f;
                    text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = Color.white;
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    text.GetComponent<Renderer>().sharedMaterial = text.font.material;
                    var bead = Shape(label + "_SelectedBeacon", markers, PrimitiveType.Sphere, new Vector3(side * 7, 3.15f, 0), Vector3.one * .9f, selected);
                    bead.gameObject.SetActive(false);
                    fs.FindProperty(left ? "leftSelectionMarker" : "rightSelectionMarker").objectReferenceValue = bead.gameObject;
                    var array = fs.FindProperty(left ? "leftMarkers" : "rightMarkers"); array.arraySize = 4;
                    array.GetArrayElementAtIndex(0).objectReferenceValue = board;
                    for (int i = 0; i < 3; i++)
                    {
                        var t = Group(label + "_GroundArrow_" + i, markers);
                        t.localPosition = i == 0 ? new Vector3(side * 4.7f, .10f, 4.2f) :
                            i == 1 ? new Vector3(side * 8.9f, .10f, 14) : new Vector3(side * 4, .5f, 22);
                        t.localRotation = Quaternion.Euler(i == 2 ? -4 : 0, i == 0 ? side * 48 : i == 1 ? -side * 12.5f : -side * 45, 0);
                        t.gameObject.AddComponent<MeshFilter>().sharedMesh = arrow;
                        var renderer = t.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = ready;
                        array.GetArrayElementAtIndex(i + 1).objectReferenceValue = renderer;
                    }
                }
                fs.ApplyModifiedPropertiesWithoutUndo();
                var reference = Group("StationaryReference_TEST_ONLY", root); reference.localPosition = new Vector3(0, 1.2f, -2);
                var input = root.gameObject.AddComponent<P6RouteTestInput>(); var inputData = new SerializedObject(input);
                inputData.FindProperty("route").objectReferenceValue = zone; inputData.FindProperty("reference").objectReferenceValue = reference;
                inputData.ApplyModifiedPropertiesWithoutUndo();
                var camera = Group("OverviewCamera_TEST_ONLY", root).gameObject.AddComponent<Camera>();
                camera.transform.localPosition = new Vector3(0, 29, -13); camera.transform.LookAt(root.TransformPoint(new Vector3(0, 0, 12)));
                camera.orthographic = true; camera.orthographicSize = 20; camera.nearClipPlane = .1f; camera.farClipPlane = 110;
                camera.depth = 40; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.20f, .28f, .20f); camera.enabled = false;
                if (PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath) == null) throw new IOException("Route prefab save failed.");
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }
        public static Transform Station()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            return scene.GetRootGameObjects().Single(g => g.name == "Jungle_Prototype").transform.Find(RootName) ?? GameObject.Find("P6_StandaloneTest")?.transform;
        }
        [MenuItem("Tools/Lost Worlds/P6/Validate Jungle Route Setup")]
        public static void ValidateSetup()
        {
            var station = Station(); if (station == null) { JungleRoadRefinement.Validate(); return; }
            var zone = station.GetComponent<RouteChoiceZone>(); var input = station.GetComponent<P6RouteTestInput>();
            if (zone == null || zone.ChoiceZone == null || !zone.ChoiceZone.isTrigger || input == null || input.Reference == null ||
                station.GetComponent<RouteChoiceFeedback>() == null || station.GetComponentInChildren<Camera>(true) == null)
                throw new InvalidOperationException("Missing route setup references.");
            if (station.GetComponentsInChildren<Component>(true).Any(c => c == null)) throw new InvalidOperationException("Missing route script.");
            Debug.Log("P6 SETUP PASS: world zone, markers, feedback and stationary test reference.");
        }
        public static int ValidatePaths()
        {
            if(Station()==null || Station().name=="P6_StandaloneTest")return 0; // The dedicated fixture tests choices, not Jungle branches.
            var root = Station().parent;
            var ground = root.Find("Environment/Terrain_Ground/JungleGround").GetComponent<MeshCollider>();
            Physics.SyncTransforms(); int samples = 0;
            foreach (float side in new[] { -1f, 1f })
            {
                var route = new[] { new Vector3(0,0,70), new Vector3(side*10,0,79), new Vector3(side*8,0,88), new Vector3(0,0,96) };
                for (int i = 0; i < route.Length - 1; i++)
                {
                    var direction = (route[i + 1] - route[i]).normalized; var across = Vector3.Cross(Vector3.up, direction);
                    int steps = Mathf.CeilToInt(Vector3.Distance(route[i], route[i + 1]));
                    for (int j = 0; j <= steps; j++)
                    foreach (float offset in new[] { -1.5f, 0, 1.5f })
                    {
                        var point = Vector3.Lerp(route[i], route[i + 1], (float)j / steps) + across * offset;
                        if (!ground.Raycast(new Ray(point + Vector3.up * 20, Vector3.down), out var hit, 40))
                            throw new InvalidOperationException("Missing branch ground at " + point);
                        if (Physics.CheckCapsule(hit.point + Vector3.up * .65f, hit.point + Vector3.up * 1.6f, .4f, ~0, QueryTriggerInteraction.Ignore))
                            throw new InvalidOperationException("Branch body clearance blocked at " + point);
                        samples++;
                    }
                }
            }
            Debug.Log("P6 PATH PASS: " + samples + " ground/body-clearance samples across both branches.");
            return samples;
        }
    }
}

