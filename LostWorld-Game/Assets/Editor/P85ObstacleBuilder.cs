using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
    // Environment authoring only. No runtime components, movement or interaction logic.
    public static class P85ObstacleBuilder
    {
        const string Folder="Assets/Prefabs/World/P85Obstacles";
        const string GroupName="P85_EnvironmentalObstacles";
        static readonly string[] Realms={"Jungle","Ruins","CrystalCave","HiddenCity"};
        static readonly string[] Types={"JungleLog","JungleBoulder","RuinsBrokenBlocks","RuinsPillarRubble","CaveCrystals","CaveRock","CityStoneBarrier","CityDebris"};
        static Vector3 V(float x,float z)=>new Vector3(x,0,z);
        static readonly Vector3[][] Routes={
            new[]{V(0,0),V(0,12),V(0,24),V(0,32),V(0,35),V(0,45),V(0,50),V(0,62),V(0,108)},
            new[]{V(0,0),V(0,18),V(3,35),V(0,52),V(0,70),V(0,88)},
            new[]{V(0,0),V(0,15),V(-3,30),V(0,46),V(3,62),V(0,78),V(0,88)},
            new[]{V(0,0),V(0,32),V(-10,43),V(-10,57),V(0,70),V(0,88)}
        };
        static string ScenePath(string realm)=>"Assets/Scenes/Person2/"+realm+"_Prototype.unity";
        static Material Mat(string name,bool jungle=false)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/"+(jungle?"JunglePrototype/":"WorldExpansion/")+name+".mat") ?? throw new InvalidOperationException("Missing material "+name);
        static Transform Part(Transform root,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;
            // Box colliders match the visible bounds of flattened/scaled geometry.
            Object.DestroyImmediate(go.GetComponent<Collider>());go.AddComponent<BoxCollider>();return go.transform;
        }
        static void MakePrefabs()
        {
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Prefabs/World","P85Obstacles");
            for(int i=0;i<Types.Length;i++)
            {
                var path=Folder+"/"+Types[i]+".prefab";if(File.Exists(path))continue;
                var go=new GameObject(Types[i]+"_ScaleRootToAdjust");var t=go.transform;
                try
                {
                    switch(i)
                    {
                        case 0:
                            var log=Part(t,"LowFallenLog",PrimitiveType.Cylinder,new Vector3(0,.22f,0),new Vector3(.44f,.9f,.44f),Mat("Bark",true));log.localRotation=Quaternion.Euler(90,0,0);
                            var end=Part(t,"CutEnd",PrimitiveType.Cylinder,new Vector3(0,.22f,-.91f),new Vector3(.37f,.012f,.37f),Mat("Wood",true));end.localRotation=Quaternion.Euler(90,0,0);Object.DestroyImmediate(end.GetComponent<BoxCollider>());break;
                        case 1: Part(t,"RoundedBoulder",PrimitiveType.Sphere,new Vector3(0,.30f,0),new Vector3(.85f,.60f,1.2f),Mat("Rock",true));break;
                        case 2: Part(t,"BrokenBlock",PrimitiveType.Cube,new Vector3(0,.22f,0),new Vector3(1.1f,.44f,.85f),Mat("Sandstone"));Part(t,"Fragment",PrimitiveType.Cube,new Vector3(.1f,.12f,.9f),new Vector3(.65f,.24f,.45f),Mat("Moss"));break;
                        case 3: var shaft=Part(t,"FallenPillarSection",PrimitiveType.Cylinder,new Vector3(0,.28f,0),new Vector3(.56f,.8f,.56f),Mat("Limestone"));shaft.localRotation=Quaternion.Euler(90,0,0);Part(t,"CapitalFragment",PrimitiveType.Cube,new Vector3(0,.18f,1.1f),new Vector3(.9f,.36f,.5f),Mat("Sandstone"));break;
                        case 4:
                            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environments/CrystalCave/Prototype/FacetedCrystal.asset");if(mesh==null)throw new Exception("Missing existing crystal mesh");
                            for(int j=0;j<3;j++){var crystal=new GameObject("LowCrystal");crystal.transform.SetParent(t,false);crystal.transform.localPosition=new Vector3((j-1)*.28f,0,(j-1)*.35f);crystal.transform.localScale=new Vector3(.32f,j==1?.23f:.15f,.32f);crystal.AddComponent<MeshFilter>().sharedMesh=mesh;crystal.AddComponent<MeshRenderer>().sharedMaterial=Mat(j==1?"CrystalTeal":"CrystalViolet");var box=crystal.AddComponent<BoxCollider>();box.center=mesh.bounds.center;box.size=mesh.bounds.size;}break;
                        case 5: Part(t,"LowCaveRock",PrimitiveType.Sphere,new Vector3(0,.28f,0),new Vector3(1.1f,.56f,1.4f),Mat("CaveRock"));break;
                        case 6: Part(t,"LowStoneBarrier",PrimitiveType.Cube,new Vector3(0,.25f,0),new Vector3(.85f,.5f,1.9f),Mat("Ivory"));Part(t,"PatinaInset",PrimitiveType.Cube,new Vector3(0,.49f,0),new Vector3(.88f,.06f,1.6f),Mat("Patina"));break;
                        case 7: Part(t,"FallenStone",PrimitiveType.Cube,new Vector3(0,.18f,0),new Vector3(1.1f,.36f,.95f),Mat("CityPaving"));Part(t,"CarvedFragment",PrimitiveType.Cube,new Vector3(.12f,.14f,.9f),new Vector3(.65f,.28f,.5f),Mat("Ivory"));break;
                    }
                    PrefabUtility.SaveAsPrefabAsset(go,path);
                }
                finally { Object.DestroyImmediate(go); }
            }
        }
        static Collider Ground(Transform root,int realm)=>root.Find(realm==0?"Environment/Terrain_Ground/JungleGround":"Environment/Terrain_Ground/ContinuousGround").GetComponent<Collider>();
        static float GroundY(Collider ground,Vector3 p)
        { if(!ground.Raycast(new Ray(p+Vector3.up*25,Vector3.down),out var hit,50))throw new Exception("Missing ground at "+p);return hit.point.y; }
        static void Place(Transform group,Collider ground,int type,Vector3 center,Vector3 tangent,float offset)
        {
            tangent.Normalize();var side=Vector3.Cross(Vector3.up,tangent);var p=center+side*offset;p.y=GroundY(ground,p)+.045f;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+Types[type]+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,group);
            go.name=Types[type]+"_"+(group.childCount);go.transform.position=p;go.transform.rotation=Quaternion.LookRotation(tangent);
        }
        [MenuItem("Tools/Lost Worlds/P8.5/Setup Obstacles")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var previous=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                MakePrefabs();
                for(int r=0;r<4;r++)
                {
                    var scene=EditorSceneManager.OpenScene(ScenePath(Realms[r]));var root=scene.GetRootGameObjects().Single().transform;
                    if(root.Find("Environment/"+GroupName)!=null)continue;
                    var group=new GameObject(GroupName).transform;group.SetParent(root.Find("Environment"),false);var ground=Ground(root,r);
                    if(r==0)
                    {
                        Place(group,ground,0,Vector3.Lerp(Routes[0][1],Routes[0][2],.35f),Routes[0][2]-Routes[0][1],2.5f);
                        Place(group,ground,1,Vector3.Lerp(Routes[0][2],Routes[0][3],.4f),Routes[0][3]-Routes[0][2],-2.5f);
                        Place(group,ground,0,V(0,60),Vector3.forward,2.5f);Place(group,ground,1,V(0,104),Vector3.forward,-2.5f);
                    }
                    else if(r==3)
                    {
                        Place(group,ground,6,V(0,18),Vector3.forward,3.25f);Place(group,ground,7,V(-10,46),Vector3.forward,-3.25f);
                        Place(group,ground,6,V(10,55),Vector3.forward,3.25f);Place(group,ground,7,V(0,77),Vector3.forward,-3.25f);
                    }
                    else
                    {
                        int[] segments={1,2,3,4};float[] fractions={.35f,r==2?.35f:.65f,.85f,.65f};
                        for(int k=0;k<4;k++){int s=segments[k];Place(group,ground,r*2+k%2,Vector3.Lerp(Routes[r][s],Routes[r][s+1],fractions[k]),Routes[r][s+1]-Routes[r][s],k%2==0?3.25f:-3.25f);}
                    }
                    if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
                }
                AssetDatabase.SaveAssets();
                ApplyJumpLayout();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }
        static void Slot(int realm,int index,out Vector3 center,out Vector3 tangent)
        {
            tangent=Vector3.forward;
            if(realm==0)
            {
                if(index<2){int segment=index+1;float fraction=index==0?.35f:.4f;center=Vector3.Lerp(Routes[0][segment],Routes[0][segment+1],fraction);tangent=(Routes[0][segment+1]-Routes[0][segment]).normalized;return;}
                center=V(0,index==2?60:102);return;
            }
            if(realm==3){center=index==0?V(0,18):index==1?V(-10,46):index==2?V(10,55):V(0,77);return;}
            int s=index+1;float f=index==0?.35f:index==1?(realm==2?.35f:.65f):index==2?(realm==2?.55f:.85f):.65f;
            center=Vector3.Lerp(Routes[realm][s],Routes[realm][s+1],f);tangent=(Routes[realm][s+1]-Routes[realm][s]).normalized;
        }
        static Bounds LocalBounds(Transform item)
        {
            bool first=true;var bounds=new Bounds();
            foreach(var box in item.GetComponentsInChildren<BoxCollider>())
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                {
                    var point=item.InverseTransformPoint(box.transform.TransformPoint(box.center+Vector3.Scale(box.size*.5f,new Vector3(x,y,z))));
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
            if(first)throw new Exception("Missing obstacle box colliders");return bounds;
        }
        [MenuItem("Tools/Lost Worlds/P8.5/Apply Main Route Jump Layout")]
        public static void ApplyJumpLayout()=>ApplyJumpLayout(false);
        public static void ApplyJungleJumpLayout()=>ApplyJumpLayout(true);
        static void ApplyJumpLayout(bool jungleOnly)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var previous=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for(int realm=0;realm<(jungleOnly?1:4);realm++)
                {
                    var scene=EditorSceneManager.OpenScene(ScenePath(Realms[realm]));var root=scene.GetRootGameObjects().Single().transform;
                    var group=root.Find("Environment/"+GroupName);if(group==null || group.childCount!=4)throw new Exception("Expected existing four obstacles");var ground=Ground(root,realm);
                    for(int i=0;i<4;i++)
                    {
                        var item=group.GetChild(i);Slot(realm,i,out var center,out var tangent);
                        item.localScale=Vector3.one;var bounds=LocalBounds(item);
                        bool lengthwise=(realm==0&&i%2==0)||(realm==1&&i%2==1)||(realm==3&&i%2==0);
                        float width=realm==0?2.4f:2.8f, depth=.7f;
                        item.rotation=Quaternion.LookRotation(tangent)*Quaternion.Euler(0,lengthwise?90:0,0);
                        item.localScale=new Vector3((lengthwise?depth:width)/bounds.size.x,.6f/bounds.size.y,(lengthwise?width:depth)/bounds.size.z);
                        center.y=GroundY(ground,center)+.02f;
                        item.position=center-item.rotation*Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),item.localScale);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                    }
                    Physics.SyncTransforms();ValidateScene(scene,realm);EditorSceneManager.SaveScene(scene);
                }
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(previous);}
        }
        static int ValidateScene(Scene scene,int realm)
        {
            var root=scene.GetRootGameObjects().Single().transform;var group=root.Find("Environment/"+GroupName);
            if(group==null || group.childCount!=4)throw new Exception("Expected four P8.5 obstacles in "+scene.name);
            Physics.SyncTransforms();var colliders=group.GetComponentsInChildren<Collider>(true);var ground=Ground(root,realm);
            if(group.GetComponentsInChildren<MonoBehaviour>(true).Length!=0 || group.GetComponentsInChildren<Rigidbody>(true).Length!=0)throw new Exception("Obstacles must be static world geometry only");
            int samples=0;
            for(int i=0;i<4;i++)
            {
                var item=group.GetChild(i);var solids=item.GetComponentsInChildren<Collider>();
                if(!item.gameObject.activeInHierarchy || !PrefabUtility.IsPartOfPrefabInstance(item) || solids.Length==0)throw new Exception("Missing active prefab/collider");
                foreach(var c in solids)if(!c.enabled || c.isTrigger)throw new Exception("Invalid solid collider");
                Slot(realm,i,out var center,out var tangent);if(realm>0 && root.GetComponentInChildren<LostWorlds.Person2.Integration.RealmBindings>().GetMainPath().All(p=>Mathf.Abs(p.x)<.01f)){center=new Vector3(0,0,item.position.z);tangent=Vector3.forward;}var side=Vector3.Cross(Vector3.up,tangent);float y=GroundY(ground,center);
                var bounds=solids[0].bounds;foreach(var c in solids)bounds.Encapsulate(c.bounds);
                if(bounds.size.y<.45f || bounds.size.y>.85f)throw new Exception("Expected low jump obstacle height: "+item.name);
                if(Mathf.Abs(bounds.min.y-y-.02f)>.035f)throw new Exception("Obstacle not grounded at route centre");
                // A straight centre-line runner intersects a solid obstacle at shin height.
                foreach(float lateral in new[]{-.3f,0,.3f})
                {
                    var ray=new Ray(center+side*lateral-tangent*3+Vector3.up*(y+.25f),tangent);
                    // Slot center has y=0. Check actual individual colliders, not aggregate bounds.
                    if(!solids.Any(c=>c.Raycast(ray,out var hit,6)))throw new Exception("Running lane misses obstacle: "+item.name);
                    samples++;
                }
                // Preserve a body-width route around each side, rather than a full-width wall.
                float bypass=realm==0?2:2.5f;
                foreach(float sign in new[]{-1f,1f})for(int step=-5;step<=5;step++)
                {
                    var p=center+side*(sign*bypass)+tangent*(step*.4f);p.y=GroundY(ground,p);
                    foreach(var c in Physics.OverlapBox(p+Vector3.up*1.05f,new Vector3(.3f,1,.25f),Quaternion.LookRotation(tangent),~0,QueryTriggerInteraction.Ignore))
                        if(c!=ground && c.bounds.max.y>p.y+.2f)throw new Exception("Side bypass obstructed by "+c.name+" near "+item.name);
                    samples++;
                }
                foreach(Transform other in group)if(other!=item && Vector3.Distance(item.position,other.position)<8)throw new Exception("Obstacles too close");
            }
            // Protect locations, actual interactable bounds, test references, and the full route-choice zone.
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                bool location=t.parent!=null && t.parent.name=="FutureGameplayPoints";
                bool interaction=t.GetComponent<LostWorlds.Person2.Interaction.InteractableBase>()!=null;
                bool reference=t.GetComponent<LostWorlds.Person2.Interaction.InteractionInteractor>()!=null;
                if(location || interaction || reference)foreach(var c in colliders)
                    if(Vector3.Distance(c.ClosestPoint(t.position),t.position)<3)throw new Exception("Obstacle too near protected location: "+t.name);
                var zone=t.GetComponent<LostWorlds.Person2.World.RouteChoiceZone>();if(zone!=null)foreach(var c in colliders)
                    if(zone.ChoiceZone.bounds.Intersects(c.bounds))throw new Exception("Obstacle overlaps route choice zone");
            }
            foreach(var renderer in group.GetComponentsInChildren<Renderer>())if(renderer.sharedMaterial==null)throw new Exception("Missing material");
            Debug.Log($"P8.5 {scene.name}: 4 prefab obstacles, {colliders.Length} solid colliders, {samples} centre-lane collision/side-bypass checks; protected locations clear.");return samples;
        }
        [MenuItem("Tools/Lost Worlds/P8.5/Validate Obstacles")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var previous=EditorSceneManager.GetSceneManagerSetup();int samples=0;
            try { for(int r=0;r<4;r++)samples+=ValidateScene(EditorSceneManager.OpenScene(ScenePath(Realms[r])),r); }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
            Directory.CreateDirectory("Documentation/P8.5");File.WriteAllText("Documentation/P8.5/VALIDATION.txt",$"Unity {Application.unityVersion}\n4 realms, 8 reusable prefabs, 16 obstacles.\n{samples} centre-lane collision/side-bypass checks; protected locations and route zone clear.\nStatic solid colliders, spacing, grounding, size limits and prefab links verified.\n");
        }
        public static void BuildAndValidateBatch()
        {
            try { EditorSceneManager.OpenScene(ScenePath("Jungle"));ApplyJumpLayout();Validate();CaptureAll();EditorApplication.Exit(0); }
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }
        static void CaptureAll()
        {
            for(int r=0;r<4;r++)
            {
                var scene=EditorSceneManager.OpenScene(ScenePath(Realms[r]));var root=scene.GetRootGameObjects().Single().transform;var group=root.Find("Environment/"+GroupName);
                var cameraObject=new GameObject("P85_TemporaryPreview");var camera=cameraObject.AddComponent<Camera>();
                var focus=group.GetChild(0).position;camera.transform.position=focus+new Vector3(10,10,-14);camera.transform.LookAt(focus+Vector3.up*.4f);camera.nearClipPlane=.1f;camera.farClipPlane=150;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;
                var current=QualitySettings.renderPipeline;var clone=Object.Instantiate(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);
                var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var active=RenderTexture.active;
                try
                {
                    clone.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;clone.useSRPBatcher=false;QualitySettings.renderPipeline=clone;
                    camera.targetTexture=target;RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});
                    RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Documentation/P8.5/"+Realms[r]+".png",image.EncodeToPNG());
                }
                finally {QualitySettings.renderPipeline=current;RenderTexture.active=active;Object.DestroyImmediate(cameraObject);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(clone);}
            }
        }
    }
}
