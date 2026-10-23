using System;
using System.IO;
using LostWorlds.Person2.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    public static class P4InteractionDemoBuilder
    {
        public const string JunglePath = "Assets/Scenes/Person2/Jungle_Prototype.unity";
        public const string DemoName = "P4InteractionDemo_TEST_ONLY";
        const string Prefabs = "Assets/Prefabs/Interactions/P4";
        const string Materials = "Assets/Art/Materials/P4Interactions";

        [MenuItem("Tools/Lost Worlds/P4/Setup Jungle Interaction Demos")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before setup.");
            var scene = SceneManager.GetSceneByPath(JunglePath);
            if (!scene.isLoaded)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)) throw new InvalidOperationException("Save the unnamed scene before opening Jungle additively.");
                scene = EditorSceneManager.OpenScene(JunglePath, OpenSceneMode.Additive);
            }
            if (scene.isDirty) throw new InvalidOperationException("Save your Jungle edits before adding P4 demos.");
            var root = scene.GetRootGameObjects()[0].transform;
            if (root.Find(DemoName) != null) { ValidateSetup(); Debug.Log("Existing P4 demos preserved; no rebuild."); return; }
            var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
            try
            {
                var grabAnchor = root.Find("FutureGameplayPoints/GrabPoint");
                var activateAnchor = root.Find("FutureGameplayPoints/ActivatePoint");
                if (grabAnchor == null || activateAnchor == null) throw new InvalidOperationException("Existing Jungle locations are missing.");
                Folder(Prefabs); Folder(Materials);
                var stone = Material("DemoStone", new Color(.53f,.57f,.53f));
                var amber = Material("ReadyAmber", new Color(.95f,.61f,.17f));
                var teal = Material("SuccessTeal", new Color(.20f,.90f,.65f), .35f);
                var blue = Material("TargetBlue", new Color(.20f,.48f,.65f));
                var demo = Group(DemoName, root);
                CreatePrefabs(demo, stone, amber, teal);
                var grabStation = Group("GRAB_Station_G", demo); grabStation.position = grabAnchor.position - Vector3.up * .15f;
                Box("SourceStand", grabStation, new Vector3(0,.55f,0), new Vector3(2.2f,1.1f,2.2f), stone);
                Box("HoldTargetStand", grabStation, new Vector3(3,.55f,0), new Vector3(2.2f,1.1f,2.2f), blue);
                var hold = Group("HoldPoint", grabStation); hold.localPosition = new Vector3(3,1.65f,0);
                var grab = Instance("GrabDemoObject", grabStation); grab.localPosition = new Vector3(0,1.65f,0);
                var grabReference = Group("GrabReference_TEST_ONLY", grabStation); grabReference.localPosition = new Vector3(0,1.2f,-2);
                var grabInteractor = grabReference.gameObject.AddComponent<InteractionInteractor>(); BindScript(grabInteractor); grabInteractor.HoldPoint = hold; grabInteractor.Range = 3.5f;
                var activateStation = Group("ACTIVATE_Station_E", demo); activateStation.position = activateAnchor.position - Vector3.up * .15f;
                Instance("ActivateBeacon", activateStation);
                var activateReference = Group("ActivateReference_TEST_ONLY", activateStation); activateReference.localPosition = new Vector3(0,1.2f,-2);
                var activateInteractor = activateReference.gameObject.AddComponent<InteractionInteractor>(); BindScript(activateInteractor); activateInteractor.Range = 3.5f;
                var input = demo.gameObject.AddComponent<P4KeyboardTestInput>(); BindScript(input); input.GrabInteractor = grabInteractor; input.ActivateInteractor = activateInteractor;
                Camera("P4_GRAB_View_TEST_ONLY", grabStation, new Vector3(6,5,-8), new Vector3(1.5f,1.1f,0), new Rect(0,0,.5f,1), 10);
                Camera("P4_ACTIVATE_View_TEST_ONLY", activateStation, new Vector3(0,5,-8), new Vector3(0,1.9f,0), new Rect(.5f,0,.5f,1), 11);
                ValidateSetup(); AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene, JunglePath)) throw new IOException("Jungle scene could not be saved.");
                Debug.Log("P4 Jungle demos saved. Play, focus Game, G = grab (left), E = activate (right). Disable the P4 demo root to restore the original overview.");
            }
            finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }

        static void Folder(string path)
        { if (AssetDatabase.IsValidFolder(path)) return; string parent = Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path)); }
        static Transform Group(string name, Transform parent)
        { var t = new GameObject(name).transform; t.SetParent(parent,false); return t; }
        static Material Material(string name, Color color, float emission = 0)
        {
            string path = Materials + "/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) throw new InvalidOperationException("Existing URP shader unavailable.");
            m = new Material(shader) { name = name }; m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",.2f);
            if (emission > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",color*emission); }
            AssetDatabase.CreateAsset(m,path); return m;
        }
        static Transform Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent,false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>()); return go.transform;
        }
        static Transform Box(string n,Transform p,Vector3 v,Vector3 s,Material m) => Shape(n,p,PrimitiveType.Cube,v,s,m);
        static Transform Instance(string name,Transform parent)
        { var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/"+name+".prefab"); return ((GameObject)PrefabUtility.InstantiatePrefab(prefab,parent)).transform; }
        static void Feedback(InteractableBase target,Renderer renderer,Material material)
        {
            BindScript(target);
            var serialized = new SerializedObject(target);
            var renderers = serialized.FindProperty("feedbackRenderers"); renderers.arraySize = 1; renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.FindProperty("successMaterial").objectReferenceValue = material; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static void BindScript(MonoBehaviour behaviour)
        {
            // Resolve the asset explicitly during fresh batch imports, before saving any
            // prefab/scene. A valid managed component alone does not prove its script GUID is wired.
            string path="Assets/Scripts/Interaction/"+behaviour.GetType().Name+".cs";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var script=AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if(script==null||script.GetClass()!=behaviour.GetType())throw new InvalidOperationException("Runtime script import is not ready: "+path);
            var serialized=new SerializedObject(behaviour);serialized.FindProperty("m_Script").objectReferenceValue=script;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static void CreatePrefabs(Transform temporaryParent,Material stone,Material amber,Material teal)
        {
            if (!File.Exists(Prefabs+"/GrabDemoObject.prefab"))
            {
                var item=Box("GrabDemoObject",temporaryParent,Vector3.zero,Vector3.one*1.1f,amber);
                try
                {
                    var body=item.gameObject.AddComponent<Rigidbody>(); body.isKinematic=true; body.useGravity=false;
                    var target=item.gameObject.AddComponent<GrabInteractable>(); Feedback(target,item.GetComponent<Renderer>(),teal);
                    PrefabUtility.SaveAsPrefabAsset(item.gameObject,Prefabs+"/GrabDemoObject.prefab");
                }
                finally { Object.DestroyImmediate(item.gameObject); }
            }
            if (!File.Exists(Prefabs+"/ActivateBeacon.prefab"))
            {
                var item=Group("ActivateBeacon",temporaryParent);
                try
                {
                    Box("Pedestal",item,new Vector3(0,.65f,0),new Vector3(2,1.3f,2),stone);
                    var button=Shape("ActivationOrb",item,PrimitiveType.Sphere,new Vector3(0,1.65f,0),Vector3.one*.85f,amber);
                    var glow=Group("ActivatedBeacon_VisibleOnSuccess",item);
                    Shape("SteadyBeacon",glow,PrimitiveType.Sphere,new Vector3(0,3,0),Vector3.one*1.7f,teal,false);
                    var light=glow.gameObject.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(.2f,1,.7f); light.intensity=3; light.range=8; light.shadows=LightShadows.None;
                    light.transform.localPosition=Vector3.zero; glow.gameObject.SetActive(false);
                    var target=item.gameObject.AddComponent<ActivateInteractable>(); target.ActivatedVisual=glow.gameObject; Feedback(target,button.GetComponent<Renderer>(),teal);
                    PrefabUtility.SaveAsPrefabAsset(item.gameObject,Prefabs+"/ActivateBeacon.prefab");
                }
                finally { Object.DestroyImmediate(item.gameObject); }
            }
        }
        static void Camera(string name,Transform parent,Vector3 position,Vector3 look,Rect rect,float depth)
        {
            var camera=Group(name,parent).gameObject.AddComponent<UnityEngine.Camera>(); camera.transform.localPosition=position; camera.transform.LookAt(parent.TransformPoint(look));
            camera.orthographic=true;camera.orthographicSize=4.4f;camera.rect=rect;camera.depth=depth;camera.nearClipPlane=.1f;camera.farClipPlane=60;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.30f,.39f,.36f);
        }
        public static Transform Demo()
        {
            var scene=SceneManager.GetSceneByPath(JunglePath); if(!scene.isLoaded)return null;
            foreach(var root in scene.GetRootGameObjects())if(root.name=="Jungle_Prototype")return root.transform.Find(DemoName);
            return null;
        }
        [MenuItem("Tools/Lost Worlds/P4/Validate Jungle Demo Setup")]
        public static void ValidateSetup()
        {
            var demo=Demo();if(demo==null)throw new InvalidOperationException("P4 demo is missing. Run setup first.");
            int grabs=demo.GetComponentsInChildren<GrabInteractable>(true).Length,activates=demo.GetComponentsInChildren<ActivateInteractable>(true).Length;
            if(grabs!=1||activates!=1)throw new InvalidOperationException($"Expected one demo of each type; found {grabs} GRAB and {activates} ACTIVATE components.");
            var input=demo.GetComponent<P4KeyboardTestInput>();if(input==null||input.GrabInteractor==null||input.ActivateInteractor==null||input.GrabInteractor.HoldPoint==null)throw new InvalidOperationException("Test input/reference wiring incomplete.");
            foreach(var component in demo.GetComponentsInChildren<Component>(true))if(component==null)throw new InvalidOperationException("Missing script in demo.");
            Debug.Log("P4 SETUP PASS: independent GRAB/ACTIVATE prefabs, references, hold point and temporary test input wired.");
        }
        [MenuItem("Tools/Lost Worlds/P4/Open Jungle Interaction Demo")]
        public static void Open()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(JunglePath,OpenSceneMode.Single);if(Demo()!=null)Selection.activeGameObject=Demo().gameObject;
        }
        public static void Capture(string suffix)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(pipeline==null)throw new InvalidOperationException("URP required.");
            var clone=Object.Instantiate(pipeline);var previous=QualitySettings.renderPipeline;var previousActive=RenderTexture.active;
            try
            {
                clone.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;clone.useSRPBatcher=false;QualitySettings.renderPipeline=clone;
                Directory.CreateDirectory("Documentation/P4");
                foreach(var camera in Demo().GetComponentsInChildren<UnityEngine.Camera>())
                {
                    var target=new RenderTexture(1000,1000,24);var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);var rect=camera.rect;
                    try
                    {
                        camera.rect=new Rect(0,0,1,1);
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});
                        RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();
                        File.WriteAllBytes("Documentation/P4/"+(camera.name.Contains("GRAB")?"Grab":"Activate")+"_"+suffix+".png",image.EncodeToPNG());
                    }
                    finally {camera.rect=rect;RenderTexture.active=previousActive;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
                }
            }
            finally {QualitySettings.renderPipeline=previous;RenderTexture.active=previousActive;Object.DestroyImmediate(clone);}
        }
    }
}
