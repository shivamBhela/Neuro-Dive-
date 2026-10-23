using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Polish;
using LostWorlds.Person2.World;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
 public static class P125JourneyBuilder
 {
  public const string Folder="Assets/Prefabs/World/P125Journey",Art="Assets/Art/Materials/P125Journey";
  public static readonly int[] Counts={10,8,8,10};
  static readonly string[] Names={"Jungle","Ruins","CrystalCave","HiddenCity"};
  static void Ref(Object target,string field,Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
  static Transform Child(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
  static Material Material(string name,Color color){string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.22f);m.SetFloat("_Cull",0);AssetDatabase.CreateAsset(m,path);return m;}
  static GameObject Shape(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material){var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go;}
  static void Prefabs()
  {
   foreach(var path in new[]{Folder,Art})Directory.CreateDirectory(path);AssetDatabase.Refresh();
   var gold=Material("TokenWarmGold",new Color(.85f,.64f,.24f));var inset=Material("TokenIvory",new Color(.94f,.89f,.67f));var portalMat=Material("QuietPortalTeal",new Color(.38f,.78f,.72f));
   if(!File.Exists(Folder+"/JourneyToken.prefab")){var go=new GameObject("JourneyToken");var token=go.AddComponent<JourneyToken>();go.GetComponent<SphereCollider>().isTrigger=true;go.GetComponent<SphereCollider>().radius=.45f;var visual=Child(go.transform,"CoinVisual");var coin=Shape(visual,"Coin",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.8f,.045f,.8f),gold);coin.transform.localRotation=Quaternion.Euler(90,0,0);var mark=Shape(visual,"QuietDiamond",PrimitiveType.Cube,new Vector3(0,0,-.052f),new Vector3(.24f,.24f,.025f),inset);mark.transform.localRotation=Quaternion.Euler(0,0,45);Ref(token,"visual",visual.gameObject);PrefabUtility.SaveAsPrefabAsset(go,Folder+"/JourneyToken.prefab");Object.DestroyImmediate(go);}
   if(!File.Exists(Folder+"/RealmExitPortal.prefab"))
   {
    var go=new GameObject("RealmExitPortal");go.AddComponent<RealmExitPortal>();var box=go.GetComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(8,6,4);
    var mesh=new Mesh{name="QuietPortalArch"};var v=new Vector3[66];var tris=new int[192];for(int i=0;i<=32;i++){float a=i*Mathf.PI/32;v[2*i]=new Vector3(Mathf.Cos(a)*3.9f,Mathf.Sin(a)*5.1f,-1.8f);v[2*i+1]=new Vector3(Mathf.Cos(a)*3.74f,Mathf.Sin(a)*4.94f,-1.8f);if(i<32){int n=i*6;tris[n]=i*2;tris[n+1]=i*2+1;tris[n+2]=i*2+2;tris[n+3]=i*2+1;tris[n+4]=i*2+3;tris[n+5]=i*2+2;}}mesh.vertices=v;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"/QuietPortalArch.asset");
    var arch=Child(go.transform,"OpenArch_NoSolidSurface");arch.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;arch.gameObject.AddComponent<MeshRenderer>().sharedMaterial=portalMat;
    foreach(float x in new[]{-3.8f,3.8f})Shape(go.transform,"QuietBase",PrimitiveType.Sphere,new Vector3(x,.12f,-1.8f),Vector3.one*.4f,portalMat);
    PrefabUtility.SaveAsPrefabAsset(go,Folder+"/RealmExitPortal.prefab");Object.DestroyImmediate(go);
   }
  }
  static Vector3 AtZ(Vector3[] path,float z){for(int i=0;i<path.Length-1;i++)if(z>=path[i].z&&z<=path[i+1].z)return Vector3.Lerp(path[i],path[i+1],Mathf.InverseLerp(path[i].z,path[i+1].z,z));throw new Exception("Token Z outside path "+z);}
  [MenuItem("Tools/Lost Worlds/P12.5/Setup Journey Enhancements")]
  public static void Setup()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;Prefabs();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
   for(int realm=0;realm<4;realm++)
   {
    var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[realm]);var root=scene.GetRootGameObjects().Single().transform;if(root.Find("P125_Journey")!=null)continue;
    var group=Child(root,"P125_Journey");var owner=group.gameObject.AddComponent<JourneyRealm>();var bindings=root.GetComponentInChildren<RealmBindings>();if(bindings==null||bindings.Exit==null)throw new Exception("P11 realm/exit missing: "+scene.name);Ref(owner,"realm",bindings);Ref(owner,"polish",root.GetComponentInChildren<RealmPolish>());
    var portalGo=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/RealmExitPortal.prefab"));if(portalGo==null)throw new Exception("Portal prefab import missing");portalGo.transform.SetParent(group);portalGo.transform.SetPositionAndRotation(bindings.Exit.position,bindings.Exit.rotation);portalGo.name=realm==3?"Portal_JourneyComplete":"Portal_To_"+Names[realm+1];var portal=portalGo.GetComponent<RealmExitPortal>();if(portal==null||portalGo.GetComponent<BoxCollider>()==null)throw new Exception("Portal components missing");Ref(portal,"realm",bindings);Ref(owner,"portal",portal);portalGo.GetComponent<BoxCollider>().size=new SerializedObject(bindings).FindProperty("exitHalfExtents").vector3Value*2;PrefabUtility.RecordPrefabInstancePropertyModifications(portal);PrefabUtility.RecordPrefabInstancePropertyModifications(portalGo.GetComponent<BoxCollider>());
    float[][] shared={new float[]{8,23,36,48,69,95},new float[]{8,17,31,39,54,60,73,78},new float[]{8,14,26,42,48,62,68,81},new float[]{6,12,23,26,71,82}};
    var list=new System.Collections.Generic.List<JourneyToken>();var ground=root.Find(realm==0?"Environment/Terrain_Ground/JungleGround":"Environment/Terrain_Ground/ContinuousGround").GetComponent<Collider>();
    void Place(string branch,float[] distances,Vector3[] path){for(int i=0;i<distances.Length;i++){var p=AtZ(path,distances[i]);if(!ground.Raycast(new Ray(p+Vector3.up*30,Vector3.down),out var hit,60))throw new Exception("Ungrounded token");p.y=hit.point.y+1;var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/JourneyToken.prefab"));go.transform.SetParent(group);go.transform.position=p;go.name="Token_"+branch+"_"+(i+1);var token=go.GetComponent<JourneyToken>();Ref(token,"owner",owner);var so=new SerializedObject(token);so.FindProperty("tokenId").stringValue=Names[realm].ToLowerInvariant()+"."+branch+"."+(i+1);so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(token);list.Add(token);}}
    Place("shared",shared[realm],bindings.GetMainPath());if(realm==0){Place("left",new float[]{77,85},bindings.GetMainPath());Place("right",new float[]{41,54},bindings.GetMainPath());}else if(bindings.Branch!=null){float[] zs=new float[]{39,63};Place("left",zs,bindings.Branch.GetPath(RouteChoice.Left));Place("right",zs,bindings.Branch.GetPath(RouteChoice.Right));}
    var serialized=new SerializedObject(owner);var array=serialized.FindProperty("tokens");array.arraySize=list.Count;for(int i=0;i<list.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=list[i];serialized.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(scene);
   }
   AssetDatabase.SaveAssets();Validate();
  }
  [MenuItem("Tools/Lost Worlds/P12.5/Validate Journey Enhancements")]
  public static void Validate()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;var previous=EditorSceneManager.GetSceneManagerSetup();var ids=new System.Collections.Generic.HashSet<string>();try{
    for(int i=0;i<4;i++){var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);var root=scene.GetRootGameObjects().Single();var realm=root.GetComponentInChildren<JourneyRealm>();if(realm==null||realm.Tokens.Length!=Counts[i]||realm.Portal==null||realm.Portal.Realm==null)throw new Exception("Invalid journey realm");if(realm.Portal.transform.position!=realm.Portal.Realm.Exit.position)throw new Exception("Portal not at P11 exit");foreach(var collider in realm.GetComponentsInChildren<Collider>())if(!collider.isTrigger)throw new Exception("Journey objects may not block paths");var obstacles=root.transform.Find("Environment/P85_EnvironmentalObstacles").GetComponentsInChildren<Collider>();foreach(var token in realm.Tokens){if(!ids.Add(token.TokenId))throw new Exception("Duplicate ID");foreach(var obstacle in obstacles){var closest=obstacle.ClosestPoint(token.Center);closest.y=token.Center.y;if(Vector3.Distance(closest,token.Center)<2.8f)throw new Exception("Token too close to obstacle: "+token.TokenId);}}
     Debug.Log("P12.5 VALID: "+scene.name+"; "+realm.Tokens.Length+" unique tokens, non-solid portal at P11 exit, obstacle clearance.");}
   }finally{EditorSceneManager.RestoreSceneManagerSetup(previous);}
  }
  [MenuItem("Tools/Lost Worlds/P12.5/Play Journey Enhancement Demo")]
  public static void Play(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0]);P12PolishBuilder.Play();EditorWindow.GetWindow<P125JourneyDemoWindow>("Journey Demo").Show();}
 }
 public sealed class P125JourneyDemoWindow : EditorWindow
 {
  void OnInspectorUpdate()=>Repaint();
  void OnGUI()
  {
   EditorGUILayout.LabelField("P12.5 • Controller-free test",EditorStyles.boldLabel);var s=IntegrationSession.Instance;
   if(!EditorApplication.isPlaying||s==null||s.CurrentRealm==null){EditorGUILayout.HelpBox("Use Play Journey Enhancement Demo to begin.",MessageType.Info);return;}
   var realm=s.CurrentRealm.GetComponentInParent<Transform>().root.GetComponentInChildren<JourneyRealm>();
   EditorGUILayout.LabelField(s.CurrentRealm.gameObject.scene.name);EditorGUILayout.LabelField("Journey Tokens: "+(realm!=null&&realm.Ledger!=null?realm.Ledger.Count:0));
   EditorGUILayout.HelpBox(s.CurrentRealm.RealmIndex==3?"Reach the golden trophy to complete the journey automatically. Tokens are optional; no ACTIVATE is required.":"Buttons exercise the real token and P11 portal APIs. No player is created or moved. Tokens are optional.",MessageType.Info);
   using(new EditorGUI.DisabledScope(realm==null||realm.Ledger==null||!s.CommandsReady)){
    var gate=s.CurrentRealm.transform.root.GetComponentInChildren<PortalUnlockMechanism>();
    if(gate!=null&&s.CurrentRealm.RealmIndex!=3){EditorGUILayout.LabelField(gate.CanExit?"Portal unlocked":"Portal locked — activate the mechanism");
     EditorGUILayout.LabelField(s.AutoRunPause.IsPauseRequested?"Auto-run: PAUSE requested — waiting for activation":"Auto-run: no pause requested (controller hook only)");
     if(GUILayout.Button("Focus lever for E / hardware ACTIVATE (test)")){s.BindPlayer(gate.transform);var camera=s.CurrentRealm.transform.root.GetComponentsInChildren<Camera>(true).FirstOrDefault(c=>c.name=="PrototypeOverviewCamera_NoController");if(camera!=null){camera.orthographic=false;camera.transform.position=gate.transform.position+new Vector3(-7,4,-9);camera.transform.LookAt(gate.transform.position+Vector3.up*1.5f);}}
     if(GUILayout.Button("Activate portal lever (controller-free test)"))gate.TestActivate();}
    if(GUILayout.Button("Collect next token (test)")){var token=realm.Tokens.FirstOrDefault(t=>!t.IsCollected);if(token!=null)token.TestCollect();}
    if(GUILayout.Button("Collect remaining realm tokens (test)"))foreach(var token in realm.Tokens)token.TestCollect();
    if(GUILayout.Button(s.CurrentRealm.RealmIndex==3?"Reach trophy / simulate collection (test)":"Enter portal / skip remaining tokens"))realm.Portal.TestEnter();
   }
   if(s.IsComplete)EditorGUILayout.HelpBox("Journey Complete. Missing tokens have no penalty.",MessageType.Info);
  }
 }
}

