using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Polish;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
 public static class CrystalCaveUpgrade
 {
  const string Art="Assets/Art/Environments/CrystalCave/MineralUpgrade";
  static void Ref(Object o,string field,Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
  static Material Mat(string name,Color tint,Color emission,bool paving=false)
  {
   string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
   m=new Material(AssetDatabase.LoadAssetAtPath<Material>(paving?"Assets/Art/Environments/Ruins/SandstoneUpgrade/GoldenCrackedPaving.mat":"Assets/Art/Environments/Jungle/NaturalVisuals/AgedLimestone.mat")){name=name,enableInstancing=true};
   m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",emission.maxColorComponent>0?.42f:.2f);m.SetFloat("_BumpScale",.5f);
   if(emission.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);}
   AssetDatabase.CreateAsset(m,path);return m;
  }
  static Mesh Crystal()
  {
   var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/HexagonalMineral.asset");if(existing!=null)return existing;
   var v=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();var uv=new System.Collections.Generic.List<Vector2>();
   void Face(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);triangles.AddRange(new[]{n,n+1,n+2});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.up});}
   for(int i=0;i<6;i++){float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;var p=new Vector3(Mathf.Cos(a)*.5f,0,Mathf.Sin(a)*.5f);var q=new Vector3(Mathf.Cos(b)*.5f,0,Mathf.Sin(b)*.5f);var pu=p+Vector3.up*.72f;var qu=q+Vector3.up*.72f;Face(p,pu,q);Face(q,pu,qu);Face(pu,Vector3.up,qu);Face(Vector3.zero,p,q);}
   var mesh=new Mesh{name="HexagonalMineral"};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"/HexagonalMineral.asset");return mesh;
  }
  static Transform Shape(Transform parent,string name,Vector3 p,Vector3 scale,Material mat,Mesh mesh=null)
  {
   var go=GameObject.CreatePrimitive(mesh==null?PrimitiveType.Cube:PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());if(mesh!=null)go.GetComponent<MeshFilter>().sharedMesh=mesh;return go.transform;
  }
  [MenuItem("Tools/Lost Worlds/Crystal Cave/Apply Mineral Upgrade")]
  public static void Build()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[2]);var root=scene.GetRootGameObjects().Single().transform;
   if(root.Find("MineralCavernPolish")!=null){RefineEmission();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Validate();return;}
   Directory.CreateDirectory(Art);AssetDatabase.Refresh();
   var rock=Mat("LayeredBasalt",new Color(.45f,.50f,.61f),Color.black);var ledge=Mat("MineralStrata",new Color(.56f,.53f,.67f),Color.black);var floor=Mat("SlatePath",new Color(.48f,.57f,.65f),Color.black,true);
   var cyan=Mat("CyanQuartz",new Color(.25f,.64f,.7f),new Color(.12f,.55f,.65f));var blue=Mat("BlueQuartz",new Color(.31f,.4f,.8f),new Color(.12f,.23f,.65f));var violet=Mat("AmethystStone",new Color(.60f,.32f,.76f),new Color(.4f,.12f,.6f));var active=Mat("PedestalActivated",new Color(.42f,.8f,.88f),new Color(.3f,.9f,1.2f));
   var pathRoot=root.Find("Environment/CavePath_8mWide");
   foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
   {
    if(r.transform.GetComponentsInParent<Transform>().Any(t=>t.name=="P125_Journey"||t.name.StartsWith("P12_")))continue;
    string n=r.sharedMaterial!=null?r.sharedMaterial.name.ToLowerInvariant():"";
    if(r.name=="LocationOnly_NoTrigger"){r.enabled=false;continue;}
    bool path=pathRoot!=null&&r.transform.IsChildOf(pathRoot);
    r.sharedMaterial=path?floor:n.Contains("crystal")?(Mathf.RoundToInt(r.transform.position.z)%2==0?cyan:violet):rock;
    if(path){var f=r.GetComponent<MeshFilter>();if(f!=null&&f.sharedMesh!=null){string p=Art+"/Road_"+f.sharedMesh.name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(m==null){m=Object.Instantiate(f.sharedMesh);m.uv=m.vertices.Select(v=>{var w=f.transform.TransformPoint(v);return new Vector2(w.x/6,w.z/6);}).ToArray();m.RecalculateTangents();AssetDatabase.CreateAsset(m,p);}f.sharedMesh=m;}}
   }
   var group=new GameObject("MineralCavernPolish").transform;group.SetParent(root,false);var crystal=Crystal();var organic=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environments/Jungle/NaturalVisuals/OrganicRock.asset");
   for(int side=-1;side<=1;side+=2)for(int i=0;i<15;i++)
   {
    float z=2+i*6,x=side*(15+i%3);
    for(int layer=0;layer<3;layer++){var shelf=Shape(group,"LayeredRockShelf",new Vector3(x,1.4f+layer*2,z),new Vector3(5.3f,2.8f,7),layer%2==0?rock:ledge,organic);shelf.localRotation=Quaternion.Euler(0,i*17,layer*4*side);}
    if(i%2==0)for(int k=0;k<3;k++){var shard=Shape(group,"MineralCluster",new Vector3(side*(10.5f+k*.8f),.1f,z+k*.6f),new Vector3(.9f+k*.15f,2.3f+(i+k)%3,1),new[]{cyan,blue,violet}[(i+k)%3],crystal);shard.localRotation=Quaternion.Euler(side*k*7,i*33,side*(k-1)*12);}
    if(i%3==1){Shape(group,"Stalagmite",new Vector3(side*12,.1f,z),new Vector3(1.4f,3.1f,1.4f),ledge,crystal);}
   }
   foreach(float z in new[]{3f,26f,66f,88f})for(int k=-3;k<=3;k++)
   {var pendant=Shape(group,"Stalactite",new Vector3(k*3.7f,10.8f,z),new Vector3(1.2f,2+(k*k%3)*.4f,1.2f),rock,crystal);pendant.localRotation=Quaternion.Euler(180,k*21,0);}
   RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.46f,.52f,.65f);RenderSettings.ambientEquatorColor=new Color(.32f,.36f,.48f);RenderSettings.ambientGroundColor=new Color(.19f,.23f,.31f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.19f,.23f,.32f);RenderSettings.fogStartDistance=100;RenderSettings.fogEndDistance=250;
   foreach(var l in root.GetComponentsInChildren<Light>()){if(l.type==LightType.Directional){l.color=new Color(.67f,.77f,1);l.intensity=1.1f;l.shadows=LightShadows.Soft;l.shadowStrength=.5f;}else{l.intensity=1.25f;l.range=16;l.shadows=LightShadows.None;}}
   foreach(var c in root.GetComponentsInChildren<Camera>()){c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=RenderSettings.fogColor;}
   var mist=root.GetComponentsInChildren<ParticleSystem>().FirstOrDefault(p=>p.name=="SlowMotes_East");if(mist!=null){var main=mist.main;main.startColor=new Color(.42f,.56f,.77f,.1f);main.startSpeed=.04f;main.startSize=.65f;}
   // Reuse the authored Ruins pedestal with all tested runtime hooks; only visuals/references change.
   var ruins=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[1],OpenSceneMode.Additive);var source=ruins.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PortalUnlockMechanism>(true)).Single();var go=Object.Instantiate(source.gameObject,root);EditorSceneManager.CloseScene(ruins,true);
   go.name="CrystalPedestal_ACTIVATE";go.transform.position=new Vector3(5,0,80);var gate=go.GetComponent<PortalUnlockMechanism>();Ref(gate,"portal",root.GetComponentInChildren<JourneyRealm>().Portal);Ref(gate,"polish",root.GetComponentInChildren<RealmPolish>());Ref(root.GetComponentInChildren<RealmBindings>(),"exitGate",gate);
   foreach(var r in go.GetComponentsInChildren<MeshRenderer>())if(r.GetComponent<TextMesh>()==null)r.sharedMaterial=rock;
   var pivot=go.transform.Find("LeverPivot");foreach(Transform child in pivot.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
   var focus=Shape(pivot,"CrystalActivationFocus",Vector3.zero,new Vector3(.9f,1.4f,.9f),blue,crystal);
   var s=new SerializedObject(gate);var reveal=s.FindProperty("revealObjects");reveal.arraySize=1;reveal.GetArrayElementAtIndex(0).objectReferenceValue=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="CityLightPortal").gameObject;
   var feedback=s.FindProperty("feedbackRenderers");feedback.arraySize=1;feedback.GetArrayElementAtIndex(0).objectReferenceValue=focus.GetComponent<Renderer>();s.FindProperty("successMaterial").objectReferenceValue=active;s.ApplyModifiedPropertiesWithoutUndo();
   RefineEmission();EditorSceneManager.SetActiveScene(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Validate();
  }
  static void RefineEmission()
  {
   string[] names={"CyanQuartz","BlueQuartz","AmethystStone","PedestalActivated"};
   Color[] colors={new Color(.18f,.8f,1),new Color(.18f,.4f,1),new Color(.7f,.22f,1),new Color(.45f,1.2f,1.6f)};
   for(int i=0;i<names.Length;i++){var m=AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+names[i]+".mat");m.SetColor("_EmissionColor",colors[i]);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;m.EnableKeyword("_EMISSION");EditorUtility.SetDirty(m);}
   var rock=AssetDatabase.LoadAssetAtPath<Material>(Art+"/LayeredBasalt.mat");rock.SetColor("_BaseColor",new Color(.62f,.67f,.79f));EditorUtility.SetDirty(rock);
   RenderSettings.ambientSkyColor=new Color(.55f,.61f,.74f);RenderSettings.ambientGroundColor=new Color(.24f,.28f,.38f);
  }
  [MenuItem("Tools/Lost Worlds/Crystal Cave/Validate Mineral Realm")]
  public static void Validate()
  {
   var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[2]);var root=scene.GetRootGameObjects().Single();var world=root.GetComponentInChildren<JourneyRealm>();var gate=root.GetComponentInChildren<PortalUnlockMechanism>();if(world.Tokens.Length!=8||gate==null||gate.Portal!=world.Portal||gate.GetComponent<PortalAutoRunPause>()==null)throw new Exception("Cave token/gate/pause setup invalid");
   if(root.transform.Find("MineralCavernPolish").GetComponentsInChildren<Collider>().Length!=0||gate.GetComponentsInChildren<Collider>().Any(c=>!c.isTrigger))throw new Exception("New decoration blocks road");
   foreach(string name in new[]{"CyanQuartz","BlueQuartz","AmethystStone","PedestalActivated"})if(!AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+name+".mat").IsKeywordEnabled("_EMISSION"))throw new Exception("Crystal emission disabled: "+name);
   Physics.SyncTransforms();var path=root.GetComponentInChildren<RealmBindings>().GetMainPath();int count=0;
   for(int i=1;i<path.Length;i++)for(int j=0;j<=20;j++){var p=Vector3.Lerp(path[i-1],path[i],j/20f);if(!Realmwalker.Authoring.RealmJumpAuthoring.CheckOpening(root.transform,p)&&!Physics.RaycastAll(p+Vector3.up*20,Vector3.down,40,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.name=="ContinuousGround"))throw new Exception("Missing floor");var hits=Physics.OverlapCapsule(p+Vector3.up*.65f,p+Vector3.up*1.65f,.3f,~0,QueryTriggerInteraction.Ignore).Where(c=>c.GetComponentInParent<Realmwalker.RealmwalkerRunner>()==null&&!c.transform.GetComponentsInParent<Transform>().Any(t=>t.name=="P85_EnvironmentalObstacles"));if(hits.Any())throw new Exception("Cave road blocker: "+hits.First().name);count++;}
   Debug.Log("CAVE VALID: 8 tokens, reused gate/pause, no decorative colliders; "+count+" road support/body-clearance samples passed.");
  }
  [MenuItem("Tools/Lost Worlds/Crystal Cave/Play Mineral Demo")]
  public static void Play(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[2]);P12PolishBuilder.Play();EditorWindow.GetWindow<P125JourneyDemoWindow>("Journey Demo").Show();}
  public static void BuildBatch()=>EditorApplication.delayCall+=()=>{try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
 }
}


