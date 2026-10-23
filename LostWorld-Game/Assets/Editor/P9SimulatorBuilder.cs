using System;
using System.IO;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
    public static class P9SimulatorBuilder
    {
        public const string ScenePath="Assets/Scenes/Test/P9_HardwareSimulatorDemo.unity";
        public const string PrefabPath="Assets/Prefabs/Hardware/P9/HardwareCommandSimulator.prefab";
        [MenuItem("Tools/Lost Worlds/P9/Setup Hardware Simulator Demo")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!AssetDatabase.IsValidFolder("Assets/Prefabs/Hardware"))AssetDatabase.CreateFolder("Assets/Prefabs","Hardware");
            if(!AssetDatabase.IsValidFolder("Assets/Prefabs/Hardware/P9"))AssetDatabase.CreateFolder("Assets/Prefabs/Hardware","P9");
            if(!File.Exists(PrefabPath))
            {
                var root=new GameObject("HardwareCommandSimulator_TEST_ONLY");
                try{var bus=root.AddComponent<CommandChannel>();root.AddComponent<HardwareCommandSimulator>().Bind(bus);root.AddComponent<InteractionCommandReceiver>();root.AddComponent<RouteCommandReceiver>();PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);}
                finally{Object.DestroyImmediate(root);}
            }
            if(File.Exists(ScenePath))return;
            if(!AssetDatabase.CopyAsset(P8EffortBuilder.ScenePath,ScenePath))throw new IOException("P8 demo must exist; could not create separate P9 demo.");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var stations=Object.FindAnyObjectByType<P7HudDemoControls>();stations.enabled=false;
            stations.Interactions.enabled=false;stations.RouteInput.enabled=false;
            var hud=stations.GetComponentInChildren<Person2Hud>();
            var rig=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var driver=rig.AddComponent<P9SimulatorDemo>();var data=new SerializedObject(driver);
            data.FindProperty("stations").objectReferenceValue=stations;data.FindProperty("channel").objectReferenceValue=rig.GetComponent<CommandChannel>();
            data.FindProperty("interactions").objectReferenceValue=rig.GetComponent<InteractionCommandReceiver>();data.FindProperty("routes").objectReferenceValue=rig.GetComponent<RouteCommandReceiver>();
            var hint=hud.transform.Find("DemoControls").GetComponent<Text>();hint.fontSize=18;hint.rectTransform.sizeDelta=new Vector2(940,76);
            data.FindProperty("hints").objectReferenceValue=hint;data.ApplyModifiedPropertiesWithoutUndo();
            var panel=new GameObject("LatestSimulatedCommand",typeof(RectTransform),typeof(Image));panel.transform.SetParent(hud.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-294);rect.sizeDelta=new Vector2(680,56);
            panel.GetComponent<Image>().color=new Color(.055f,.12f,.12f,.96f);panel.GetComponent<Image>().raycastTarget=false;
            var label=new GameObject("Command",typeof(RectTransform),typeof(Text)).GetComponent<Text>();label.transform.SetParent(rect,false);label.rectTransform.sizeDelta=new Vector2(640,48);
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=24;label.color=new Color(.96f,.95f,.90f);label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            panel.AddComponent<CommandDebugFeedback>().Bind(rig.GetComponent<CommandChannel>(),label);
            driver.ShowView(1);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Lost Worlds/P9/Play Hardware Simulator Demo")]
        public static void Play()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(ScenePath))Setup();EditorSceneManager.OpenScene(ScenePath);EditorApplication.isPlaying=true;
        }
    }
}
