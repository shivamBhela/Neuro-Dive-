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
    // P3 authoring only. Nothing in this file runs in a player build.
    public static class WorldExpansionBuilder
    {
        const string Materials = "Assets/Art/Materials/WorldExpansion";
        const string Prefabs = "Assets/Prefabs/World/WorldExpansion";
        const string Meshes = "Assets/Art/Environments/CrystalCave/Prototype";
        static readonly string[] Names = { "Ruins", "CrystalCave", "HiddenCity" };
        static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        static string ScenePath(string name) => "Assets/Scenes/Person2/" + name + "_Prototype.unity";
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static readonly Vector3[] RuinsRoute = { V(0,0,0), V(0,0,18), V(3,0,35), V(0,0,52), V(0,0,70), V(0,0,88) };
        static readonly Vector3[] CaveRoute = { V(0,0,0), V(0,0,15), V(-3,0,30), V(0,0,46), V(3,0,62), V(0,0,78), V(0,0,88) };
        static readonly Vector3[] CityLeft = { V(0,0,0), V(0,0,32), V(-10,0,43), V(-10,0,57), V(0,0,70), V(0,0,88) };
        static readonly Vector3[] CityRight = { V(0,0,32), V(10,0,43), V(10,0,57), V(0,0,70) };

        [MenuItem("Tools/Lost Worlds/P3/Build All World Prototypes")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play mode before authoring P3 scenes."); return; }
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                { Debug.LogWarning("Save the unnamed open scene under its own name first. No content was changed."); return; }
            // Construct prefab templates in our own scratch scene, never in the user's
            // active scene (even temporary object creation would mark it dirty).
            var original = SceneManager.GetActiveScene();
            var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scratch);
            try { PrepareAssets(); }
            finally
            {
                EditorSceneManager.CloseScene(scratch, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
            foreach (var name in Names)
            {
                if (File.Exists(ScenePath(name))) { Debug.Log("Preserved existing P3 scene: " + ScenePath(name)); continue; }
                var previous = SceneManager.GetActiveScene();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                try
                {
                    var root = Group(name + "_Prototype", null);
                    var env = Group("Environment", root);
                    var landmarks = Group("Landmarks", root);
                    var points = Group("FutureGameplayPoints", root);
                    var lights = Group("Lighting", root);
                    if (name == "Ruins") Ruins(env, landmarks, points, lights);
                    else if (name == "CrystalCave") Cave(env, landmarks, points, lights);
                    else City(env, landmarks, points, lights);
                    AssetDatabase.SaveAssets();
                    ValidateScene(scene, name);
                    if (!EditorSceneManager.SaveScene(scene, ScenePath(name))) throw new IOException("Scene save failed: " + name);
                    Debug.Log("P3 GENERATED AND SAVED: " + ScenePath(name));
                    // Close only our newly saved scene so cameras/lights never accumulate.
                    EditorSceneManager.CloseScene(scene, true);
                }
                finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
            }
            Debug.Log("P3 authoring complete. Open each new scene from Assets/Scenes/Person2. Existing scenes were not rebuilt.");
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\','/');
            Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void PrepareAssets()
        {
            Folder(Materials); Folder(Prefabs); Folder(Meshes); Folder("Assets/Scenes/Person2");
            palette.Clear();
            Mat("Sandstone", new Color(.61f,.53f,.35f)); Mat("Limestone", new Color(.80f,.75f,.58f));
            Mat("RuinsGround", new Color(.28f,.35f,.24f)); Mat("Moss", new Color(.30f,.43f,.22f));
            Mat("Water", new Color(.19f,.52f,.57f)); Mat("Waterfall", new Color(.58f,.85f,.84f), .12f);
            Mat("Foam", new Color(.79f,.91f,.86f)); Mat("Slate", new Color(.20f,.25f,.34f));
            Mat("CaveRock", new Color(.29f,.34f,.43f)); Mat("CaveTrail", new Color(.52f,.57f,.65f));
            Mat("CrystalTeal", new Color(.15f,.70f,.74f), .45f); Mat("CrystalViolet", new Color(.54f,.40f,.78f), .35f);
            Mat("Ivory", new Color(.84f,.79f,.65f)); Mat("CityGround", new Color(.57f,.59f,.51f));
            Mat("CityPaving", new Color(.76f,.76f,.64f)); Mat("Patina", new Color(.15f,.43f,.42f));
            Mat("Gold", new Color(.68f,.48f,.22f)); Mat("WindowGlow", new Color(.79f,.77f,.48f), .18f);
            Mat("Location", new Color(.32f,.69f,.61f));
            MakePrefab("SandstoneColumn", t => Column(t, "Sandstone", "Moss"));
            MakePrefab("IvoryColumn", t => Column(t, "Ivory", "Gold"));
            MakePrefab("CaveFormation", t =>
            {
                Shape("RockMass", t, PrimitiveType.Sphere, V(0,3,0), V(5.5f,7,5), "CaveRock", true);
                Shape("UpperFacet", t, PrimitiveType.Cube, V(.4f,6,0), V(3,3,3), "Slate", true).localRotation = Quaternion.Euler(10,25,16);
            });
            MakePrefab("CrystalCluster", t =>
            {
                Shape("RockFooting",t,PrimitiveType.Sphere,V(0,.3f,0),V(3,.9f,2.6f),"Slate",true);
                Crystal(t,V(0,.35f,0),V(1.1f,1.5f,1.1f),"CrystalTeal",0);
                Crystal(t,V(1,.3f,.3f),V(.65f,.85f,.65f),"CrystalViolet",-15);
                Crystal(t,V(-.8f,.3f,-.2f),V(.65f,1,.65f),"CrystalTeal",18);
            });
            MakePrefab("CityPavilion", t =>
            {
                Box("Building",t,V(0,3,0),V(7,6,6),"Ivory");
                Box("PatinaCornice",t,V(0,6.2f,0),V(8,.5f,7),"Patina");
                Box("UpperStorey",t,V(0,7.8f,0),V(5.4f,2.8f,4.7f),"Ivory");
                Box("RoofTier",t,V(0,9.4f,0),V(6.5f,.5f,5.8f),"Patina");
                Shape("RoofCrown",t,PrimitiveType.Cylinder,V(0,10.1f,0),V(2,.5f,2),"Gold",false);
                foreach(float x in new[]{-2f,0f,2f})
                {
                    Shape("WindowInset",t,PrimitiveType.Cube,V(x,3,-3.02f),V(.9f,2.5f,.06f),"Patina",false);
                    Shape("WindowLight",t,PrimitiveType.Cube,V(x,3,-3.06f),V(.34f,1.8f,.04f),"WindowGlow",false);
                }
            });
            MakePrefab("LowBalustrade", t =>
            {
                Box("Rail",t,V(0,1.05f,0),V(5,.25f,.45f),"Limestone");
                foreach(float x in new[]{-2.25f,0,2.25f}) Box("Support",t,V(x,.45f,0),V(.4f,.9f,.4f),"Sandstone");
            });
        }

        static void Mat(string name, Color color, float emission = 0)
        {
            string path = Materials + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("Existing URP/Lit shader unavailable.");
                m = new Material(shader) { name = name };
                m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", .2f);
                if (emission > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",color * emission); }
                AssetDatabase.CreateAsset(m,path);
            }
            palette[name] = m;
        }
        static Transform Group(string name, Transform parent)
        { var t = new GameObject(name).transform; if(parent != null)t.SetParent(parent,false); return t; }
        static Transform Shape(string name, Transform parent, PrimitiveType type, Vector3 p, Vector3 s, string material, bool solid)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent,false);
            g.transform.localPosition = p; g.transform.localScale = s; g.GetComponent<Renderer>().sharedMaterial = palette[material];
            if(!solid) Object.DestroyImmediate(g.GetComponent<Collider>()); return g.transform;
        }
        static Transform Box(string n,Transform t,Vector3 p,Vector3 s,string m) => Shape(n,t,PrimitiveType.Cube,p,s,m,true);
        static void Disc(string n,Transform t,float x,float z,float radius,string m,float height=.04f)
            => Shape(n,t,PrimitiveType.Cylinder,V(x,height,z),V(radius*2,.02f,radius*2),m,false);
        static void Ground(Transform env,string material,float width = 64)
        { var t=Group("Terrain_Ground",env); Box("ContinuousGround",t,V(0,-.5f,44),V(width,1,104),material); }
        static void Trail(Transform parent,string name,Vector3[] points,string material,float width = 8)
        {
            var t=Group(name,parent);
            for(int i=0;i<points.Length-1;i++)
            {
                var d=points[i+1]-points[i];
                var segment=Shape("LevelPath",t,PrimitiveType.Cube,(points[i]+points[i+1])*.5f+Vector3.up*.035f,V(width,.035f,d.magnitude),material,false);
                segment.localRotation=Quaternion.LookRotation(d);
            }
            foreach(var p in points)Disc("RoundedJunction",t,p.x,p.z,width*.5f,material);
        }
        static void Column(Transform t,string stone,string trim)
        {
            Box("Footing",t,V(0,.2f,0),V(2,.4f,2),stone);
            Shape("Shaft",t,PrimitiveType.Cylinder,V(0,2.7f,0),V(1.25f,2.3f,1.25f),stone,true);
            Box("Capital",t,V(0,5.1f,0),V(2,.45f,2),trim);
        }
        static void MakePrefab(string name,Action<Transform> make)
        {
            string path=Prefabs+"/"+name+".prefab"; if(File.Exists(path))return;
            var t=Group(name,null); try{make(t); PrefabUtility.SaveAsPrefabAsset(t.gameObject,path);}finally{Object.DestroyImmediate(t.gameObject);}
        }
        static Transform Instance(string name,Transform parent,Vector3 p,float scale=1,float yaw=0,bool jungle=false)
        {
            string path=(jungle?"Assets/Prefabs/World/JunglePrototype":Prefabs)+"/"+name+".prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(prefab==null)throw new InvalidOperationException("Missing reusable prefab: "+path);
            var t=((GameObject)PrefabUtility.InstantiatePrefab(prefab,parent)).transform;
            t.localPosition=p; t.localScale=Vector3.one*scale; t.localRotation=Quaternion.Euler(0,yaw,0); return t;
        }
        static void Crystal(Transform parent,Vector3 p,Vector3 scale,string material,float tilt)
        {
            string path=Meshes+"/FacetedCrystal.asset"; var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var vertices=new List<Vector3>(); var triangles=new List<int>();
                Action<Vector3,Vector3,Vector3> face=(a,b,c)=>{int n=vertices.Count;vertices.AddRange(new[]{a,b,c});triangles.AddRange(new[]{n,n+1,n+2});};
                for(int i=0;i<6;i++)
                {
                    float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;
                    var a0=V(Mathf.Cos(a)*.7f,0,Mathf.Sin(a)*.7f); var b0=V(Mathf.Cos(b)*.7f,0,Mathf.Sin(b)*.7f);
                    var a1=a0+Vector3.up*2.4f; var b1=b0+Vector3.up*2.4f;
                    face(a0,a1,b1); face(a0,b1,b0); face(a1,V(0,3.4f,0),b1); face(a0,b0,Vector3.zero);
                }
                mesh=new Mesh{name="FacetedCrystal"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            var t=Group("FacetedCrystal_VisualOnly",parent);t.localPosition=p;t.localScale=scale;t.localRotation=Quaternion.Euler(0,25,tilt);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=palette[material];
        }
        static void Anchor(Transform parent,string name,float x,float z)
        {
            var t=Group(name,parent);t.localPosition=V(x,.15f,z);
            Shape("LocationOnly_NoTrigger",t,PrimitiveType.Cylinder,V(0,-.07f,0),V(1.2f,.02f,1.2f),"Location",false);
        }
        static void Gate(Transform parent,string name,float z,string stone,string trim,float width=14)
        {
            var t=Group(name,parent);
            foreach(float x in new[]{-width*.5f,width*.5f}) {Box("Pier",t,V(x,3.5f,z),V(2,7,2.5f),stone);Box("Capital",t,V(x,7,z),V(2.8f,.5f,3),trim);}
            Box("Lintel",t,V(0,7.6f,z),V(width+3,.8f,3),stone);
        }

        static void Ruins(Transform env,Transform landmarks,Transform points,Transform lights)
        {
            Ground(env,"RuinsGround");var path=Group("StonePath_8mWide",env);Trail(path,"MainPromenade",RuinsRoute,"Limestone");
            Disc("ArrivalCourt",path,0,8,9,"Sandstone",.065f);Disc("OpenCourtyard",path,0,52,12,"Sandstone",.065f);
            Trail(path,"WaterfallViewingApproach",new[]{V(2,0,30),V(-9,0,30)},"Limestone",6);
            Disc("ViewingCourt",path,-9,30,5,"Limestone");
            var ruins=Group("AncientStructures",landmarks);
            foreach(float x in new[]{-9f,9f})for(int z=12;z<=76;z+=16)
                if(!(x<0&&z==28))Instance("SandstoneColumn",ruins,V(x,0,z),1,z);
            Box("FragmentedEastWallA",ruins,V(16,2.5f,26),V(1.6f,5,14),"Sandstone");
            Box("FragmentedEastWallB",ruins,V(16,1.2f,45),V(1.6f,2.4f,12),"Sandstone");
            Box("RuinedLintel",ruins,V(14,6,67),V(10,1,2.5f),"Sandstone");
            foreach(float x in new[]{10f,18f})Instance("SandstoneColumn",ruins,V(x,0,67),1.1f);
            var water=Group("Waterfall_StaticScenicFeature",landmarks);
            Box("RockCliff",water,V(-25,7,43),V(8,14,22),"Slate");
            Box("UpperMossShelf",water,V(-23,14,43),V(10,1,22),"Moss");
            Shape("StillPool_VisualOnly",water,PrimitiveType.Cylinder,V(-18,.12f,43),V(17,.1f,21),"Water",false);
            for(int i=0;i<5;i++)Shape("FallingWater_StaticRibbon",water,PrimitiveType.Cube,V(-20.6f,7,38+i*2.4f),V(.16f,13,1.8f),i%2==0?"Waterfall":"Water",false);
            for(int i=0;i<7;i++)Shape("FoamAtBase",water,PrimitiveType.Sphere,V(-20,.4f,37+i*2),V(2.3f,.35f,2.6f),"Foam",false);
            // Continuous, visible low pool border: no stepping stones or precision crossings.
            Box("PoolSafetyBorder",water,V(-9.2f,.6f,44),V(.7f,1.2f,20),"Sandstone");
            for(int i=0;i<4;i++)Instance("LowBalustrade",water,V(-11.5f-i*4,.0f,33),.8f);
            var vegetation=Group("RocksAndVegetation",env);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<13;i++)
            {
                float z=-2+i*7.5f;if(side<0&&z>29&&z<58)continue;
                Instance("CanopyTree",vegetation,V(side*26,0,z),.8f+(i%3)*.1f,i*31,true);
                Instance("MossyBoulder",vegetation,V(side*21,0,z+2),.9f,i*23,true);
                Instance("FernCluster",vegetation,V(side*13,0,z),1.1f,i*41,true);
            }
            Gate(landmarks,"JungleArrival",0,"Sandstone","Moss");Gate(landmarks,"CaveThreshold",88,"Slate","Moss");
            Anchor(points,"RuinsEntrance",0,0);Anchor(points,"RuinsExit",0,88);Anchor(points,"GrabPoint_LocationOnly",-8,29);Anchor(points,"ActivatePoint_LocationOnly",5,53);Anchor(points,"RouteChoice_LocationOnly",0,62);
            Lighting(lights,new Color(.66f,.75f,.72f),new Color(1,.91f,.74f),1.5f,false);
        }

        static void Cave(Transform env,Transform landmarks,Transform points,Transform lights)
        {
            Ground(env,"Slate",58);var paths=Group("CavePath_8mWide",env);Trail(paths,"ContinuousCavePath",CaveRoute,"CaveTrail");
            Disc("ArrivalChamber",paths,0,12,10,"CaveTrail");Disc("CrystalChamber",paths,0,46,12,"CaveTrail");Disc("ExitChamber",paths,0,77,10,"CaveTrail");
            var walls=Group("CaveWalls_OpenRoofCutaway",env);var crystals=Group("CrystalGalleries",landmarks);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<16;i++)
            {
                float z=-3+i*6;float x=side*(18+Mathf.Sin(i*.7f)*3);
                Instance("CaveFormation",walls,V(x,0,z),1+(i%3)*.15f,i*39);
                if(i%2==0)Instance("CrystalCluster",crystals,V(side*(10.5f+i%3),0,z+1),.8f+(i%3)*.22f,i*47);
            }
            // Sparse overhead ribs provide enclosure while keeping an overview readable.
            foreach(float z in new[]{3f,26f,66f,88f})
            {
                var rib=Shape("HighRockRib",walls,PrimitiveType.Sphere,V(0,12,z),V(34,3,3),"CaveRock",false);
                // A stretched SphereCollider expands to its largest axis; use a box
                // matching this overhead rib so it cannot obstruct the path below.
                rib.gameObject.AddComponent<BoxCollider>();
                foreach(float side in new[]{-1f,1f})
                    Shape("RibShoulder",walls,PrimitiveType.Sphere,V(side*17,9,z),V(6,7,5),"CaveRock",true);
            }
            var heart=Group("CrystalSanctuary",landmarks);
            Instance("CrystalCluster",heart,V(-10,0,48),1.7f,20);Instance("CrystalCluster",heart,V(11,0,43),1.5f,120);
            Gate(landmarks,"RuinsArrivalPortal",0,"Sandstone","Moss");Gate(landmarks,"CityLightPortal",88,"Ivory","Gold");
            Anchor(points,"CrystalCaveEntrance",0,0);Anchor(points,"CrystalCaveExit",0,88);Anchor(points,"GrabPoint_LocationOnly",-5,44);Anchor(points,"ActivatePoint_LocationOnly",5,48);Anchor(points,"RouteChoice_LocationOnly",0,61);
            Lighting(lights,new Color(.30f,.39f,.49f),new Color(.76f,.88f,1),1.2f,true);
            for(int i=0;i<6;i++)
            {
                var light=Group("SteadyCrystalFill",lights).gameObject.AddComponent<Light>();light.type=LightType.Point;
                light.transform.position=V(i%2==0?-8:8,4,10+i*14);light.color=i%2==0?new Color(.36f,.82f,.86f):new Color(.67f,.57f,.93f);
                light.intensity=2.1f;light.range=18;light.shadows=LightShadows.None;
            }
        }

        static void City(Transform env,Transform landmarks,Transform points,Transform lights)
        {
            Ground(env,"CityGround",72);var paths=Group("CityPromenade",env);
            Trail(paths,"WestPromenade",CityLeft,"CityPaving",9);Trail(paths,"EastPromenade",CityRight,"CityPaving",9);
            Disc("ArrivalSquare",paths,0,12,11,"CityPaving");Disc("CentralPlaza",paths,0,50,18,"CityPaving");Disc("DestinationCourt",paths,0,85,13,"CityPaving");
            var buildings=Group("PavilionsAndColonnades",env);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++)
            {
                Instance("CityPavilion",buildings,V(side*25,0,8+i*18),.9f+(i%2)*.2f,side>0?90:-90);
                Instance("IvoryColumn",buildings,V(side*16,0,10+i*18),1.2f);
            }
            var plaza=Group("CentralPlazaLandmark",landmarks);
            Shape("LandmarkBase",plaza,PrimitiveType.Cylinder,V(0,.4f,50),V(7,.4f,7),"Patina",true);
            Box("IvoryMonument",plaza,V(0,4.8f,50),V(2.4f,8,2.4f),"Ivory");
            foreach(float y in new[]{1.4f,7.8f,9f})Box("GoldBand",plaza,V(0,y,50),V(2.9f,.3f,2.9f),"Gold");
            Shape("MonumentCrown",plaza,PrimitiveType.Sphere,V(0,10,50),V(2,2,2),"WindowGlow",false);
            var destination=Group("FinalDestination_Sanctuary",landmarks);
            Gate(destination,"SanctuaryColonnade",89,"Ivory","Gold",20);
            Box("SanctuaryBackWall",destination,V(0,5,95),V(28,10,2),"Ivory");
            Box("PatinaFrieze",destination,V(0,10,95),V(30,.7f,3),"Patina");
            Shape("SanctuaryEmblem",destination,PrimitiveType.Cylinder,V(0,6,93.9f),V(3,.15f,3),"Gold",false).localRotation=Quaternion.Euler(90,0,0);
            foreach(float x in new[]{-11f,11f})Instance("CityPavilion",destination,V(x,0,91),.8f);
            var gardens=Group("QuietCourtyardGardens",env);
            for(int side=-1;side<=1;side+=2)foreach(float z in new[]{25f,69f})
            {
                Box("RaisedPlanter",gardens,V(side*20,.5f,z),V(5,1,7),"Patina");
                Instance("FernCluster",gardens,V(side*20,1,z),2,0,true);
                Instance("CanopyTree",gardens,V(side*21,1,z+1),.65f,0,true);
            }
            Gate(landmarks,"CaveArrivalReveal",0,"Slate","Gold",16);
            Anchor(points,"HiddenCityEntrance",0,0);Anchor(points,"FinalDestination_LocationOnly",0,86);Anchor(points,"GrabPoint_LocationOnly",-7,51);Anchor(points,"ActivatePoint_LocationOnly",7,51);Anchor(points,"RouteChoice_LocationOnly",0,31);
            Lighting(lights,new Color(.68f,.76f,.76f),new Color(1,.92f,.76f),1.5f,false);
        }

        static void Lighting(Transform parent,Color background,Color sunColor,float intensity,bool cave)
        {
            RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=cave?new Color(.60f,.68f,.80f):new Color(.68f,.74f,.77f);
            RenderSettings.ambientEquatorColor=cave?new Color(.40f,.47f,.60f):new Color(.45f,.49f,.43f);
            RenderSettings.ambientGroundColor=new Color(.26f,.29f,.31f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=background;
            RenderSettings.fogStartDistance=150;RenderSettings.fogEndDistance=320;
            var sun=Group("SteadyEnvironmentLight",parent).gameObject.AddComponent<Light>();sun.type=LightType.Directional;
            sun.color=sunColor;sun.intensity=intensity;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(52,-28,0);RenderSettings.sun=sun;
            var camera=Group("PrototypeOverviewCamera_NoController",parent).gameObject.AddComponent<Camera>();
            camera.transform.position=V(63,85,-28);camera.transform.LookAt(V(0,2,44));camera.orthographic=true;camera.orthographicSize=61;
            camera.nearClipPlane=.3f;camera.farClipPlane=350;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=background;
        }

        [MenuItem("Tools/Lost Worlds/P3/Validate Open World Prototype")]
        public static void ValidateOpen()
        {
            var scene=SceneManager.GetActiveScene();foreach(var name in Names)if(scene.path==ScenePath(name)){ValidateScene(scene,name);return;}
            Debug.LogWarning("Open and activate one of the three P3 scenes first.");
        }

        [MenuItem("Tools/Lost Worlds/P3/Open Ruins Prototype")]
        public static void OpenRuins() => Open("Ruins");
        [MenuItem("Tools/Lost Worlds/P3/Open Crystal Cave Prototype")]
        public static void OpenCave() => Open("CrystalCave");
        [MenuItem("Tools/Lost Worlds/P3/Open Hidden City Prototype")]
        public static void OpenCity() => Open("HiddenCity");
        static void Open(string name)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play mode before switching prototype scenes."); return; }
            if (!File.Exists(ScenePath(name))) { Debug.LogWarning("Run Tools > Lost Worlds > P3 > Build All World Prototypes first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath(name), OpenSceneMode.Single);
            Selection.activeGameObject = scene.GetRootGameObjects()[0];
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(V(0,2,44),Quaternion.Euler(48,-30,0),75);
        }

        static void ValidateScene(Scene scene,string name)
        {
            var roots=scene.GetRootGameObjects();if(roots.Length!=1||roots[0].name!=name+"_Prototype")throw new InvalidOperationException("Unexpected root: "+name);
            var root=roots[0].transform;
            foreach(var path in new[]{"Environment/Terrain_Ground/ContinuousGround","Landmarks","FutureGameplayPoints","Lighting"})
                if(root.Find(path)==null)throw new InvalidOperationException("Missing hierarchy: "+path);
            string entrance=name=="Ruins"?"RuinsEntrance":name=="CrystalCave"?"CrystalCaveEntrance":"HiddenCityEntrance";
            string exit=name=="Ruins"?"RuinsExit":name=="CrystalCave"?"CrystalCaveExit":"FinalDestination_LocationOnly";
            foreach(string anchor in new[]{entrance,exit,"GrabPoint_LocationOnly","ActivatePoint_LocationOnly","RouteChoice_LocationOnly"})
            {
                var t=root.Find("FutureGameplayPoints/"+anchor);if(t==null||t.GetComponents<Component>().Length!=1)throw new InvalidOperationException("Missing or functional anchor: "+anchor);
            }
            int solids=0;
            foreach(var c in root.GetComponentsInChildren<Component>(true))
            {
                if(c==null||c is MonoBehaviour||c is Rigidbody||c is CharacterController||c is Animator||c is AudioSource)
                    throw new InvalidOperationException("Unexpected runtime/missing component in "+name);
                if(c is Collider collider){solids++;if(collider.isTrigger)throw new InvalidOperationException("Trigger found.");}
                if(c is Renderer renderer)foreach(var m in renderer.sharedMaterials)
                    if(m==null||m.shader==null||m.shader.name!="Universal Render Pipeline/Lit")throw new InvalidOperationException("Invalid material on "+renderer.name);
            }
            var ground=root.Find("Environment/Terrain_Ground/ContinuousGround").GetComponent<Collider>();
            var routes=name=="Ruins"?new[]{RuinsRoute,new[]{V(2,0,30),V(-9,0,30)}}:name=="CrystalCave"?new[]{CaveRoute}:new[]{CityLeft,CityRight};
            Physics.SyncTransforms();int samples=0;
            foreach(var route in routes)for(int i=0;i<route.Length-1;i++)
            {
                var delta=route[i+1]-route[i];var side=Vector3.Cross(Vector3.up,delta.normalized);int steps=Mathf.CeilToInt(delta.magnitude);
                for(int s=0;s<=steps;s++)for(int edge=-1;edge<=1;edge++)
                {
                    var p=Vector3.Lerp(route[i],route[i+1],(float)s/steps)+side*edge*2;
                    if(!ground.Raycast(new Ray(p+Vector3.up*20,Vector3.down),out var hit,25)||Mathf.Abs(hit.point.y)>.01f)throw new InvalidOperationException("Missing level ground at "+p);
                    foreach(var obstruction in Physics.OverlapBox(p+Vector3.up*1.15f,V(.35f,.95f,.35f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                        if(obstruction!=ground&&obstruction.transform.IsChildOf(root))throw new InvalidOperationException("Route obstruction: "+obstruction.name+" at "+p);
                    samples++;
                }
            }
            Debug.Log("P3 VALIDATION PASS "+name+": "+solids+" solid colliders; "+samples+" level-ground/clearance samples across 4m corridors; named anchors; no runtime behaviours or triggers.");
        }

        // Explicit batch entry point used on an isolated source copy, never auto-run on import.
        public static void BuildAndCapture()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use Build All World Prototypes in the interactive Editor.");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Single);BuildAll();
            foreach(var name in Names)
            {
                var scene=EditorSceneManager.OpenScene(ScenePath(name),OpenSceneMode.Single);ValidateScene(scene,name);Capture(scene,name);
            }
        }
        static void Capture(Scene scene,string name)
        {
            var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(source==null)throw new InvalidOperationException("URP required for preview.");
            var clone=Object.Instantiate(source);var previousPipeline=QualitySettings.renderPipeline;var previousTarget=RenderTexture.active;
            var target=new RenderTexture(1600,1200,24);var image=new Texture2D(1600,1200,TextureFormat.RGB24,false);
            try
            {
                // Same isolated first-frame capture workaround verified in P2. Never saved.
                clone.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;clone.useSRPBatcher=false;QualitySettings.renderPipeline=clone;
                var camera=scene.GetRootGameObjects()[0].GetComponentInChildren<Camera>();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1200),0,0);image.Apply();
                Directory.CreateDirectory("Documentation/P3");File.WriteAllBytes("Documentation/P3/"+name+"_Overview.png",image.EncodeToPNG());
                Debug.Log("P3 CAPTURED: "+name);
            }
            finally
            {
                QualitySettings.renderPipeline=previousPipeline;RenderTexture.active=previousTarget;
                target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(clone);
            }
        }
    }
}
