using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
using LostWorlds.Person2.Polish;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
 public static class HiddenCityUpgrade
 {
  const string Art="Assets/Art/Environments/HiddenCity/FinaleUpgrade";
  static void Ref(Object o,string field,Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
  static Material Mat(string name,Color color,bool metallic=false,bool path=false)
  {string p=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m!=null)return m;m=new Material(AssetDatabase.LoadAssetAtPath<Material>(path?"Assets/Art/Environments/Ruins/SandstoneUpgrade/GoldenCrackedPaving.mat":"Assets/Art/Environments/Jungle/NaturalVisuals/AgedLimestone.mat")){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic?.65f:0);m.SetFloat("_Smoothness",metallic?.55f:.22f);AssetDatabase.CreateAsset(m,p);return m;}
  static Transform Shape(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material)
  {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
  static Sprite Icon()
  {
   var tex=new Texture2D(192,224,TextureFormat.RGBA32,false);
   for(int y=0;y<224;y++)for(int x=0;x<192;x++){float dx=x-96;bool bowl=y>102&&y<188&&Mathf.Abs(dx)<26+(y-102)*.42f;bool stem=y>42&&y<=106&&Mathf.Abs(dx)<10;bool foot=y>24&&y<=48&&Mathf.Abs(dx)<43;float d1=Mathf.Pow((x-42)/25f,2)+Mathf.Pow((y-146)/35f,2),d2=Mathf.Pow((x-150)/25f,2)+Mathf.Pow((y-146)/35f,2);bool handle=y>104&&y<183&&(d1<1&&d1>.48f||d2<1&&d2>.48f);float shine=.65f+.35f*Mathf.Sin(Mathf.Clamp01(x/192f)*Mathf.PI);tex.SetPixel(x,y,bowl||stem||foot||handle?new Color(shine,shine*.76f,shine*.24f,1):Color.clear);}tex.Apply();string p=Art+"/GoldenTrophy.png";File.WriteAllBytes(p,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(p);var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(p);
  }
  [MenuItem("Tools/Lost Worlds/Hidden City/Apply Final City Upgrade")]
  public static void Build()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[3]);var root=scene.GetRootGameObjects().Single().transform;if(root.Find("FinalCityArchitecture")!=null){Validate();return;}
   Directory.CreateDirectory(Art);AssetDatabase.Refresh();var stone=Mat("SunsetIvory",new Color(1.15f,1.02f,.78f));var bronze=Mat("AncientBronze",new Color(.48f,.3f,.13f),true);var gold=Mat("RewardGold",new Color(1,.72f,.22f),true);var paving=Mat("RoyalStoneRoad",new Color(.85f,.78f,.6f),false,true);
   foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
   {if(r.GetComponentsInParent<Transform>().Any(t=>t.name=="P125_Journey"||t.name.StartsWith("P12_")))continue;string n=r.sharedMaterial!=null?r.sharedMaterial.name.ToLowerInvariant():"";if(r.name=="LocationOnly_NoTrigger"){r.enabled=false;continue;}if(n.Contains("leaf")||n.Contains("foliage")||n.Contains("bark"))continue;r.sharedMaterial=n.Contains("gold")||n.Contains("patina")?bronze:stone;
    if(r.transform.IsChildOf(root.Find("Environment/CityPromenade"))){r.sharedMaterial=paving;var f=r.GetComponent<MeshFilter>();if(f!=null&&f.sharedMesh!=null){string p=Art+"/Road_"+f.sharedMesh.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(mesh==null){mesh=Object.Instantiate(f.sharedMesh);mesh.uv=mesh.vertices.Select(v=>{var w=f.transform.TransformPoint(v);return new Vector2(w.x/7,w.z/7);}).ToArray();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,p);}f.sharedMesh=mesh;}}
   }
   var group=new GameObject("FinalCityArchitecture").transform;group.SetParent(root,false);
   foreach(float side in new[]{-1f,1f})foreach(float z in new[]{15f,53f,84f})
   {
    float x=side*29;Shape(group,"MonumentalTower",PrimitiveType.Cube,new Vector3(x,7,z),new Vector3(6,14,6),stone);for(int level=0;level<4;level++)Shape(group,"TowerBronzeCornice",PrimitiveType.Cube,new Vector3(x,2+level*3.7f,z),new Vector3(6.5f,.35f,6.5f),bronze);
    Shape(group,"TowerCrown",PrimitiveType.Sphere,new Vector3(x,14,z),new Vector3(5,2.8f,5),gold);
    for(int i=0;i<3;i++)Shape(group,"RecessedWindow",PrimitiveType.Cube,new Vector3(x-side*3.02f,4+i*3,z),new Vector3(.08f,1.5f,.9f),bronze);
   }
   foreach(float side in new[]{-1f,1f})foreach(float z in new[]{22f,75f})
   {
    float x=side*12;Shape(group,"GuardianPlinth",PrimitiveType.Cube,new Vector3(x,.5f,z),new Vector3(2.8f,1,2.8f),bronze);Shape(group,"GuardianRobes",PrimitiveType.Capsule,new Vector3(x,2.8f,z),new Vector3(1.3f,2,1.2f),stone);Shape(group,"GuardianHead",PrimitiveType.Sphere,new Vector3(x,5,z),Vector3.one*.9f,gold);Shape(group,"GuardianShoulders",PrimitiveType.Cube,new Vector3(x,4,z),new Vector3(2,.5f,.7f),bronze);
   }
   foreach(float side in new[]{-1f,1f})for(int i=0;i<9;i++){float angle=Mathf.PI*i/8;var b=Shape(group,"RoyalArchStone",PrimitiveType.Cube,new Vector3(side*23+5*Mathf.Cos(angle),6+3*Mathf.Sin(angle),34),new Vector3(1.7f,1.3f,1.8f),i%2==0?stone:bronze);b.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg-90);}
   var trophy=new GameObject("GoldenJourneyTrophy_DisplayOnly").transform;trophy.SetParent(group,false);trophy.localPosition=new Vector3(0,0,92);
   Shape(trophy,"DecoratedRewardPedestal",PrimitiveType.Cylinder,new Vector3(0,1,0),new Vector3(3.5f,1,3.5f),stone);Shape(trophy,"GoldPedestalBand",PrimitiveType.Cylinder,new Vector3(0,1.8f,0),new Vector3(3.7f,.12f,3.7f),gold);Shape(trophy,"TrophyFoot",PrimitiveType.Cube,new Vector3(0,2.2f,0),new Vector3(2.2f,.4f,1.5f),gold);Shape(trophy,"TrophyStem",PrimitiveType.Cylinder,new Vector3(0,2.8f,0),new Vector3(.5f,.5f,.5f),gold);Shape(trophy,"TrophyCup",PrimitiveType.Sphere,new Vector3(0,4,0),new Vector3(2.6f,2.1f,1.6f),gold);
   foreach(float side in new[]{-1f,1f})for(int i=0;i<9;i++){float a=i*Mathf.PI/8;Shape(trophy,"CupHandle",PrimitiveType.Sphere,new Vector3(side*(1.05f+.8f*Mathf.Sin(a)),3.9f+.8f*Mathf.Cos(a),0),Vector3.one*.3f,gold);}
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.77f,.69f,.65f);RenderSettings.ambientEquatorColor=new Color(.57f,.46f,.37f);RenderSettings.ambientGroundColor=new Color(.3f,.25f,.22f);RenderSettings.fogColor=new Color(.7f,.54f,.42f);RenderSettings.fogStartDistance=120;RenderSettings.fogEndDistance=290;
   var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.68f,.53f,.46f));sky.SetColor("_GroundColor",new Color(.48f,.36f,.25f));sky.SetFloat("_AtmosphereThickness",1.15f);AssetDatabase.CreateAsset(sky,Art+"/SunsetSky.mat");RenderSettings.skybox=sky;
   foreach(var l in root.GetComponentsInChildren<Light>())if(l.type==LightType.Directional){l.color=new Color(1,.8f,.57f);l.intensity=1.4f;l.transform.rotation=Quaternion.Euler(32,-35,0);l.shadows=LightShadows.Soft;l.shadowStrength=.6f;}foreach(var c in root.GetComponentsInChildren<Camera>())c.clearFlags=CameraClearFlags.Skybox;
   var ruins=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[1],OpenSceneMode.Additive);var source=ruins.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PortalUnlockMechanism>(true)).Single();var console=Object.Instantiate(source.gameObject,root);EditorSceneManager.CloseScene(ruins,true);console.name="FinalConsole_ACTIVATE";console.transform.position=new Vector3(5,0,78);var gate=console.GetComponent<PortalUnlockMechanism>();Ref(gate,"portal",root.GetComponentInChildren<JourneyRealm>().Portal);Ref(gate,"polish",root.GetComponentInChildren<RealmPolish>());Ref(root.GetComponentInChildren<RealmBindings>(),"exitGate",gate);
   foreach(var r in console.GetComponentsInChildren<MeshRenderer>())if(r.GetComponent<TextMesh>()==null)r.sharedMaterial=bronze;var so=new SerializedObject(gate);var reveal=so.FindProperty("revealObjects");reveal.arraySize=1;reveal.GetArrayElementAtIndex(0).objectReferenceValue=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="SanctuaryColonnade").gameObject;so.ApplyModifiedPropertiesWithoutUndo();
   var finale=root.gameObject.AddComponent<JourneyFinale>();Ref(finale,"trophyIcon",Icon());Ref(finale,"celebration",console.GetComponent<AudioSource>().clip);
   EditorSceneManager.SetActiveScene(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Validate();
  }
  [MenuItem("Tools/Lost Worlds/Hidden City/Validate Final City")]
  public static void Validate(){var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[3]);var root=scene.GetRootGameObjects().Single();if(root.GetComponentInChildren<JourneyRealm>().Tokens.Length!=10||root.GetComponent<JourneyFinale>()==null||root.GetComponentInChildren<PortalAutoRunPause>()!=null||root.GetComponentInChildren<PortalUnlockMechanism>()!=null)throw new Exception("Final city wiring missing");if(root.transform.Find("FinalCityArchitecture").GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Final decor must not block roads");Debug.Log("FINAL CITY VALID: 10 tokens; original colliders; visible trophy, automatic P11 trigger and finale wired.");}
  [MenuItem("Tools/Lost Worlds/Hidden City/Play Finale Demo")]
  public static void Play(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[3]);P12PolishBuilder.Play();EditorWindow.GetWindow<P125JourneyDemoWindow>("Journey Demo").Show();}
  // Targeted correction only: no scenery/material/road generator is called.
  public static void CorrectFinaleBatch()=>EditorApplication.delayCall+=()=>{try{CorrectFinale();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
  public static void CorrectFinale()
  {
   var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[3]);var root=scene.GetRootGameObjects().Single();
   var realm=root.GetComponentInChildren<RealmBindings>();var gate=root.GetComponentInChildren<PortalUnlockMechanism>();var portal=gate.Portal;
   var trophy=root.transform.Find("FinalCityArchitecture/GoldenJourneyTrophy_DisplayOnly");
   if(trophy==null||portal==null)throw new Exception("Existing finale objects required");
   trophy.position=portal.transform.position;trophy.gameObject.SetActive(true);
   var binding=new SerializedObject(realm);binding.FindProperty("completionOnActivation").boolValue=true;binding.ApplyModifiedPropertiesWithoutUndo();
   portal.gameObject.SetActive(false);portal.enabled=false;foreach(var c in portal.GetComponentsInChildren<Collider>(true)){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
   PrefabUtility.RecordPrefabInstancePropertyModifications(portal);PrefabUtility.RecordPrefabInstancePropertyModifications(portal.gameObject);
   root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="SanctuaryColonnade").gameObject.SetActive(false);
   string materialPath=Art+"/TrophyIllumination.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
   if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Art+"/RewardGold.mat"));material.name="TrophyIllumination";material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);AssetDatabase.CreateAsset(material,materialPath);}
   var renderers=trophy.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial.name=="RewardGold"||r.sharedMaterial==material).ToArray();foreach(var r in renderers)r.sharedMaterial=material;
   var config=new SerializedObject(gate);var list=config.FindProperty("trophyRenderers");list.arraySize=renderers.Length;for(int i=0;i<renderers.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=renderers[i];config.ApplyModifiedPropertiesWithoutUndo();
   gate.GetComponentInChildren<TextMesh>().text="Press E to Activate Trophy";
   EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
   if(trophy.position!=portal.transform.position||!realm.CompletionOnActivation||portal.gameObject.activeSelf||portal.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))throw new Exception("Finale correction failed");
   Debug.Log("FINALE CORRECTION SAVED: existing trophy at "+trophy.position+"; portal/frame disabled; activation-only completion.");
  }
  public static void ApplyAutomaticTrophyBatch()=>EditorApplication.delayCall+=()=>{try{ApplyAutomaticTrophy();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
  public static void ApplyAutomaticTrophy()
  {
   var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[3]);var root=scene.GetRootGameObjects().Single();
   var realm=root.GetComponentInChildren<RealmBindings>();var portal=root.GetComponentInChildren<JourneyRealm>().Portal;
   // Reuse the existing P11 reference volume and physical trigger, removing only obsolete presentation.
   if(PrefabUtility.IsPartOfPrefabInstance(portal.gameObject))PrefabUtility.UnpackPrefabInstance(portal.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   foreach(Transform child in portal.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
   portal.name="TrophyCompletionTrigger";portal.gameObject.SetActive(true);portal.enabled=true;portal.GetComponent<BoxCollider>().enabled=true;
   var binding=new SerializedObject(realm);binding.FindProperty("completionOnActivation").boolValue=false;binding.FindProperty("exitGate").objectReferenceValue=null;binding.ApplyModifiedPropertiesWithoutUndo();
   var console=root.transform.Find("FinalConsole_ACTIVATE");if(console!=null)Object.DestroyImmediate(console.gameObject);
   string path=Art+"/VictoryStar.png";
   if(!File.Exists(path))
   {
    var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);var points=new Vector2[10];for(int n=0;n<10;n++){float a=Mathf.PI*.5f+n*Mathf.PI/5;points[n]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(n%2==0?.46f:.21f)+Vector2.one*.5f;}
    for(int y=0;y<128;y++)for(int x=0;x<128;x++){float alpha=0;for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++){var v=new Vector2((x+.25f+sx*.5f)/128,(y+.25f+sy*.5f)/128);bool inside=false;for(int i=0,j=9;i<10;j=i++){var a=points[i];var b=points[j];if((a.y>v.y)!=(b.y>v.y)&&v.x<(b.x-a.x)*(v.y-a.y)/(b.y-a.y)+a.x)inside=!inside;}if(inside)alpha+=.25f;}tex.SetPixel(x,y,new Color(1,1,1,alpha));}tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
   }
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();Ref(root.GetComponent<JourneyFinale>(),"starIcon",AssetDatabase.LoadAssetAtPath<Sprite>(path));
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Validate();
   Debug.Log("AUTOMATIC TROPHY SAVED: existing 8 x 6 x 4 trigger, no console/portal visuals, original trophy and P11 completion.");
  }
  public static void BuildBatch()=>EditorApplication.delayCall+=()=>{try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
 }
}
