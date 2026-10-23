using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using LostWorlds.Person2.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.World;
using LostWorlds.Person2.Interaction;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
 [InitializeOnLoad] public static class P11IntegrationValidation
 {
  static IEnumerator routine;static int checks;static double deadline;
  static P11IntegrationValidation(){EditorApplication.playModeStateChanged+=State;}
  public static void BuildAndValidateBatch(){P11IntegrationBuilder.SetupBatch();P85ObstacleBuilder.Validate();EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);SessionState.SetBool("P11.pending",true);EditorApplication.isPlaying=true;}
  static void State(PlayModeStateChange state)
  {
   if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("P11.pending",false)){SessionState.SetBool("P11.pending",false);routine=Run();deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;}
   if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("P11.done",false)){SessionState.SetBool("P11.done",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool("P11.pass",false)?0:1);}
  }
  static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("P11 timeout");if(routine.MoveNext())return;End(true);}catch(Exception ex){Debug.LogException(ex);End(false);}}
  static void End(bool pass){EditorApplication.update-=Tick;SessionState.SetBool("P11.pass",pass);SessionState.SetBool("P11.done",true);Debug.Log("P11 RESULT: "+checks+" checks; passed="+pass);EditorApplication.isPlaying=false;}
  static void Check(bool condition,string text){if(!condition)throw new Exception("P11 FAIL: "+text);checks++;Debug.Log("P11 PASS: "+text);}
  static IEnumerator Run()
  {
   while(IntegrationSession.Instance==null||IntegrationSession.Instance.CurrentRealm==null)yield return null;
   var s=IntegrationSession.Instance;Check(s.PlayerReference==null&&!s.Interactor.enabled,"Optional player boot is safe");int commands=0,branches=0,arrivals=0,completed=0;
   s.PlayerBridge.PlayerCommandRequested+=c=>commands++;s.BranchSelected+=(realm,choice,path)=>{branches++;Check(path.Length==4,"Selected branch exposes four world points");};s.RealmReady+=r=>arrivals++;s.Completed+=()=>completed++;
   s.Channel.Send(HardwareCommand.LEFT);s.Channel.Send(HardwareCommand.RIGHT);s.Channel.Send(HardwareCommand.JUMP);Check(commands==3,"LEFT RIGHT JUMP exposed without movement");
   var probe=new GameObject("ValidationReference_NOT_A_CONTROLLER").transform;Object.DontDestroyOnLoad(probe.gameObject);var initial=probe.position;s.BindPlayer(probe,probe);s.Channel.Send(HardwareCommand.JUMP);Check(probe.position==initial,"JUMP does not move reference");
   Check(s.CurrentRealm.Branch==null,"Jungle has no required route choice");s.Channel.Send(HardwareCommand.LEFT);s.Channel.Send(HardwareCommand.RIGHT);Check(commands==6,"Jungle directions reach optional player callbacks");
   foreach(var target in Object.FindObjectsByType<InteractableBase>().ToArray())
   {
    probe.position=target.transform.position;Physics.SyncTransforms();s.RefreshContext(true);s.Channel.Send(target.Command==InteractionCommand.Grab?HardwareCommand.GRAB:HardwareCommand.ACTIVATE);Check(target.HasInteracted,"Existing P4 interaction through shared channel: "+target.name);
    float until=Time.unscaledTime+.3f;while(Time.unscaledTime<until)yield return null;
   }
   for(int realm=0;realm<4;realm++)
   {
    Check(s.CurrentRealm.RealmIndex==realm,"Correct realm "+realm);Check(s.CurrentRealm.GetMainPath().Length>=4,"Main path data available");
    if(realm==3){int before=commands;s.BindPlayer(probe,probe);var branch=s.CurrentRealm.Branch;probe.position=branch.Zone.transform.position;Physics.SyncTransforms();s.RefreshContext(true);s.Channel.Send(HardwareCommand.LEFT);s.Channel.Send(HardwareCommand.RIGHT);Check(branches==1&&branch.Zone.Selection==RouteChoice.Left,"Route accepts exactly one LEFT choice");branch.Zone.BeginNewEncounter();s.Channel.Send(HardwareCommand.RIGHT);Check(branches==2&&branch.Zone.Selection==RouteChoice.Right,"RIGHT exposes alternate branch");Check(commands==before+3,"Route preserves all three player steering callbacks");}
    var gate=s.CurrentRealm.transform.root.GetComponentInChildren<LostWorlds.Person2.Journey.PortalUnlockMechanism>();if(gate!=null&&!gate.CanExit){Check(!s.TryAdvance(s.CurrentRealm,s.CurrentRealm.Exit),"Realm gate rejects premature exit");Check(gate.TestActivate(),"Realm activation unlocks exit");}
    var source=s.CurrentRealm;Check(!s.TryAdvance(source,source.Entrance),"Entrance cannot trigger exit");if(source.CompletionOnActivation){while(!s.IsComplete)yield return null;Check(!s.TryAdvance(source,source.Exit),"Final console completes without exit entry");}else Check(s.TryAdvance(source,source.Exit),"Exit accepted");Check(!s.TryAdvance(source,source.Exit),"Duplicate transition/completion blocked");
    if(realm<3){int prior=commands;s.Channel.Send(HardwareCommand.JUMP);Check(commands==prior,"Commands gated during transition");while(s.IsTransitioning)yield return null;Check(IntegrationSession.Instance==s,"Session survives scene replacement");Check(s.PlayerReference==null,"Arrival waits for Person 1 rebinding");}
   }
   Capture(Object.FindObjectsByType<Camera>().First(c=>c.enabled),s.Hud,1280,720,"Completion720");Capture(Object.FindObjectsByType<Camera>().First(c=>c.enabled),s.Hud,1920,1080,"Completion1080");
   Check(completed==1&&s.IsComplete,"Final completion fires once without a collection requirement");Check(arrivals==3,"Three next-realm arrival callbacks");Check(Object.FindObjectsByType<IntegrationSession>().Length==1,"Exactly one persistent session");Object.Destroy(probe.gameObject);
  }
        static void Capture(Camera camera,Person2Hud hud,int width,int height,string name)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var clone=Object.Instantiate(pipeline);var previous=QualitySettings.renderPipeline;
            var canvas=hud.GetComponent<Canvas>();var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var plane=canvas.planeDistance;
            var oldTarget=camera.targetTexture;var active=RenderTexture.active;
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
                Directory.CreateDirectory("Documentation/P11");File.WriteAllBytes("Documentation/P11/"+name+".png",image.EncodeToPNG());
                foreach(var text in hud.GetComponentsInChildren<Text>())
                {
                    Check(text.preferredHeight<=text.rectTransform.rect.height+2,"Text fits at "+width+"x"+height+": "+text.name);
                    var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
                    foreach(var corner in corners)
                    {
                        var point=camera.WorldToViewportPoint(corner);
                        if(point.x<-.005f||point.x>1.005f||point.y<-.005f||point.y>1.005f)throw new InvalidOperationException("UI outside screen: "+text.name);
                    }
                }
                Check(Mathf.Abs(canvas.scaleFactor-(float)width/1600)<.02f,"Canvas scale matches "+width+"x"+height);
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

