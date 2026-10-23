using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;

namespace Realmwalker.Authoring
{
    public static class RealmwalkerR6QA
    {
        [MenuItem("Tools/Lost Worlds/R6/Run Final QA")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Require(enabled.Take(5).SequenceEqual(new[]{MainMenu.ScenePath}.Concat(IntegrationSession.RealmScenes)), "Startup and realm build order");
            Require(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64), "Windows x64 build support installed");
            int[] counts = { 0, 17, 18, 29, 30, 36 }, stars = { 1, 1, 2, 2, 3, 3 };
            for (int i = 0; i < counts.Length; i++) Require(JourneyFinale.StarsForTokens(counts[i]) == stars[i], "Stars at " + counts[i]);
            foreach (var path in IntegrationSession.RealmScenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script check", false);
                    foreach (var component in transform.GetComponents<Component>())
                    {
                        if (component == null) throw new Exception("Missing component: " + transform.name);
                        var serialized = new SerializedObject(component); var property = serialized.GetIterator();
                        while (property.NextVisible(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceEntityIdValue != default)
                                throw new Exception("Broken reference: " + path + "/" + transform.name + "/" + property.propertyPath);
                    }
                }
                Debug.Log("R6 PASS: scene references " + path);
            }
            Debug.Log("R6 preflight passed; running existing full-journey Play Mode regression.");
            RealmwalkerR5Validation.Validate();
        }
        static void Require(bool value, string label, bool log = true) { if (!value) throw new Exception("R6 FAIL: " + label); if (log) Debug.Log("R6 PASS: " + label); }
        public static void Batch() => EditorApplication.delayCall += () => { try { Run(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } };
        [MenuItem("Tools/Lost Worlds/R6/Validate Transition Stability")]
        public static void Stability() { SessionState.SetBool("R6.stability",true); Run(); }
        public static void StabilityBatch() => EditorApplication.delayCall += () => { try { Stability(); } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } };
    }
}
