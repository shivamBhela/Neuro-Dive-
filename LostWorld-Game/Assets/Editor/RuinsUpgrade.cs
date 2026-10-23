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
 public static class RuinsUpgrade
 {
  const string Art="Assets/Art/Environments/Ruins/SandstoneUpgrade";
  static void Ref(Object target,string field,Object value){var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
  static Material Mat(string name,Color tint,bool paving=false)
  {
   string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
   m=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/Jungle/NaturalVisuals/AgedLimestone.mat")){name=name,enableInstancing=true};m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",.16f);
   if(paving){var tex=new Texture2D(256,256,TextureFormat.RGB24,true);for(int y=0;y<256;y++)for(int x=0;x<256;x++){int row=y/64;float joint=(y%64<2||(x+row%2*32)%64<2)?.42f:1;float crack=Mathf.Abs((x%64)-28-5*Mathf.Sin(y*.19f))<.65f&&y%64>20?.62f:1;float n=.82f+.18f*Mathf.PerlinNoise(x*.18f,y*.18f);tex.SetPixel(x,y,new Color(n*joint*crack,n*joint*crack,n*joint*crack));}tex.Apply();string png=Art+"/CrackedPaving.png";File.WriteAllBytes(png,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(png);var importer=(TextureImporter)AssetImporter.GetAtPath(png);importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.SaveAndReimport();m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(png));}
   AssetDatabase.CreateAsset(m,path);return m;
  }
  static Transform Shape(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat)
  {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());return go.transform;}
  [MenuItem("Tools/Lost Worlds/Ruins/Apply Sandstone Upgrade")]
  public static void Build()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[1]);var root=scene.GetRootGameObjects().Single().transform;
   if(root.Find("RuinsSandstonePolish")!=null){RefreshLandmarks(root);EditorSceneManager.SaveScene(scene);Validate();return;}
   Directory.CreateDirectory(Art);AssetDatabase.Refresh();
   var stone=Mat("SunwashedSandstone",new Color(1.28f,1.03f,.69f));var trim=Mat("CarvedOchre",new Color(.92f,.65f,.36f));var paving=Mat("GoldenCrackedPaving",new Color(.82f,.65f,.42f),true);var sand=Mat("WarmSand",new Color(1.05f,.81f,.49f));var dry=Mat("DryGrass",new Color(.68f,.57f,.29f));var shrub=Mat("SageShrub",new Color(.45f,.48f,.27f));
   var env=root.Find("Environment");
   foreach(var r in env.GetComponentsInChildren<MeshRenderer>(true))
   {
    if(r.GetComponentInParent<JourneyToken>()!=null)continue;
    string n=r.name.ToLowerInvariant();string mn=r.sharedMaterial!=null?r.sharedMaterial.name.ToLowerInvariant():"";
    if(n.Contains("locationonly")||n.Contains("notrigger")){r.enabled=false;continue;}
    if(n.Contains("water")||n.Contains("foam"))continue;
    bool path=r.transform.IsChildOf(env.Find("StonePath_8mWide"));
    r.sharedMaterial=path?paving:n=="continuousground"?sand:mn.Contains("moss")?trim:stone;
    if(path){var f=r.GetComponent<MeshFilter>();if(f!=null&&f.sharedMesh!=null){string p=Art+"/Road_"+f.sharedMesh.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(mesh==null){mesh=Object.Instantiate(f.sharedMesh);mesh.uv=mesh.vertices.Select(v=>{var w=f.transform.TransformPoint(v);return new Vector2(w.x/6,w.z/6);}).ToArray();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,p);}f.sharedMesh=mesh;}}
   }
   var vegetation=env.Find("RocksAndVegetation");if(vegetation!=null)foreach(var t in vegetation.Cast<Transform>().Where(t=>t.name.Contains("CanopyTree")||t.name.Contains("FernCluster")).ToArray())Object.DestroyImmediate(t.gameObject);
   // Hide location-only markers without deleting anchors or changing gameplay components.
   foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name=="LocationOnly_NoTrigger")foreach(var r in t.GetComponentsInChildren<Renderer>())r.enabled=false;
   var group=new GameObject("RuinsSandstonePolish").transform;group.SetParent(root,false);
   for(int side=-1;side<=1;side+=2)foreach(float z in new[]{17f,66f})
   {
    float x=side*20;
    Shape(group,"TemplePlinth",PrimitiveType.Cube,new Vector3(x,.35f,z),new Vector3(11,.7f,12),stone);
    for(int row=0;row<5;row++)for(int col=0;col<5;col++){if(row>2&&col==4)continue;Shape(group,"WeatheredAshlar",PrimitiveType.Cube,new Vector3(x-4+col*2+(row%2)*.3f,1.25f+row*1.35f,z+5),new Vector3(1.94f,1.29f,1.3f),row%3==0?trim:stone);}
    foreach(float dx in new[]{-4f,4f}){Shape(group,"FlutedColumn",PrimitiveType.Cylinder,new Vector3(x+dx,3,z-3),new Vector3(1.3f,3,1.3f),stone);Shape(group,"CarvedCapital",PrimitiveType.Cube,new Vector3(x+dx,6,z-3),new Vector3(2,.5f,2),trim);}
    for(int i=0;i<11;i++){float a=Mathf.PI*i/10;var block=Shape(group,"ArchVoussoir",PrimitiveType.Cube,new Vector3(x+4*Mathf.Cos(a),6+2.5f*Mathf.Sin(a),z-3),new Vector3(1.25f,1.1f,1.5f),i%2==0?stone:trim);block.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90);}
    for(int i=0;i<6;i++){var rubble=Shape(group,"CollapsedStone",PrimitiveType.Cube,new Vector3(x-4+i*1.5f,.5f,z+1+i%2),new Vector3(1.5f,.8f,1.1f),stone);rubble.localRotation=Quaternion.Euler(i*7,i*39,i*4);}
   }
   for(int side=-1;side<=1;side+=2)for(int i=0;i<13;i++)
   {
    float z=4+i*6.4f,x=side*(12+i%3*1.7f);
    for(int b=0;b<4;b++){var blade=Shape(group,"DryGrassTuft",PrimitiveType.Cube,new Vector3(x+b*.13f,.25f,z),new Vector3(.045f,.5f+b*.08f,.08f),dry);blade.localRotation=Quaternion.Euler(0,b*48,b*12-18);}
    if(i%3==0){var bush=Shape(group,"SparseSage",PrimitiveType.Sphere,new Vector3(x+side*2,.48f,z+1),new Vector3(1.5f,.9f,1.2f),shrub);bush.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environments/Jungle/NaturalVisuals/OrganicCanopy.asset");}
   }
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.78f,.78f,.72f);RenderSettings.ambientEquatorColor=new Color(.58f,.49f,.37f);RenderSettings.ambientGroundColor=new Color(.29f,.24f,.18f);RenderSettings.fogColor=new Color(.76f,.68f,.53f);RenderSettings.fogStartDistance=115;RenderSettings.fogEndDistance=290;
   var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.55f,.61f,.67f));sky.SetColor("_GroundColor",new Color(.55f,.43f,.28f));sky.SetFloat("_AtmosphereThickness",.8f);AssetDatabase.CreateAsset(sky,Art+"/DesertSky.mat");RenderSettings.skybox=sky;
   foreach(var light in root.GetComponentsInChildren<Light>())if(light.type==LightType.Directional){light.color=new Color(1,.88f,.69f);light.intensity=1.35f;light.shadows=LightShadows.Soft;light.shadowStrength=.7f;light.transform.rotation=Quaternion.Euler(43,-32,0);}
   foreach(var camera in root.GetComponentsInChildren<Camera>()){camera.clearFlags=CameraClearFlags.Skybox;camera.backgroundColor=RenderSettings.fogColor;}
   var dust=root.GetComponentsInChildren<ParticleSystem>().FirstOrDefault(p=>p.name=="SlowMotes_East");if(dust!=null){var main=dust.main;main.startColor=new Color(.85f,.70f,.44f,.12f);main.startSpeed=.05f;}
   // Clone the already-tested mechanism, then replace only its presentation and realm references.
   var jungle=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[0],OpenSceneMode.Additive);
   var source=jungle.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PortalUnlockMechanism>(true)).Single();
   var lever=Object.Instantiate(source.gameObject,root);EditorSceneManager.CloseScene(jungle,true);
   lever.name="RuinsStonePedestal_ACTIVATE";lever.transform.position=new Vector3(5,0,80);
   var gate=lever.GetComponent<PortalUnlockMechanism>();var realm=root.GetComponentInChildren<RealmBindings>();Ref(gate,"portal",root.GetComponentInChildren<JourneyRealm>().Portal);Ref(gate,"polish",root.GetComponentInChildren<RealmPolish>());Ref(realm,"exitGate",gate);
   foreach(var child in lever.GetComponentsInChildren<Transform>().Where(t=>t.parent==lever.transform&&t.name!="PortalActivationPrompt"&&t.name!="PromptBacking").ToArray())Object.DestroyImmediate(child.gameObject);
   Shape(lever.transform,"CarvedPedestalBase",PrimitiveType.Cube,new Vector3(0,.2f,0),new Vector3(1.9f,.4f,1.9f),trim);Shape(lever.transform,"AncientPedestal",PrimitiveType.Cylinder,new Vector3(0,.9f,0),new Vector3(1.2f,.7f,1.2f),stone);
   var pivot=new GameObject("LeverPivot").transform;pivot.SetParent(lever.transform,false);pivot.localPosition=new Vector3(0,1.65f,0);Shape(pivot,"StonePressPlate",PrimitiveType.Cylinder,Vector3.zero,new Vector3(1.3f,.12f,1.3f),trim);Ref(gate,"handle",pivot);
   var so=new SerializedObject(gate);so.FindProperty("activatedEuler").vector3Value=new Vector3(12,0,0);var reveal=so.FindProperty("revealObjects");reveal.arraySize=1;reveal.GetArrayElementAtIndex(0).objectReferenceValue=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="CaveThreshold").gameObject;so.ApplyModifiedPropertiesWithoutUndo();
   lever.transform.Find("PromptBacking").GetComponent<Renderer>().sharedMaterial=trim;
   RefreshLandmarks(root);EditorSceneManager.SetActiveScene(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Validate();
  }
  static void RefreshLandmarks(Transform root)
  {
   var stone=AssetDatabase.LoadAssetAtPath<Material>(Art+"/SunwashedSandstone.mat");var trim=AssetDatabase.LoadAssetAtPath<Material>(Art+"/CarvedOchre.mat");
   var landmarks=root.Find("Landmarks");if(landmarks==null)return;
   foreach(var r in landmarks.GetComponentsInChildren<MeshRenderer>(true))
   {if(r.name.Contains("Water")||r.name.Contains("Foam"))continue;r.sharedMaterial=r.name.Contains("Capital")||r.name.Contains("Moss")?trim:stone;}
  }
  [MenuItem("Tools/Lost Worlds/Ruins/Validate Sandstone Realm")]
  public static void Validate()
  {
   var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[1]);var root=scene.GetRootGameObjects().Single();var world=root.GetComponentInChildren<JourneyRealm>();var gate=root.GetComponentInChildren<PortalUnlockMechanism>();
   if(world.Tokens.Length!=8||gate==null||gate.Portal!=world.Portal||gate.GetComponent<PortalAutoRunPause>()==null)throw new Exception("Ruins tokens/gate/pause missing");
   if(root.transform.Find("RuinsSandstonePolish").GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Decor adds blockers");
   if(gate.GetComponentsInChildren<Collider>().Any(c=>!c.isTrigger))throw new Exception("Pedestal blocks traversal");
   Physics.SyncTransforms();var path=root.GetComponentInChildren<RealmBindings>().GetMainPath();int samples=0;
   for(int i=1;i<path.Length;i++)for(int j=0;j<=20;j++)
   {
    var p=Vector3.Lerp(path[i-1],path[i],j/20f);var hits=Physics.RaycastAll(p+Vector3.up*20,Vector3.down,40,~0,QueryTriggerInteraction.Ignore);
    if(!Realmwalker.Authoring.RealmJumpAuthoring.CheckOpening(root.transform,p)&&!hits.Any(h=>h.collider.name=="ContinuousGround"))throw new Exception("Unsupported road sample");
    var blocking=Physics.OverlapCapsule(p+Vector3.up*.65f,p+Vector3.up*1.65f,.3f,~0,QueryTriggerInteraction.Ignore).Where(c=>c.GetComponentInParent<Realmwalker.RealmwalkerRunner>()==null&&!c.transform.GetComponentsInParent<Transform>().Any(t=>t.name=="P85_EnvironmentalObstacles"));
    if(blocking.Any())throw new Exception("Unexpected road blocker: "+blocking.First().name);samples++;
   }
   Debug.Log("RUINS CLEARANCE PASS: "+samples+" supported main-route body-clearance samples excluding authored jump obstacles.");
   Debug.Log("RUINS VALID: 8 tokens; existing colliders retained; nonblocking decoration and pedestal; P11 gate/pause configured.");
  }
  [MenuItem("Tools/Lost Worlds/Ruins/Play Sandstone Demo")]
  public static void Play(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[1]);P12PolishBuilder.Play();EditorWindow.GetWindow<P125JourneyDemoWindow>("Journey Demo").Show();}
  public static void BuildBatch()=>EditorApplication.delayCall+=()=>{try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
 }
}
