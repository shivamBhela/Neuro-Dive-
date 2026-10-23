using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using LostWorlds.Person2.Integration;
namespace LostWorlds.Person2.Editor
{
    public static class P14DemoBuild
    {
        public const string Output="Builds/LostWorldsSIH/LostWorlds.exe";
        [MenuItem("Tools/Lost Worlds/P14/Validate Demo Build")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before build validation.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())throw new OperationCanceledException();
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).Take(5).ToArray();
            if(!scenes.SequenceEqual(new[]{MainMenu.ScenePath}.Concat(IntegrationSession.RealmScenes)))throw new Exception("Main Menu and four realms are not in startup order");
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new Exception("Windows build support is missing");
            RelicRemovalValidation.Validate();P11IntegrationBuilder.Validate();P125JourneyBuilder.Validate();P12PolishBuilder.Validate();
            foreach(var path in IntegrationSession.RealmScenes)
                foreach(var dependency in AssetDatabase.GetDependencies(path,true))
                    if(!File.Exists(dependency)&&!AssetDatabase.IsValidFolder(dependency))throw new Exception("Missing dependency: "+dependency);
            Debug.Log("P14 build validation PASS: Windows x64 support, ordered realms, dependencies, P11/P12/P12.5 wiring.");
        }
        [MenuItem("Tools/Lost Worlds/P14/Build Windows Demo")]
        public static void Build()
        {
            Validate();Directory.CreateDirectory(Path.GetDirectoryName(Output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{MainMenu.ScenePath}.Concat(IntegrationSession.RealmScenes).ToArray(),locationPathName=Output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            Directory.CreateDirectory("Documentation/P14");
            File.WriteAllText("Documentation/P14/BUILD_RESULT.txt",$"Unity {Application.unityVersion}\nResult: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nBytes: {report.summary.totalSize}\nOutput: {Output}\n");
            foreach(var step in report.steps)foreach(var message in step.messages)
                if(message.type==LogType.Warning||message.type==LogType.Error||message.type==LogType.Exception)
                    File.AppendAllText("Documentation/P14/BUILD_RESULT.txt",message.type+": "+message.content+"\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows demo build failed");
            File.WriteAllText("Builds/LostWorldsSIH/RunDemo.cmd","@echo off\r\nstart \"Lost Worlds SIH Demo\" \"%~dp0LostWorlds.exe\" -screen-fullscreen 0 -screen-width 1600 -screen-height 900\r\n");
        }
        public static void BuildBatch()=>EditorApplication.delayCall+=()=>{try{EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
    }
}
