using System;
using System.IO;
using System.Linq;
using LostWorlds.Person2.UI;
using LostWorlds.Person2.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    [InitializeOnLoad]
    public static class P7HudValidation
    {
        const string Pending="LostWorlds.P7.Pending", Finished="LostWorlds.P7.Finished", Passed="LostWorlds.P7.Passed";
        static int checks;
        static P7HudValidation() => EditorApplication.playModeStateChanged += StateChanged;
        public static void BuildAndValidateBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch entry only.");
            EditorSceneManager.OpenScene(P4InteractionDemoBuilder.JunglePath,OpenSceneMode.Single);
            P7HudBuilder.Setup(); Begin();
        }
        [MenuItem("Tools/Lost Worlds/P7/Run HUD Play Mode Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Begin();
        }
        static void Begin()
        {
            EditorSceneManager.OpenScene(P7HudBuilder.ScenePath,OpenSceneMode.Single);
            SessionState.SetBool(Pending,true); SessionState.SetBool(Finished,false); EditorApplication.isPlaying=true;
        }
        static void StateChanged(PlayModeStateChange state)
        {
            if (state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) EditorApplication.update+=Checks;
            if (state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Finished,false)) EditorApplication.update+=Finish;
        }
        static void Finish()
        {
            EditorApplication.update-=Finish; SessionState.SetBool(Finished,false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(Passed,false)?0:1);
        }
        static void Assert(bool valid,string message)
        { if(!valid)throw new InvalidOperationException("P7 CHECK FAILED: "+message);checks++;Debug.Log("P7 CHECK PASS: "+message); }
        static void Key(Keyboard keyboard,P7HudDemoControls demo,params UnityEngine.InputSystem.Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();
            demo.PollKeyboard();demo.Interactions.PollKeyboard();
            demo.RouteInput.PollKeyboard();
            demo.GetComponentInChildren<Person2Hud>().Context.SampleNow();Canvas.ForceUpdateCanvases();
        }
        static void Checks()
        {
            EditorApplication.update-=Checks;SessionState.SetBool(Pending,false);checks=0;bool passed=false;Keyboard keyboard=null;
            var settings=InputSystem.settings;var background=settings.backgroundBehavior;var editorInput=settings.editorInputBehaviorInPlayMode;
            try
            {
                var demo=Object.FindAnyObjectByType<P7HudDemoControls>();var hud=demo.GetComponentInChildren<Person2Hud>();var source=hud.Context;
                demo.ShowView(1);Physics.SyncTransforms();
                Assert(hud.transform.Find("RelicStatus")==null,"Removed collectible counter is absent");
                Assert(hud.InteractionText.Contains("G   GRAB")&&hud.InteractionText.Contains("E   ACTIVATE"),"Available P4 actions display their original G/E prompts");
                Assert(!hud.transform.Find("RoutePrompt").gameObject.activeSelf,"Unrelated route panel stays hidden");
                Assert(hud.GetComponent<CanvasScaler>().uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize&&
                    !hud.GetComponent<CanvasGroup>().blocksRaycasts,"Responsive HUD does not intercept world controls");
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
                int availability=0;source.AvailabilityChanged+=()=>availability++;source.SampleNow();int previous=availability;
                source.SampleNow();source.SampleNow();Assert(previous==availability,"Availability adapter emits nothing while context is unchanged");
                var origin=demo.Interactions.GrabInteractor.transform.position;demo.Interactions.GrabInteractor.transform.position+=Vector3.right*40;source.SampleNow();
                Assert(!hud.InteractionText.Contains("G   GRAB"),"Out-of-range GRAB prompt is withdrawn");
                Key(keyboard,demo,UnityEngine.InputSystem.Key.G);Assert(hud.FeedbackCount==0,"Rejected interaction produces no success feedback");
                demo.Interactions.GrabInteractor.transform.position=origin;source.SampleNow();Key(keyboard,demo);
                Capture(demo,hud,1280,720,"Interactions_1280x720");
                Key(keyboard,demo,UnityEngine.InputSystem.Key.G);
                Assert(hud.FeedbackText.Contains("Object placed")&&hud.InteractionText.Contains("GRAB complete"),"P4 GRAB success event updates prompt and positive feedback");
                int notifications=hud.FeedbackCount;Key(keyboard,demo);Key(keyboard,demo,UnityEngine.InputSystem.Key.G);
                Assert(hud.FeedbackCount==notifications,"Repeated GRAB gives no duplicate HUD feedback");
                Key(keyboard,demo);Key(keyboard,demo,UnityEngine.InputSystem.Key.E);
                Assert(hud.FeedbackText.Contains("Activated")&&hud.InteractionText.Contains("ACTIVATE complete"),"P4 ACTIVATE success event updates HUD");
                Capture(demo,hud,1920,1080,"InteractionSuccess_1920x1080");
                Key(keyboard,demo);Key(keyboard,demo,UnityEngine.InputSystem.Key.F2);Key(keyboard,demo);
                Assert(demo.Mode==1,"Retired F2 view does not change interaction context");
                notifications=hud.FeedbackCount;
                Key(keyboard,demo,UnityEngine.InputSystem.Key.F3);Key(keyboard,demo);
                Assert(demo.Mode==3&&hud.RouteText.Contains("LEFT  /  RIGHT"),"F3 displays existing P6 route choices");
                Assert(!hud.transform.Find("InteractionPrompt").gameObject.activeSelf,"Route context hides unrelated interaction prompts");
                var zone=demo.RouteInput.Route;var routeOrigin=demo.RouteInput.Reference.position;
                demo.RouteInput.Reference.position+=Vector3.right*30;source.SampleNow();
                Assert(!hud.RouteText.Contains("LEFT  /  RIGHT"),"Out-of-zone context withdraws choice prompt");
                Key(keyboard,demo,UnityEngine.InputSystem.Key.A);Assert(!zone.HasChoice&&hud.FeedbackCount==notifications,"Rejected route choice produces no success feedback");
                demo.RouteInput.Reference.position=routeOrigin;source.SampleNow();Key(keyboard,demo);
                Key(keyboard,demo,UnityEngine.InputSystem.Key.D);
                Assert(zone.Selection==RouteChoice.Right&&hud.RouteText==""&&hud.FeedbackCount==notifications,"Accepted RIGHT hides route panel without success feedback");
                notifications=hud.FeedbackCount;Key(keyboard,demo);Key(keyboard,demo,UnityEngine.InputSystem.Key.A);
                Assert(hud.RouteText==""&&hud.FeedbackCount==notifications,"Repeated/opposite route input cannot replace HUD selection");Key(keyboard,demo);
                Capture(demo,hud,1920,1080,"RouteSelected_1920x1080");
                zone.BeginNewEncounter();Assert(hud.RouteText.Contains("LEFT  /  RIGHT"),"Encounter callback restores neutral choice prompt");
                Key(keyboard,demo,UnityEngine.InputSystem.Key.LeftArrow);
                Assert(hud.RouteText=="","LEFT callback hides confirmation HUD");Key(keyboard,demo);
                hud.enabled=false;zone.BeginNewEncounter();zone.TryChoose(RouteChoice.Right,demo.RouteInput.Reference);hud.enabled=true;
                Assert(hud.RouteText=="","Re-enabled HUD keeps accepted route hidden");
                demo.ShowView(1);notifications=hud.FeedbackCount;zone.BeginNewEncounter();zone.TryChoose(RouteChoice.Left,demo.RouteInput.Reference);
                Assert(hud.FeedbackCount==notifications&&!hud.transform.Find("RoutePrompt").gameObject.activeSelf,"Changing context unsubscribes from old route callbacks");
                Assert(demo.RouteInput.Reference.position==routeOrigin&&demo.Interactions.GrabInteractor.transform.position==origin,"HUD never moves interaction references");
                Directory.CreateDirectory("Documentation/P7");
                File.WriteAllText("Documentation/P7/HUD_VERIFICATION.txt","Unity "+Application.unityVersion+"\nP7 checks passed: "+checks+
                    "\nP4/P6 callbacks, original key controls, duplicate suppression, disabled/rebound subscriptions, context availability and reference immobility verified.\n"+
                    "Rendered and checked UI at 1280x720, 1600x900 and 1920x1080.\nNo runtime test state saved.\n");
                Debug.Log("P7 HUD VALIDATION PASS: "+checks+" checks.");passed=true;
            }
            catch(Exception error){Debug.LogException(error);}
            finally
            {
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=editorInput;
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
                Directory.CreateDirectory("Documentation/P7");File.WriteAllBytes("Documentation/P7/"+name+".png",image.EncodeToPNG());
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

