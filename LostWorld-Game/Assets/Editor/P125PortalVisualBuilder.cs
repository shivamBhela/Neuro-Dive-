using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using LostWorlds.Person2.Journey;

namespace LostWorlds.Person2.Editor
{
    public static class P125PortalVisualBuilder
    {
        const string Art = "Assets/Art/Materials/P125Journey/";
        public static void BuildAndValidateBatch()
        {
            EditorApplication.delayCall += () => {
                try { Apply(); P125JourneyValidation.BuildAndValidateBatch(); }
                catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
            };
        }
        [MenuItem("Tools/Lost Worlds/P12.5/Apply Portal Visual Refinement")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "PortalEnergySurface.asset");
            if (mesh == null)
            {
                mesh = new Mesh { name = "PortalEnergySurface" };
                var vertices = new Vector3[34]; var uv = new Vector2[34]; var triangles = new int[96];
                for (int i=0;i<=32;i++)
                {
                    float angle=i*Mathf.PI/32;
                    uv[i+1]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    vertices[i+1]=new Vector3(uv[i+1].x*3.74f,uv[i+1].y*4.94f,0);
                    if(i<32){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}
                }
                mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,Art+"PortalEnergySurface.asset");
            }
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"PortalEnergy.mat");
            if(mat==null){var shader=Shader.Find("Lost Worlds/Quiet Portal Energy");if(shader==null)throw new Exception("Portal shader missing");mat=new Material(shader);AssetDatabase.CreateAsset(mat,Art+"PortalEnergy.mat");}
            string path=P125JourneyBuilder.Folder+"/RealmExitPortal.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(root.transform.Find("PortalAtmosphere_VisualOnly")!=null)return;
                var group=new GameObject("PortalAtmosphere_VisualOnly");group.transform.SetParent(root.transform,false);
                var surface=new GameObject("TranslucentEnergy");surface.transform.SetParent(group.transform,false);surface.transform.localPosition=new Vector3(0,0,-1.79f);
                surface.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=surface.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                var go=new GameObject("SlowCyanMotes");go.transform.SetParent(group.transform,false);go.transform.localPosition=new Vector3(0,1.9f,-1.85f);
                var particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particles.main;main.loop=true;main.playOnAwake=false;main.maxParticles=12;main.startLifetime=6;main.startSpeed=.06f;main.startSize=new ParticleSystem.MinMaxCurve(.08f,.14f);main.startColor=new Color(.35f,.8f,1,.32f);main.simulationSpace=ParticleSystemSimulationSpace.Local;
                var emission=particles.emission;emission.rateOverTime=1.3f;
                var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(5.6f,3.2f,.2f);
                var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.y=.10f;
                var color=particles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.25f),new GradientAlphaKey(1,.65f),new GradientAlphaKey(0,1)});color.color=gradient;
                var pr=particles.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/P12Polish/SoftMotes.mat");pr.shadowCastingMode=ShadowCastingMode.Off;pr.receiveShadows=false;
                var effect=group.AddComponent<PortalAtmosphere>();var so=new SerializedObject(effect);so.FindProperty("energySurface").objectReferenceValue=renderer;so.FindProperty("motes").objectReferenceValue=particles;so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
