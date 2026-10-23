using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.UI;
using LostWorlds.Person2.Polish;
using LostWorlds.Person2.World;

namespace LostWorlds.Person2.Demo
{
    /// <summary>Opt-in presentation/test harness. Never a player controller; delegates to frozen public APIs.</summary>
    public sealed class Person2BuildDemo : MonoBehaviour
    {
        IntegrationSession session;
        RealmBindings shown;
        Transform reference;
        bool smoke,finished;
        int issues,checks;
        float started;
        string selected="No test target selected";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();
            if(!args.Contains("-person2-demo")&&!args.Contains("-person2-smoke"))return;
            var go=new GameObject("Person2_BuildDemo_TEST_ONLY");DontDestroyOnLoad(go);
            var demo=go.AddComponent<Person2BuildDemo>();demo.smoke=args.Contains("-person2-smoke");
            if(demo.smoke)Application.runInBackground=true;
        }
        void Awake(){started=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string message,string trace,LogType type){if(type==LogType.Warning||type==LogType.Error||type==LogType.Exception||type==LogType.Assert)issues++;}
        IEnumerator Start()
        {
            while(IntegrationSession.Instance==null||IntegrationSession.Instance.CurrentRealm==null)yield return null;
            session=IntegrationSession.Instance;
            reference=new GameObject("DemoStationReference_NO_CONTROLLER").transform;reference.SetParent(transform,false);
            PrepareView();if(smoke)yield return Smoke();
        }
        void Update()
        {
            if(session!=null&&session.CurrentRealm!=shown)PrepareView();
            if(smoke&&!finished&&Time.realtimeSinceStartup-started>120){Debug.LogError("P14 SMOKE timeout");finished=true;Application.Quit(1);}
        }
        void PrepareView()
        {
            shown=session.CurrentRealm;if(shown==null)return;
            foreach(var camera in shown.transform.root.GetComponentsInChildren<Camera>(true))camera.enabled=camera.name=="PrototypeOverviewCamera_NoController";
            foreach(var listener in shown.transform.root.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;
            // The persistent IntegrationSession owns the listener, including scene transitions.
            selected="No test target selected";
        }
        JourneyRealm World=>shown!=null?shown.transform.root.GetComponentInChildren<JourneyRealm>():null;
        void Focus(Transform target,string label)
        {
            reference.position=target.position;session.BindPlayer(reference,reference);Physics.SyncTransforms();session.RefreshContext(true);selected=label;
            session.GetComponent<HardwareCommandSimulator>().enabled=true;
        }
        void FocusAction(InteractionCommand command)
        {
            var target=shown.transform.root.GetComponentsInChildren<InteractableBase>().FirstOrDefault(t=>t.Command==command&&!t.HasInteracted);
            if(target!=null)Focus(target.transform,target.name);else selected="No remaining target in this realm";
        }
        void OnGUI()
        {
            if(smoke||session==null||shown==null||session.IsComplete)return;
            float scale=Mathf.Max(.6f,Screen.height/900f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            GUILayout.BeginArea(new Rect(18,Screen.height/scale-300,355,285),GUI.skin.box);
            GUILayout.Label("PERSON 2 DEMO • No player controller");GUILayout.Label(selected);
            GUI.enabled=session.CommandsReady;
            GUILayout.BeginHorizontal();if(GUILayout.Button("Focus GRAB"))FocusAction(InteractionCommand.Grab);if(GUILayout.Button("Focus ACTIVATE"))FocusAction(InteractionCommand.Activate);GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();if(GUILayout.Button("Focus route")&&shown.Branch!=null)Focus(shown.Branch.Zone.transform,"Route: A/D or arrows");GUILayout.EndHorizontal();
            GUILayout.Label("G/R: GRAB • E: ACTIVATE • A/D: route\nSpace: JUMP event only • PgUp/PgDn/Backspace: effort");
            if(GUILayout.Button("Collect next Journey Token (test)")){var token=World.Tokens.FirstOrDefault(t=>!t.IsCollected);if(token!=null)token.TestCollect();}
            if(!shown.CompletionOnActivation&&GUILayout.Button("Enter portal / skip remaining tokens"))World.Portal.TestEnter();
            GUI.enabled=true;
            var adapter=session.GetComponent<ProcessedHardwareAdapter>();if(GUILayout.Button(adapter.enabled?"Disable hardware listener":"Enable hardware listener (localhost:55010)"))adapter.enabled=!adapter.enabled;
            var polish=shown.transform.root.GetComponentInChildren<RealmPolish>();polish.ReducedMotion=GUILayout.Toggle(polish.ReducedMotion,"Reduced Motion");
            if(GUILayout.Button("Quit demo"))Application.Quit();GUILayout.EndArea();
        }
        void Check(bool ok,string label){if(!ok)throw new InvalidOperationException("P14 SMOKE FAIL: "+label);checks++;Debug.Log("P14 SMOKE PASS: "+label);}
        IEnumerator Smoke()
        {
            while(World.Ledger==null)yield return null;
            var ledger=World.Ledger;int completions=0;session.Completed+=()=>completions++;
            for(int index=0;index<4;index++)
            {
                while(World==null||World.Ledger==null)yield return null;
                Check(shown.RealmIndex==index,"Realm order "+index);Check(World.Ledger==ledger,"Persistent ledger "+index);
                foreach(var target in shown.transform.root.GetComponentsInChildren<InteractableBase>())
                {
                    if(shown.CompletionOnActivation&&target is PortalUnlockMechanism)continue;
                    Focus(target.transform,target.name);float until=Time.unscaledTime+.3f;while(Time.unscaledTime<until)yield return null;
                    session.Channel.Send(target.Command==InteractionCommand.Grab?HardwareCommand.GRAB:HardwareCommand.ACTIVATE);
                    Check(target.HasInteracted,"Interaction "+target.name);Check(!target.TryInteract(session.Interactor),"Duplicate interaction rejected");
                }
                if(shown.Branch!=null){Focus(shown.Branch.Zone.transform,"route");session.Channel.Send(HardwareCommand.LEFT);session.Channel.Send(HardwareCommand.RIGHT);Check(shown.Branch.Zone.Selection==RouteChoice.Left,"Route locked");}
                session.BindPlayer(null);
                foreach(var token in World.Tokens){token.TestCollect();Check(token.IsCollected&&!token.TestCollect(),"Unique token "+token.TokenId);}
                Check(ledger.Count==new[]{10,18,26,36}[index],"Token total");Check(session.Hud.GetComponentInChildren<JourneyTokenHud>().DisplayText=="Journey Tokens: "+ledger.Count,"Build token HUD");
                foreach(var r in World.Portal.GetComponentsInChildren<Renderer>())Check(r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported,"Portal shader supported");
                var polish=shown.transform.root.GetComponentInChildren<RealmPolish>();polish.ReducedMotion=true;yield return null;yield return null;Check(shown.transform.root.GetComponentsInChildren<ParticleSystem>().All(p=>!p.isPlaying),"Reduced Motion");polish.ReducedMotion=false;
                yield return null;yield return null;
                CaptureRealm(index);
                for(int f=0;f<10;f++)yield return null;
                var portal=World.Portal;if(shown.CompletionOnActivation){Check(shown.transform.root.GetComponentInChildren<PortalUnlockMechanism>().TestActivate(),"Final ACTIVATE completes");while(!session.IsComplete)yield return null;}else Check(portal.TestEnter(),"Portal advances");Check(!portal.TestEnter(),"Duplicate transition blocked");
                if(index<3){while(session.IsTransitioning)yield return null;PrepareView();}
            }
            Check(session.IsComplete&&completions==1&&ledger.Count==36,"Complete once with 36 tokens");
            Check(issues==0,"No runtime warnings/errors");finished=true;Debug.Log("P14 SMOKE RESULT: "+checks+" checks; passed=True");Application.Quit(0);
        }
        void CaptureRealm(int index)
        {
            // Explicit render request also works when the automated player window is hidden.
            // Uses the actual build pipeline unchanged: no Editor capture/batching workaround.
            var camera=shown.transform.root.GetComponentsInChildren<Camera>(true).First(c=>c.enabled);
            var canvas=session.Hud.GetComponent<Canvas>();var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var target=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var active=RenderTexture.active;
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(Application.persistentDataPath,"P14_Realm"+index+".png"),image.EncodeToPNG());
            }
            finally{canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;RenderTexture.active=active;target.Release();Destroy(target);Destroy(image);}
        }
    }
}

