using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Polish;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
    public static class JunglePortalUnlockBuilder
    {
        const string Folder="Assets/Art/Environments/Jungle/PortalUnlock";
        static void Ref(Object target,string field,Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static Transform Shape(string name,Transform parent,PrimitiveType type,Vector3 p,Vector3 scale,Material material)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());return go.transform;}
        static AudioClip Chime()
        {
            string path=Folder+"/GentleUnlock.wav";const int rate=22050,count=17640;
            using(var writer=new BinaryWriter(File.Create(path)))
            {writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);for(int i=0;i<count;i++){float t=i/(float)rate;float envelope=Mathf.Min(t/.08f,1)*Mathf.Pow(1-i/(float)count,2);float wave=(Mathf.Sin(t*2*Mathf.PI*440)*.65f+Mathf.Sin(t*2*Mathf.PI*660)*.35f)*envelope*.35f;writer.Write((short)(wave*32767));}}
            AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Setup Portal Unlock Lever")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);var root=scene.GetRootGameObjects().Single().transform;
            var existing=root.GetComponentInChildren<PortalUnlockMechanism>(true);
            if(existing!=null){if(existing.GetComponent<PortalUnlockKeyboardInput>()==null)existing.gameObject.AddComponent<PortalUnlockKeyboardInput>();if(existing.GetComponent<PortalAutoRunPause>()==null)existing.gameObject.AddComponent<PortalAutoRunPause>();EditorSceneManager.SaveScene(scene);Validate();return;}
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var realm=root.GetComponentInChildren<RealmBindings>();var portal=root.GetComponentInChildren<JourneyRealm>().Portal;
            var lever=new GameObject("JunglePortalLever_ACTIVATE");lever.transform.SetParent(root,false);lever.transform.position=new Vector3(5,3.63f,103);
            var gate=lever.AddComponent<PortalUnlockMechanism>();Ref(gate,"portal",portal);Ref(gate,"polish",root.GetComponentInChildren<RealmPolish>());Ref(realm,"exitGate",gate);
            lever.AddComponent<PortalUnlockKeyboardInput>();
            var zone=lever.AddComponent<BoxCollider>();zone.isTrigger=true;zone.center=new Vector3(-5,1,0);zone.size=new Vector3(12,4,20);
            lever.AddComponent<PortalAutoRunPause>();
            var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/Jungle/NaturalVisuals/RoughBark.mat");var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/Jungle/NaturalVisuals/AgedLimestone.mat");
            Shape("AncientStoneFoot",lever.transform,PrimitiveType.Cube,new Vector3(0,.2f,0),new Vector3(1.8f,.4f,1.5f),stone);
            foreach(float x in new[]{-.48f,.48f})Shape("WeatheredWoodSupport",lever.transform,PrimitiveType.Cube,new Vector3(x,.8f,0),new Vector3(.25f,1.3f,.4f),wood);
            var pivot=new GameObject("LeverPivot").transform;pivot.SetParent(lever.transform,false);pivot.localPosition=new Vector3(0,1.1f,0);
            Shape("WoodenLever",pivot,PrimitiveType.Cylinder,new Vector3(0,.55f,0),new Vector3(.18f,.7f,.18f),wood);
            var grip=Shape("WideHandGrip",pivot,PrimitiveType.Cylinder,new Vector3(0,1.2f,0),new Vector3(.25f,.5f,.25f),wood);grip.localRotation=Quaternion.Euler(0,0,90);Ref(gate,"handle",pivot);
            var prompt=new GameObject("PortalActivationPrompt").AddComponent<TextMesh>();prompt.transform.SetParent(lever.transform,false);prompt.transform.localPosition=new Vector3(-2,3,-.5f);prompt.text="Press E to Activate Portal";prompt.anchor=TextAnchor.MiddleCenter;prompt.alignment=TextAlignment.Center;prompt.fontSize=64;prompt.characterSize=.065f;prompt.color=new Color(1,.96f,.82f);prompt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");prompt.GetComponent<Renderer>().sharedMaterial=prompt.font.material;Ref(gate,"prompt",prompt);
            Shape("PromptBacking",lever.transform,PrimitiveType.Cube,new Vector3(-2,3,-.35f),new Vector3(5.4f,.65f,.1f),wood);
            var audio=lever.AddComponent<AudioSource>();audio.playOnAwake=false;audio.loop=false;audio.volume=.06f;audio.spatialBlend=.5f;audio.minDistance=3;audio.maxDistance=20;audio.clip=Chime();Ref(gate,"revealAudio",audio);
            var test=lever.AddComponent<InteractionInteractor>();test.Range=3.5f;test.Reference=lever.transform;Ref(gate,"testInteractor",test);
            var threshold=root.Find("Environment/Landmarks/JungleExit_ThresholdOnly");if(threshold==null)threshold=root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="JungleExit_ThresholdOnly");
            if(threshold!=null){var so=new SerializedObject(gate);var a=so.FindProperty("revealObjects");a.arraySize=1;a.GetArrayElementAtIndex(0).objectReferenceValue=threshold.gameObject;so.ApplyModifiedPropertiesWithoutUndo();}
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Validate();
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Validate Portal Unlock Setup")]
        public static void Validate()
        {
            var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);var root=scene.GetRootGameObjects().Single();var gate=root.GetComponentInChildren<PortalUnlockMechanism>(true);if(gate==null||gate.Portal==null)throw new Exception("Portal unlock references missing");
            var s=new SerializedObject(root.GetComponentInChildren<RealmBindings>());if(s.FindProperty("exitGate").objectReferenceValue!=gate)throw new Exception("P11 exit gate not wired");
            if(gate.GetComponentsInChildren<Collider>(true).Any(c=>!c.isTrigger))throw new Exception("Lever blocks the path");
            if(gate.GetComponent<BoxCollider>().size.z<16||gate.GetComponent<AudioSource>().volume>.1f)throw new Exception("Lever range/audio outside intended limits");
            Debug.Log("PORTAL UNLOCK SETUP PASS: Jungle-only ACTIVATE mechanism, generous trigger, existing P11 gate and portal linked.");
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Play Portal Unlock Demo")]
        public static void Play()=>P125JourneyBuilder.Play();
        public static void SetupBatch()=>EditorApplication.delayCall+=()=>{try{Setup();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
    }
}
