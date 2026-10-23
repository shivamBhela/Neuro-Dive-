using System;
using System.IO;
using LostWorlds.Person2.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
    [InitializeOnLoad]
    public static class P8EffortValidation
    {
        static int checks;
        const string Pending="P8.Pending",Finished="P8.Finished",Passed="P8.Passed";
        static P8EffortValidation() => EditorApplication.playModeStateChanged+=State;
        public static void BuildAndValidateBatch()
        {
            EditorSceneManager.OpenScene(P7HudBuilder.ScenePath);P8EffortBuilder.Setup();Begin();
        }
        [MenuItem("Tools/Lost Worlds/P8/Run Effort Rest Checks")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Begin();
        }
        static void Begin() { EditorSceneManager.OpenScene(P8EffortBuilder.ScenePath);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true; }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))EditorApplication.update+=Checks;
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Finished,false))EditorApplication.update+=Finish;
        }
        static void Finish() { EditorApplication.update-=Finish;SessionState.SetBool(Finished,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool(Passed,false)?0:1); }
        static void Assert(bool ok,string message) { if(!ok)throw new Exception("P8 FAIL: "+message);checks++;Debug.Log("P8 PASS: "+message); }
        static void Key(Keyboard keyboard,P8EffortTestInput input,params UnityEngine.InputSystem.Key[] keys)
        { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();input.PollKeyboard(); }
        static void Checks()
        {
            EditorApplication.update-=Checks;SessionState.SetBool(Pending,false);checks=0;bool passed=false;Keyboard keyboard=null;
            var settings=InputSystem.settings;var background=settings.backgroundBehavior;var editorInput=settings.editorInputBehaviorInPlayMode;
            try
            {
                var hud=Object.FindAnyObjectByType<EffortHud>();var model=hud.Model;var input=Object.FindAnyObjectByType<P8EffortTestInput>();
                Assert(model.Value==0 && model.State==EffortState.Normal,"Fresh session starts Normal at zero");
                Assert(hud.DisplayText.Contains("Normal") && hud.DisplayText.Contains("Cap 55"),"HUD snapshot and cap");
                var adapterObject=new GameObject("EffortAdapterValidation");adapterObject.SetActive(false);
                try
                {
                    var adapter=adapterObject.AddComponent<LostWorlds.Person2.Hardware.ProcessedHardwareAdapter>();
                    adapter.BindEffort(model);adapter.ConfigureEndpoint("127.0.0.1",55119);adapterObject.SetActive(true);
                    Assert(adapter.ReceiveMessage("{\"effort\":44}") && model.Value==44 && model.State==EffortState.NearLimit,"Processed effort updates HUD model");
                    Assert(adapter.ReceiveMessage("{\"effort\":55}") && hud.DisplayText.Contains("REST"),"Hardware effort reaches beginner cap");
                    Assert(adapter.ReceiveMessage("{\"effort\":55}") && model.Value==55,"Repeated hardware sample accepted");
                    foreach(var invalid in new[]{"{\"effort\":101}","{\"effort\":-1}","{\"effort\":NaN}","{\"effort\":\"42\"}","{\"effort\":44,\"raw\":1}"})
                        Assert(!adapter.ReceiveMessage(invalid) && model.Value==55,"Malformed/out-of-range effort rejected");
                    Assert(adapter.ReceiveMessage("{\"effort\":12.5}") && model.Value==12.5f,"Fractional effort accepted");
                }
                finally {Object.DestroyImmediate(adapterObject);model.ResetEffort();}
                int changes=0,states=0;model.Changed+=()=>changes++;model.StateChanged+=s=>states++;
                model.SetEffort(43);Assert(model.State==EffortState.Normal,"Below near threshold");
                model.SetEffort(44);Assert(model.State==EffortState.NearLimit,"Exact near threshold");
                model.SetEffort(55);Assert(model.State==EffortState.RestNeeded && hud.DisplayText.Contains("REST"),"Exact cap recommends rest");
                Assert(states==2 && changes==3,"Change and state callbacks");
                Assert(!model.SetEffort(55) && changes==3,"Repeated values do not spam callbacks");
                Assert(!model.SetEffort(float.NaN) && !model.SetEffort(float.PositiveInfinity),"Invalid samples ignored");
                model.SetEffort(150);Assert(model.Value==100,"Upper clamp");
                model.SetEffort(-5);Assert(model.Value==0 && model.State==EffortState.Normal,"Lower clamp and recovery");
                model.SetEffort(50);model.Configure(50,.8f);Assert(model.State==EffortState.RestNeeded && hud.DisplayText.Contains("Cap 50"),"Cap changes recalculate HUD/state");
                model.Configure(100,.8f);Assert(model.State==EffortState.Normal,"Raising cap recalculates");
                model.Configure(55,.8f);model.ResetEffort();
                hud.enabled=false;string before=hud.DisplayText;model.SetEffort(55);Assert(hud.DisplayText==before,"Disabled view unsubscribes");
                hud.enabled=true;Assert(hud.DisplayText.Contains("REST"),"Reenabled view reads snapshot");
                bool nested=true;Action listener=()=>nested=model.SetEffort(20);model.Changed+=listener;model.SetEffort(90);model.Changed-=listener;Assert(!nested && model.Value==90,"Reentrant mutation guarded");
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
                Key(keyboard,input,UnityEngine.InputSystem.Key.Backspace);Assert(model.Value==0,"Backspace reset");Key(keyboard,input);
                Key(keyboard,input,UnityEngine.InputSystem.Key.PageUp);Assert(model.Value==10,"Page Up adds effort");Key(keyboard,input);
                Key(keyboard,input,UnityEngine.InputSystem.Key.PageDown);Assert(model.Value==0,"Page Down lowers effort");Key(keyboard,input);
                input.enabled=false;Key(keyboard,input,UnityEngine.InputSystem.Key.PageUp);Assert(model.Value==0,"Disabled test input ignored");input.enabled=true;Key(keyboard,input);
                model.SetEffort(100);
                var demo=Object.FindAnyObjectByType<P7HudDemoControls>();demo.ShowView(1);Physics.SyncTransforms();
                Assert(demo.Interactions.GrabInteractor.TryGrab(),"GRAB remains available during Rest Needed");
                Assert(demo.Interactions.ActivateInteractor.TryActivate(),"ACTIVATE remains available during Rest Needed");
                demo.ShowView(3);Assert(demo.RouteInput.Route.TryChoose(LostWorlds.Person2.World.RouteChoice.Left,demo.RouteInput.Reference),"Route remains available during rest");
                Assert(Time.timeScale==1,"Rest does not pause game");
                demo.ShowView(1); model.ResetEffort(); Capture(demo,demo.GetComponentInChildren<Person2Hud>(),1280,720,"Normal"); model.SetEffort(44); Capture(demo,demo.GetComponentInChildren<Person2Hud>(),1600,900,"NearLimit"); model.SetEffort(55); Capture(demo,demo.GetComponentInChildren<Person2Hud>(),1920,1080,"RestNeeded");
                Directory.CreateDirectory("Documentation/P8");File.WriteAllText("Documentation/P8/VERIFICATION.txt",$"P8: {checks} Play Mode checks passed.\n");passed=true;
            }
            catch(Exception error) { Debug.LogException(error); }
            finally
            {
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=editorInput;
                SessionState.SetBool(Passed,passed);SessionState.SetBool(Finished,true);EditorApplication.isPlaying=false;
            }
        }
        static void Capture(P7HudDemoControls demo,Person2Hud hud,int width,int height,string name)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var clone=Object.Instantiate(pipeline);var previous=QualitySettings.renderPipeline;
            var canvas=hud.GetComponent<Canvas>();var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var plane=canvas.planeDistance;
            var camera=demo.ActiveCamera;var oldTarget=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                clone.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;clone.useSRPBatcher=false;QualitySettings.renderPipeline=clone;
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                canvas.GetComponent<CanvasScaler>().SendMessage("Update");Canvas.ForceUpdateCanvases();
                // Warm the dynamic font atlas at the new target resolution, then rebuild every label.
                foreach(var label in hud.GetComponentsInChildren<Text>())
                    label.font.RequestCharactersInTexture(label.text,Mathf.RoundToInt(label.fontSize*canvas.scaleFactor),label.fontStyle);
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});
                foreach(var graphic in hud.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                Directory.CreateDirectory("Documentation/P8");File.WriteAllBytes("Documentation/P8/"+name+".png",image.EncodeToPNG());
                foreach(var text in hud.GetComponentsInChildren<Text>())
                {
                    Assert(text.preferredHeight<=text.rectTransform.rect.height+2,"Text fits at "+width+"x"+height+": "+text.name);
                    var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
                    foreach(var corner in corners)
                    {
                        var point=camera.WorldToViewportPoint(corner);
                        if(point.x<-.005f||point.x>1.005f||point.y<-.005f||point.y>1.005f)throw new InvalidOperationException("UI outside screen: "+text.name);
                    }
                }
                Assert(Mathf.Abs(canvas.scaleFactor-(float)width/1600)<.02f,"Canvas scale matches "+width+"x"+height);
            }
            finally
            {
                canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;camera.targetTexture=oldTarget;
                QualitySettings.renderPipeline=previous;RenderTexture.active=active;target.Release();
                Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(clone);
                canvas.GetComponent<CanvasScaler>().SendMessage("Update");Canvas.ForceUpdateCanvases();
            }
        }

    }
}

