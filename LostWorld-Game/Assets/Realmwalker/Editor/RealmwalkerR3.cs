using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Realmwalker.Authoring
{
    public static class RealmwalkerR3
    {
        const string Root = "Assets/Realmwalker";
        const string Prefab = Root + "/Realmwalker_Player.prefab";

        [MenuItem("Tools/Lost Worlds/R3/Apply Animation and Camera")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory(Root + "/Animations");
            AssetDatabase.Refresh();
            var model = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var bones = model.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("RW_")).ToArray();
                if (bones.Length != 24) throw new Exception("Unexpected R1 skeleton; no regeneration allowed.");
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "/Rig/RealmwalkerAnimator.controller");
                if (!controller.parameters.Any(p => p.name == "StrideRate")) controller.AddParameter("StrideRate", AnimatorControllerParameterType.Float);
                var machine = controller.layers[0].stateMachine;
                foreach (var child in machine.states.ToArray()) machine.RemoveState(child.state);
                foreach (string name in new[] { "Idle", "Run", "Takeoff", "Airborne", "Fall", "Land" })
                {
                    float duration = name == "Idle" ? 3 : name == "Run" ? .64f : name == "Land" ? .24f : name == "Takeoff" ? .18f : 1;
                    var clip = CreateClip(model.transform, bones, name, duration);
                    string path = Root + "/Animations/" + name + ".anim";
                    var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (existing != null) { EditorUtility.CopySerialized(clip, existing); Object.DestroyImmediate(clip); clip = existing; }
                    else AssetDatabase.CreateAsset(clip, path);
                    var state = machine.AddState(name);
                    state.motion = clip; state.writeDefaultValues = false;
                    if (name == "Idle") machine.defaultState = state;
                    if (name == "Run") { state.speedParameter = "StrideRate"; state.speedParameterActive = true; }
                }
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = null;
                animator.runtimeAnimatorController = controller;
                animator.enabled = true; animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (model.GetComponent<RealmwalkerAnimation>() == null) model.AddComponent<RealmwalkerAnimation>();
                if (model.GetComponent<RealmwalkerFollowCamera>() == null) model.AddComponent<RealmwalkerFollowCamera>();
                PrefabUtility.SaveAsPrefabAsset(model, Prefab);
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(model); }
        }

        static AnimationClip CreateClip(Transform root, Transform[] bones, string name, float duration)
        {
            var clip = new AnimationClip { name = name, frameRate = 60 };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = name == "Idle" || name == "Run" || name == "Airborne" || name == "Fall";
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            foreach (var bone in bones)
            {
                // Humanoid joints must use muscle/body curves; transform curves are ignored by retargeting.
                if (!bone.name.StartsWith("RW_Cape")) continue;
                var curves = Enumerable.Range(0, 7).Select(_ => new AnimationCurve()).ToArray();
                for (int i = 0; i <= 60; i++)
                {
                    float t = i / 60f;
                    Pose(bone.name.Substring(3), name, t, out var rotation, out var offset);
                    var q = bone.localRotation * Quaternion.Euler(rotation);
                    var p = bone.localPosition + offset;
                    float[] values = { p.x, p.y, p.z, q.x, q.y, q.z, q.w };
                    for (int c = 0; c < 7; c++) curves[c].AddKey(t * duration, values[c]);
                }
                string path = AnimationUtility.CalculateTransformPath(bone, root);
                string[] properties = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
                for (int c = 0; c < 7; c++)
                {
                    for (int k = 0; k < curves[c].length; k++) curves[c].SmoothTangents(k, 0);
                    clip.SetCurve(path, typeof(Transform), properties[c], curves[c]);
                }
            }
            var restPositions = bones.Select(b => b.localPosition).ToArray();
            var restRotations = bones.Select(b => b.localRotation).ToArray();
            var humanoidCurves = Enumerable.Range(0, HumanTrait.MuscleCount + 7).Select(_ => new AnimationCurve()).ToArray();
            using (var handler = new HumanPoseHandler(root.GetComponent<Animator>().avatar, root))
            {
                for (int i = 0; i <= 60; i++)
                {
                    float t = i / 60f;
                    for (int b = 0; b < bones.Length; b++)
                    {
                        Pose(bones[b].name.Substring(3), name, t, out var rotation, out var offset);
                        bones[b].localPosition = restPositions[b] + offset;
                        bones[b].localRotation = restRotations[b] * Quaternion.Euler(rotation);
                    }
                    var human = new HumanPose();
                    handler.GetHumanPose(ref human);
                    float[] body = { human.bodyPosition.x, human.bodyPosition.y, human.bodyPosition.z,
                        human.bodyRotation.x, human.bodyRotation.y, human.bodyRotation.z, human.bodyRotation.w };
                    for (int c = 0; c < humanoidCurves.Length; c++)
                        humanoidCurves[c].AddKey(t * duration, c < HumanTrait.MuscleCount ? human.muscles[c] : body[c - HumanTrait.MuscleCount]);
                }
            }
            string[] bodyProperties = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
            for (int c = 0; c < humanoidCurves.Length; c++)
            {
                for (int k = 0; k < humanoidCurves[c].length; k++) humanoidCurves[c].SmoothTangents(k, 0);
                clip.SetCurve("", typeof(Animator), c < HumanTrait.MuscleCount ? HumanTrait.MuscleName[c] : bodyProperties[c - HumanTrait.MuscleCount], humanoidCurves[c]);
            }
            for (int b = 0; b < bones.Length; b++) { bones[b].localPosition = restPositions[b]; bones[b].localRotation = restRotations[b]; }
            clip.EnsureQuaternionContinuity();
            return clip;
        }

        static void Pose(string bone, string state, float t, out Vector3 e, out Vector3 p)
        {
            e = p = Vector3.zero;
            float wave = Mathf.Sin(t * Mathf.PI * 2), stride = wave;
            bool left = bone.StartsWith("Left"); float side = left ? 1 : -1;
            if (bone.EndsWith("UpperArm")) e.z = side * 20;
            if (bone.EndsWith("LowerArm")) e.x = -12;
            if (state == "Idle")
            {
                if (bone == "Chest") e.x = .8f * wave;
                if (bone == "Hips") p.y = .006f * wave;
                if (bone == "Head") e.y = wave * 1.2f;
            }
            else if (state == "Run")
            {
                float phase = Mathf.Repeat(t + (left ? 0 : .5f), 1);
                // Contact, compression, toe-off, folded recovery, knee drive, extension.
                // Opposite legs are half a cycle apart; short support leaves two flight intervals.
                float thigh = RunKey(phase, -25, -7, 25, 8, -34, -33, -25);
                float knee = RunKey(phase, 15, 24, 36, 88, 74, 36, 15);
                if (bone.EndsWith("UpperLeg")) e.x = thigh;
                if (bone.EndsWith("LowerLeg")) e.x = knee;
                if (bone.EndsWith("Foot")) e.x = RunKey(phase, 10, -19, -45, -25, -20, -3, 10);
                if (bone.EndsWith("UpperArm")) { e.x = -thigh * .70f; e.z = side * 5; }
                if (bone.EndsWith("LowerArm")) e.x = -70 + thigh * .12f;
                if (bone == "Hips") { p.y = -.018f + .025f * (1 - Mathf.Cos((t-.12f) * Mathf.PI * 4)); e.y = 1.5f * wave; }
                if (bone == "Spine") e.x = 6;
                if (bone == "Chest") { e.x = 1; e.y = -2 * wave; }
                if (bone == "Head") e.x = -5;
            }
            else
            {
                float weight = state == "Land" ? Mathf.Sin(Mathf.PI * t) : state == "Takeoff" ? Mathf.Sin(Mathf.PI * t * .5f) : 1;
                if (bone.EndsWith("UpperLeg")) e.x = -(state == "Airborne" ? (left ? 25 : 17) : state == "Fall" ? 10 : 14) * weight;
                if (bone.EndsWith("LowerLeg")) e.x = (state == "Airborne" ? 38 : state == "Fall" ? 20 : 26) * weight;
                if (bone.EndsWith("Foot")) e.x = -8 * weight;
                if (bone.EndsWith("UpperArm")) e.x = -18 * weight;
                if (bone.EndsWith("LowerArm")) e.x = -25 * weight;
                if (bone == "Hips" && state == "Land") p.y = -.055f * weight;
                if (bone == "Chest") e.x = (state == "Land" ? 5 : -2) * weight;
            }
            // Scarf follows chest skinning; cape has three existing secondary joints.
            if (bone.StartsWith("Cape")) e.x = (state == "Run" ? -8 : -3) + 2 * Mathf.Sin(t * Mathf.PI * 2 + (bone == "CapeEnd" ? .6f : 0));
        }

        static float RunKey(float t, params float[] values)
        {
            float[] times = { 0, .12f, .30f, .50f, .70f, .85f, 1 };
            for(int i=1;i<times.Length;i++) if(t<=times[i])
                return Mathf.Lerp(values[i-1],values[i],Mathf.SmoothStep(0,1,(t-times[i-1])/(times[i]-times[i-1])));
            return values[0];
        }

        // Targeted clip replacement: preserve GUID, controller, prefab and all other clips.
        public static void RunOnlyBatch() => EditorApplication.delayCall += () =>
        {
            var model=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var bones=model.GetComponentsInChildren<Transform>().Where(b=>b.name.StartsWith("RW_")).ToArray();
                var clip=CreateClip(model.transform,bones,"Run",.64f);
                var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Animations/Run.anim");
                EditorUtility.CopySerialized(clip,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(clip);AssetDatabase.SaveAssets();
                Debug.Log("RUN UPDATE: shared .64s cycle, 187.5 steps/min at 2.8m/s; root motion remains disabled.");
            }
            finally { PrefabUtility.UnloadPrefabContents(model); }
            RealmwalkerR3Validation.PreviewBatch();
        };

        public static void BatchAuthor() => EditorApplication.delayCall += () =>
        {
            try
            {
                Apply();
                var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
                var a = go.GetComponent<Animator>();
                a.Rebind(); a.Play("Run", 0, .25f); a.Update(0);
                var hips = go.GetComponentsInChildren<Transform>().Single(t => t.name == "RW_Hips");
                Debug.Log("R3 AUTHOR: hips=" + hips.localPosition + ", avatar=" + a.avatar.isHuman);
                Object.DestroyImmediate(go);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        };
    }
}
