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
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
    // Only overrides existing visual components in the first three scenes. Never edits portal/root transforms or triggers.
    public static class RealmVortexVisuals
    {
        const string Art="Assets/Art/Materials/RealmVortex/";
        static readonly string[] Frames={"JungleExit_ThresholdOnly","CaveThreshold","CityLightPortal"};
        static readonly string[] Names={"RuinsAmber","CaveIceViolet","CityRoyalBlue"};
        static readonly Color[] Primary={new Color(1,.61f,.08f),new Color(.24f,.78f,1),new Color(.43f,.12f,.95f)};
        static readonly Color[] Secondary={new Color(.65f,.19f,.012f),new Color(.3f,.12f,.7f),new Color(.035f,.36f,1)};
        static readonly Color[] Core={new Color(1,.92f,.62f),new Color(.75f,.95f,1),new Color(.72f,.8f,1)};
        static Transform Frame(GameObject root,int index)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==Frames[index]);
        static Bounds Opening(Transform frame,int index)
        {
            var sides=frame.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name==(index==0?"ThresholdPillar":"Pier")).OrderBy(r=>r.bounds.center.x).ToArray();
            var lintel=frame.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name==(index==0?"ExitLintel":"Lintel"));
            if(sides.Length!=2)throw new Exception("Expected two original gateway piers");
            float left=sides[0].bounds.max.x,right=sides[1].bounds.min.x,bottom=Mathf.Min(sides[0].bounds.min.y,sides[1].bounds.min.y),top=lintel.bounds.min.y;
            if(right<=left||top<=bottom)throw new Exception("Invalid gateway opening");
            return new Bounds(new Vector3((left+right)*.5f,(bottom+top)*.5f,(sides[0].bounds.center.z+sides[1].bounds.center.z)*.5f),new Vector3(right-left,top-bottom,0));
        }
        static Mesh Quad()
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"GatewayPlane.asset");if(mesh!=null)return mesh;
            mesh=new Mesh{name="GatewayPlane"};mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"GatewayPlane.asset");return mesh;
        }
        static string ProtectedState(GameObject root)
        {
            // References remain in the same loaded scene during this comparison.
            var components=root.GetComponentsInChildren<MonoBehaviour>(true).Cast<Component>().Concat(root.GetComponentsInChildren<Collider>(true));
            return string.Join("\n",components.Select(c=>c.GetEntityId()+":"+EditorJsonUtility.ToJson(c)+":"+EditorJsonUtility.ToJson(c.transform)));
        }
        static void Record(Object target){EditorUtility.SetDirty(target);if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
        [MenuItem("Tools/Lost Worlds/Portal Visuals/Apply Gateway Vortexes")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var previous=EditorSceneManager.GetSceneManagerSetup();string cityHash=Hash(IntegrationSession.RealmScenes[3]);
            try
            {
                var shader=Shader.Find("Lost Worlds/Realm Vortex");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Vortex shader unavailable");var mesh=Quad();
                for(int i=0;i<3;i++)
                {
                    var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);var root=scene.GetRootGameObjects().Single();var portal=root.GetComponentInChildren<JourneyRealm>(true).Portal;
                    var effect=portal.GetComponentsInChildren<PortalAtmosphere>(true).Single();var surface=(Renderer)new SerializedObject(effect).FindProperty("energySurface").objectReferenceValue;
                    var frame=Frame(root,i);var size=Opening(frame,i);string protectedState=ProtectedState(root);string frameState=EditorJsonUtility.ToJson(frame.gameObject)+string.Join("",frame.GetComponentsInChildren<Component>(true).Select(c=>EditorJsonUtility.ToJson(c)));
                    string path=Art+Names[i]+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(shader){name=Names[i]};AssetDatabase.CreateAsset(mat,path);}
                    mat.SetColor("_ColorA",Primary[i]);mat.SetColor("_ColorB",Secondary[i]);mat.SetColor("_Core",Core[i]);Record(mat);
                    foreach(var r in portal.GetComponentsInChildren<Renderer>(true)){r.enabled=r==surface;Record(r);}
                    surface.GetComponent<MeshFilter>().sharedMesh=mesh;surface.sharedMaterial=mat;
                    surface.transform.SetPositionAndRotation(size.center,Quaternion.identity);
                    var scale=surface.transform.parent.lossyScale;surface.transform.localScale=new Vector3(size.size.x/scale.x,size.size.y/scale.y,1/scale.z);
                    Record(surface.transform);Record(surface.GetComponent<MeshFilter>());Record(surface);
                    if(protectedState!=ProtectedState(root)||frameState!=EditorJsonUtility.ToJson(frame.gameObject)+string.Join("",frame.GetComponentsInChildren<Component>(true).Select(c=>EditorJsonUtility.ToJson(c))))throw new Exception("Nonvisual portal/frame state changed");
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    Debug.Log("VORTEX APPLIED: "+scene.name+" opening "+size.size+" at "+size.center+"; triggers, callbacks and frame unchanged.");
                }
                AssetDatabase.SaveAssets();if(cityHash!=Hash(IntegrationSession.RealmScenes[3]))throw new Exception("Hidden City changed");Validate();
            }
            finally{if(previous.Any(s=>s.isLoaded)&&previous.Where(s=>s.isLoaded).All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);}
        }
        static string Hash(string path)=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(path)));
        [MenuItem("Tools/Lost Worlds/Portal Visuals/Validate Gateway Vortexes")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;var previous=EditorSceneManager.GetSceneManagerSetup();
            try{for(int i=0;i<3;i++)
            {
                var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);var root=scene.GetRootGameObjects().Single();var portal=root.GetComponentInChildren<JourneyRealm>(true).Portal;
                var effect=portal.GetComponentsInChildren<PortalAtmosphere>(true).Single();var surface=(Renderer)new SerializedObject(effect).FindProperty("energySurface").objectReferenceValue;var size=Opening(Frame(root,i),i);
                if(portal.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled)!=1||surface==null||!surface.enabled)throw new Exception("Duplicate/missing visible portal surface");
                var bounds=surface.bounds;if(Vector3.Distance(bounds.center,size.center)>.005f||Vector3.Distance(bounds.size,size.size)>.005f)throw new Exception("Gateway not filled edge to edge");
                var m=surface.sharedMaterial;if(m.shader.name!="Lost Worlds/Realm Vortex"||ShaderUtil.ShaderHasError(m.shader)||!m.shader.isSupported||m.GetColor("_ColorA")!=Primary[i]||m.GetColor("_ColorB")!=Secondary[i])throw new Exception("Incorrect palette/shader");
                if(surface.GetComponent<MeshFilter>().sharedMesh.vertexCount!=4||effect.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Visual added complexity/collision");
                Debug.Log("VORTEX VALID: "+scene.name+"; "+Names[i]+"; full "+bounds.size+" coverage; 2 triangles; one visible renderer; no new collider.");
            }}finally{if(previous.Any(s=>s.isLoaded)&&previous.Where(s=>s.isLoaded).All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(previous);}
        }
        public static void ApplyBatch()=>EditorApplication.delayCall+=()=>{try{Apply();Capture();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
        static void Capture()
        {
            Directory.CreateDirectory("Documentation/PortalVortex");
            for(int i=0;i<3;i++)
            {
                var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);var root=scene.GetRootGameObjects().Single();var portal=root.GetComponentInChildren<JourneyRealm>(true).Portal;var frame=Frame(root,i);var bounds=Opening(frame,i);
                // Transient preview only. Never save the unlocked state or camera/pipeline overrides.
                portal.gameObject.SetActive(true);frame.gameObject.SetActive(true);
                var effect=portal.GetComponentInChildren<PortalAtmosphere>();var surface=(Renderer)new SerializedObject(effect).FindProperty("energySurface").objectReferenceValue;
                var camera=root.GetComponentsInChildren<Camera>(true).First(c=>c.name=="PrototypeOverviewCamera_NoController");camera.orthographic=false;camera.fieldOfView=50;
                camera.transform.position=bounds.center+new Vector3(0,0,-Mathf.Max(bounds.size.x,bounds.size.y)*1.6f);camera.transform.LookAt(bounds.center);
                var pipeline=Object.Instantiate(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);var previous=QualitySettings.renderPipeline;var active=RenderTexture.active;var target=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var block=new MaterialPropertyBlock();Color32[] first=null;
                try
                {
                    pipeline.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;pipeline.useSRPBatcher=false;QualitySettings.renderPipeline=pipeline;
                    for(int phase=0;phase<2;phase++)
                    {
                        block.SetFloat("_MotionTime",phase*8);surface.SetPropertyBlock(block);
                        for(int n=0;n<3;n++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});
                        RenderTexture.active=target;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
                        File.WriteAllBytes("Documentation/PortalVortex/"+Names[i]+"_"+phase+".png",tex.EncodeToPNG());var pixels=tex.GetPixels32();
                        if(phase==0)first=pixels;else{long delta=0;for(int p=0;p<pixels.Length;p++)delta+=Math.Abs(first[p].r-pixels[p].r)+Math.Abs(first[p].g-pixels[p].g)+Math.Abs(first[p].b-pixels[p].b);if(delta<100000)throw new Exception("Vortex shader did not animate");Debug.Log("VORTEX ANIMATION VALID: "+Names[i]+" rendered delta="+delta);}
                    }
                }
                finally{QualitySettings.renderPipeline=previous;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(tex);Object.DestroyImmediate(pipeline);}
            }
        }
    }
}



