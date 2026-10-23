using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using Object=UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    // One-time migration plus permanent absence/reference regression checks. No runtime behaviour.
    public static class RelicRemovalValidation
    {
        static readonly string[] RetiredFolders={"Assets/Prefabs/Collectibles/P5","Assets/Art/Collectibles/P5","Assets/Art/Materials/P5Relics","Assets/Scripts/Collectibles"};
        static string[] Scenes()=>AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes/Person2","Assets/Scenes/Test"}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).ToArray();
        static bool RetiredName(string name)=>name=="P5_RelicStation"||name=="RelicStatus"||(name.StartsWith("InteractionFocus_")&&name.EndsWith("Relic"))||(name.StartsWith("Relic_")&&name.EndsWith("_TEST_ONLY"));
        static void Clean(GameObject root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>RetiredName(t.name)).ToArray())
                if(t!=null)Object.DestroyImmediate(t.gameObject);
            foreach(var text in root.GetComponentsInChildren<Text>(true))
                if(text.text.IndexOf("relic",StringComparison.OrdinalIgnoreCase)>=0)
                {text.text=text.text.Replace(" / F2 Relics","");if(text.text.IndexOf("relic",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception("Unremoved visible text: "+text.name+" / "+text.text);}
        }
        static string TokenSnapshot(GameObject[] roots)=>string.Join("\n",roots.SelectMany(r=>r.GetComponentsInChildren<JourneyToken>(true)).Select(t=>t.TokenId+"|"+EditorJsonUtility.ToJson(t)+"|"+EditorJsonUtility.ToJson(t.transform)).OrderBy(s=>s));
        public static void MigrateBatch()=>EditorApplication.delayCall+=()=>{try{Migrate();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
        static void Migrate()
        {
            EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);
            foreach(var path in new[]{P7HudBuilder.HudPath,"Assets/Prefabs/Integration/P11/IntegrationSession.prefab"})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try{Clean(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(var path in Scenes())
            {
                var scene=EditorSceneManager.OpenScene(path);var roots=scene.GetRootGameObjects();var tokens=TokenSnapshot(roots);
                foreach(var root in roots)Clean(root);
                if(tokens!=TokenSnapshot(roots))throw new Exception("Token identity, position or reference changed: "+path);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            // Check external dependencies before deleting any retired source asset.
            var candidates=AssetDatabase.GetAllAssetPaths().Where(p=>RetiredFolders.Any(f=>p==f||p.StartsWith(f+"/"))).ToHashSet();
            foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!candidates.Contains(p)&&!AssetDatabase.IsValidFolder(p)))
                foreach(var dependency in AssetDatabase.GetDependencies(path,false))
                    if(candidates.Contains(dependency))throw new Exception("Retired asset still needed by "+path+": "+dependency);
            foreach(var folder in RetiredFolders)if(AssetDatabase.IsValidFolder(folder)&&!AssetDatabase.DeleteAsset(folder))throw new Exception("Cannot remove "+folder);
            AssetDatabase.SaveAssets();Debug.Log("Relic migration saved all realms/demo scenes and removed retired assets. Run validation after compilation.");
        }
        static void Inspect(GameObject root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(t.name.IndexOf("relic",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception("Obsolete collectible object: "+t.name);
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script: "+t.name);
            }
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                if(component==null)throw new Exception("Missing component");
                if(component.GetType().Name.IndexOf("Relic",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception("Obsolete component");
                var serialized=new SerializedObject(component);var property=serialized.GetIterator();
                while(property.NextVisible(true))
                    if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue==null&&property.objectReferenceEntityIdValue!=default)
                        throw new Exception("Missing reference: "+component.name+" / "+property.propertyPath);
            }
            foreach(var text in root.GetComponentsInChildren<Text>(true))
                if(text.text.IndexOf("relic",StringComparison.OrdinalIgnoreCase)>=0||text.text.Replace(" ","")=="0/3")throw new Exception("Obsolete HUD text");
        }
        [MenuItem("Tools/Lost Worlds/P5/Validate Relic Removal")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before structural validation.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var previous=EditorSceneManager.GetSceneManagerSetup();int tokens=0,portals=0,scenes=0;
            try
            {
                foreach(var folder in RetiredFolders)if(AssetDatabase.IsValidFolder(folder))throw new Exception("Retired assets remain: "+folder);
                foreach(var path in Scenes())
                {
                    var scene=EditorSceneManager.OpenScene(path);scenes++;
                    foreach(var root in scene.GetRootGameObjects())
                    {Inspect(root);tokens+=root.GetComponentsInChildren<JourneyToken>(true).Length;portals+=root.GetComponentsInChildren<RealmExitPortal>(true).Length;}
                }
                foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
                {var path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);try{Inspect(root);}finally{PrefabUtility.UnloadPrefabContents(root);}}
                if(tokens!=36||portals!=4)throw new Exception("Expected 36 tokens and 4 portals");
                Debug.Log($"REMOVAL VALIDATION PASS: {scenes} scenes and all prefabs; no retired objects/UI/scripts or missing references; 36 tokens and 4 portals.");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(previous);}
        }
        public static void ValidateBatch()=>EditorApplication.delayCall+=()=>{try{EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);Validate();JungleRoadRefinement.Validate();P14DemoBuild.Validate();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
    }
}
