using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.UI;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
    public static class P10AdapterBuilder
    {
        public const string ScenePath="Assets/Scenes/Test/P10_HardwareAdapterDemo.unity";
        public const string PrefabPath="Assets/Prefabs/Hardware/P10/ProcessedHardwareAdapter.prefab";
        [MenuItem("Tools/Lost Worlds/P10/Setup Hardware Adapter Demo")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!AssetDatabase.IsValidFolder("Assets/Prefabs/Hardware/P10"))AssetDatabase.CreateFolder("Assets/Prefabs/Hardware","P10");
            if(!File.Exists(PrefabPath))
            {var go=new GameObject("ProcessedHardwareAdapter");try{go.AddComponent<ProcessedHardwareAdapter>();PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);}finally{Object.DestroyImmediate(go);}}
            if(File.Exists(ScenePath))return;
            if(!AssetDatabase.CopyAsset(P9SimulatorBuilder.ScenePath,ScenePath))throw new IOException("P9 demo must exist.");
            var scene=EditorSceneManager.OpenScene(ScenePath);var channel=Object.FindAnyObjectByType<CommandChannel>();
            var goAdapter=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));var adapter=goAdapter.GetComponent<ProcessedHardwareAdapter>();adapter.Bind(channel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(adapter);
            var client=new GameObject("LocalTcpTestClient_TEST_ONLY").AddComponent<P10LocalTestClient>();client.Bind(adapter);
            var hud=Object.FindAnyObjectByType<Person2Hud>();
            var panel=new GameObject("HardwareConnectionStatus",typeof(RectTransform),typeof(Image));panel.transform.SetParent(hud.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(32,-110);rect.sizeDelta=new Vector2(380,264);
            panel.GetComponent<Image>().color=new Color(.055f,.12f,.12f,.96f);panel.GetComponent<Image>().raycastTarget=false;
            var text=Label(rect,"Connection",new Vector2(348,118),new Vector2(16,-12),21);
            panel.AddComponent<HardwareAdapterStatus>().Bind(adapter,text);
            var hint=Label(rect,"TcpTestControls",new Vector2(348,116),new Vector2(16,-142),18);
            hint.text="LOCAL TCP TEST\nF4 Connect / F11 Disconnect\nF5 LEFT / F6 RIGHT / F7 JUMP\nF8 GRAB / F9 ACTIVATE\nF10 Invalid command";
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static Text Label(Transform parent,string name,Vector2 size,Vector2 offset,int fontSize)
        {
            var label=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();label.transform.SetParent(parent,false);
            label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(0,1);label.rectTransform.pivot=new Vector2(0,1);label.rectTransform.sizeDelta=size;label.rectTransform.anchoredPosition=offset;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=fontSize;label.color=new Color(.96f,.95f,.9f);label.raycastTarget=false;label.supportRichText=false;return label;
        }
        [MenuItem("Tools/Lost Worlds/P10/Play Hardware Adapter Demo")]
        public static void Play(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;if(!File.Exists(ScenePath))Setup();EditorSceneManager.OpenScene(ScenePath);EditorApplication.isPlaying=true;}
    }
}
