using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    public static class JungleBridgeGap
    {
        public const float Near=71.4f,Far=72.6f;
        const string Folder="Assets/Art/Environments/Jungle/BridgeGap";
        public static bool Exists(Transform root)=>root.Find("Environment/JungleScenicPolish/WideWoodenBridge/JumpGap_1p2m")!=null;
        public static bool InOpening(float x,float z)=>Mathf.Abs(x)<4.4f&&z>Near-.001f&&z<Far+.001f;
        struct Vertex {public Vector3 p;public Vector2 uv;public Vertex(Vector3 p,Vector2 uv){this.p=p;this.uv=uv;}}
        static List<Vertex> Clip(List<Vertex> input,Func<Vector3,float> distance,bool inside)
        {
            var output=new List<Vertex>();if(input.Count==0)return output;
            var a=input[input.Count-1];float da=distance(a.p)*(inside?1:-1);
            foreach(var b in input){float db=distance(b.p)*(inside?1:-1);if((da>=0)!=(db>=0)){float t=da/(da-db);output.Add(new Vertex(Vector3.Lerp(a.p,b.p,t),Vector2.Lerp(a.uv,b.uv,t)));}if(db>=0)output.Add(b);a=b;da=db;}return output;
        }
        static Mesh Cut(Mesh source,string name)
        {
            var v=source.vertices;var uv=source.uv;var indices=source.triangles;var vertices=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();
            Func<Vector3,float>[] sides={p=>p.x+4.4f,p=>4.4f-p.x,p=>p.z-Near,p=>Far-p.z};
            Action<List<Vertex>> emit=poly=>{for(int k=1;k+1<poly.Count;k++){if(Vector3.Cross(poly[k].p-poly[0].p,poly[k+1].p-poly[0].p).sqrMagnitude<1e-12f)continue;foreach(var item in new[]{poly[0],poly[k],poly[k+1]}){triangles.Add(vertices.Count);vertices.Add(item.p);uvs.Add(item.uv);}}};
            for(int i=0;i<indices.Length;i+=3){var poly=new List<Vertex>();for(int j=0;j<3;j++){int n=indices[i+j];poly.Add(new Vertex(v[n],uv.Length==v.Length?uv[n]:Vector2.zero));}foreach(var side in sides){emit(Clip(poly,side,false));poly=Clip(poly,side,true);if(poly.Count==0)break;}}
            var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");return mesh;
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Create Bridge Jump Gap")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Person2/Jungle_Prototype.unity");var root=scene.GetRootGameObjects().Single().transform;
            if(Exists(root)){ValidateGeometry(root);return;}
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var bridge=root.Find("Environment/JungleScenicPolish/WideWoodenBridge");
            if(bridge==null)throw new Exception("Scenic bridge missing");
            var log=bridge.Find("FallenLog_JumpObstacle");if(log!=null)Object.DestroyImmediate(log.gameObject);
            foreach(Transform t in bridge.Cast<Transform>().ToArray())if(t.name=="DeckPlank"&&t.localPosition.z>Near&&t.localPosition.z<Far)Object.DestroyImmediate(t.gameObject);
            // Split all spanning timbers and rails, so the opening is visibly missing across the whole bridge.
            foreach(Transform t in bridge.Cast<Transform>().ToArray())if(t.name=="LongitudinalSupport"||t.name=="SimpleRail")
            {float start=t.localPosition.z-t.localScale.z*.5f,end=t.localPosition.z+t.localScale.z*.5f;var other=Object.Instantiate(t.gameObject,bridge).transform;Resize(t,start,Near);Resize(other,Far,end);}
            foreach(var c in bridge.GetComponents<BoxCollider>())Object.DestroyImmediate(c);
            foreach(var span in new[]{new Vector2(67,Near),new Vector2(Far,77)})
            {
                var c=bridge.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,2.025f,(span.x+span.y)*.5f);c.size=new Vector3(8.4f,.15f,span.y-span.x);
                foreach(float side in new[]{-1f,1f}){var rail=bridge.gameObject.AddComponent<BoxCollider>();rail.center=new Vector3(side*4.12f,2.55f,(span.x+span.y)*.5f);rail.size=new Vector3(.2f,1.1f,span.y-span.x);}
            }
            var marker=new GameObject("JumpGap_1p2m");marker.transform.SetParent(bridge,false);
            // Broad, flush edge timbers clearly mark takeoff and landing without adding a hurdle.
            foreach(float z in new[]{Near-.18f,Far+.18f})
            {var edge=GameObject.CreatePrimitive(PrimitiveType.Cube);edge.name="LandingEdgeTimber";edge.transform.SetParent(bridge,false);edge.transform.localPosition=new Vector3(0,2.075f,z);edge.transform.localScale=new Vector3(8.4f,.05f,.34f);Object.DestroyImmediate(edge.GetComponent<Collider>());edge.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/Jungle/NaturalVisuals/WeatheredEarth.mat");}
            var ground=root.Find("Environment/Terrain_Ground/JungleGround");var visual=ground.GetComponent<MeshFilter>();var collision=ground.GetComponent<MeshCollider>();
            visual.sharedMesh=Cut(visual.sharedMesh,"GapGroundVisual");collision.sharedMesh=Cut(collision.sharedMesh,"GapGroundCollision");
            var road=root.Find("Environment/JunglePath/ContinuousMainRoad").GetComponent<MeshFilter>();road.sharedMesh=Cut(road.sharedMesh,"GapRoadVisual");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);JungleScenicPolish.Validate();
        }
        static void Resize(Transform t,float start,float end){var p=t.localPosition;var s=t.localScale;p.z=(start+end)*.5f;s.z=end-start;t.localPosition=p;t.localScale=s;}
        public static void ValidateGeometry(Transform root)
        {
            if(!Exists(root))throw new Exception("Expected bridge gap missing");Physics.SyncTransforms();int empty=0,landings=0;
            var bridge=root.Find("Environment/JungleScenicPolish/WideWoodenBridge");if(bridge.Find("FallenLog_JumpObstacle")!=null)throw new Exception("Bridge log still exists");
            for(float x=-4.15f;x<=4.16f;x+=.415f)for(float z=Near+.05f;z<Far;z+=.1f)
            {if(Physics.Raycast(new Vector3(x,5,z),Vector3.down,30,~0,QueryTriggerInteraction.Ignore))throw new Exception("Floor/collider beneath gap at "+x+","+z);empty++;}
            foreach(float x in new[]{-3.3f,-1.5f,0,1.5f,3.3f})foreach(float z in new[]{67.5f,70f,Near-.1f,Far+.1f,74f,76.5f})
            {if(!Physics.Raycast(new Vector3(x,5,z),Vector3.down,out var hit,5,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-2.1f)>.01f)throw new Exception("Landing support missing");if(Physics.CheckCapsule(new Vector3(x,2.5f,z),new Vector3(x,3.7f,z),.3f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Landing clearance blocked");landings++;}
            Debug.Log($"BRIDGE GAP PASS: 1.2m opening; {empty} no-floor rays; {landings} supported landing/clearance samples; log removed.");
        }
        public static void ApplyBatch()=>EditorApplication.delayCall+=()=>{try{Apply();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
    }
}
