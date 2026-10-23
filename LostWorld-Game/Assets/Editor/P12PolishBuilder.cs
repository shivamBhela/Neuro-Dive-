using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Interaction;
using LostWorlds.Person2.Polish;
using Object=UnityEngine.Object;
namespace LostWorlds.Person2.Editor
{
 [InitializeOnLoad] public static class P12PolishBuilder
 {
  static UnityEngine.SceneManagement.Scene previewScene;
  static P12PolishBuilder(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("P12.preview",false)){previewScene=default;EditorApplication.update+=PreviewCamera;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("P12.preview",false);EditorApplication.update-=PreviewCamera;}};}
  static void PreviewCamera(){var session=IntegrationSession.Instance;if(session==null||session.CurrentRealm==null)return;var handle=session.CurrentRealm.gameObject.scene;if(handle==previewScene)return;previewScene=handle;foreach(var root in session.CurrentRealm.gameObject.scene.GetRootGameObjects())foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.enabled=camera.name=="PrototypeOverviewCamera_NoController";if(Object.FindAnyObjectByType<AudioListener>()==null)session.gameObject.AddComponent<AudioListener>();}
  const string Art="Assets/Art/Materials/P12Polish",Audio="Assets/Audio/P12",Prefabs="Assets/Prefabs/World/P12Polish";
  static readonly string[] Names={"Jungle","Ruins","CrystalCave","HiddenCity"};
  static readonly Color[] Sky={new Color(.53f,.65f,.57f),new Color(.64f,.76f,.77f),new Color(.30f,.40f,.52f),new Color(.76f,.76f,.65f)};
  static readonly Color[] Accent={new Color(.65f,.79f,.53f),new Color(.55f,.80f,.77f),new Color(.48f,.77f,.87f),new Color(.90f,.72f,.39f)};
  static void Ref(Object target,string key,Object value){var s=new SerializedObject(target);s.FindProperty(key).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
  static Transform Group(Transform parent,string name,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent);t.position=position;return t;}
  static Material Mat(string name,Color color)
  {
   string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
   m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(m,path);return m;
  }
  static Material ParticleMaterial()
  {
   string path=Art+"/SoftMotes.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
   var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="SoftDisc",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
   for(int y=0;y<32;y++)for(int x=0;x<32;x++){float r=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;float a=Mathf.Pow(Mathf.Clamp01(1-r),2);texture.SetPixel(x,y,new Color(1,1,1,a));}texture.Apply();AssetDatabase.CreateAsset(texture,Art+"/SoftDisc.asset");
   material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="SoftMotes",renderQueue=3000};material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.SetOverrideTag("RenderType","Transparent");material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");AssetDatabase.CreateAsset(material,path);return material;
  }
  static void Configure(ParticleSystem ps,Color tint,bool feedback,bool mist=false)
  {
   ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.loop=true;main.playOnAwake=true;main.maxParticles=feedback?16:24;main.startLifetime=feedback?3:10;main.startSpeed=feedback?.12f:.04f;main.startSize=feedback?.45f:mist?2.2f:.24f;main.startColor=tint;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=0;
   var emission=ps.emission;emission.rateOverTime=feedback?0:1.5f;var shape=ps.shape;shape.enabled=!feedback;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=mist?new Vector3(2,1,11):new Vector3(8,2,20);
   var colors=ps.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(feedback?.28f:mist?.13f:.20f,.35f),new GradientAlphaKey(0,1)});colors.color=gradient;
   var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=ParticleMaterial();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;renderer.maxParticleSize=.04f;
  }
  static ParticleSystem Emitter(Transform parent,string name,Vector3 position,Color color,bool feedback=false,bool mist=false)
  {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/CalmMotes.prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=position;var ps=go.GetComponent<ParticleSystem>();Configure(ps,color,feedback,mist);PrefabUtility.RecordPrefabInstancePropertyModifications(ps);PrefabUtility.RecordPrefabInstancePropertyModifications(ps.GetComponent<ParticleSystemRenderer>());return ps;
  }
  static Mesh RingMesh()
  {
   string path=Art+"/QuietRing.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;mesh=new Mesh{name="QuietRing"};var vertices=new Vector3[64];var tris=new int[192];
   for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;vertices[i*2]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));vertices[i*2+1]=vertices[i*2]*.9f;int n=(i+1)%32*2;int k=i*6;tris[k]=i*2;tris[k+1]=n;tris[k+2]=i*2+1;tris[k+3]=n;tris[k+4]=n+1;tris[k+5]=i*2+1;}for(int j=0;j<tris.Length;j+=3){int swap=tris[j+1];tris[j+1]=tris[j+2];tris[j+2]=swap;}mesh.vertices=vertices;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
  }
  static void Decoration(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
  {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent);go.transform.position=position;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;}
  [MenuItem("Tools/Lost Worlds/P12/Setup Polish")]
  public static void Setup()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   foreach(var path in new[]{Art,Audio,Prefabs})Directory.CreateDirectory(path);AssetDatabase.Refresh();
   if(!File.Exists(Prefabs+"/CalmMotes.prefab")){var temp=new GameObject("CalmMotes",typeof(ParticleSystem));Configure(temp.GetComponent<ParticleSystem>(),Color.white,false);PrefabUtility.SaveAsPrefabAsset(temp,Prefabs+"/CalmMotes.prefab");Object.DestroyImmediate(temp);}
   for(int i=0;i<4;i++)
   {
    var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[i]);var root=scene.GetRootGameObjects().Single().transform;if(root.Find("P12_Polish")!=null)continue;
    var group=Group(root,"P12_Polish",Vector3.zero);var polish=group.gameObject.AddComponent<RealmPolish>();
    string profilePath=Audio+"/"+Names[i]+"Audio.asset";var profile=AssetDatabase.LoadAssetAtPath<RealmPolishProfile>(profilePath);if(profile==null){profile=ScriptableObject.CreateInstance<RealmPolishProfile>();var sp=new SerializedObject(profile);sp.FindProperty("realmName").stringValue=Names[i];sp.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(profile,profilePath);}Ref(polish,"profile",profile);
    var ambient=Group(group,"Ambience_OptionalLicensedClip",Vector3.zero).gameObject.AddComponent<AudioSource>();ambient.playOnAwake=false;ambient.loop=true;ambient.volume=0;ambient.spatialBlend=0;ambient.priority=200;
    var success=Group(group,"Success_OptionalLicensedClip",Vector3.zero).gameObject.AddComponent<AudioSource>();success.playOnAwake=false;success.volume=.07f;success.spatialBlend=0;success.priority=180;Ref(polish,"ambienceSource",ambient);Ref(polish,"successSource",success);
    var one=Emitter(group,i==1?"WaterfallMist":"SlowMotes_West",i==1?new Vector3(-20,1.3f,43):new Vector3(-14,i==0?4:3,40),Accent[i],false,i==1);var two=Emitter(group,"SlowMotes_East",new Vector3(14,i==0?5:3,70),Accent[i]);var feedback=Emitter(group,"AcceptedAction_SoftRise",Vector3.zero,Accent[i],true);Ref(polish,"successParticles",feedback);var serialized=new SerializedObject(polish);var list=serialized.FindProperty("atmosphere");list.arraySize=2;list.GetArrayElementAtIndex(0).objectReferenceValue=one;list.GetArrayElementAtIndex(1).objectReferenceValue=two;serialized.ApplyModifiedPropertiesWithoutUndo();
    RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=i==2?new Color(.59f,.67f,.79f):Color.Lerp(Sky[i],Color.white,.3f);RenderSettings.ambientEquatorColor=Color.Lerp(Sky[i],Color.gray,.4f);RenderSettings.ambientGroundColor=i==2?new Color(.28f,.33f,.41f):new Color(.28f,.30f,.25f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=Sky[i];RenderSettings.fogStartDistance=i==2?100:115;RenderSettings.fogEndDistance=i==2?235:270;
    foreach(var light in root.GetComponentsInChildren<Light>()){if(light.type==LightType.Directional){light.color=i==2?new Color(.77f,.87f,1):i==3?new Color(1,.88f,.66f):new Color(1,.94f,.81f);light.intensity=i==2?1.05f:i==3?1.45f:1.35f;light.shadowStrength=.65f;}else{light.shadows=LightShadows.None;light.intensity=Mathf.Min(light.intensity,1.4f);}}
    foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.backgroundColor=Sky[i];
    string[] pathNames={"JunglePath","StonePath_8mWide","CavePath_8mWide","CityPromenade"};var pathRoot=root.Find("Environment/"+pathNames[i]);if(pathRoot==null)pathRoot=root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.Contains("Path")&&t.parent!=null&&t.parent.name=="Environment");
    Color[] paths={new Color(.69f,.49f,.27f),new Color(.73f,.70f,.55f),new Color(.44f,.53f,.61f),new Color(.82f,.75f,.56f)};Color[] obstacles={new Color(.31f,.23f,.14f),new Color(.39f,.44f,.41f),new Color(.28f,.44f,.54f),new Color(.42f,.45f,.39f)};
    var pathMaterial=Mat(Names[i]+"_ReadablePath",paths[i]);if(pathRoot==null)throw new Exception("Missing path hierarchy "+Names[i]);foreach(var r in pathRoot.GetComponentsInChildren<Renderer>()){r.sharedMaterial=pathMaterial;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
    var obstacleRoot=root.Find("Environment/P85_EnvironmentalObstacles");foreach(var r in obstacleRoot.GetComponentsInChildren<Renderer>()){r.sharedMaterial=Mat(Names[i]+"_ObstacleContrast",obstacles[i]);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
    var accent=Mat(Names[i]+"_QuietAccent",Accent[i]);var ground=root.Find(i==0?"Environment/Terrain_Ground/JungleGround":"Environment/Terrain_Ground/ContinuousGround").GetComponent<Collider>();
    foreach(var target in root.GetComponentsInChildren<InteractableBase>(true)){var point=target.transform.position;if(ground.Raycast(new Ray(new Vector3(point.x,50,point.z),Vector3.down),out var hit,100))point.y=hit.point.y+.09f;var ring=Group(group,"InteractionFocus_"+target.name,point);ring.localScale=Vector3.one*1.35f;ring.gameObject.AddComponent<MeshFilter>().sharedMesh=RingMesh();var renderer=ring.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=accent;renderer.shadowCastingMode=ShadowCastingMode.Off;}
    if(i==3)
    {
     var ivory=Mat("City_DestinationIvory",new Color(.81f,.79f,.66f));
     foreach(float x in new[]{-5.5f,5.5f}){Decoration(group,"DestinationPillar",PrimitiveType.Cylinder,new Vector3(x,3,84),new Vector3(1,3,1),ivory);Decoration(group,"DestinationCrown",PrimitiveType.Sphere,new Vector3(x,6.3f,84),Vector3.one*.7f,accent);}
     Decoration(group,"DestinationLintel",PrimitiveType.Cube,new Vector3(0,6.2f,84),new Vector3(12,.45f,1),ivory);
    }
    EditorSceneManager.SaveScene(scene);
   }
   AssetDatabase.SaveAssets();Validate();
  }
  [MenuItem("Tools/Lost Worlds/P12/Validate Polish")]
  public static void Validate()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;var previous=EditorSceneManager.GetSceneManagerSetup();try{
    foreach(var path in IntegrationSession.RealmScenes){var scene=EditorSceneManager.OpenScene(path);var root=scene.GetRootGameObjects().Single();var polish=root.GetComponentInChildren<RealmPolish>();if(polish==null||polish.Profile==null||root.GetComponentInChildren<RealmBindings>()==null)throw new Exception("Missing polish/integration binding");var systems=polish.GetComponentsInChildren<ParticleSystem>();if(systems.Length!=3||systems.Sum(p=>p.main.maxParticles)>64)throw new Exception("Particle budget exceeded");if(polish.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Polish must not add collision geometry");foreach(var source in polish.GetComponentsInChildren<AudioSource>())if(source.playOnAwake||source.volume>.18f)throw new Exception("Audio defaults exceed limit");if(RenderSettings.fogStartDistance<90||RenderSettings.fogEndDistance<200)throw new Exception("Fog obscures route");Debug.Log("P12 VALID: "+scene.name+"; 64-particle cap, no added colliders, low audio defaults, P11 binding retained.");}
   }finally{EditorSceneManager.RestoreSceneManagerSetup(previous);}
  }
  [MenuItem("Tools/Lost Worlds/P12/Play Polish Preview")]
  public static void Play(){if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;EditorSceneManager.OpenScene(IntegrationSession.RealmScenes.Contains(current)?current:IntegrationSession.RealmScenes[0]);SessionState.SetBool("P12.preview",true);EditorApplication.isPlaying=true;}
 }
}
