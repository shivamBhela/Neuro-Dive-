using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using Object=UnityEngine.Object;

namespace Realmwalker.Authoring
{
    public static class RealmJumpAuthoring
    {
        const string Art="Assets/Realmwalker/JumpChallenges";
        public static void ApplyBatch()=>EditorApplication.delayCall+=()=>{try{Apply();Validate();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
        static void Apply()
        {
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            for(int index=0;index<4;index++)
            {
                var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[index]);
                var realm=Object.FindAnyObjectByType<RealmBindings>();var root=realm.transform.root;
                if(realm.GetComponent<RealmFallSafety>()!=null)continue;
                Physics.SyncTransforms();
                var safety=realm.gameObject.AddComponent<RealmFallSafety>();
                var ground=root.Find(index==0?"Environment/Terrain_Ground/JungleGround":"Environment/Terrain_Ground/ContinuousGround");
                var b=ground.GetComponent<Collider>().bounds;
                safety.safeBounds=new Bounds(new Vector3(b.center.x,12,b.center.z),new Vector3(b.size.x+4,34,b.size.z+8));
                var group=new GameObject("JumpSafety").transform;group.SetParent(root,false);
                var hazards=new List<BoxCollider>();
                if(index==0)hazards.Add(Hazard(group,"BridgeStreamFall",71.4f,72.6f,8.8f,.7f));
                else
                {
                    float width=index==3?10:9;
                    var tokens=root.GetComponentInChildren<JourneyRealm>().Tokens;
                    var obstacles=root.Find("Environment/P85_EnvironmentalObstacles").GetComponentsInChildren<Collider>().Where(c=>!c.isTrigger).ToArray();
                    var zone=realm.Branch==null?null:realm.Branch.Zone.GetComponent<Collider>();
                    var chosen=new List<float>();
                    for(int n=0;n<2;n++)
                    {
                        var candidates=new List<float>();
                        for(float z=22;z<realm.Exit.position.z-15;z+=.5f)
                        {
                            if(chosen.Any(c=>Mathf.Abs(c-z)<23)||tokens.Any(t=>Mathf.Abs(t.transform.position.z-z)<2.8f))continue;
                            if(obstacles.Any(c=>c.bounds.max.z>z-6&&c.bounds.min.z<z+5))continue;
                            if(zone!=null&&zone.bounds.max.z>z-4&&zone.bounds.min.z<z+4)continue;
                            candidates.Add(z);
                        }
                        if(candidates.Count==0){if(chosen.Count==0)throw new Exception("No safe gap location in "+scene.name);Debug.Log("JUMP PLACEMENT: "+scene.name+" retains one gap to preserve token, route and obstacle recovery space.");break;}
                        float center=candidates.OrderBy(z=>Mathf.Abs(z-(n==0?28:64))).First();chosen.Add(center);
                        float near=center-.6f,far=center+.6f;
                        var challenge=new GameObject("ShortGap_"+center.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)).transform;challenge.SetParent(group,false);
                        var data=challenge.gameObject.AddComponent<RealmJumpChallenge>();data.near=near;data.far=far;data.width=width;
                        hazards.Add(Hazard(challenge,"FallVolume",near,far,width+.4f,-.65f));
                        var cutBounds=new Bounds(new Vector3(0,-.3f,center),new Vector3(width+.4f,2,1.2f));
                        int meshCount=0;
                        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                        {
                            var renderer=filter.GetComponent<Renderer>();
                            if(renderer==null||!renderer.bounds.Intersects(cutBounds)||renderer.bounds.min.y>.2f||renderer.bounds.max.y<-.6f)continue;
                            if(filter.GetComponentInParent<RealmwalkerRunner>()!=null||filter.GetComponentInParent<JourneyToken>()!=null)continue;
                            if(filter.sharedMesh.subMeshCount!=1)throw new Exception("Unexpected layered mesh: "+filter.name);
                            var mesh=Cut(filter,near,far,width*.5f+.2f,index+"_"+n+"_"+meshCount++);
                            filter.sharedMesh=mesh;
                            var collider=filter.GetComponent<Collider>();
                            if(collider!=null)
                            {
                                if(collider.isTrigger)throw new Exception("Unexpected trigger in cut: "+filter.name);
                                var material=collider.sharedMaterial;Object.DestroyImmediate(collider);
                                var mc=filter.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mesh;mc.sharedMaterial=material;
                            }
                        }
                        var materialSource=ground.GetComponent<Renderer>().sharedMaterial;
                        string[] paths={"","Ruins/SandstoneUpgrade/CarvedOchre","CrystalCave/MineralUpgrade/CyanQuartz","HiddenCity/FinaleUpgrade/RewardGold"};
                        var accent=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/"+paths[index]+".mat")??materialSource;
                        foreach(float z in new[]{near-.24f,far+.24f})Part(challenge,"BroadLandingEdge",new Vector3(0,.025f,z),new Vector3(width,.04f,.4f),accent);
                        // Flush, static approach inlays: no flash or additional collision hurdle.
                        foreach(float offset in new[]{6f,4f})Part(challenge,"AdvanceJumpCue",new Vector3(0,.06f,near-offset),new Vector3(width*.85f,.015f,.16f),accent);
                        foreach(float z in new[]{near-.035f,far+.035f})Part(challenge,"ExposedStoneEdge",new Vector3(0,-1.5f,z),new Vector3(width,3,.07f),materialSource);
                        Part(challenge,"RecessShadow",new Vector3(0,-3,center),new Vector3(width,.05f,1.2f),materialSource);
                        Debug.Log($"JUMP AUTHORED {scene.name}: near={near:F2}, far={far:F2}, width={width}, meshes={meshCount}; tokens unchanged");
                    }
                }
                safety.hazards=hazards.ToArray();EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        static BoxCollider Hazard(Transform parent,string name,float near,float far,float width,float top)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=new Vector3(0,(top-5)*.5f,(near+far)*.5f);
            var box=go.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(width,top+5,far-near);return box;
        }
        static void Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());}
        struct Vertex{public Vector3 p,n;public Vector2 uv;public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t),uv=Vector2.Lerp(a.uv,b.uv,t)};}
        static List<Vertex> Clip(List<Vertex> source,Func<Vector3,float> plane,bool keep)
        {
            var result=new List<Vertex>();if(source.Count==0)return result;var a=source[source.Count-1];float da=plane(a.p)*(keep?1:-1);
            foreach(var b in source){float db=plane(b.p)*(keep?1:-1);if((da>=0)!=(db>=0))result.Add(Vertex.Lerp(a,b,da/(da-db)));if(db>=0)result.Add(b);a=b;da=db;}return result;
        }
        static Mesh Cut(MeshFilter filter,float near,float far,float halfWidth,string name)
        {
            var source=filter.sharedMesh;var v=source.vertices;var normals=source.normals;var uv=source.uv;var output=new List<Vertex>();
            Func<Vector3,float>[] planes={p=>filter.transform.TransformPoint(p).x+halfWidth,p=>halfWidth-filter.transform.TransformPoint(p).x,p=>filter.transform.TransformPoint(p).z-near,p=>far-filter.transform.TransformPoint(p).z};
            var tris=source.triangles;
            for(int i=0;i<tris.Length;i+=3)
            {
                var polygon=new List<Vertex>();for(int j=0;j<3;j++){int k=tris[i+j];polygon.Add(new Vertex{p=v[k],n=normals.Length==v.Length?normals[k]:Vector3.up,uv=uv.Length==v.Length?uv[k]:Vector2.zero});}
                foreach(var plane in planes){var outside=Clip(polygon,plane,false);for(int j=1;j+1<outside.Count;j++)if(Vector3.Cross(outside[j].p-outside[0].p,outside[j+1].p-outside[0].p).sqrMagnitude>1e-14f){output.Add(outside[0]);output.Add(outside[j]);output.Add(outside[j+1]);}polygon=Clip(polygon,plane,true);}
            }
            var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=output.Select(a=>a.p).ToArray();mesh.normals=output.Select(a=>a.n.normalized).ToArray();mesh.uv=output.Select(a=>a.uv).ToArray();mesh.triangles=Enumerable.Range(0,output.Count).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,Art+"/"+name+".asset");return mesh;
        }
        [MenuItem("Tools/Lost Worlds/Jump Recovery/Validate Geometry")]
        public static void ValidateMenu(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;var previous=EditorSceneManager.GetSceneManagerSetup();try{Validate();}finally{EditorSceneManager.RestoreSceneManagerSetup(previous);}}
        public static void ValidateBatch()=>EditorApplication.delayCall+=()=>{try{Validate();LostWorlds.Person2.Editor.RunningPathPolish.Validate();LostWorlds.Person2.Editor.P14DemoBuild.Validate();Capture();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
        static void Capture()
        {
            const string folder="Assets/Realmwalker/Validation/JumpChallenges";Directory.CreateDirectory(folder);
            for(int i=1;i<4;i++)
            {
                EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);var gap=Object.FindAnyObjectByType<RealmJumpChallenge>();
                var go=new GameObject("ValidationCamera");var camera=go.AddComponent<Camera>();camera.transform.position=new Vector3(7,6,gap.near-9);camera.transform.LookAt(new Vector3(0,0,(gap.near+gap.far)*.5f));camera.fieldOfView=55;camera.farClipPlane=180;
                var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=RenderTexture.active;
                try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(folder+"/Realm"+i+".png",image.EncodeToPNG());}
                finally{RenderTexture.active=old;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(go);}
            }
        }
        public static bool CheckOpening(Transform root,Vector3 p)
        {
            if(!root.GetComponentsInChildren<RealmJumpChallenge>().Any(g=>Mathf.Abs(p.x)<g.width*.5f+.2f&&p.z>g.near+.001f&&p.z<g.far-.001f))return false;
            if(Physics.Raycast(p+Vector3.up*8,Vector3.down,30,~0,QueryTriggerInteraction.Ignore))throw new Exception("Authored gap unexpectedly has a floor at "+p);
            return true;
        }
        public static void Validate()
        {
            var ids=new HashSet<string>();int rays=0;
            for(int i=0;i<4;i++)
            {
                var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);var realm=Object.FindAnyObjectByType<RealmBindings>();var root=realm.transform.root;Physics.SyncTransforms();
                if(realm.GetComponent<RealmFallSafety>()==null)throw new Exception("Missing fall safety");
                foreach(var t in root.GetComponentInChildren<JourneyRealm>().Tokens){if(!ids.Add(t.TokenId)||!Physics.Raycast(t.transform.position+Vector3.up*2,Vector3.down,8,~0,QueryTriggerInteraction.Ignore))throw new Exception("Duplicate/unsupported token "+t.TokenId);}
                var gaps=root.GetComponentsInChildren<RealmJumpChallenge>();if(gaps.Length!=(i==0?0:i==3?1:2))throw new Exception("Unexpected challenge count");
                foreach(var gap in gaps)for(float x=-gap.width*.5f+.2f;x<gap.width*.5f;x+=.4f)
                {
                    for(float z=gap.near+.05f;z<gap.far;z+=.1f){if(Physics.Raycast(new Vector3(x,5,z),Vector3.down,30,~0,QueryTriggerInteraction.Ignore))throw new Exception("Invisible gap floor "+scene.name+" "+x+","+z);rays++;}
                    foreach(float z in new[]{gap.near-4,gap.near-.1f,gap.far+.1f,gap.far+4})if(!Physics.Raycast(new Vector3(x,5,z),Vector3.down,out var h,6,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(h.point.y)>.12f)throw new Exception("Unsafe landing "+scene.name);
                }
                foreach(var c in root.GetComponentsInChildren<Component>(true))if(c==null)throw new Exception("Missing script");
                Debug.Log("JUMP GEOMETRY PASS "+scene.name);
            }
            if(ids.Count!=36)throw new Exception("Token count changed");Debug.Log("JUMP GEOMETRY PASS: "+rays+" empty gap rays; 36 unique supported tokens");
        }
    }
}
