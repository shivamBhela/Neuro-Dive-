using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.Polish;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
 [InitializeOnLoad] public static class P12PolishValidation
 {
  static IEnumerator routine;static double deadline;static int checks;
  static P12PolishValidation(){EditorApplication.playModeStateChanged+=State;}
  public static void BuildAndValidateBatch(){P12PolishBuilder.Setup();EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);SessionState.SetBool("P12.test",true);EditorApplication.isPlaying=true;}
  static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("P12.test",false)){SessionState.SetBool("P12.test",false);routine=Run();deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("P12.done",false)){SessionState.SetBool("P12.done",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool("P12.pass",false)?0:1);}}
  static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("P12 timeout");if(routine.MoveNext())return;Finish(true);}catch(Exception ex){Debug.LogException(ex);Finish(false);}}
  static void Finish(bool pass){EditorApplication.update-=Tick;SessionState.SetBool("P12.pass",pass);SessionState.SetBool("P12.done",true);Debug.Log("P12 RESULT: "+checks+" checks; passed="+pass);EditorApplication.isPlaying=false;}
  static void Check(bool ok,string message){if(!ok)throw new Exception("P12 FAIL: "+message);checks++;Debug.Log("P12 PASS: "+message);}
  static IEnumerator Run()
  {
   while(IntegrationSession.Instance==null||IntegrationSession.Instance.CurrentRealm==null)yield return null;var session=IntegrationSession.Instance;
   var probe=new GameObject("P12ValidationReference_NO_CONTROLLER").transform;Object.DontDestroyOnLoad(probe.gameObject);
   for(int realm=0;realm<4;realm++)
   {
    var scene=session.CurrentRealm.gameObject.scene;var polish=scene.GetRootGameObjects().Single().GetComponentInChildren<RealmPolish>();Check(polish!=null&&polish.Profile!=null,"Realm polish and profile present "+realm);
    var systems=polish.GetComponentsInChildren<ParticleSystem>();Check(systems.Length==3&&systems.Sum(p=>p.main.maxParticles)==64,"Bounded particle count");Check(polish.GetComponentsInChildren<Collider>().Length==0,"No collision or route changes from decoration");
    Check(polish.Profile.Ambience==null&&polish.Profile.SuccessSound==null,"Missing audio clips safely remain silent");Check(polish.Profile.AmbienceVolume<=.18f&&polish.Profile.SuccessVolume<=.12f,"Conservative audio caps");
    polish.Muted=true;Check(polish.GetComponentsInChildren<AudioSource>().All(s=>s.mute),"Mute affects ambience and success");polish.Muted=false;
    polish.ReducedMotion=true;Check(systems.All(s=>!s.isPlaying&&s.particleCount==0),"Reduced motion stops and clears all particles");polish.ReducedMotion=false;Check(systems.All(s=>s.isPlaying),"Motion can resume without gameplay changes");
    polish.enabled=false;Check(systems.All(s=>!s.isPlaying),"Disable clears particle activity");polish.enabled=true;
    foreach(var target in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<InteractableBase>()).ToArray())
    {
     probe.position=target.transform.position;session.BindPlayer(probe,probe);Physics.SyncTransforms();int before=polish.FeedbackCount;
     Check(target.TryInteract(session.Interactor),"Existing action succeeds: "+target.name);Check(polish.FeedbackCount==before+1,"One success callback after disable/re-enable");Check(polish.SuccessParticles.particleCount>0&&polish.SuccessParticles.particleCount<=16,"Success emits a bounded soft effect");Check(!target.TryInteract(session.Interactor)&&polish.FeedbackCount==before+1,"Rejected repeat produces no polish feedback");
    }
    var camera=scene.GetRootGameObjects().Single().GetComponentsInChildren<Camera>(true).First(c=>c.name=="PrototypeOverviewCamera_NoController");foreach(var ps in systems.Where(s=>s!=polish.SuccessParticles))ps.Simulate(5,true,true);Capture(camera,session,scene.name);
    var source=session.CurrentRealm;if(source.CompletionOnActivation){while(!session.IsComplete)yield return null;Check(!session.TryAdvance(source,source.Exit),"Final console completes with polish and no portal");}else Check(session.TryAdvance(source,source.Exit),"P11 exit works with polish");Check(!session.TryAdvance(source,source.Exit),"P11 duplicate guard retained");if(realm<3){while(session.IsTransitioning)yield return null;Check(polish==null,"Old realm polish destroyed on unload");Check(IntegrationSession.Instance==session,"P11 session persists independently");}
   }
   Check(session.IsComplete,"Completion needs no collectibles");Object.Destroy(probe.gameObject);
  }
  static void Capture(Camera camera,IntegrationSession session,string name)
  {
   var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;var clone=Object.Instantiate(pipeline);var previous=QualitySettings.renderPipeline;var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var oldTarget=camera.targetTexture;var active=RenderTexture.active;var hud=session.Hud.GetComponent<Canvas>();bool enabled=hud.enabled;
   try{clone.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;clone.useSRPBatcher=false;QualitySettings.renderPipeline=clone;hud.enabled=false;camera.targetTexture=target;RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("Documentation/P12");File.WriteAllBytes("Documentation/P12/"+name+".png",image.EncodeToPNG());}
   finally{hud.enabled=enabled;camera.targetTexture=oldTarget;QualitySettings.renderPipeline=previous;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(clone);}
  }
 }
}
