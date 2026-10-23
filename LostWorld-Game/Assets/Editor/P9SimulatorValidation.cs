using System;
using System.IO;
using System.Linq;
using LostWorlds.Person2.Hardware;
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
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
    [InitializeOnLoad]
    public static class P9SimulatorValidation
    {
        const string Pending="P9.Pending",Finished="P9.Finished",Passed="P9.Passed";
        static int checks;
        static P9SimulatorValidation()=>EditorApplication.playModeStateChanged+=State;
        public static void BuildAndValidateBatch(){EditorSceneManager.OpenScene(P8EffortBuilder.ScenePath);P9SimulatorBuilder.Setup();Begin();}
        [MenuItem("Tools/Lost Worlds/P9/Run Simulator Checks")]
        public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;Begin();}
        public static void ValidateObstaclesBatch(){EditorSceneManager.OpenScene(P9SimulatorBuilder.ScenePath);P85ObstacleBuilder.Validate();}
        static void Begin(){EditorSceneManager.OpenScene(P9SimulatorBuilder.ScenePath);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;}
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))EditorApplication.update+=Checks;
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Finished,false))EditorApplication.update+=Finish;
        }
        static void Finish(){EditorApplication.update-=Finish;SessionState.SetBool(Finished,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool(Passed,false)?0:1);}
        static void Assert(bool ok,string message){if(!ok)throw new Exception("P9 FAIL: "+message);checks++;Debug.Log("P9 PASS: "+message);}
        static void Key(Keyboard keyboard,P9SimulatorDemo demo,HardwareCommandSimulator simulator,params UnityEngine.InputSystem.Key[] keys)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();demo.PollKeyboard();simulator.PollKeyboard();demo.Stations.GetComponentInChildren<Person2Hud>().Context.SampleNow();Canvas.ForceUpdateCanvases();}
        static void Checks()
        {
            EditorApplication.update-=Checks;SessionState.SetBool(Pending,false);checks=0;bool passed=false;Keyboard keyboard=null;
            var settings=InputSystem.settings;var background=settings.backgroundBehavior;var editorInput=settings.editorInputBehaviorInPlayMode;
            try
            {
                var demo=Object.FindAnyObjectByType<P9SimulatorDemo>();var simulator=Object.FindAnyObjectByType<HardwareCommandSimulator>();var channel=demo.Channel;
                var hud=demo.Stations.GetComponentInChildren<Person2Hud>();var feedback=Object.FindAnyObjectByType<CommandDebugFeedback>();
                demo.ShowView(1);Physics.SyncTransforms();
                Assert(channel.SentCount==0&&!channel.Latest.HasValue,"Fresh channel has no command history");
                Assert(channel is ICommandSource&&channel is ICommandSink,"Typed source and sink contracts");
                Assert(!demo.Stations.enabled&&!demo.Stations.Interactions.enabled&&!demo.Stations.RouteInput.enabled,"Original keyboard adapters disabled only in P9 copy; no double dispatch");
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
                var actor=demo.Stations.Interactions.GrabInteractor;var origin=actor.transform.position;actor.transform.position+=Vector3.right*50;
                Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.G);Assert(channel.Latest==HardwareCommand.GRAB&&hud.FeedbackCount==0,"Sent GRAB remains subject to existing range rejection");
                actor.transform.position=origin;Physics.SyncTransforms();Key(keyboard,demo,simulator);
                Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.G);Assert(hud.FeedbackText.Contains("Object placed"),"Simulated GRAB calls unchanged P4 and updates P7");
                int count=channel.SentCount;Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.G);Assert(channel.SentCount==count,"Held key does not auto-repeat");
                Key(keyboard,demo,simulator);Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.E);Assert(hud.FeedbackText.Contains("Activated"),"Simulated ACTIVATE calls unchanged P4");
                int successes=hud.FeedbackCount;Key(keyboard,demo,simulator);Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.E);Assert(hud.FeedbackCount==successes,"Core one-shot protection remains");
                var positions=Object.FindObjectsByType<Transform>().ToDictionary(t=>t,t=>t.position);int jumps=0;channel.CommandSent+=c=>{if(c==HardwareCommand.JUMP)jumps++;};
                Key(keyboard,demo,simulator);Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.Space);
                Assert(jumps==1&&feedback.DisplayText.Contains("JUMP")&&feedback.DisplayText.Contains("event only"),"JUMP is published with explicit debug feedback");
                Assert(positions.All(pair=>pair.Key.position==pair.Value),"JUMP moves no transforms");
                Capture(demo.Stations,hud,1280,720,"JumpEventOnly");
                Key(keyboard,demo,simulator);Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.F2);Assert(demo.Mode==1,"Retired F2 leaves interaction station selected");Key(keyboard,demo,simulator);
                count=channel.SentCount;Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.G,UnityEngine.InputSystem.Key.R);Assert(channel.SentCount==count+1,"GRAB aliases publish only once");Key(keyboard,demo,simulator);
                Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.F3);Key(keyboard,demo,simulator);var route=demo.Stations.RouteInput.Route;var reference=demo.Stations.RouteInput.Reference;var original=reference.position;
                count=channel.SentCount;Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.A,UnityEngine.InputSystem.Key.D);Assert(channel.SentCount==count&&!route.HasChoice,"Ambiguous direction is ignored");Key(keyboard,demo,simulator);
                reference.position+=Vector3.right*100;Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.A);Assert(!route.HasChoice,"Existing route zone still rejects outside reference");reference.position=original;Key(keyboard,demo,simulator);
                Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.LeftArrow);Assert(route.Selection==RouteChoice.Left&&hud.RouteText.Contains("LEFT selected"),"LEFT reaches P6 and P7 feedback");Key(keyboard,demo,simulator);
                Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.D);Assert(route.Selection==RouteChoice.Left,"Opposite repeated choice cannot override encounter");route.BeginNewEncounter();Key(keyboard,demo,simulator);
                Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.RightArrow);Assert(route.Selection==RouteChoice.Right,"RIGHT reaches new encounter");
                Assert(reference.position==original,"Route selection does not move reference");Capture(demo.Stations,hud,1920,1080,"RightAccepted");
                var receiver=demo.GetComponent<RouteCommandReceiver>();route.BeginNewEncounter();receiver.enabled=false;channel.Send(HardwareCommand.LEFT);Assert(!route.HasChoice,"Disabled receiver unsubscribes");receiver.enabled=true;
                Assert(channel.Send(HardwareCommand.LEFT)&&route.Selection==RouteChoice.Left,"Direct clean producer call uses same receiver contract");
                bool nested=true;Action<HardwareCommand> callback=c=>nested=channel.Send(HardwareCommand.JUMP);channel.CommandSent+=callback;channel.Send(HardwareCommand.JUMP);channel.CommandSent-=callback;Assert(!nested,"Nested dispatch rejected");
                count=channel.SentCount;Assert(!channel.Send((HardwareCommand)99)&&channel.SentCount==count,"Unknown enum rejected");
                simulator.enabled=false;Key(keyboard,demo,simulator);Key(keyboard,demo,simulator,UnityEngine.InputSystem.Key.Space);Assert(channel.SentCount==count,"Disabled simulator produces no input");simulator.enabled=true;
                channel.enabled=false;Assert(!channel.Send(HardwareCommand.JUMP),"Disabled channel rejects delivery");channel.enabled=true;
                var effort=Object.FindAnyObjectByType<EffortModel>();effort.SetEffort(80);Assert(effort.State==EffortState.RestNeeded&&channel.Send(HardwareCommand.JUMP),"Rest feedback does not gate commands");
                Assert(!hud.GetComponent<CanvasGroup>().blocksRaycasts,"Debug/HUD does not intercept input");
                Directory.CreateDirectory("Documentation/P9");File.WriteAllText("Documentation/P9/VALIDATION.txt",$"Unity {Application.unityVersion}\nP9 checks passed: {checks}\nKeyboard commands, existing P4/P6 receivers, P7/P8 feedback, guards and layout checked. No movement receiver.\n");passed=true;
            }
            catch(Exception error){Debug.LogException(error);}
            finally{if(keyboard!=null)InputSystem.RemoveDevice(keyboard);settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=editorInput;SessionState.SetBool(Passed,passed);SessionState.SetBool(Finished,true);EditorApplication.isPlaying=false;}
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
                Directory.CreateDirectory("Documentation/P9");File.WriteAllBytes("Documentation/P9/"+name+".png",image.EncodeToPNG());
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
