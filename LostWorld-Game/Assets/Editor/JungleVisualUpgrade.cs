using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace LostWorlds.Person2.Editor
{
    // Baked authoring only: no runtime scripts, collider changes or shared prefab edits.
    public static class JungleVisualUpgrade
    {
        const string ScenePath="Assets/Scenes/Person2/Jungle_Prototype.unity";
        const string Art="Assets/Art/Environments/Jungle/NaturalVisuals";
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static float Height(float z)=>z<24?0:z<50?(z-24)/26*2:z<88?2:2+Mathf.Clamp01((z-88)/20)*2;
        static float Noise(float x,float y)=>.55f*Mathf.PerlinNoise(x*3,y*3)+.3f*Mathf.PerlinNoise(x*13+7,y*13+7)+.15f*Mathf.PerlinNoise(x*57,y*57);

        // Small seamless, mipmapped surface maps; normals generated from the same height field.
        static Material Material(string name,Color color,float roughness,bool bark=false)
        {
            string path=Art+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null)return material;
            const int size=256;
            var heights=new float[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                // Blend opposite edges to remove seams at texture repeats.
                float n=Mathf.Lerp(Mathf.Lerp(Noise(u,v),Noise(u-1,v),u),Mathf.Lerp(Noise(u,v-1),Noise(u-1,v-1),u),v);
                heights[y*size+x]=bark?.55f*n+.45f*(.5f+.5f*Mathf.Sin(u*Mathf.PI*32+Mathf.Sin(v*Mathf.PI*2)*.7f)):n;
            }
            var albedo=new Texture2D(size,size,TextureFormat.RGB24,true);
            var normal=new Texture2D(size,size,TextureFormat.RGB24,true,true);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float h=heights[y*size+x];albedo.SetPixel(x,y,Color.Lerp(color*.72f,color*1.18f,h));
                float dx=heights[y*size+(x+1)%size]-heights[y*size+(x+size-1)%size];
                float dy=heights[((y+1)%size)*size+x]-heights[((y+size-1)%size)*size+x];
                var n=new Vector3(-dx*2,-dy*2,1).normalized;normal.SetPixel(x,y,new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f));
            }
            albedo.Apply();normal.Apply();
            string a=Art+"/"+name+"_Color.png",b=Art+"/"+name+"_Normal.png";
            File.WriteAllBytes(a,albedo.EncodeToPNG());File.WriteAllBytes(b,normal.EncodeToPNG());Object.DestroyImmediate(albedo);Object.DestroyImmediate(normal);
            AssetDatabase.ImportAsset(a);AssetDatabase.ImportAsset(b);
            foreach(string file in new[]{a,b})
            {var importer=(TextureImporter)AssetImporter.GetAtPath(file);importer.textureType=file==b?TextureImporterType.NormalMap:TextureImporterType.Default;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.maxTextureSize=256;importer.SaveAndReimport();}
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(a));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(b));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.45f);material.SetFloat("_Smoothness",1-roughness);material.SetFloat("_Metallic",0);
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static Mesh Surface(MeshFilter filter)
        {
            string path=Art+"/Surface_"+filter.name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=Object.Instantiate(filter.sharedMesh);mesh.name="Natural_"+filter.name;mesh.uv=mesh.vertices.Select(v=>new Vector2(v.x/5,v.z/5)).ToArray();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);}
            return mesh;
        }
        static Mesh Foliage(Mesh source)
        {
            string path=Art+"/OrganicCanopy.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            mesh=Object.Instantiate(source);mesh.name="OrganicCanopy";
            mesh.vertices=mesh.vertices.Select(v=>{float n=Mathf.PerlinNoise(v.x*8+4,v.z*8+v.y*3+4);return v*(.86f+n*.27f);}).ToArray();
            Smooth(mesh);AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static void Smooth(Mesh mesh)
        {
            mesh.RecalculateNormals();var vertices=mesh.vertices;var normals=mesh.normals;
            var groups=new Dictionary<Vector3Int,List<int>>();
            for(int i=0;i<vertices.Length;i++){var p=Vector3Int.RoundToInt(vertices[i]*10000);if(!groups.TryGetValue(p,out var list)){list=new List<int>();groups.Add(p,list);}list.Add(i);}
            foreach(var list in groups.Values){var sum=Vector3.zero;foreach(int i in list)sum+=normals[i];foreach(int i in list)normals[i]=sum.normalized;}
            mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();
        }
        static Mesh NaturalShape(Mesh source,string name)
        {
            string path=Art+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            mesh=Object.Instantiate(source);mesh.name=name;
            mesh.vertices=mesh.vertices.Select(v=>{
                if(name=="TaperedTrunk"){float factor=Mathf.Lerp(1.28f,.77f,Mathf.InverseLerp(-1,1,v.y));v.x*=factor;v.z*=factor;}
                else if(name=="ButtressWedge"){float factor=Mathf.Lerp(1,.12f,v.y+.5f);v.x*=factor;v.z*=factor;}
                else v*=.9f+.2f*Mathf.PerlinNoise(v.x*5+11,v.z*5+v.y*2+11);
                return v;
            }).ToArray();Smooth(mesh);AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh Leaf()
        {
            string path=Art+"/CurvedLeaf.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int row=0;row<=8;row++)for(int col=0;col<3;col++)
            {float t=row/8f;v.Add(new Vector3((col-1)*Mathf.Sin(t*Mathf.PI)*.5f,Mathf.Sin(t*Mathf.PI)*.3f+(col==1?.1f:0),t-.5f));uv.Add(new Vector2(col*.5f,t));}
            for(int row=0;row<8;row++)for(int col=0;col<2;col++){int a=row*3+col;triangles.AddRange(new[]{a,a+3,a+1,a+1,a+3,a+4});}
            mesh=new Mesh{name="CurvedUnderstoryLeaf"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Apply Natural Visuals")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(ScenePath);var root=scene.GetRootGameObjects().Single().transform;
            if(root.Find("JungleNaturalVisuals")!=null){Validate();return;}
            long trianglesBefore=root.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh!=null?(long)f.sharedMesh.triangles.Length/3:0);
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            materials.Clear();
            materials["Ground"]=Material("ForestFloor",new Color(.24f,.29f,.15f),.94f);
            materials["Trail"]=Material("WeatheredEarth",new Color(.51f,.40f,.27f),.9f);
            materials["Bark"]=Material("RoughBark",new Color(.29f,.22f,.15f),.91f,true);
            materials["Canopy"]=Material("DeepFoliage",new Color(.12f,.26f,.12f),.77f);
            materials["LeafLight"]=Material("SunlitFoliage",new Color(.29f,.39f,.17f),.8f);
            materials["Fern"]=Material("Understory",new Color(.18f,.32f,.16f),.83f);
            materials["Fern"].SetFloat("_Cull",0);
            materials["Rock"]=Material("WeatheredRock",new Color(.39f,.40f,.34f),.93f);
            materials["Moss"]=Material("SoftMoss",new Color(.29f,.35f,.16f),.98f);
            materials["Ruin"]=Material("AgedLimestone",new Color(.54f,.52f,.40f),.9f);
            materials["Wood"]=materials["Bark"];
            var environment=root.Find("Environment");
            foreach(var renderer in environment.GetComponentsInChildren<MeshRenderer>())
            {
                // Never touch interaction, token, obstacle or portal materials/components.
                if(renderer.transform.GetComponentsInParent<Transform>().Any(t=>t.name=="P85_EnvironmentalObstacles"))continue;
                var old=renderer.sharedMaterial;if(old==null)continue;
                string key=old.name=="Jungle_ReadablePath"?"Trail":old.name;
                if(materials.TryGetValue(key,out var replacement)){renderer.sharedMaterial=replacement;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
            }
            foreach(var filter in root.Find("Environment/JunglePath").GetComponentsInChildren<MeshFilter>())
            {filter.sharedMesh=Surface(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(filter);}
            var ground=root.Find("Environment/Terrain_Ground/JungleGround").GetComponent<MeshFilter>();ground.sharedMesh=Surface(ground);
            foreach(var filter in environment.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.name=="Frond"){filter.sharedMesh=Leaf();filter.GetComponent<Renderer>().sharedMaterial=materials["Fern"];PrefabUtility.RecordPrefabInstancePropertyModifications(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(filter.GetComponent<Renderer>());continue;}
                string shape=filter.name=="Trunk"?"TaperedTrunk":filter.name=="ButtressRoot"?"ButtressWedge":filter.name=="Stone"?"OrganicRock":null;
                if(shape==null)continue;filter.sharedMesh=NaturalShape(filter.sharedMesh,shape);PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            }
            // Retain original collision mesh and all collision geometry exactly.
            Mesh canopy=null;int trees=0;
            foreach(Transform item in root.Find("Environment/Vegetation"))
            {
                var foliage=item.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Canopy")||f.name=="Crown").ToArray();
                if(foliage.Length==0)continue;
                trees++;
                foreach(var filter in foliage)
                {
                    if(canopy==null)canopy=Foliage(filter.sharedMesh);filter.sharedMesh=canopy;
                    float variation=.91f+.16f*Mathf.PerlinNoise(item.position.x*.31f+10,filter.transform.localPosition.y+item.position.z*.17f);
                    filter.transform.localScale=Vector3.Scale(filter.transform.localScale,new Vector3(variation,.88f+variation*.14f,variation));
                    filter.transform.localRotation=Quaternion.Euler(0,trees*137.5f,0);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(filter.transform);
                }
                // Cull only small distant foliage, not trunks or their colliders; no duplicated LOD geometry.
                var lod=item.gameObject.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.None;
                lod.SetLODs(new[]{new LOD(.012f,foliage.Select(f=>f.GetComponent<Renderer>()).ToArray())});lod.RecalculateBounds();
            }
            var visual=new GameObject("JungleNaturalVisuals");visual.transform.SetParent(root,false);
            // Clumps soften the geometric roadside and frame the exit, outside the full 7m road.
            var fern=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/JunglePrototype/FernCluster.prefab");
            if(fern==null)throw new Exception("Existing fern prefab missing");
            foreach(float z in new[]{10f,31f,66f,88f,100f,106f})foreach(float side in new[]{-1f,1f})
            {
                var bush=(GameObject)PrefabUtility.InstantiatePrefab(fern,scene);bush.name="RoadsideUnderstory";bush.transform.SetParent(visual.transform);bush.transform.position=new Vector3(side*5.7f,Height(z),z);bush.transform.localScale=Vector3.one*(z>99?1.5f:1.15f);bush.transform.rotation=Quaternion.Euler(0,z*37,0);
                foreach(var r in bush.GetComponentsInChildren<Renderer>()){r.sharedMaterial=materials["Fern"];PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(bush.transform);
                foreach(var f in bush.GetComponentsInChildren<MeshFilter>()){f.sharedMesh=Leaf();PrefabUtility.RecordPrefabInstancePropertyModifications(f);}
            }
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="JungleNaturalGrade";
            var grade=profile.Add<ColorAdjustments>(true);grade.postExposure.Override(.12f);grade.contrast.Override(7);grade.saturation.Override(-7);grade.colorFilter.Override(new Color(1,.98f,.94f));
            var white=profile.Add<WhiteBalance>(true);white.temperature.Override(4);
            AssetDatabase.CreateAsset(profile,Art+"/JungleNaturalGrade.asset");
            var volume=visual.AddComponent<Volume>();volume.isGlobal=true;volume.priority=5;volume.sharedProfile=profile;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.57f,.65f,.65f);RenderSettings.ambientEquatorColor=new Color(.37f,.43f,.33f);RenderSettings.ambientGroundColor=new Color(.20f,.24f,.17f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.58f,.66f,.59f);RenderSettings.fogStartDistance=95;RenderSettings.fogEndDistance=220;
            var sky=new Material(Shader.Find("Skybox/Procedural")){name="JungleHazeSky"};sky.SetFloat("_SunSize",.025f);sky.SetFloat("_AtmosphereThickness",.85f);sky.SetColor("_SkyTint",new Color(.53f,.58f,.61f));sky.SetColor("_GroundColor",new Color(.43f,.49f,.40f));sky.SetFloat("_Exposure",1);AssetDatabase.CreateAsset(sky,Art+"/JungleHazeSky.mat");RenderSettings.skybox=sky;
            foreach(var light in root.GetComponentsInChildren<Light>())if(light.type==LightType.Directional){light.color=new Color(1,.91f,.76f);light.intensity=1.45f;light.shadows=LightShadows.Soft;light.shadowStrength=.72f;light.transform.rotation=Quaternion.Euler(46,-32,0);}
            foreach(var camera in root.GetComponentsInChildren<Camera>()){camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;camera.backgroundColor=RenderSettings.fogColor;camera.clearFlags=CameraClearFlags.Skybox;}
            long trianglesAfter=root.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh!=null?(long)f.sharedMesh.triangles.Length/3:0);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Validate();Debug.Log("JUNGLE NATURAL VISUALS APPLIED; trees="+trees+"; mesh instance triangles before="+trianglesBefore+"; after="+trianglesAfter);
        }
        [MenuItem("Tools/Lost Worlds/Jungle/Validate Natural Visuals")]
        public static void Validate()
        {
            JungleRoadRefinement.Validate();
            var root=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single();
            if(root.transform.Find("JungleNaturalVisuals")==null)throw new Exception("Visual upgrade missing");
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)if(material==null||material.shader==null||!material.shader.isSupported)throw new Exception("Invalid material: "+renderer.name);
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh==null)throw new Exception("Missing mesh "+filter.name);
            var systems=root.GetComponentsInChildren<ParticleSystem>();if(systems.Length!=4||systems.Sum(p=>p.main.maxParticles)!=76)throw new Exception("Particle budget changed");
            if(root.transform.Find("JungleNaturalVisuals").GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Decorative collider introduced");
            Debug.Log("JUNGLE VISUAL VALIDATION PASS: road, 10 tokens, references, no fork, no new colliders; particles 4/76; LODs="+root.GetComponentsInChildren<LODGroup>().Length);
        }
        public static void CaptureBatch()=>EditorApplication.delayCall+=()=>{try{Validate();Capture();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
        static void Capture()
        {
            var camera=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single().GetComponentsInChildren<Camera>().First();
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;var clone=Object.Instantiate(pipeline);var previous=QualitySettings.renderPipeline;
            var target=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var active=RenderTexture.active;
            var position=camera.transform.position;var rotation=camera.transform.rotation;bool ortho=camera.orthographic;float fov=camera.fieldOfView;
            try
            {
                clone.gpuResidentDrawerMode=GPUResidentDrawerMode.Disabled;clone.useSRPBatcher=false;QualitySettings.renderPipeline=clone;
                camera.orthographic=false;camera.fieldOfView=60;Directory.CreateDirectory("Documentation/JungleVisuals");
                for(int i=0;i<2;i++)
                {
                    camera.transform.position=i==0?new Vector3(15,17,-12):new Vector3(0,3,85);
                    camera.transform.LookAt(i==0?new Vector3(0,3,32):new Vector3(0,7,108));
                    for(int frame=0;frame<3;frame++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target,isPreview=true});
                    RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Documentation/JungleVisuals/"+(i==0?"Jungle":"Portal")+".png",image.EncodeToPNG());
                }
            }
            finally{camera.transform.SetPositionAndRotation(position,rotation);camera.orthographic=ortho;camera.fieldOfView=fov;QualitySettings.renderPipeline=previous;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(clone);}
        }
        public static void ApplyBatch()=>EditorApplication.delayCall+=()=>{try{Apply();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};
    }
}
