using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LostWorlds.Person2.Integration;
using LostWorlds.Person2.Journey;
namespace LostWorlds.Person2.Editor {
// Editor-only layout adjustment. No movement, input, trigger or gameplay changes.
public static class RunningPathPolish {
 static readonly string[] Roads={"Environment/JunglePath/ContinuousMainRoad","Environment/StonePath_8mWide/MainPromenade","Environment/CavePath_8mWide/ContinuousCavePath","Environment/CityPromenade/WestPromenade"};
 static readonly float[][] Zs={new float[]{8,12,23,36,41,48,54,69,77,95},new float[]{8,13,31,39,54,60,73,77},new float[]{8,14,26,42,48,62,68,81},new float[]{6,12,23,29,36,40,51,63,71,82}};
 static readonly int[][] Sides={new[]{-1,-1,0,1,0,-1,1,0,-1,1},new[]{1,1,0,-1,0,1,-1,0},new[]{-1,0,1,0,-1,1,0,-1},new[]{1,1,0,-1,0,1,-1,0,1,0}};
 static Vector3 At(Vector3[] path,float z,out Vector3 right){for(int i=1;i<path.Length;i++)if(z>=path[i-1].z&&z<=path[i].z){var d=path[i]-path[i-1];d.y=0;right=Vector3.Cross(Vector3.up,d.normalized);return Vector3.Lerp(path[i-1],path[i],Mathf.InverseLerp(path[i-1].z,path[i].z,z));}throw new Exception("Outside path");}
 static void Save(Transform t){PrefabUtility.RecordPrefabInstancePropertyModifications(t);EditorUtility.SetDirty(t);}
 public static void ApplyBatch()=>EditorApplication.delayCall+=()=>{try{Apply();Validate();JungleRoadRefinement.Validate();P14DemoBuild.Validate();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
 static void Apply(){Directory.CreateDirectory("Documentation/RunningPath");using(var w=new StreamWriter("Documentation/RunningPath/Layout.txt"))for(int r=0;r<4;r++){
 var s=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[r]);var root=s.GetRootGameObjects().Single().transform;var road=root.Find(Roads[r]);
 if(r==0){road.localScale=new Vector3(8f/7f,1,1);Save(road);}else foreach(Transform t in road){var scale=t.localScale;if(t.name=="LevelPath")scale.x=r==3?10:9;else if(t.name=="RoundedJunction"){scale.x=r==3?10:9;scale.z=scale.x;}else continue;t.localScale=scale;Save(t);}
 var path=root.GetComponentInChildren<RealmBindings>().GetMainPath();var realm=root.GetComponentInChildren<JourneyRealm>();var ground=root.Find(r==0?"Environment/Terrain_Ground/JungleGround":"Environment/Terrain_Ground/ContinuousGround").GetComponent<Collider>();
 // Keep stable IDs/array order; only authored transforms change. Low raised accents remain optional.
 for(int i=0;i<realm.Tokens.Length;i++){var p=At(path,Zs[r][i],out var right)+right*(Sides[r][i]*2f);if(!ground.Raycast(new Ray(p+Vector3.up*30,Vector3.down),out var hit,60))throw new Exception("No token floor");p.y=hit.point.y+(i==4?1.8f:1f);if(r==0&&p.z>=67&&p.z<=77)p.y=2.1f+1;realm.Tokens[i].transform.position=p;Save(realm.Tokens[i].transform);w.WriteLine(s.name+" "+realm.Tokens[i].TokenId+" "+p);}
 EditorSceneManager.SaveScene(s);
 }AssetDatabase.SaveAssets();}
 [MenuItem("Tools/Lost Worlds/Layout/Validate Running Paths and Tokens")]
 public static void Validate(){int samples=0;var ids=new HashSet<string>();using(var w=new StreamWriter("Documentation/RunningPath/Validation.txt"))for(int r=0;r<4;r++){
 var s=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[r]);var root=s.GetRootGameObjects().Single().transform;var realm=root.GetComponentInChildren<JourneyRealm>();var path=root.GetComponentInChildren<RealmBindings>().GetMainPath();Physics.SyncTransforms();if(r>0&&path.All(p=>Mathf.Abs(p.x)<.01f)){
 var road=root.Find(Roads[r]);float width=r==3?10:9,end=path[0].z;foreach(var t in road.Cast<Transform>().Where(t=>t.gameObject.activeSelf&&t.name=="LevelPath").OrderBy(t=>t.position.z)){if(Mathf.Abs(t.position.x)>.001f||Quaternion.Angle(t.rotation,Quaternion.identity)>.01f||Mathf.Abs(t.localScale.x-width)>.001f||Mathf.Abs(t.position.z-t.localScale.z*.5f-end)>.001f)throw new Exception("Nonrectangular/gapped road");end=t.position.z+t.localScale.z*.5f;}if(Mathf.Abs(end-path.Last().z)>.001f)throw new Exception("Road endpoint mismatch");
 var ground=root.Find("Environment/Terrain_Ground/ContinuousGround").GetComponent<Collider>();for(float z=0;z<=end;z+=.5f)foreach(float x in new[]{-width*.5f,0,width*.5f})if(!Realmwalker.Authoring.RealmJumpAuthoring.CheckOpening(root,new Vector3(x,0,z))&&!ground.Raycast(new Ray(new Vector3(x,5,z),Vector3.down),out var h,10))throw new Exception("Unsupported road edge");
 }if(realm.Tokens.Length!=(r==0||r==3?10:8))throw new Exception("Token count");
 foreach(var t in realm.Tokens){if(!ids.Add(t.TokenId))throw new Exception("Duplicate token ID");var p=t.transform.position;if(!Physics.Raycast(p+Vector3.up*3,Vector3.down,out var floor,8,~0,QueryTriggerInteraction.Ignore))throw new Exception("Token floor missing: "+t.TokenId);if(Physics.OverlapSphere(p,.48f,~0,QueryTriggerInteraction.Ignore).Any())throw new Exception("Token overlaps solid: "+t.TokenId);foreach(var other in realm.Tokens)if(other!=t&&Vector3.Distance(p,other.transform.position)<1.1f)throw new Exception("Token visual overlap");w.WriteLine(t.TokenId+" supported at "+p);}
 for(float z=1;z<path.Last().z-4;z+=.5f)foreach(float offset in new[]{-2.35f,-2f,0,2f,2.35f}){var p=At(path,z,out var right)+right*offset;if(r==0&&JungleBridgeGap.InOpening(p.x,p.z))continue;if(Realmwalker.Authoring.RealmJumpAuthoring.CheckOpening(root,p))continue;if(!Physics.Raycast(p+Vector3.up*8,Vector3.down,out var floor,15,~0,QueryTriggerInteraction.Ignore))throw new Exception("Running floor missing "+s.name+p);p.y=floor.point.y;
 var hits=Physics.OverlapCapsule(p+Vector3.up*.95f,p+Vector3.up*1.65f,.28f,~0,QueryTriggerInteraction.Ignore).Where(c=>c.GetComponentInParent<Realmwalker.RealmwalkerRunner>()==null&&!c.transform.GetComponentsInParent<Transform>().Any(t=>t.name=="P85_EnvironmentalObstacles"));if(hits.Any())throw new Exception("Running clearance blocked "+s.name+" "+p+" "+hits.First().name);samples++;}
 if(r==0)JungleBridgeGap.ValidateGeometry(root);w.WriteLine(s.name+" PASS: floor, 3 position corridor, token clearance/count and IDs");
 }Debug.Log("LAYOUT PASS: "+samples+" supported corridor samples; "+ids.Count+" unique tokens; bridge gap unchanged.");}

