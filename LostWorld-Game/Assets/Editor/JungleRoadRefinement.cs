using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
namespace LostWorlds.Person2.Editor
{
    public static class JungleRoadRefinement
    {
        const string Scene="Assets/Scenes/Person2/Jungle_Prototype.unity";
        const string MeshPath="Assets/Art/Environments/Jungle/Prototype/ContinuousRoad.asset";
        static Vector2 Section(float z)=>new Vector2(0,3.5f);
        static float Height(float z)=>z<24?0:z<50?(z-24)/26*2:z<88?2:2+Mathf.Clamp01((z-88)/20)*2;
        static void Remove(Transform t){if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}
        static float Ground(Collider ground,float x,float z)
        {
            if(!ground.Raycast(new Ray(new Vector3(x,30,z),Vector3.down),out var hit,60))throw new Exception("Missing road ground at "+x+","+z);
            return hit.point.y;
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Refine Continuous Road")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(Scene);var root=scene.GetRootGameObjects().Single().transform;
            var path=root.Find("Environment/JunglePath");var ground=root.Find("Environment/Terrain_Ground/JungleGround").GetComponent<Collider>();
            if(path.Find("ContinuousMainRoad")!=null){Validate();return;}
            // Fill the former bridge ravine with the same sloped ground, without editing the shared P2 mesh.
            var groundMesh=UnityEngine.Object.Instantiate(ground.GetComponent<MeshFilter>().sharedMesh);
            var gv=groundMesh.vertices.ToList();var gt=groundMesh.triangles.ToList();
            for(int z=36;z<44;z+=2)for(int x=-4;x<4;x+=2){int k=gv.Count;gv.Add(new Vector3(x,Height(z),z));gv.Add(new Vector3(x,Height(z+2),z+2));gv.Add(new Vector3(x+2,Height(z+2),z+2));gv.Add(new Vector3(x+2,Height(z),z));gt.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});}
            groundMesh.name="ContinuousJungleGround";groundMesh.vertices=gv.ToArray();groundMesh.triangles=gt.ToArray();groundMesh.RecalculateNormals();groundMesh.RecalculateBounds();
            AssetDatabase.CreateAsset(groundMesh,"Assets/Art/Environments/Jungle/Prototype/ContinuousGround.asset");ground.GetComponent<MeshFilter>().sharedMesh=groundMesh;((MeshCollider)ground).sharedMesh=groundMesh;
            foreach(Transform t in root.Find("Environment/Terrain_Ground").Cast<Transform>().ToArray())if(t.name.StartsWith("Ravine"))Remove(t);
            foreach(string name in new[]{"Environment/Landmarks/BrokenBridge","Environment/Obstacles/BridgeApproach_Rubble","Environment/Obstacles/BridgeFarBank_Rubble"}){var t=root.Find(name);if(t!=null){t.position+=Vector3.left*16;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}}
            var binding=root.GetComponentInChildren<RealmBindings>();var bs=new SerializedObject(binding);bs.FindProperty("branch").objectReferenceValue=null;bs.ApplyModifiedPropertiesWithoutUndo();
            foreach(var branch in root.GetComponentsInChildren<BranchPathBinding>(true))UnityEngine.Object.DestroyImmediate(branch);
            foreach(Transform t in binding.transform.Cast<Transform>().ToArray())if(t.name.StartsWith("left_")||t.name.StartsWith("right_"))Remove(t);
            Remove(root.Find("P6_RouteChoice"));Remove(root.Find("FutureGameplayPoints/RouteSplit"));
            var points=bs.FindProperty("mainPath");for(int j=0;j<points.arraySize;j++){var t=(Transform)points.GetArrayElementAtIndex(j).objectReferenceValue;t.position=new Vector3(0,Height(t.position.z)+.15f,t.position.z);}
            var tokens=root.GetComponentInChildren<JourneyRealm>().Tokens;float[] zs={8,23,36,48,69,95,77,85,41,54};
            for(int j=0;j<tokens.Length;j++){tokens[j].transform.position=new Vector3(0,Height(zs[j])+1,zs[j]);PrefabUtility.RecordPrefabInstancePropertyModifications(tokens[j].transform);}
            Physics.SyncTransforms();
            var vertices=new Vector3[229*25];var triangles=new int[228*24*6];int n=0;
            for(int row=0;row<229;row++)
            {
                float z=-4+row*.5f;var section=Section(z);
                for(int col=0;col<25;col++){float x=section.x+Mathf.Lerp(-section.y,section.y,col/24f);vertices[row*25+col]=new Vector3(x,Ground(ground,x,z)+.055f,z);}
                if(row==228)continue;
                for(int col=0;col<24;col++){int a=row*25+col;triangles[n++]=a;triangles[n++]=a+25;triangles[n++]=a+1;triangles[n++]=a+1;triangles[n++]=a+25;triangles[n++]=a+26;}
            }
            var mesh=new Mesh{name="ContinuousMainRoad"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,MeshPath);
            // Remove superseded road surfaces from this scene; keep shared source assets intact.
            foreach(var r in path.GetComponentsInChildren<Renderer>().ToArray())
                if(r.name.StartsWith("MainTrail")||r.name.StartsWith("LeftRoute")||r.name.StartsWith("RightRoute")||r.name.StartsWith("ExitApproach")||r.name=="RouteSplitClearing")Remove(r.transform);
            var road=new GameObject("ContinuousMainRoad");road.transform.SetParent(path,false);road.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=road.AddComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P12Polish/Jungle_ReadablePath.mat");renderer.shadowCastingMode=ShadowCastingMode.Off;
            int moved=0;
            foreach(string groupName in new[]{"Vegetation","Rocks"})
            foreach(Transform item in root.Find("Environment/"+groupName))
            {
                float z=item.position.z;if(z< -4||z>110)continue;
                var section=Section(z);var renderers=item.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                // Reserve full visible road width. Move only natural decorations, never gameplay objects.
                if(bounds.max.x<section.x-section.y-.6f||bounds.min.x>section.x+section.y+.6f)continue;
                float side=item.position.x<section.x?-1:1;float x=section.x+side*(section.y+bounds.extents.x+1.2f);
                var pos=item.position;pos.x=x;pos.y=Ground(ground,x,z);item.position=pos;
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);moved++;
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();P85ObstacleBuilder.ApplyJungleJumpLayout();Validate();Capture();Debug.Log("Jungle road refined; natural decorations repositioned: "+moved);
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Validate Continuous Road")]
        public static void Validate()
        {
            var scene=EditorSceneManager.OpenScene(Scene);var root=scene.GetRootGameObjects().Single().transform;
            var road=root.Find("Environment/JunglePath/ContinuousMainRoad");if(road==null||road.GetComponent<Collider>()!=null)throw new Exception("Visual road missing or unexpected collider");
            var ground=root.Find("Environment/Terrain_Ground/JungleGround").GetComponent<Collider>();
            bool bridgeGap=JungleBridgeGap.Exists(root);
            foreach(var v in road.GetComponent<MeshFilter>().sharedMesh.vertices)if(!(bridgeGap&&JungleBridgeGap.InOpening(v.x,v.z))&&Mathf.Abs(Ground(ground,v.x,v.z)+.055f-v.y)>.01f)throw new Exception("Floating road vertex");
            var world=root.GetComponentInChildren<JourneyRealm>();if(world.Tokens.Length!=10||world.Tokens.Select(t=>t.TokenId).Distinct().Count()!=10)throw new Exception("Jungle tokens changed");
            var binding=root.GetComponentInChildren<RealmBindings>();if(binding.Branch!=null||root.GetComponentsInChildren<LostWorlds.Person2.World.RouteChoiceZone>(true).Length!=0)throw new Exception("Obsolete Jungle route binding");
            foreach(var point in binding.GetMainPath())if(Mathf.Abs(point.x)>.01f)throw new Exception("Main path is not straight");
            if(root.Find("P6_RouteChoice")!=null||root.Find("FutureGameplayPoints/RouteSplit")!=null)throw new Exception("Obsolete fork objects remain");
            foreach(var token in world.Tokens)if(Mathf.Abs(token.transform.position.x)>2.1f)throw new Exception("Token outside main road");
            Physics.SyncTransforms();
            for(int z=0;z<=108;z++)foreach(float x in new[]{-2.5f,0,2.5f}){
                if(bridgeGap&&JungleBridgeGap.InOpening(x,z))continue;
                var p=new Vector3(x,Ground(ground,x,z),z);
                if(Physics.CheckCapsule(p+Vector3.up*.95f,p+Vector3.up*1.7f,.3f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Road body clearance blocked at "+p);
            }
            if(bridgeGap)JungleBridgeGap.ValidateGeometry(root);
            Debug.Log("Jungle road PASS: grounded road outside intentional bridge opening, no route zone/branch, straight integration path, body clearance and 10 unique tokens.");
        }
        static void Capture()
        {
            var camera=UnityEngine.Object.FindObjectsByType<Camera>().First(c=>c.name=="PrototypeOverviewCamera_NoController");
            var oldPos=camera.transform.position;var oldRot=camera.transform.rotation;bool oldOrtho=camera.orthographic;float oldSize=camera.orthographicSize;
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;var clone=UnityEngine.Object.Instantiate(pipeline);var previous=QualitySettings.renderPipeline;
            var target=new RenderTexture(1000,1500,24);var image=new Texture2D(1000,1500,TextureFormat.RGB24,false);var active=RenderTexture.active;
            try{clone.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;clone.useSRPBatcher=false;QualitySettings.renderPipeline=clone;camera.orthographic=true;camera.orthographicSize=64;camera.transform.position=new Vector3(0,140,52);camera.transform.rotation=Quaternion.Euler(90,0,0);RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1000,1500),0,0);image.Apply();Directory.CreateDirectory("Documentation/JungleRoad");File.WriteAllBytes("Documentation/JungleRoad/Overview.png",image.EncodeToPNG());}
            finally{camera.transform.SetPositionAndRotation(oldPos,oldRot);camera.orthographic=oldOrtho;camera.orthographicSize=oldSize;QualitySettings.renderPipeline=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(clone);}
        }
        public static void ApplyBatch()=>EditorApplication.delayCall+=()=>{try{Apply();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
        public static void ValidateP14Batch()=>EditorApplication.delayCall+=()=>{try{EditorSceneManager.OpenScene(Scene);P14DemoBuild.Validate();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
    }
}
