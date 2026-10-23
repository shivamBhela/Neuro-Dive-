using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.UI;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
 [InitializeOnLoad] public static class P125JourneyValidation
 {
  static IEnumerator routine;static int checks;static double deadline;
  static P125JourneyValidation(){EditorApplication.playModeStateChanged+=State;}
  public static void BuildAndValidateBatch(){EditorApplication.delayCall+=Begin;}
  static void Begin(){try{P125JourneyBuilder.Setup();EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);SessionState.SetBool("P125.pending",true);EditorApplication.isPlaying=true;}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}}
  static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("P125.pending",false)){SessionState.SetBool("P125.pending",false);routine=Run();deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("P125.done",false)){SessionState.SetBool("P125.done",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetBool("P125.pass",false)?0:1);}}
  static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("P12.5 timeout");if(routine.MoveNext())return;Finish(true);}catch(Exception e){Debug.LogException(e);Finish(false);}}
  static void Finish(bool pass){EditorApplication.update-=Tick;SessionState.SetBool("P125.pass",pass);SessionState.SetBool("P125.done",true);Debug.Log("P12.5 RESULT: "+checks+" checks; passed="+pass);EditorApplication.isPlaying=false;}
  static void Check(bool ok,string message){if(!ok)throw new Exception("P12.5 FAIL: "+message);checks++;Debug.Log("P12.5 PASS: "+message);}
  static JourneyRealm Current(IntegrationSession s)=>s.CurrentRealm.transform.root.GetComponentInChildren<JourneyRealm>();
  static void CheckPortalRecovery(PortalAtmosphere effect,int realm)
  {
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   var type=typeof(PortalAtmosphere);var surfaceField=type.GetField("energySurface",flags);var motesField=type.GetField("motes",flags);var cache=type.GetField("properties",flags);
   var surface=surfaceField.GetValue(effect);var motes=motesField.GetValue(effect);
   Check(surface is Renderer&&motes is ParticleSystem,"Both authored portal visual references present in realm "+realm);
   int issues=0;Application.LogCallback listener=(message,trace,kind)=>{if(kind==LogType.Warning||kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)issues++;};Application.logMessageReceived+=listener;
   try
   {
    cache.SetValue(effect,null);effect.SendMessage("Update");Check(cache.GetValue(effect)!=null,"Lost property cache recovers without Awake in realm "+realm);
    surfaceField.SetValue(effect,null);cache.SetValue(effect,null);for(int i=0;i<60;i++)effect.SendMessage("Update");
    motesField.SetValue(effect,null);for(int i=0;i<60;i++)effect.SendMessage("Update");
    surfaceField.SetValue(effect,surface);for(int i=0;i<60;i++)effect.SendMessage("Update");
    Check(cache.GetValue(effect)!=null,"Restored surface works without optional particles in realm "+realm);
    motesField.SetValue(effect,motes);effect.enabled=false;cache.SetValue(effect,null);effect.enabled=true;effect.SendMessage("Update");
    Check(cache.GetValue(effect)!=null,"Re-enable restores cache in realm "+realm);
    Check(issues==0,"Zero portal warnings/errors with missing visuals and cache in realm "+realm);
   }
   finally{surfaceField.SetValue(effect,surface);motesField.SetValue(effect,motes);Application.logMessageReceived-=listener;}
  }
  static IEnumerator Run()
  {
   while(IntegrationSession.Instance==null||IntegrationSession.Instance.CurrentRealm==null||Current(IntegrationSession.Instance).Ledger==null)yield return null;
   var session=IntegrationSession.Instance;var ledger=Current(session).Ledger;Check(ledger.Count==0,"Fresh session starts with no tokens");Check(session.PlayerReference==null,"No player required");int events=0,loads=0,complete=0;bool nestedRejected=false;
   ledger.TokenCollected+=id=>{events++;nestedRejected=!ledger.TryCollect("nested-test");};session.TransitionStarted+=()=>loads++;session.Completed+=()=>complete++;
   var probe=new GameObject("P125_TestReference_NO_CONTROLLER");Object.DontDestroyOnLoad(probe);var sphere=probe.AddComponent<SphereCollider>();sphere.radius=.1f;var body=probe.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
   for(int realm=0;realm<4;realm++)
   {
    while(Current(session).Ledger==null)yield return null;var world=Current(session);Check(world.Tokens.Length==P125JourneyBuilder.Counts[realm],"Authored token count for realm "+realm);Check(world.Ledger==ledger,"Same persistent token ledger");Check(world.Portal.GetComponentsInChildren<Collider>().All(c=>c.isTrigger),"Portal cannot physically block route");
    var gate=world.transform.root.GetComponentInChildren<PortalUnlockMechanism>();if(gate!=null&&realm!=3){Check(!world.Portal.TestEnter()&&!session.TryAdvance(session.CurrentRealm,session.CurrentRealm.Exit),"Locked portal rejects both entry paths");Check(gate.TestActivate(),"Controller-free ACTIVATE uses real interaction path");Check(!gate.TestActivate(),"Repeated unlock rejected");float until=Time.unscaledTime+1;while(Time.unscaledTime<until)yield return null;Check(gate.CanExit&&world.Portal.gameObject.activeInHierarchy,"Portal revealed and remains unlocked");}
    if(realm!=3){var effect=world.Portal.GetComponentInChildren<PortalAtmosphere>();Check(effect!=null,"Portal visual inherited from shared prefab");CheckPortalRecovery(effect,realm);var motes=effect.GetComponentInChildren<ParticleSystem>();Check(motes!=null&&motes.main.maxParticles==12,"Portal particle budget");Check(effect.GetComponentsInChildren<Collider>().Length==0,"Portal visual adds no colliders");var polish=world.transform.root.GetComponentInChildren<LostWorlds.Person2.Polish.RealmPolish>();polish.ReducedMotion=true;yield return null;yield return null;Check(!motes.isPlaying&&motes.particleCount==0,"Reduced Motion clears portal motes");polish.ReducedMotion=false;yield return null;Check(motes.isPlaying,"Portal motes resume");}else Check(gate==null&&world.Portal.gameObject.activeInHierarchy&&world.Portal.GetComponentsInChildren<Renderer>(true).Length==0,"Trophy trigger active with no portal visuals or activation mechanism");
    var portalCamera=world.transform.root.GetComponentsInChildren<Camera>(true).First(c=>c.name=="PrototypeOverviewCamera_NoController");var portalPos=portalCamera.transform.position;var portalRot=portalCamera.transform.rotation;bool portalOrtho=portalCamera.orthographic;portalCamera.orthographic=false;portalCamera.transform.position=world.Portal.transform.position+new Vector3(8,4,-12);portalCamera.transform.LookAt(world.Portal.transform.position+new Vector3(0,2,-1.8f));try{Capture(portalCamera,session.Hud,1280,720,"Portal"+realm);}finally{portalCamera.transform.SetPositionAndRotation(portalPos,portalRot);portalCamera.orthographic=portalOrtho;}
    var first=world.Tokens[0];if(realm==0){var detailCamera=session.CurrentRealm.transform.root.GetComponentsInChildren<Camera>(true).First(c=>c.name=="PrototypeOverviewCamera_NoController");var oldPos=detailCamera.transform.position;var oldRot=detailCamera.transform.rotation;bool oldOrtho=detailCamera.orthographic;detailCamera.orthographic=false;detailCamera.transform.position=first.Center+new Vector3(4,3,-7);detailCamera.transform.LookAt(first.Center);try{Capture(detailCamera,session.Hud,1280,720,"TokenCloseup");}finally{detailCamera.transform.SetPositionAndRotation(oldPos,oldRot);detailCamera.orthographic=oldOrtho;}}int start=ledger.Count;Check(!first.TryPass(first.Center+Vector3.right*20,first.Center+Vector3.right*20),"Out-of-range collection rejected");Check(!first.TryPass(new Vector3(float.NaN,0,0),first.Center),"Malformed positions rejected");
    probe.transform.position=first.Center;Physics.SyncTransforms();double wait=EditorApplication.timeSinceStartup+.15;while(EditorApplication.timeSinceStartup<wait)yield return null;Check(ledger.Count==start,"Unbound physics object cannot collect");
    session.BindPlayer(probe.transform);wait=EditorApplication.timeSinceStartup+.2;while(EditorApplication.timeSinceStartup<wait)yield return null;Check(first.IsCollected&&ledger.Count==start+1,"Automatic bound-reference collection");Check(!first.TestCollect(),"Repeated token collection blocked");
    session.BindPlayer(null);yield return null;var second=world.Tokens[1];sphere.enabled=false;probe.transform.position=second.Center+Vector3.left*3;session.BindPlayer(probe.transform);yield return null;probe.transform.position=second.Center+Vector3.right*3;yield return null;yield return null;Check(second.IsCollected,"Passing through collects between sampled endpoints without physics");
    session.BindPlayer(null);yield return null;var third=world.Tokens[2];probe.transform.position=third.Center+Vector3.right*3.2f;sphere.radius=1;sphere.enabled=true;session.BindPlayer(probe.transform);Physics.SyncTransforms();wait=EditorApplication.timeSinceStartup+.25;while(EditorApplication.timeSinceStartup<wait)yield return null;Check(third.IsCollected,"Collider surface touch collects while reference center is outside zone");
    session.BindPlayer(null);sphere.enabled=false;yield return null;
    var copy=Object.Instantiate(first.gameObject).GetComponent<JourneyToken>();copy.Refresh(ledger);Check(copy.IsCollected&&!copy.TestCollect(),"Duplicate/reloaded ID restores collected state");Object.Destroy(copy.gameObject);
    foreach(var token in world.Tokens){if(realm==3&&token==world.Tokens.Last())continue;token.TestCollect();}
    int expected=realm==0?10:realm==1?18:realm==2?26:35;Check(ledger.Count==expected,"Token count persists: "+expected);Check(events==expected&&nestedRejected,"Exactly one event per token and reentrant collection rejected");Check(session.Hud.GetComponentInChildren<JourneyTokenHud>().DisplayText=="Journey Tokens: "+expected,"HUD reflects persistent tally");
    var camera=session.CurrentRealm.transform.root.GetComponentsInChildren<Camera>(true).First(c=>c.name=="PrototypeOverviewCamera_NoController");Capture(camera,session.Hud,1280,720,"Realm"+realm);
    var oldRealm=session.CurrentRealm;Check(!world.Portal.TryEnter(oldRealm.Entrance),"Portal rejects entrance/far reference");
    if(realm==1){probe.transform.position=oldRealm.Exit.position;session.BindPlayer(probe.transform);while(session.CurrentRealm==oldRealm&&!session.IsTransitioning)yield return null;Check(!session.TryAdvance(oldRealm,probe.transform),"Automatic portal entry uses P11 duplicate guard");}
    else if(realm==3){Check(world.Portal.TestEnter(),"Trophy demo completes after optional tokens");Check(!world.Portal.TestEnter(),"Repeated trophy collection rejected");}
    else{Check(world.Portal.TestEnter(),"Public portal test uses P11 flow");Check(!world.Portal.TestEnter(),"Repeated portal test rejected");}
    if(realm<3){while(session.IsTransitioning)yield return null;Check(IntegrationSession.Instance==session&&ledger.Count==expected,"Session tally survives scene unload");Check(session.PlayerReference==null,"P11 arrival binding remains optional");}
   }
   Check(loads==3&&complete==1&&session.IsComplete,"Exactly three loads and one Journey Complete");Check(ledger.Count==35,"Missing tokens do not gate completion");Check(Object.FindObjectsByType<JourneyLedger>().Length==1,"Only one session ledger");Capture(session.CurrentRealm.transform.root.GetComponentsInChildren<Camera>(true).First(c=>c.name=="PrototypeOverviewCamera_NoController"),session.Hud,1920,1080,"Completion1080");Object.Destroy(probe);
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
                Directory.CreateDirectory("Documentation/P125");File.WriteAllBytes("Documentation/P125/"+name+".png",image.EncodeToPNG());
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
