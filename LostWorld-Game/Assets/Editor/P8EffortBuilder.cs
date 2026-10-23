using System;
using System.IO;
using LostWorlds.Person2.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
    public static class P8EffortBuilder
    {
        public const string ScenePath="Assets/Scenes/Test/P8_EffortDemo.unity";
        public const string PrefabPath="Assets/Prefabs/UI/P8/EffortPanel.prefab";
        [MenuItem("Tools/Lost Worlds/P8/Setup Effort Rest Demo")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!AssetDatabase.IsValidFolder("Assets/Prefabs/UI/P8"))AssetDatabase.CreateFolder("Assets/Prefabs/UI","P8");
            if(!File.Exists(PrefabPath)) MakePanel();
            if(File.Exists(ScenePath))return;
            if(!File.Exists(P7HudBuilder.ScenePath))throw new InvalidOperationException("P7 demo must exist first.");
            // Copy the dedicated test scene only; no environment scenes are merged or changed.
            if(!AssetDatabase.CopyAsset(P7HudBuilder.ScenePath,ScenePath))throw new IOException("Cannot copy P7 demo.");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var hud=Object.FindAnyObjectByType<Person2Hud>();
            var panel=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),hud.transform);
            var input=panel.AddComponent<P8EffortTestInput>();input.Bind(panel.GetComponent<EffortModel>());
            var hint=MakeText("EffortTestControls",hud.transform,new Vector2(620,40));
            hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,1);
            hint.rectTransform.pivot=new Vector2(.5f,1);hint.rectTransform.anchoredPosition=new Vector2(0,-112);
            hint.fontSize=20;hint.alignment=TextAnchor.MiddleCenter;
            hint.text="TEST: Page Up +10  •  Page Down -10  •  Backspace reset";
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static Text MakeText(string name,Transform parent,Vector2 size)
        {
            var label=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent,false);label.rectTransform.sizeDelta=size;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=24;
            label.color=new Color(.96f,.95f,.90f);label.raycastTarget=false;label.supportRichText=false;return label;
        }
        static void MakePanel()
        {
            var go=new GameObject("EffortRestPanel",typeof(RectTransform),typeof(Image));
            try
            {
                var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
                rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-164);rect.sizeDelta=new Vector2(620,112);
                var bg=go.GetComponent<Image>();bg.color=new Color(.055f,.12f,.12f,.96f);bg.raycastTarget=false;
                var model=go.AddComponent<EffortModel>();var hud=go.AddComponent<EffortHud>();
                var label=MakeText("EffortStatus",rect,new Vector2(580,72));label.alignment=TextAnchor.MiddleCenter;label.rectTransform.anchoredPosition=new Vector2(0,8);
                var track=new GameObject("EffortBarTrack",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();track.SetParent(rect,false);
                track.sizeDelta=new Vector2(572,7);track.anchoredPosition=new Vector2(0,-39);track.GetComponent<Image>().color=new Color(.23f,.34f,.32f);track.GetComponent<Image>().raycastTarget=false;
                var bar=new GameObject("Value",typeof(RectTransform),typeof(Image)).GetComponent<Image>();bar.transform.SetParent(track,false);
                bar.rectTransform.anchorMin=Vector2.zero;bar.rectTransform.anchorMax=Vector2.one;bar.rectTransform.offsetMin=bar.rectTransform.offsetMax=Vector2.zero;bar.raycastTarget=false;
                var data=new SerializedObject(hud);data.FindProperty("label").objectReferenceValue=label;data.FindProperty("bar").objectReferenceValue=bar;data.ApplyModifiedPropertiesWithoutUndo();hud.Bind(model);
                PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [MenuItem("Tools/Lost Worlds/P8/Play Effort/Rest Demo")]
        public static void Play()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(ScenePath))Setup();
            EditorSceneManager.OpenScene(ScenePath);EditorApplication.isPlaying=true;
        }
    }
}

