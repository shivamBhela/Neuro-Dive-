using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.World;
using LostWorlds.Person2.UI;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
 public static class P11IntegrationBuilder
 {
  public const string Prefab="Assets/Prefabs/Integration/P11/IntegrationSession.prefab";
  public static void SetupBatch(){Setup();Validate();}
  static void Ref(Object target,string field,Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
  static Transform Child(Transform parent,string name,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent);t.position=position;return t;}
  static void Paths(Object target,string field,Transform parent,Vector2[] points,int realm)
  {
   var s=new SerializedObject(target);var a=s.FindProperty(field);a.arraySize=points.Length;
   for(int i=0;i<points.Length;i++){float z=points[i].y;float h=realm==0?(z<24?0:z<50?(z-24)/26*2:z<88?2:2+Mathf.Clamp01((z-88)/20)*2):0;a.GetArrayElementAtIndex(i).objectReferenceValue=Child(parent,field+"_"+i,new Vector3(points[i].x,h+.15f,z));}s.ApplyModifiedPropertiesWithoutUndo();
  }
  [MenuItem("Tools/Lost Worlds/P11/Setup Integration Readiness")]
  public static void Setup()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   Directory.CreateDirectory("Assets/Prefabs/Integration/P11");AssetDatabase.Refresh();
   if(!File.Exists(Prefab))
   {
    var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    var go=new GameObject("Person2_IntegrationSession");var channel=go.AddComponent<CommandChannel>();var session=go.AddComponent<IntegrationSession>();var bridge=go.AddComponent<PlayerCommandBridge>();var actor=go.AddComponent<InteractionInteractor>();actor.enabled=false;
    var simulator=go.AddComponent<HardwareCommandSimulator>();simulator.Bind(channel);simulator.enabled=false;
    var adapter=go.AddComponent<ProcessedHardwareAdapter>();adapter.Bind(channel);adapter.enabled=false;
    var hudGo=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/P7/Person2Hud.prefab"));hudGo.transform.SetParent(go.transform,false);var hud=hudGo.GetComponent<Person2Hud>();
    var effort=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/P8/EffortPanel.prefab"));effort.transform.SetParent(hud.transform,false);
    var label=new GameObject("JourneyStatus",typeof(RectTransform),typeof(Text)).GetComponent<Text>();label.transform.SetParent(hud.transform,false);label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,1);label.rectTransform.anchoredPosition=new Vector2(0,-70);label.rectTransform.sizeDelta=new Vector2(850,72);label.alignment=TextAnchor.MiddleCenter;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=24;label.color=Color.white;label.raycastTarget=false;
    var debug=Object.Instantiate(label,label.transform.parent);debug.name="CommandStatus";debug.rectTransform.anchorMin=debug.rectTransform.anchorMax=new Vector2(0,1);debug.rectTransform.pivot=new Vector2(0,1);debug.rectTransform.anchoredPosition=new Vector2(32,-130);debug.rectTransform.sizeDelta=new Vector2(360,100);debug.fontSize=20;debug.alignment=TextAnchor.UpperLeft;debug.gameObject.AddComponent<CommandDebugFeedback>().Bind(channel,debug);
    var connection=Object.Instantiate(label,label.transform.parent);connection.name="HardwareStatus";connection.rectTransform.anchorMin=connection.rectTransform.anchorMax=new Vector2(0,1);connection.rectTransform.pivot=new Vector2(0,1);connection.rectTransform.anchoredPosition=new Vector2(32,-240);connection.rectTransform.sizeDelta=new Vector2(360,150);connection.fontSize=20;connection.alignment=TextAnchor.UpperLeft;connection.gameObject.AddComponent<HardwareAdapterStatus>().Bind(adapter,connection);
    Ref(session,"channel",channel);Ref(session,"bridge",bridge);Ref(session,"interactor",actor);Ref(session,"hud",hud);Ref(session,"status",label);Ref(bridge,"session",session);Ref(bridge,"channel",channel);
    PrefabUtility.SaveAsPrefabAsset(go,Prefab);Object.DestroyImmediate(go);
   }
   for(int i=0;i<4;i++)
   {
    var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);var root=scene.GetRootGameObjects().Single();if(root.transform.Find("P11_Integration")!=null){var existing=root.transform.Find("P11_Integration").GetComponent<RealmBindings>();Ref(existing,"sessionPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<IntegrationSession>());EditorSceneManager.SaveScene(scene);continue;}
    var p=Child(root.transform,"P11_Integration",Vector3.zero);var binding=p.gameObject.AddComponent<RealmBindings>();var so=new SerializedObject(binding);so.FindProperty("realmIndex").intValue=i;so.ApplyModifiedPropertiesWithoutUndo();
    Ref(binding,"sessionPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<IntegrationSession>());
    var anchors=root.transform.Find("FutureGameplayPoints");string[] entrances={"StartPoint","RuinsEntrance","CrystalCaveEntrance","HiddenCityEntrance"};string[] exits={"JungleExit","RuinsExit","CrystalCaveExit","FinalDestination_LocationOnly"};Ref(binding,"entrance",anchors.Find(entrances[i]));Ref(binding,"exit",anchors.Find(exits[i]));
    Vector2[][] main={new[]{new Vector2(0,0),new Vector2(0,12),new Vector2(0,24),new Vector2(0,32),new Vector2(0,35),new Vector2(0,45),new Vector2(0,50),new Vector2(0,62),new Vector2(0,70),new Vector2(0,79),new Vector2(0,88),new Vector2(0,96),new Vector2(0,108)},new[]{new Vector2(0,0),new Vector2(0,18),new Vector2(3,35),new Vector2(0,52),new Vector2(0,70),new Vector2(0,88)},new[]{new Vector2(0,0),new Vector2(0,15),new Vector2(-3,30),new Vector2(0,46),new Vector2(3,62),new Vector2(0,78),new Vector2(0,88)},new[]{new Vector2(0,0),new Vector2(0,32),new Vector2(-10,43),new Vector2(-10,57),new Vector2(0,70),new Vector2(0,86)}};
    Paths(binding,"mainPath",p,main[i],i);
    if(i==3)
    {
     var branch=p.gameObject.AddComponent<BranchPathBinding>();var zone=root.GetComponentInChildren<RouteChoiceZone>();
     if(zone==null){var t=Child(p,"CityRouteChoice",new Vector3(0,1,31));zone=t.gameObject.AddComponent<RouteChoiceZone>();zone.GetComponent<BoxCollider>().size=new Vector3(12,4,8);zone.GetComponent<BoxCollider>().isTrigger=true;Ref(zone,"choiceZone",zone.GetComponent<BoxCollider>());}
     Ref(branch,"zone",zone);Ref(binding,"branch",branch);
     Paths(branch,"left",p,new[]{new Vector2(0,32),new Vector2(-10,43),new Vector2(-10,57),new Vector2(0,70)},i);
     Paths(branch,"right",p,new[]{new Vector2(0,32),new Vector2(10,43),new Vector2(10,57),new Vector2(0,70)},i);
    }
    EditorSceneManager.SaveScene(scene);
   }
   EditorBuildSettings.scenes=IntegrationSession.RealmScenes.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(EditorBuildSettings.scenes.Where(s=>!IntegrationSession.RealmScenes.Contains(s.path))).ToArray();AssetDatabase.SaveAssets();
  }
  [MenuItem("Tools/Lost Worlds/P11/Validate Integration Readiness")]
  public static void Validate()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   var previous=EditorSceneManager.GetSceneManagerSetup();try{
    for(int i=0;i<4;i++){var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);var b=Object.FindAnyObjectByType<RealmBindings>();if(b==null||b.RealmIndex!=i||b.Entrance==null||b.Exit==null||b.GetMainPath().Length<4||new SerializedObject(b).FindProperty("sessionPrefab").objectReferenceValue==null)throw new Exception("Invalid realm binding "+i);if(i==3&&(b.Branch==null||b.Branch.GetPath(RouteChoice.Left).Length<4||b.Branch.GetPath(RouteChoice.Right).Length<4))throw new Exception("Invalid branches");if(i==0&&(b.Branch!=null||b.GetComponentInParent<Transform>().root.GetComponentsInChildren<RouteChoiceZone>(true).Length!=0))throw new Exception("Jungle must not require route selection");if(!EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==scene.path))throw new Exception("Realm not enabled in build");}
    Debug.Log("P11 structural validation PASS: four realms, anchors, paths, branches and Build Settings.");
   }finally{EditorSceneManager.RestoreSceneManagerSetup(previous);}
  }
  [MenuItem("Tools/Lost Worlds/P11/Play Integration Preview")]
  public static void Play(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);EditorApplication.isPlaying=true;}
  [MenuItem("Tools/Lost Worlds/P11/Bind Selected Reference + Enable Simulator (Play Mode Test)")]
  public static void BindSelected(){var s=IntegrationSession.Instance;if(s!=null&&Selection.activeTransform!=null){s.BindPlayer(Selection.activeTransform);s.GetComponent<HardwareCommandSimulator>().enabled=true;}}
  [MenuItem("Tools/Lost Worlds/P11/Enable Hardware Adapter (Play Mode Test)")]
  public static void Hardware(){var s=IntegrationSession.Instance;if(s!=null)s.GetComponent<ProcessedHardwareAdapter>().enabled=true;}
  [MenuItem("Tools/Lost Worlds/P11/Advance From Exit (Play Mode Test)")]
  public static void Advance(){var s=IntegrationSession.Instance;if(s!=null&&s.CurrentRealm!=null)s.TryAdvance(s.CurrentRealm,s.CurrentRealm.Exit);}
 }
}
