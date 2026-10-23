using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Interaction;
using Object = UnityEngine.Object;

namespace Realmwalker.Authoring
{
    public static class RealmwalkerR5
    {
        const string Root = "Assets/Realmwalker", Prefab = Root + "/Realmwalker_Player.prefab";
        static void Set(Object target, string property, Object value) { var so = new SerializedObject(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        static void Number(Object target, string property, float value) { var so = new SerializedObject(target); so.FindProperty(property).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }

        [MenuItem("Tools/Lost Worlds/R5/Apply All-Realm Integration")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var model = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var back = model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name == "HairBack");
                var sourcePath = AssetDatabase.FindAssets("HairBack t:Mesh", new[] { Root + "/Rig/SkinnedMeshes" }).Select(AssetDatabase.GUIDToAssetPath).Single();
                var mesh = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Mesh>(sourcePath));
                mesh.name = "RealmwalkerCrownHair";
                // A fitted scalp shell with a higher front hairline and tapered nape; existing swept locks stay.
                var bindposes = mesh.bindposes; var weight = mesh.boneWeights[0];
                const int sides = 20, rings = 9;
                var vertices = new Vector3[(sides + 1) * (rings + 1)]; var uv = new Vector2[vertices.Length];
                var triangles = new System.Collections.Generic.List<int>();
                for (int r = 0; r <= rings; r++) for (int s = 0; s <= sides; s++)
                {
                    float a = s * Mathf.PI * 2 / sides;
                    float low = Mathf.Lerp(-.62f, .55f, (Mathf.Sin(a) + 1) * .5f);
                    float latitude = Mathf.Lerp(low, Mathf.PI * .5f, r / (float)rings);
                    float ridge = 1 + .012f * Mathf.Cos(a * 8 + latitude * 2);
                    Vector3 world = new Vector3(.127f * Mathf.Cos(latitude) * Mathf.Cos(a) * ridge,
                        1.748f + .169f * Mathf.Sin(latitude), .116f * Mathf.Cos(latitude) * Mathf.Sin(a) * ridge);
                    int n = r * (sides + 1) + s; vertices[n] = back.transform.InverseTransformPoint(world); uv[n] = new Vector2(s/(float)sides, r/(float)rings);
                    if(r < rings && s < sides) { int b=n+sides+1; triangles.AddRange(new[]{n,b,n+1,n+1,b,b+1}); }
                }
                mesh.Clear(); mesh.vertices = vertices; mesh.triangles = triangles.ToArray(); mesh.uv = uv;
                mesh.bindposes = bindposes; mesh.boneWeights = Enumerable.Repeat(weight, vertices.Length).ToArray();
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
                string path = Root + "/Rig/RealmwalkerCrownHair.asset";
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved != null)
                {
                    // Mesh native vertex buffers must be updated explicitly when the vertex count changes.
                    saved.Clear(); saved.vertices = mesh.vertices; saved.triangles = mesh.triangles;
                    saved.uv = mesh.uv; saved.normals = mesh.normals; saved.tangents = mesh.tangents;
                    saved.bindposes = mesh.bindposes; saved.boneWeights = mesh.boneWeights;
                    saved.RecalculateBounds(); EditorUtility.SetDirty(saved);
                    Object.DestroyImmediate(mesh); mesh = saved;
                }
                else AssetDatabase.CreateAsset(mesh, path);
                back.sharedMesh = mesh;
                back.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Hair.mat");
                PrefabUtility.SaveAsPrefabAsset(model, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(model); }
            AssetDatabase.SaveAssets();
            for (int i = 0; i < 4; i++)
            {
                var scene = EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);
                var realm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RealmBindings>(true)).Single();
                var runner = realm.transform.root.GetComponentInChildren<RealmwalkerRunner>();
                if (runner == null)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
                    go.transform.SetParent(realm.transform.root, false);
                    var position = realm.Entrance.position; position.x = 0; position.z += 1;
                    Physics.SyncTransforms();
                    if (!Physics.Raycast(position + Vector3.up, Vector3.down, out var hit, 3, ~0, QueryTriggerInteraction.Ignore)) throw new Exception("No arrival floor in " + scene.name);
                    position.y = hit.point.y + .06f;
                    go.transform.SetPositionAndRotation(position, Quaternion.identity);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                    runner = go.GetComponent<RealmwalkerRunner>();
                }
                if (i > 0)
                {
                    var p = realm.Entrance.position; p.x = 0; p.z += 1;
                    runner.GetComponent<CharacterController>().enabled = false; Physics.SyncTransforms();
                    if (!Physics.Raycast(p + Vector3.up, Vector3.down, out var floor, 3, ~0, QueryTriggerInteraction.Ignore)) throw new Exception("Arrival floor missing");
                    runner.transform.position = new Vector3(p.x, floor.point.y + .06f, p.z);
                    runner.GetComponent<CharacterController>().enabled = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(runner.transform);
                }
                Debug.Log("R5 REALM " + i + " entrance=" + realm.Entrance.position + " runner=" + runner.transform.position + " exit=" + realm.Exit.position);
                if (realm.Exit.position.z <= realm.Entrance.position.z) throw new Exception("Unexpected road direction");
                if (i < 3)
                {
                    var gate = realm.transform.root.GetComponentInChildren<PortalUnlockMechanism>(true);
                    var zone = gate.GetComponent<BoxCollider>();
                    Vector3 center = realm.Exit.position + new Vector3(0, 1.0f, -3.2f); center.x = 0;
                    zone.center = gate.transform.InverseTransformPoint(center);
                    zone.size = new Vector3(5.2f, 3.0f, 1.2f); zone.isTrigger = true;
                    Set(gate, "activationZone", zone); Number(gate, "activationRadius", .2f);
                    var point = gate.transform.Find("GatewayTestPoint");
                    if (point == null) { point = new GameObject("GatewayTestPoint").transform; point.SetParent(gate.transform, false); }
                    point.position = center;
                    var so = new SerializedObject(gate);
                    var interactor = (InteractionInteractor)so.FindProperty("testInteractor").objectReferenceValue;
                    Set(interactor, "reference", point);
                    Debug.Log("R5 GATE " + i + " zone=" + center + " size=" + zone.size + " margin=.2m");
                }
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }

        public static void Batch() => EditorApplication.delayCall += () => { try { Apply(); HairPreview(); EditorApplication.Exit(0); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } };
        public static void PreviewHairBatch() => EditorApplication.delayCall += () => { try { HairPreview(); EditorApplication.Exit(0); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } };

        static void HairPreview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            model.GetComponent<Animator>().enabled = false;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh(); skin.BakeMesh(mesh);
                if (skin.name == "HairBack")
                {
                    float error = mesh.vertices.Zip(skin.sharedMesh.vertices, Vector3.Distance).Max();
                    if (error > .001f) throw new Exception("Hair bind pose mismatch: " + error);
                    Debug.Log("R5 HAIR BIND: " + mesh.vertexCount + " vertices, rest error=" + error);
                }
                var visual = new GameObject("UnsavedHairPreview"); visual.transform.SetParent(skin.transform, false);
                visual.AddComponent<MeshFilter>().sharedMesh = mesh; visual.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials; skin.enabled = false;
            }
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.48f, .48f, .48f);
            var light = new GameObject("PreviewLight").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.6f; light.transform.rotation = Quaternion.Euler(30, -30, 0);
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = .27f; camera.nearClipPlane = .05f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.18f,.22f,.26f);
            var center = new Vector3(0, 1.76f, 0);
            string folder = Root + "/Validation/R5Hair"; Directory.CreateDirectory(folder);
            var directions = new[] { Vector3.forward, Vector3.back, Vector3.right, new Vector3(1, .2f, -1).normalized };
            var names = new[] { "Front", "Rear", "Side", "ThreeQuarter" };
            for (int i = 0; i < directions.Length; i++)
            {
                camera.transform.position = center + directions[i] * 2; camera.transform.LookAt(center);
                var rt = new RenderTexture(640, 640, 24); var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0,0,640,640),0,0); image.Apply(); File.WriteAllBytes(folder + "/" + names[i] + ".png", image.EncodeToPNG());
                camera.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            }
            Debug.Log("R5 HAIR: original weights/bindposes and material; four preview angles saved.");
        }
    }
}