 public static void StraightBatch()=>EditorApplication.delayCall+=()=>{try{Straighten();Validate();P14DemoBuild.Validate();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
 static void Straighten(){Directory.CreateDirectory("Documentation/StraightRoads");using(var report=new StreamWriter("Documentation/StraightRoads/Layout.txt"))for(int r=1;r<4;r++){
 var scene=EditorSceneManager.OpenScene(IntegrationSession.RealmScenes[r]);var root=scene.GetRootGameObjects().Single().transform;var road=root.Find(Roads[r]);float width=r==3?10:9;
 var binding=root.GetComponentInChildren<RealmBindings>();var oldPath=binding.GetMainPath();var so=new SerializedObject(binding);var points=so.FindProperty("mainPath");
 for(int i=0;i<points.arraySize;i++){var t=(Transform)points.GetArrayElementAtIndex(i).objectReferenceValue;var p=t.position;p.x=0;t.position=p;Save(t);}
 if(binding.Branch!=null){var bs=new SerializedObject(binding.Branch);foreach(var field in new[]{"left","right"}){var a=bs.FindProperty(field);for(int i=0;i<a.arraySize;i++){var t=(Transform)a.GetArrayElementAtIndex(i).objectReferenceValue;var p=t.position;p.x=i==0||i==a.arraySize-1?0:field=="left"?-2:2;t.position=p;Save(t);}}}
 // Reuse the original textured rectangular segments; adjacent ends meet exactly.
 var segments=road.Cast<Transform>().Where(t=>t.name=="LevelPath").OrderBy(t=>t.position.z).ToArray();
 for(int i=0;i<segments.Length;i++){var t=segments[i];float start=oldPath[i].z,end=i==segments.Length-1?oldPath.Last().z:oldPath[i+1].z;t.position=new Vector3(0,t.position.y,(start+end)*.5f);t.rotation=Quaternion.identity;var scale=t.localScale;scale.x=width;scale.z=end-start;t.localScale=scale;Save(t);}
 foreach(Transform t in road)if(t.name=="RoundedJunction")t.gameObject.SetActive(false);
 var parent=road.parent;foreach(Transform t in parent){if(t==road)continue;if(t.name=="EastPromenade"||t.name=="WaterfallViewingApproach"){t.gameObject.SetActive(false);continue;}var renderer=t.GetComponent<Renderer>();if(renderer!=null){var p=t.position;p.x=(t.name.Contains("Central")||t.name=="ViewingCourt"?-1:1)*(width*.5f+renderer.bounds.extents.x+1);t.position=p;Save(t);report.WriteLine("Side plaza "+t.name+" "+p);}}
 if(r==3){var landmark=root.Find("Landmarks/CentralPlazaLandmark");if(landmark!=null){landmark.position=new Vector3(-24,landmark.position.y,landmark.position.z);Save(landmark);report.WriteLine("Central monument moved beside road");}}
 var realm=root.GetComponentInChildren<JourneyRealm>();for(int i=0;i<realm.Tokens.Length;i++){var t=realm.Tokens[i].transform;var p=t.position;p.x=Sides[r][i]*2;p.z=Zs[r][i];t.position=p;Save(t);}
 // Keep the existing four low obstacle prefabs on the newly straight centre line.
 foreach(Transform t in root.Find("Environment/P85_EnvironmentalObstacles")){t.rotation=Quaternion.identity;Physics.SyncTransforms();var cs=t.GetComponentsInChildren<Collider>();var bounds=cs[0].bounds;foreach(var c in cs)bounds.Encapsulate(c.bounds);var p=t.position;p.x-=bounds.center.x;t.position=p;Save(t);}
 Physics.SyncTransforms();
 var blockers=new HashSet<string>();for(float z=1;z<oldPath.Last().z-5;z+=.5f)for(float x=-width*.5f+.35f;x<=width*.5f-.3f;x+=.5f)foreach(var c in Physics.OverlapBox(new Vector3(x,1.2f,z),new Vector3(.25f,.85f,.25f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))if(!c.transform.GetComponentsInParent<Transform>().Any(t=>t.name=="P85_EnvironmentalObstacles"))blockers.Add(c.transform.GetHierarchyPath());
 foreach(var b in blockers)report.WriteLine("BLOCKER "+scene.name+" "+b);
 EditorSceneManager.SaveScene(scene);report.WriteLine(scene.name+" straight road width="+width+"; blockers="+blockers.Count);
 }AssetDatabase.SaveAssets();}
 static string GetHierarchyPath(this Transform t)=>t.parent==null?t.name:t.parent.GetHierarchyPath()+"/"+t.name;
}}
