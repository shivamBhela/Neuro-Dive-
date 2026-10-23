using System;
using System.IO;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
    public static class P10AdapterValidation
    {
        const string Pending="P10.Pending",Finished="P10.Finished",Passed="P10.Passed";
        static IEnumerator routine;static Func<bool> condition;static double deadline;static int checks;static Keyboard keyboard;
        static InputSettings.BackgroundBehavior background;static InputSettings.EditorInputBehaviorInPlayMode editorInput;
        static P10AdapterValidation()=>EditorApplication.playModeStateChanged+=State;
        public static void BuildAndValidateBatch(){EditorSceneManager.OpenScene(P9SimulatorBuilder.ScenePath);P10AdapterBuilder.Setup();Begin();}
        [MenuItem("Tools/Lost Worlds/P10/Run Adapter Checks")]
        public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;Begin();}
        static void Begin(){EditorSceneManager.OpenScene(P10AdapterBuilder.ScenePath);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;}
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))
            {SessionState.SetBool(Pending,false);checks=0;condition=null;routine=Checks();background=InputSystem.settings.backgroundBehavior;editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Finished,false))EditorApplication.update+=Finish;
        }
        static void Finish(){EditorApplication.update-=Finish;SessionState.SetBool(Finished,false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool(Passed,false)?0:1);}
        static void Tick()
        {
            try
            {
                if(condition!=null&&!condition()){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("P10 asynchronous check timed out");return;}
                condition=null;
                if(routine.MoveNext()){condition=routine.Current as Func<bool>;deadline=EditorApplication.timeSinceStartup+5;return;}
                End(true);
            }
            catch(Exception ex){Debug.LogException(ex);End(false);}
        }
        static void End(bool passed)
        {
            EditorApplication.update-=Tick;(routine as IDisposable)?.Dispose();routine=null;
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            SessionState.SetBool(Passed,passed);SessionState.SetBool(Finished,true);EditorApplication.isPlaying=false;
        }
        static void Assert(bool ok,string message){if(!ok)throw new Exception("P10 FAIL: "+message);checks++;Debug.Log("P10 PASS: "+message);}
        static void Key(P10LocalTestClient client,params UnityEngine.InputSystem.Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();client.PollKeyboard();}
        static Func<bool> Received(ProcessedHardwareAdapter adapter,int count)=>()=>{adapter.Pump();return adapter.Forwarded>=count;};
        static IEnumerator Checks()
        {
            var adapter=Object.FindAnyObjectByType<ProcessedHardwareAdapter>();var client=Object.FindAnyObjectByType<P10LocalTestClient>();var demo=Object.FindAnyObjectByType<P9SimulatorDemo>();var channel=demo.Channel;
            var hud=demo.Stations.GetComponentInChildren<Person2Hud>();var status=Object.FindAnyObjectByType<HardwareAdapterStatus>();
            Assert(adapter.Listening&&!adapter.Connected&&adapter.Error=="","Listening without a device; no false Connected state");
            Assert(status.DisplayText.Contains("Disconnected"),"Status starts Disconnected");
            foreach(var name in new[]{"LEFT","RIGHT","JUMP","GRAB","ACTIVATE"})Assert(ProcessedCommandParser.TryParse("{\"command\":\""+name+"\"}",out var command)&&command.ToString()==name,"Strict parser accepts "+name);
            foreach(var invalid in new[]{"", "null", "{}", "{\"command\":\"FLY\"}","{\"command\":\"jump\"}","{\"command\":2}","{\"command\":\"JUMP\",\"raw\":1}","{\"command\":\"LEFT\",\"command\":\"RIGHT\"}","{\"command\":\"JUMP\"}garbage",new string('x',257)})Assert(!ProcessedCommandParser.TryParse(invalid,out _),"Reject malformed/unknown envelope");
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            Key(client,UnityEngine.InputSystem.Key.F4);yield return (Func<bool>)(()=>{adapter.Pump();return adapter.Connected;});Assert(status.DisplayText.Contains("Connected")&&!status.DisplayText.Contains("Disconnected"),"Real TCP connect updates status");Key(client);
            demo.ShowView(3);var route=demo.Stations.RouteInput.Route;var reference=demo.Stations.RouteInput.Reference;var origin=reference.position;
            Key(client,UnityEngine.InputSystem.Key.F5);Key(client);yield return Received(adapter,1);Assert(channel.Latest==HardwareCommand.LEFT&&route.Selection==RouteChoice.Left,"LEFT travels TCP/parser/P9/P6");
            route.BeginNewEncounter();Key(client,UnityEngine.InputSystem.Key.F6);Key(client);yield return Received(adapter,2);Assert(route.Selection==RouteChoice.Right,"RIGHT travels real adapter path");
            Key(client,UnityEngine.InputSystem.Key.F7);Key(client);yield return Received(adapter,3);Assert(channel.Latest==HardwareCommand.JUMP&&reference.position==origin,"JUMP delivered without movement");
            demo.ShowView(1);Physics.SyncTransforms();Key(client,UnityEngine.InputSystem.Key.F8);Key(client);yield return Received(adapter,4);Assert(hud.InteractionText.Contains("GRAB complete"),"GRAB reaches original P4 interaction");
            Key(client,UnityEngine.InputSystem.Key.F9);Key(client);yield return Received(adapter,5);Assert(hud.InteractionText.Contains("ACTIVATE complete"),"ACTIVATE reaches original P4 interaction");
            int sent=channel.SentCount;Key(client,UnityEngine.InputSystem.Key.F10);Key(client);yield return (Func<bool>)(()=>{adapter.Pump();return adapter.Rejected==1;});Assert(channel.SentCount==sent&&adapter.LatestReceived==HardwareCommand.ACTIVATE,"Invalid wire command rejected without channel publication");
            Assert(status.DisplayText.Contains("Rejected: 1"),"Rejection visible with latest valid command retained");
            Capture(demo.Stations,hud,1280,720,"ConnectedAndRejected");
            client.SendRaw("{\"command\":\"JU");double until=EditorApplication.timeSinceStartup+.15;yield return (Func<bool>)(()=>EditorApplication.timeSinceStartup>=until);Assert(adapter.Forwarded==5,"Partial TCP frame not dispatched");
            client.SendRaw("MP\"}\r\n{\"command\":\"LEFT\"}\n");yield return Received(adapter,7);Assert(adapter.LatestReceived==HardwareCommand.LEFT,"Fragmented and coalesced frames supported");
            int rejected=adapter.Rejected;client.SendRaw(new string('x',600)+"\n{\"command\":\"JUMP\"}\n");yield return Received(adapter,8);Assert(adapter.Rejected==rejected+1,"Oversized frame discarded; next valid frame recovered");
            rejected=adapter.Rejected;client.SendRaw("{\"command\":\"é\"}\n");yield return (Func<bool>)(()=>{adapter.Pump();return adapter.Rejected>rejected;});Assert(adapter.Forwarded==8,"Non-ASCII payload rejected");
            client.SendRaw("{\"command\":\"JUMP\"}");client.Disconnect();yield return (Func<bool>)(()=>{adapter.Pump();return !adapter.Connected&&adapter.Rejected>rejected+1;});Assert(adapter.Forwarded==8,"Disconnect discards unterminated frame");
            Assert(status.DisplayText.Contains("Disconnected"),"Disconnect updates UI");
            Assert(client.Connect(),"Reconnect succeeds");yield return (Func<bool>)(()=>adapter.Connected);client.SendRaw("{\"command\":\"JUMP\"}\n");yield return Received(adapter,9);
            channel.enabled=false;rejected=adapter.Rejected;client.SendRaw("{\"command\":\"JUMP\"}\n");yield return (Func<bool>)(()=>{adapter.Pump();return adapter.Rejected>rejected;});Assert(adapter.Forwarded==9,"Unavailable channel does not falsely report forwarding");channel.enabled=true;
            client.Disconnect();adapter.enabled=false;sent=channel.SentCount;Assert(!adapter.ReceiveMessage("{\"command\":\"JUMP\"}"),"Disabled adapter cannot forward");
            var probe=new TcpListener(IPAddress.Loopback,adapter.Port);probe.Start();probe.Stop();Assert(true,"Disabling adapter releases listening port");
            Key(client,UnityEngine.InputSystem.Key.Space);Object.FindAnyObjectByType<HardwareCommandSimulator>().PollKeyboard();Key(client);Assert(channel.SentCount==sent+1&&channel.Latest==HardwareCommand.JUMP,"P9 keyboard fallback works without adapter");
            adapter.enabled=true;Assert(adapter.Listening,"Adapter restarts cleanly");
            Object.FindAnyObjectByType<EffortModel>().SetEffort(80);Assert(client.Connect(),"Connect after restart");yield return (Func<bool>)(()=>adapter.Connected);client.SendRaw("{\"command\":\"JUMP\"}\n");yield return Received(adapter,1);Assert(adapter.LatestReceived==HardwareCommand.JUMP,"P8 rest feedback does not block hardware commands");
            client.Disconnect();yield return (Func<bool>)(()=>{adapter.Pump();return !adapter.Connected;});Capture(demo.Stations,hud,1920,1080,"DisconnectedFallback");
            Directory.CreateDirectory("Documentation/P10");File.WriteAllText("Documentation/P10/VALIDATION.txt",$"Unity {Application.unityVersion}\nP10 checks passed: {checks}\nActual localhost TCP tested for all five commands, invalid input, framing, reconnect, cleanup, fallback, core interactions and UI.\n");
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
                Directory.CreateDirectory("Documentation/P10");File.WriteAllBytes("Documentation/P10/"+name+".png",image.EncodeToPNG());
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
