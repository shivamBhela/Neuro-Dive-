using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    // Authoring only. No runtime behaviour, input, triggers, or player dependencies.
    public static class JunglePrototypeBuilder
    {
        public const string ScenePath = "Assets/Scenes/Person2/Jungle_Prototype.unity";
        const string Art = "Assets/Art/Environments/Jungle/Prototype";
        const string Mats = "Assets/Art/Materials/JunglePrototype";
        const string Prefabs = "Assets/Prefabs/World/JunglePrototype";
        static readonly Dictionary<string, Material> Palette = new Dictionary<string, Material>();
        static System.Random random;

        [MenuItem("Tools/Lost Worlds/Build Jungle Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play mode before building the Jungle prototype.");
                return;
            }
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                {
                    Debug.LogWarning("Save any untitled scene under its own name, or open a saved scene, before building/opening Jungle additively. No open content was changed.");
                    return;
                }
            // Repeated use never overwrites hand-edited scenes or reusable assets.
            if (File.Exists(ScenePath))
            {
                var existing = SceneManager.GetSceneByPath(ScenePath);
                if (!existing.isLoaded) existing = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(existing);
                FocusScene(existing);
                Debug.Log("Jungle prototype already exists. Opened without rebuilding or overwriting it.");
                return;
            }

            EnsureFolder(Art); EnsureFolder(Mats); EnsureFolder(Prefabs);
            EnsureFolder("Assets/Scenes/Person2");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader is unavailable. Check the existing URP package before building.");
            Palette.Clear();
            ColorMaterial("Ground", new Color(.18f, .25f, .12f), shader);
            ColorMaterial("Trail", new Color(.61f, .40f, .20f), shader);
            ColorMaterial("Bark", new Color(.23f, .13f, .075f), shader);
            ColorMaterial("Canopy", new Color(.12f, .34f, .15f), shader);
            ColorMaterial("LeafLight", new Color(.32f, .49f, .12f), shader);
            ColorMaterial("Fern", new Color(.24f, .43f, .19f), shader);
            ColorMaterial("Rock", new Color(.32f, .38f, .34f), shader);
            ColorMaterial("Moss", new Color(.37f, .44f, .19f), shader);
            ColorMaterial("Ruin", new Color(.61f, .60f, .43f), shader);
            ColorMaterial("Wood", new Color(.42f, .25f, .12f), shader);
            ColorMaterial("Ravine", new Color(.095f, .14f, .13f), shader);
            ColorMaterial("LocationTeal", new Color(.21f, .67f, .58f), shader);
            ColorMaterial("LocationAmber", new Color(.86f, .59f, .18f), shader);

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                random = new System.Random(2718);
                var root = Group("Jungle_Prototype", null);
                var environment = Group("Environment", root);
                var terrain = Group("Terrain_Ground", environment);
                var paths = Group("JunglePath", environment);
                var vegetation = Group("Vegetation", environment);
                var rocks = Group("Rocks", environment);
                var obstacles = Group("Obstacles", environment);
                var landmarks = Group("Landmarks", environment);
                var locations = Group("FutureGameplayPoints", root);
                var lighting = Group("Lighting", root);

                BuildGround(terrain);
                var route = new[] { P(0, 0), P(-2, 12), P(3, 24), P(0, 32), P(-10, 35), P(-10, 45), P(0, 50), P(0, 62), P(0, 70) };
                var left = new[] { P(0, 70), P(-10, 79), P(-8, 88), P(0, 96) };
                var right = new[] { P(0, 70), P(10, 79), P(8, 88), P(0, 96) };
                var exit = new[] { P(0, 96), P(0, 110) };
                Trail("MainTrail_WithWestBridgeBypass", paths, route, 5.2f);
                Trail("LeftRoute_Placeholder", paths, left, 4.5f);
                Trail("RightRoute_Placeholder", paths, right, 4.5f);
                Trail("ExitApproach", paths, exit, 5.2f);
                Trail("RuinClearingApproach", paths, new[] { P(0, 55), P(9, 55) }, 4);
                Clearing("StartClearing", paths, 0, 2, 8, "Trail");
                Clearing("ExplorationGlade", paths, -7, 20, 6, "Trail");
                Trail("GladeApproach", paths, new[] { P(2, 20), P(-7, 20) }, 3.5f);
                Clearing("RuinCourtyard", paths, 8, 56, 7, "Trail");
                Clearing("RouteSplitClearing", paths, 0, 70, 5, "Trail");

                var tree = Prefab("CanopyTree", MakeTree);
                var fern = Prefab("FernCluster", MakeFern);
                var boulder = Prefab("MossyBoulder", MakeBoulder);
                var column = Prefab("RuinColumn", MakeColumn);
                var bridge = Prefab("BrokenBridge", MakeBridge);
                Instance(bridge, "BrokenBridge", landmarks, new Vector3(0, Height(40), 40), 1, 0);
                BuildRuins(landmarks, column);

                // Repeated boundary rhythm frames the route without covering its clear width.
                for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 19; i++)
                {
                    float z = -5 + i * 6.5f;
                    float x = side * (24 + Range(-2, 2));
                    Instance(tree, "BoundaryCanopy", vegetation, P(x, z), Range(.95f, 1.4f), Range(0, 360));
                    Instance(boulder, "BoundaryRock", rocks, P(side * 29, z), Range(1.3f, 2.1f), Range(0, 360));
                }
                var trails = new[] { route, left, right, exit, new[] { P(0, 55), P(9, 55) }, new[] { P(2, 20), P(-7, 20) } };
                for (int i = 0; i < 240; i++)
                {
                    float x = Range(-24, 24), z = Range(-4, 110);
                    if (Reserved(x, z, trails)) continue;
                    if (i % 3 == 0) Instance(tree, "InteriorCanopy", vegetation, P(x, z), Range(.70f, 1.15f), Range(0, 360));
                    else if (i % 3 == 1) Instance(fern, "Understory", vegetation, P(x, z), Range(.7f, 1.6f), Range(0, 360));
                    else Instance(boulder, "NaturalRock", rocks, P(x, z), Range(.5f, 1.2f), Range(0, 360));
                }
                for (int i = 0; i < route.Length - 1; i++)
                {
                    var midpoint = (route[i] + route[i + 1]) * .5f;
                    Instance(fern, "TrailsideFern", vegetation, P(midpoint.x + 4.7f, midpoint.z), 1, 20 * i);
                }
                // Block entry to the broken deck; the west bypass is the usable environmental route.
                Box("BridgeApproach_Rubble", obstacles, P(0, 33.8f) + Vector3.up * .6f, new Vector3(4, 1.2f, 1), "Rock");
                Box("BridgeFarBank_Rubble", obstacles, P(0, 46.2f) + Vector3.up * .6f, new Vector3(4, 1.2f, 1), "Rock");
                var log = Shape("FallenTrunk_OffTrail", obstacles, PrimitiveType.Cylinder, P(-17, 58) + Vector3.up * .65f, new Vector3(1.1f, 4, 1.1f), "Bark", true);
                log.localRotation = Quaternion.Euler(0, 30, 90);
                for (int i = 0; i < 5; i++)
                    Instance(boulder, "SplitIslandRock", rocks, P(-2 + i, 81 + i % 2), 1.1f, i * 37);
                Instance(tree, "SplitIslandCanopy", vegetation, P(0, 84), 1.4f, 0);
                // Solid perimeter berms; entrance and exit remain open. No invisible walls/triggers.
                for (int side = -1; side <= 1; side += 2)
                    for (int z = -6; z < 117; z += 8)
                        Box("OuterBoundaryBerm", terrain, P(side * 31, z) + Vector3.up, new Vector3(3, 3, 8.1f), "Ground");
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("StartBoundary", terrain, P(side * 19, -9) + Vector3.up, new Vector3(25, 3, 3), "Ground");
                    Box("ExitBoundary", terrain, P(side * 19, 115) + Vector3.up, new Vector3(25, 3, 3), "Ground");
                }
                BuildExit(landmarks);
                Location("StartPoint", locations, 0, 0, "LocationTeal");
                Location("RouteSplit", locations, 0, 70, "LocationTeal");
                Location("GrabPoint", locations, -7, 20, "LocationAmber");
                Location("ActivatePoint", locations, 9, 56, "LocationAmber");
                Location("JungleExit", locations, 0, 108, "LocationTeal");
                SetupLighting(lighting);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save " + ScenePath);
                FocusScene(scene);
                Validate();
                Debug.Log("P2 Jungle prototype generated and saved: " + ScenePath + ". Environment only; no gameplay. Other scenes remain open and unchanged.");
            }
            catch
            {
                // Leave failed construction visible for diagnosis; never close/discard user scenes.
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                throw;
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }

        static void ColorMaterial(string name, Color color, Shader shader)
        {
            string path = Mats + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", .12f);
                AssetDatabase.CreateAsset(material, path);
            }
            Palette[name] = material;
        }

        static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Transform Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, string material, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Palette[material];
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.transform;
        }

        static Transform Box(string name, Transform parent, Vector3 position, Vector3 scale, string material)
            => Shape(name, parent, PrimitiveType.Cube, position, scale, material, true);

        // Gentle elevation change, shared by the ground, trails, and placement functions.
        static float Height(float z) => z < 24 ? 0 : z < 50 ? (z - 24) / 26 * 2 : z < 88 ? 2 : 2 + Mathf.Clamp01((z - 88) / 20) * 2;
        static Vector3 P(float x, float z) => new Vector3(x, Height(z), z);
        static float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

        static void BuildGround(Transform parent)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            // 2m quads, with a real 8 x 8m bridge ravine opening and a lowered visible floor.
            for (int z = -10; z < 118; z += 2)
            for (int x = -32; x < 32; x += 2)
            {
                if (x >= -4 && x < 4 && z >= 36 && z < 44) continue;
                int n = vertices.Count;
                vertices.Add(P(x, z)); vertices.Add(P(x, z + 2));
                vertices.Add(P(x + 2, z + 2)); vertices.Add(P(x + 2, z));
                triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            }
            MeshObject("JungleGround", parent, vertices, triangles, "Ground", true);
            Box("RavineFloor", parent, new Vector3(0, -2, 40), new Vector3(8, 1, 8), "Ravine");
            Box("RavineWestWall", parent, new Vector3(-4.3f, -.1f, 40), new Vector3(.6f, 3.6f, 8), "Rock");
            Box("RavineEastWall", parent, new Vector3(4.3f, -.1f, 40), new Vector3(.6f, 3.6f, 8), "Rock");
            Box("RavineSouthWall", parent, new Vector3(0, -.3f, 35.8f), new Vector3(8, 3.4f, .4f), "Rock");
            Box("RavineNorthWall", parent, new Vector3(0, .05f, 44.2f), new Vector3(8, 4.1f, .4f), "Rock");
        }

        static void MeshObject(string name, Transform parent, List<Vector3> vertices, List<int> triangles, string material, bool collider)
        {
            string path = Art + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = name };
                mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, path);
            }
            var go = Group(name, parent).gameObject;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Palette[material];
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void Trail(string name, Transform parent, Vector3[] points, float width)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            // Ribbon subdivided along each segment to follow ground elevation precisely.
            for (int i = 0; i < points.Length - 1; i++)
            {
                var direction = (points[i + 1] - points[i]); direction.y = 0;
                var side = Vector3.Cross(Vector3.up, direction.normalized) * width * .5f;
                int steps = Mathf.CeilToInt(direction.magnitude);
                for (int s = 0; s < steps; s++)
                {
                    var a = Vector3.Lerp(points[i], points[i + 1], (float)s / steps);
                    var b = Vector3.Lerp(points[i], points[i + 1], (float)(s + 1) / steps);
                    int n = vertices.Count;
                    foreach (var p in new[] { a - side, b - side, b + side, a + side })
                        vertices.Add(P(p.x, p.z) + Vector3.up * .035f);
                    indices.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
                }
            }
            MeshObject(name, parent, vertices, indices, "Trail", false);
            // Round junction caps remove visible cracks at bends, with no collision seams.
            for (int i = 0; i < points.Length; i++)
                Clearing(name + "_Junction" + i, parent, points[i].x, points[i].z, width * .51f, "Trail");
        }

        static void Clearing(string name, Transform parent, float x, float z, float radius, string material)
        {
            var vertices = new List<Vector3> { P(x, z) + Vector3.up * .045f };
            var indices = new List<int>();
            const int segments = 32;
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                vertices.Add(P(x + Mathf.Sin(a) * radius, z + Mathf.Cos(a) * radius) + Vector3.up * .045f);
                if (i > 0) indices.AddRange(new[] { 0, i, i + 1 });
            }
            MeshObject(name, parent, vertices, indices, material, false);
        }

        static GameObject Prefab(string name, Action<Transform> make)
        {
            string path = Prefabs + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = Group(name, null);
            try { make(root); return PrefabUtility.SaveAsPrefabAsset(root.gameObject, path); }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        static void Instance(GameObject prefab, string name, Transform parent, Vector3 position, float scale, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name; go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        static void MakeTree(Transform root)
        {
            Shape("Trunk", root, PrimitiveType.Cylinder, new Vector3(0, 3.5f, 0), new Vector3(.85f, 3.5f, .85f), "Bark", true);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * .5f;
                var buttress = Box("ButtressRoot", root, new Vector3(Mathf.Sin(angle) * .6f, .65f, Mathf.Cos(angle) * .6f), new Vector3(.4f, 1.5f, 1.7f), "Bark");
                buttress.localRotation = Quaternion.Euler(0, i * 90, 0);
                Shape("CanopyLobe", root, PrimitiveType.Sphere, new Vector3(Mathf.Sin(angle) * 1.5f, 7 + i % 2, Mathf.Cos(angle) * 1.5f), new Vector3(5, 2.3f, 4.5f), i % 2 == 0 ? "Canopy" : "LeafLight", false);
            }
            Shape("Crown", root, PrimitiveType.Sphere, new Vector3(0, 8.4f, 0), new Vector3(4.5f, 2.3f, 4.5f), "Canopy", false);
        }

        static void MakeFern(Transform root)
        {
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2 / 7;
                var leaf = Shape("Frond", root, PrimitiveType.Sphere, new Vector3(Mathf.Sin(angle) * .55f, .48f, Mathf.Cos(angle) * .55f), new Vector3(.28f, .15f, 1.8f), i % 2 == 0 ? "Fern" : "LeafLight", false);
                leaf.localRotation = Quaternion.Euler(-22, i * 360f / 7, 0);
            }
        }

        static void MakeBoulder(Transform root)
        {
            var rock = Shape("Stone", root, PrimitiveType.Sphere, new Vector3(0, .6f, 0), new Vector3(2.7f, 1.8f, 2.1f), "Rock", true);
            rock.localRotation = Quaternion.Euler(12, 20, 9);
            Shape("MossCap", root, PrimitiveType.Sphere, new Vector3(-.2f, 1.32f, 0), new Vector3(2, .35f, 1.7f), "Moss", false);
        }

        static void MakeColumn(Transform root)
        {
            Box("Footing", root, new Vector3(0, .2f, 0), new Vector3(1.8f, .4f, 1.8f), "Ruin");
            for (int i = 0; i < 4; i++)
            {
                var block = Box("WeatheredStoneCourse", root, new Vector3(0, .8f + i * .85f, 0), new Vector3(1.25f, .8f, 1.25f), "Ruin");
                block.localRotation = Quaternion.Euler(0, i % 2 == 0 ? -3 : 3, 0);
            }
            Box("Capital", root, new Vector3(0, 3.95f, 0), new Vector3(1.7f, .35f, 1.7f), "Moss");
        }

        static void MakeBridge(Transform root)
        {
            for (int i = -6; i <= 6; i++)
            {
                if (i >= -1 && i <= 1) continue;
                var plank = Box("BrokenDeckPlank", root, new Vector3(0, .18f, i * .7f), new Vector3(3.5f, .22f, .60f), "Wood");
                plank.localRotation = Quaternion.Euler(i == -2 ? 24 : 0, i % 3 * 3, 0);
            }
            foreach (float x in new[] { -1.9f, 1.9f })
            foreach (float z in new[] { -4.5f, -2.2f, 2.2f, 4.5f })
                Box("BridgePost", root, new Vector3(x, .9f, z), new Vector3(.24f, 1.8f, .24f), "Wood");
            foreach (float x in new[] { -1.9f, 1.9f })
            foreach (float z in new[] { -3.3f, 3.3f })
                Box("BrokenHandrail", root, new Vector3(x, 1.45f, z), new Vector3(.15f, .18f, 2.5f), "Wood");
        }

        static void BuildRuins(Transform parent, GameObject column)
        {
            var ruins = Group("AncientRuins", parent);
            foreach (float x in new[] { 4.5f, 12.5f })
            foreach (float z in new[] { 52f, 60f })
                Instance(column, "CourtyardColumn", ruins, P(x, z), 1, 0);
            Box("AncientLintel", ruins, P(8.5f, 60) + Vector3.up * 4.3f, new Vector3(9.8f, .65f, 1.7f), "Ruin");
            Box("MossOnLintel", ruins, P(8.5f, 60) + Vector3.up * 4.68f, new Vector3(9.4f, .12f, 1.7f), "Moss");
            Box("LowEastWall", ruins, P(14, 56) + Vector3.up * .6f, new Vector3(.9f, 1.2f, 8), "Ruin");
            for (int i = 0; i < 5; i++)
                Box("CourtyardPaving", ruins, P(8.5f, 52 + i * 1.5f) + Vector3.up * .08f, new Vector3(2.1f, .10f, 1.2f), "Ruin");
            Box("EmptyStoneDais", ruins, P(11, 58) + Vector3.up * .35f, new Vector3(1.8f, .7f, 1.8f), "Ruin");
        }

        static void BuildExit(Transform parent)
        {
            var exit = Group("JungleExit_ThresholdOnly", parent);
            foreach (float x in new[] { -4f, 4f })
            {
                Box("ThresholdPillar", exit, P(x, 108) + Vector3.up * 2.8f, new Vector3(1.8f, 5.6f, 2), "Ruin");
                Box("MossCrown", exit, P(x, 108) + Vector3.up * 5.7f, new Vector3(2.2f, .3f, 2.4f), "Moss");
            }
            Box("ExitLintel", exit, P(0, 108) + Vector3.up * 6, new Vector3(10.2f, .7f, 2.3f), "Ruin");
        }

        static bool Reserved(float x, float z, Vector3[][] trails)
        {
            var point = new Vector2(x, z);
            if (Vector2.Distance(point, new Vector2(0, 2)) < 10 || Vector2.Distance(point, new Vector2(-7, 20)) < 8 || Vector2.Distance(point, new Vector2(8, 56)) < 10) return true;
            if (Mathf.Abs(x) < 7 && z > 30 && z < 49) return true;
            if (Mathf.Abs(x) < 7 && z > 103) return true;
            foreach (var trail in trails)
            for (int i = 0; i < trail.Length - 1; i++)
            {
                var a = new Vector2(trail[i].x, trail[i].z);
                var b = new Vector2(trail[i + 1].x, trail[i + 1].z);
                float t = Mathf.Clamp01(Vector2.Dot(point - a, b - a) / (b - a).sqrMagnitude);
                if (Vector2.Distance(point, Vector2.Lerp(a, b, t)) < 6) return true;
            }
            return false;
        }

        static void Location(string name, Transform parent, float x, float z, string material)
        {
            var location = Group(name, parent);
            location.localPosition = P(x, z) + Vector3.up * .15f;
            Shape("LocationMarker_VisualOnly_NoTrigger", location, PrimitiveType.Cylinder, new Vector3(0, -.08f, 0), new Vector3(1.5f, .025f, 1.5f), material, false);
        }

        static void SetupLighting(Transform parent)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.61f, .73f, .70f);
            RenderSettings.ambientEquatorColor = new Color(.35f, .43f, .31f);
            RenderSettings.ambientGroundColor = new Color(.19f, .23f, .14f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.57f, .68f, .62f);
            RenderSettings.fogStartDistance = 110;
            RenderSettings.fogEndDistance = 240;
            RenderSettings.skybox = null;
            var sun = Group("JungleSun", parent).gameObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1, .89f, .69f);
            sun.intensity = 1.65f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            RenderSettings.sun = sun;
            var camera = Group("PrototypeOverviewCamera_NoController", parent).gameObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(66, 91, -27);
            camera.transform.LookAt(new Vector3(0, 1.5f, 51));
            camera.orthographic = true; camera.orthographicSize = 68;
            camera.nearClipPlane = .3f; camera.farClipPlane = 350;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            // Deliberately no AudioListener or MainCamera tag: Person 1 owns the future player camera.
        }

        static void FocusScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Jungle_Prototype") Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0, 2, 50), Quaternion.Euler(48, -30, 0), 85);
        }

        // Batch authoring entry point; the image is documentation, not a runtime asset.
        public static void BuildAndCapture()
        {
            // A fresh batch Editor starts with an unnamed default scene. Use a saved
            // source scene as the additive host; never save changes to that source.
            if (Application.isBatchMode)
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Build();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            Validate();
            if (Application.isBatchMode)
            {
                var host = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
                if (host.isLoaded) EditorSceneManager.CloseScene(host, true);
            }
            Camera camera = null;
            foreach (var root in scene.GetRootGameObjects()) camera = root.GetComponentInChildren<Camera>();
            if (camera == null) throw new InvalidOperationException("Overview camera missing.");
            var target = new RenderTexture(1600, 1200, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var texture = new Texture2D(1600, 1200, TextureFormat.RGB24, false);
            var previousPipeline = QualitySettings.renderPipeline;
            var sourcePipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (sourcePipeline == null) throw new InvalidOperationException("URP must be active for the overview capture.");
            var previewPipeline = Object.Instantiate(sourcePipeline);
            try
            {
                // A one-off capture has no normal frame warmup. Temporarily bypass GPU
                // instance registration on an unsaved clone; restore before returning.
                previewPipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
                previewPipeline.useSRPBatcher = false;
                QualitySettings.renderPipeline = previewPipeline;
                var request = new RenderPipeline.StandardRequest { destination = target, isPreview = true };
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1600, 1200), 0, 0);
                texture.Apply();
                Directory.CreateDirectory("Documentation/P2");
                File.WriteAllBytes("Documentation/P2/Jungle_Overview.png", texture.EncodeToPNG());
                Debug.Log("P2 overview captured: Documentation/P2/Jungle_Overview.png");
            }
            finally
            {
                QualitySettings.renderPipeline = previousPipeline;
                Object.DestroyImmediate(previewPipeline);
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }

        [MenuItem("Tools/Lost Worlds/Validate Jungle Prototype")]
        public static void Validate()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) throw new InvalidOperationException("Open " + ScenePath + " before validation.");
            var roots = scene.GetRootGameObjects();
            if (roots.Length != 1 || roots[0].name != "Jungle_Prototype") throw new InvalidOperationException("Expected a single Jungle_Prototype root.");
            var root = roots[0].transform;
            if(root.Find("Environment/JunglePath/ContinuousMainRoad")!=null){JungleRoadRefinement.Validate();return;}
            foreach (string path in new[] { "Environment/Terrain_Ground", "Environment/JunglePath", "Environment/Vegetation", "Environment/Rocks", "Environment/Obstacles", "Environment/Landmarks/BrokenBridge", "Environment/Landmarks/AncientRuins", "FutureGameplayPoints/StartPoint", "FutureGameplayPoints/RouteSplit", "FutureGameplayPoints/GrabPoint", "FutureGameplayPoints/ActivatePoint", "FutureGameplayPoints/JungleExit", "Lighting" })
                if (root.Find(path) == null) throw new InvalidOperationException("Missing hierarchy location: " + path);
            int colliders = 0;
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) throw new InvalidOperationException("Missing script detected.");
                if (component is MonoBehaviour || component is Rigidbody || component is CharacterController || component is Animator)
                    throw new InvalidOperationException("Unexpected functional component in environment-only prototype: " + component.GetType().Name);
                if (component is Collider c) { colliders++; if (c.isTrigger) throw new InvalidOperationException("P2 must not contain triggers."); }
                if (component is Renderer renderer)
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || material.shader.name != "Universal Render Pipeline/Lit")
                            throw new InvalidOperationException("Missing or unexpected prototype material on " + renderer.name);
            }
            if (colliders == 0) throw new InvalidOperationException("Environment collision is missing.");
            var ground = root.Find("Environment/Terrain_Ground/JungleGround").GetComponent<MeshCollider>();
            var routes = new[]
            {
                new[] { P(0, 0), P(-2, 12), P(3, 24), P(0, 32), P(-10, 35), P(-10, 45), P(0, 50), P(0, 62), P(0, 70) },
                new[] { P(0, 70), P(-10, 79), P(-8, 88), P(0, 96), P(0, 110) },
                new[] { P(0, 70), P(10, 79), P(8, 88), P(0, 96) },
                new[] { P(2, 20), P(-7, 20) }, new[] { P(0, 55), P(9, 55) }
            };
            Physics.SyncTransforms();
            int groundSamples = 0;
            foreach (var route in routes)
            for (int i = 0; i < route.Length - 1; i++)
            {
                var delta = route[i + 1] - route[i]; delta.y = 0;
                var side = Vector3.Cross(Vector3.up, delta.normalized);
                int steps = Mathf.CeilToInt(delta.magnitude);
                for (int s = 0; s <= steps; s++)
                for (int edge = -1; edge <= 1; edge++)
                {
                    var p = Vector3.Lerp(route[i], route[i + 1], (float)s / steps) + side * edge;
                    var ray = new Ray(new Vector3(p.x, 20, p.z), Vector3.down);
                    if (!ground.Raycast(ray, out var hit, 30) || Mathf.Abs(hit.point.y - Height(p.z)) > .05f)
                        throw new InvalidOperationException("Missing route ground at " + p);
                    groundSamples++;
                }
            }
            Debug.Log("P2 route grounding PASS: " + groundSamples + " samples over a 2m-wide corridor along the main path, bypass, both branches, glade and ruin approaches.");
            Debug.Log("P2 validation PASS: organized hierarchy, URP materials, " + colliders + " solid colliders; no runtime scripts, triggers, rigidbodies, controllers or animators.");
        }
    }
}
