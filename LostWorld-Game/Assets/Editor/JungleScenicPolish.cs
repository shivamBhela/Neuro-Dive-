using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LostWorlds.Person2.Interaction;
using Object=UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    public static class JungleScenicPolish
    {
        const string Scene="Assets/Scenes/Person2/Jungle_Prototype.unity";
        const string Art="Assets/Art/Environments/Jungle/ScenicPolish";
        const string Natural="Assets/Art/Environments/Jungle/NaturalVisuals/";
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Natural+name+".mat");
        static GameObject Shape(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool solid=false)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static Transform Group(string name,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static void SetStone(Transform t,Mesh mesh,Material material)
        {
            if(t==null)throw new Exception("Expected interaction visual missing");
            t.GetComponent<MeshFilter>().sharedMesh=mesh;t.GetComponent<Renderer>().sharedMaterial=material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(t.GetComponent<MeshFilter>());PrefabUtility.RecordPrefabInstancePropertyModifications(t.GetComponent<Renderer>());
        }
        static float Depression(Vector3 v)=>.85f*Mathf.Clamp01((4-Mathf.Abs(v.z-72))/1.5f)*Mathf.Clamp01((16-Mathf.Abs(v.x))/3);
        static void RefineWater(Transform group)
        {
            var water=group.Find("ShallowStreamAndBanks/StillWater_NoPhysics");if(water==null)throw new Exception("Stream visual missing");
            string path=Art+"/StreamRibbon.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var vertices=new Vector3[58];var uv=new Vector2[58];var triangles=new int[28*6];
                for(int i=0;i<=28;i++)
                {
                    float x=i-14;float center=Mathf.Sin(x*.41f)*.23f;float width=(1.75f+.18f*Mathf.Sin(x*.73f))*Mathf.Clamp01((14-Mathf.Abs(x))/1.2f);
                    vertices[i*2]=new Vector3(x,0,center-width);vertices[i*2+1]=new Vector3(x,0,center+width);uv[i*2]=new Vector2(i*.2f,0);uv[i*2+1]=new Vector2(i*.2f,1);
                    if(i==28)continue;int a=i*2,j=i*6;triangles[j]=a;triangles[j+1]=a+1;triangles[j+2]=a+2;triangles[j+3]=a+2;triangles[j+4]=a+1;triangles[j+5]=a+3;
                }
                mesh=new Mesh{name="StreamRibbon"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            water.localScale=Vector3.one;water.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=water.GetComponent<Renderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
            var material=renderer.sharedMaterial;material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Natural+"WeatheredRock_Normal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.13f);EditorUtility.SetDirty(material);
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Apply Scenic Bridge Polish")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(Scene);var root=scene.GetRootGameObjects().Single().transform;
            if(root.Find("Environment/JungleScenicPolish")!=null){RefineWater(root.Find("Environment/JungleScenicPolish"));AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Validate();return;}
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            var rock=AssetDatabase.LoadAssetAtPath<Mesh>(Natural+"OrganicRock.asset");
            var demo=root.Find(P4InteractionDemoBuilder.DemoName);
            SetStone(demo.Find("GRAB_Station_G/SourceStand"),rock,Mat("AgedLimestone"));
            SetStone(demo.Find("GRAB_Station_G/HoldTargetStand"),rock,Mat("WeatheredRock"));
            SetStone(demo.GetComponentInChildren<GrabInteractable>(true).transform,rock,Mat("SoftMoss"));
            var activate=demo.GetComponentInChildren<ActivateInteractable>(true).transform;
            SetStone(activate.Find("Pedestal"),rock,Mat("AgedLimestone"));
            SetStone(activate.Find("ActivationOrb"),rock,Mat("WeatheredRock"));
            // Only unused visual locator discs, never their anchors or functional components.
            foreach(var t in root.Find("FutureGameplayPoints").GetComponentsInChildren<Transform>().ToArray())
                if(t.name=="LocationMarker_VisualOnly_NoTrigger"&&t.GetComponents<MonoBehaviour>().Length==0&&t.GetComponent<Collider>()==null)Object.DestroyImmediate(t.gameObject);
            var group=Group("JungleScenicPolish",root.Find("Environment"));var bridge=Group("WideWoodenBridge",group);
            var wood=Mat("RoughBark");var stone=Mat("WeatheredRock");
            // Flat ten-metre deck, 8.4m wide. One simple collider; planks have no seams in collision.
            for(int i=0;i<25;i++)Shape("DeckPlank",bridge,PrimitiveType.Cube,new Vector3(0,2.025f,67.2f+i*.4f),new Vector3(8.4f,.15f,.395f),wood);
            var deck=bridge.gameObject.AddComponent<BoxCollider>();deck.center=new Vector3(0,2.025f,72);deck.size=new Vector3(8.4f,.15f,10);
            foreach(float side in new[]{-1f,1f})
            {
                Shape("LongitudinalSupport",bridge,PrimitiveType.Cube,new Vector3(side*3.5f,1.72f,72),new Vector3(.32f,.38f,10),wood);
                for(int i=0;i<6;i++)Shape("RailingPost",bridge,PrimitiveType.Cube,new Vector3(side*4.12f,2.55f,67.2f+i*1.92f),new Vector3(.2f,1.1f,.2f),wood);
                foreach(float y in new[]{2.5f,3.02f})Shape("SimpleRail",bridge,PrimitiveType.Cube,new Vector3(side*4.12f,y,72),new Vector3(.15f,.13f,9.8f),wood);
                var rail=bridge.gameObject.AddComponent<BoxCollider>();rail.center=new Vector3(side*4.12f,2.55f,72);rail.size=new Vector3(.2f,1.1f,9.8f);
            }
            // Short blended approaches avoid a step at either end of the deck.
            foreach(float z in new[]{66.7f,77.3f})Shape("EarthApproach",bridge,PrimitiveType.Cube,new Vector3(0,2.02f,z),new Vector3(8.4f,.1f,.6f),Mat("WeatheredEarth"));
            var ground=root.Find("Environment/Terrain_Ground/JungleGround");var original=ground.GetComponent<MeshFilter>().sharedMesh;
            var visual=Object.Instantiate(original);visual.name="ShallowStreamGround";visual.vertices=visual.vertices.Select(v=>v-Vector3.up*Depression(v)).ToArray();visual.RecalculateNormals();visual.RecalculateTangents();visual.RecalculateBounds();AssetDatabase.CreateAsset(visual,Art+"/StreamGroundVisual.asset");ground.GetComponent<MeshFilter>().sharedMesh=visual;
            // Retain the existing physical road under the deck; slope the stream sides only.
            var collision=Object.Instantiate(ground.GetComponent<MeshCollider>().sharedMesh);collision.name="StreamBanksCollision";
            collision.vertices=collision.vertices.Select(v=>v-Vector3.up*Depression(v)*Mathf.Clamp01((Mathf.Abs(v.x)-4)/2)).ToArray();collision.RecalculateNormals();collision.RecalculateBounds();AssetDatabase.CreateAsset(collision,Art+"/StreamBanksCollision.asset");ground.GetComponent<MeshCollider>().sharedMesh=collision;
            var stream=Group("ShallowStreamAndBanks",group);
            var water=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="QuietStream",enableInstancing=true};water.SetColor("_BaseColor",new Color(.18f,.34f,.31f));water.SetFloat("_Smoothness",.65f);water.SetFloat("_Metallic",0);AssetDatabase.CreateAsset(water,Art+"/QuietStream.mat");
            Shape("StillWater_NoPhysics",stream,PrimitiveType.Cube,new Vector3(0,1.37f,72),new Vector3(28,.025f,4),water);
            foreach(float side in new[]{-1f,1f})for(int i=0;i<7;i++)
            {
                float x=side*(5.5f+i*1.2f);float z=i%2==0?68.3f:75.7f;
                var bank=Shape("StreamBankStone",stream,PrimitiveType.Sphere,new Vector3(x,1.9f,z),new Vector3(1.5f,.65f,1.1f),stone);bank.GetComponent<MeshFilter>().sharedMesh=rock;
                var fern=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/JunglePrototype/FernCluster.prefab");var bush=(GameObject)PrefabUtility.InstantiatePrefab(fern,scene);bush.name="BankUnderstory";bush.transform.SetParent(stream,false);bush.transform.position=new Vector3(x,2,z+(z<72?-1.2f:1.2f));bush.transform.localScale=Vector3.one*.85f;
                foreach(var f in bush.GetComponentsInChildren<MeshFilter>()){f.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Natural+"CurvedLeaf.asset");PrefabUtility.RecordPrefabInstancePropertyModifications(f);}
                foreach(var r in bush.GetComponentsInChildren<Renderer>()){r.sharedMaterial=Mat("Understory");PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(bush.transform);
            }
            foreach(float side in new[]{-1f,1f})
            {
                var log=Shape("ScenicFallenLog",stream,PrimitiveType.Cylinder,new Vector3(side*10,2.25f,side<0?65:80),new Vector3(.6f,2,.6f),wood);log.transform.rotation=Quaternion.Euler(85,side*35,0);
                for(int i=0;i<4;i++){var s=Shape("StartEarthStone",group,PrimitiveType.Sphere,new Vector3(side*(6.9f+i%2*.7f),.12f,-1+i*1.7f),new Vector3(1.6f,.3f,1.1f),Mat("AgedLimestone"));s.GetComponent<MeshFilter>().sharedMesh=rock;}
            }
            RefineWater(group);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Validate();
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Validate Scenic Bridge")]
        public static void Validate()
        {
            JungleVisualUpgrade.Validate();P4InteractionDemoBuilder.ValidateSetup();
            var root=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single().transform;
            var bridge=root.Find("Environment/JungleScenicPolish/WideWoodenBridge");if(bridge==null)throw new Exception("Bridge missing");Physics.SyncTransforms();int samples=0;
            for(int z=66;z<=78;z++)foreach(float x in new[]{-3.3f,-1.7f,0,1.7f,3.3f})
            {
                if(JungleBridgeGap.Exists(root)&&JungleBridgeGap.InOpening(x,z))continue;
                var log=bridge.Find("FallenLog_JumpObstacle");var logCollider=log!=null?log.GetComponent<Collider>():null;
                var hits=Physics.RaycastAll(new Vector3(x,5,z),Vector3.down,5,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider!=logCollider).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0||Mathf.Abs(hits[0].point.y-2)>.12f)throw new Exception("Bridge walking surface not continuous: "+x+","+z);
                if(Physics.OverlapCapsule(new Vector3(x,2.5f,z),new Vector3(x,3.7f,z),.3f,~0,QueryTriggerInteraction.Ignore).Any(c=>c!=logCollider))throw new Exception("Bridge clearance blocked");samples++;
            }
            var obstacle=bridge.Find("FallenLog_JumpObstacle");
            var group=bridge.parent;if(group.GetComponentsInChildren<Collider>().Length!=(JungleBridgeGap.Exists(root)?6:obstacle==null?3:4))throw new Exception("Unexpected decorative collider");
            if(obstacle!=null)
            {
                var c=obstacle.GetComponent<CapsuleCollider>();if(c==null||!c.enabled||c.isTrigger||Mathf.Abs(c.bounds.size.y-.45f)>.01f||Mathf.Abs(c.bounds.min.y-2.1f)>.01f)throw new Exception("Log collision/height invalid");
                foreach(float x in new[]{-.3f,0,.3f})if(!c.Raycast(new Ray(new Vector3(x,2.32f,71),Vector3.forward),out _,4))throw new Exception("Runner misses bridge log");
                foreach(float x in new[]{-1.8f,1.8f})if(Physics.CheckCapsule(new Vector3(x,2.5f,73),new Vector3(x,3.7f,73),.3f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Log side clearance blocked");
                if(Physics.CheckCapsule(new Vector3(0,2.91f,73),new Vector3(0,4.11f,73),.3f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Log overhead clearance blocked");
                Debug.Log("BRIDGE LOG PASS: 0.45m high, 2.4m wide, solid rounded collider; three running-line hits; two safe bypasses and overhead clearance.");
            }
            if(root.GetComponentsInChildren<GrabInteractable>(true).Length!=1||root.GetComponentsInChildren<ActivateInteractable>(true).Length!=1)throw new Exception("Interaction count changed");
            Debug.Log("JUNGLE SCENIC PASS: "+samples+" bridge surface/clearance samples; original interaction components retained; 10 tokens and continuous road preserved.");
        }
        public static void ApplyBatch()=>EditorApplication.delayCall+=()=>{try{Apply();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
        [MenuItem("Tools/Lost Worlds/Jungle/Add Bridge Jump Log")]
        public static void AddBridgeLog()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(Scene);var root=scene.GetRootGameObjects().Single().transform;var bridge=root.Find("Environment/JungleScenicPolish/WideWoodenBridge");
            if(bridge==null)throw new Exception("Existing scenic bridge required");
            if(JungleBridgeGap.Exists(root))throw new InvalidOperationException("Current bridge design uses a gap; do not restore the retired log.");
            if(bridge.Find("FallenLog_JumpObstacle")==null)
            {
                var log=Shape("FallenLog_JumpObstacle",bridge,PrimitiveType.Capsule,new Vector3(0,2.325f,73),new Vector3(.45f,1.2f,.45f),Mat("RoughBark"),true);
                log.transform.localRotation=Quaternion.Euler(0,0,90);
                EditorSceneManager.SaveScene(scene);
            }
            Validate();
        }
        public static void AddLogBatch()=>EditorApplication.delayCall+=()=>{try{AddBridgeLog();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
        public static void CaptureBatch()=>EditorApplication.delayCall+=()=>{try{Validate();Capture();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
        static void Capture()
        {
            var root=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single();var camera=root.GetComponentsInChildren<Camera>().First();
            var clone=Object.Instantiate(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);var previous=QualitySettings.renderPipeline;var active=RenderTexture.active;
            var target=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            var pos=camera.transform.position;var rot=camera.transform.rotation;bool ortho=camera.orthographic;float fov=camera.fieldOfView;
            try
            {
                clone.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;clone.useSRPBatcher=false;QualitySettings.renderPipeline=clone;camera.orthographic=false;camera.fieldOfView=65;
                Directory.CreateDirectory("Documentation/JungleBridge");
                for(int i=0;i<2;i++)
                {
                    camera.transform.position=i==0?new Vector3(0,11,60):new Vector3(-12,6,12);camera.transform.LookAt(i==0?new Vector3(0,1.7f,73):new Vector3(-5,1,20));
                    for(int frame=0;frame<3;frame++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});
                    RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Documentation/JungleBridge/"+(i==0?"Bridge":"Interaction")+".png",image.EncodeToPNG());
                }
            }
            finally{camera.transform.SetPositionAndRotation(pos,rot);camera.orthographic=ortho;camera.fieldOfView=fov;QualitySettings.renderPipeline=previous;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(clone);}
        }
    }
}
