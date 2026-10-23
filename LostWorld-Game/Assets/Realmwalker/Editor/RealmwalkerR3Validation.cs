using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Realmwalker.Authoring
{
    public static class RealmwalkerR3Validation
    {
        static RealmwalkerRunner runner;
        static Transform hips, head;
        static readonly HashSet<string> seen = new HashSet<string>();
        static int frames;
        static bool active;
        static Quaternion firstLeg, firstArm;
        static bool haveRunPose;
        static float legTravel, armTravel;
        public static void Begin(RealmwalkerRunner value)
        {
            runner = value;
            active = runner.GetComponent<RealmwalkerAnimation>() != null;
            seen.Clear(); frames = 0;
            haveRunPose = false; legTravel = armTravel = 0;
            if (!active) return;
            hips = runner.GetComponentsInChildren<Transform>().Single(t => t.name == "RW_Hips");
            head = runner.GetComponentsInChildren<Transform>().Single(t => t.name == "RW_Head");
            var animator = runner.GetComponent<Animator>();
            Require(animator.enabled && animator.avatar.isHuman && !animator.applyRootMotion, "Humanoid enabled; root motion disabled");
            Require(animator.runtimeAnimatorController.animationClips.Distinct().Count() == 6, "Six reusable clips");
            foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                Require(clip.humanMotion && clip.length > .1f && AnimationUtility.GetCurveBindings(clip).Length > 90, "Muscle/body curves: " + clip.name);
        }

        public static void Observe()
        {
            if (!active || runner == null) return;
            frames++;
            Require(hips.localPosition.y > .80f && hips.localPosition.y < 1.12f, "Upright pelvis: " + hips.localPosition);
            float headY = runner.transform.InverseTransformPoint(head.position).y;
            Require(headY > 1.42f && headY < 1.85f, "Upright head: " + headY);
            Require(Quaternion.Angle(runner.transform.rotation, Quaternion.identity) < .1f, "Visual clips do not rotate movement root");
            var follow = runner.GetComponent<RealmwalkerFollowCamera>();
            Require(follow != null && follow.View != null && !follow.View.orthographic, "Follow camera reference/projection");
            Require(Camera.allCameras.Count(c => c.gameObject.scene == runner.gameObject.scene && c.targetTexture == null) == 1, "Only one active realm camera");
            var a = runner.GetComponent<Animator>();
            string state = runner.GetComponent<RealmwalkerAnimation>().CurrentState;
            if (state == "Run" && !a.IsInTransition(0))
            {
                var leg = a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).localRotation;
                var arm = a.GetBoneTransform(HumanBodyBones.LeftUpperArm).localRotation;
                if (!haveRunPose) { firstLeg = leg; firstArm = arm; haveRunPose = true; }
                legTravel = Mathf.Max(legTravel, Quaternion.Angle(firstLeg, leg));
                armTravel = Mathf.Max(armTravel, Quaternion.Angle(firstArm, arm));
            }
            if (state != null && !a.IsInTransition(0) && a.GetCurrentAnimatorStateInfo(0).IsName(state) && seen.Add(state))
            {
                Debug.Log("R3 PASS: observed " + state + "; pelvis=" + hips.localPosition.y);
            }
        }

        static void Capture(Camera camera, string name)
        {
            string folder = "Assets/Realmwalker/Validation/R3";
            Directory.CreateDirectory(folder);
            var target = new RenderTexture(960, 540, 24);
            var old = camera.targetTexture; var activeTexture = RenderTexture.active;
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
                File.WriteAllBytes(folder + "/" + name + ".png", image.EncodeToPNG());
            }
            finally { camera.targetTexture = old; RenderTexture.active = activeTexture; UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        }

        public static void Finish()
        {
            if (!active) return;
            foreach (string state in new[] { "Idle", "Run", "Takeoff", "Airborne", "Fall", "Land" })
                Require(seen.Contains(state), "State not observed: " + state);
            Require(frames > 100, "Insufficient Play Mode coverage");
            Require(legTravel > 20 && armTravel > 15, "Run must physically animate limbs: leg=" + legTravel + " arm=" + armTravel);
            Debug.Log("R3 PASS: physical leg/arm swing " + legTravel + "/" + armTravel + " degrees.");
            Debug.Log("R3 PASS: all six animation states, camera and upright pose checked across " + frames + " Play Mode samples.");
            active = false;
        }
        static void Require(bool ok, string message) { if (!ok) throw new Exception("R3 FAIL: " + message); }

        [MenuItem("Tools/Lost Worlds/R3/Play Jungle Preview")]
        public static void Play() => RealmwalkerR2.Play();
        [MenuItem("Tools/Lost Worlds/R3/Validate Jungle Animation and Camera")]
        public static void Validate() => RealmwalkerR2.Tests();
        public static void Batch() => EditorApplication.delayCall += Validate;

        public static void PreviewBatch() => EditorApplication.delayCall += () =>
        {
            try
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(LostWorlds.Person2.Integration.IntegrationSession.RealmScenes[0]);
                var model = UnityEngine.Object.FindAnyObjectByType<RealmwalkerRunner>();
                var camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude).First(c => c.enabled);
                model.transform.position = new Vector3(0, .05f, 8);
                camera.orthographic = false; camera.fieldOfView = 48; camera.nearClipPlane = .1f;
                camera.rect = new Rect(0, 0, 1, 1);
                var animator = model.GetComponent<Animator>();
                animator.Rebind(); animator.SetFloat("StrideRate", 1);
                // Edit-mode Camera.Render does not consistently refresh GPU skinning after manual Animator.Update.
                // Bake only these unsaved preview renderers so captures show the sampled pose, not stale vertices.
                var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
                var baked = skins.Select(s => new Mesh()).ToArray();
                for (int i = 0; i < skins.Length; i++)
                {
                    var visual = new GameObject("UnsavedPosePreview");
                    visual.transform.SetParent(skins[i].transform, false);
                    visual.AddComponent<MeshFilter>().sharedMesh = baked[i];
                    visual.AddComponent<MeshRenderer>().sharedMaterials = skins[i].sharedMaterials;
                    skins[i].enabled = false;
                }
                foreach (string state in new[] { "Idle", "Run", "Takeoff", "Airborne", "Fall", "Land" })
                {
                    animator.Play(state, 0, state == "Run" ? .125f : .5f); animator.Update(.016f);
                    for (int i = 0; i < skins.Length; i++) skins[i].BakeMesh(baked[i]);
                    Debug.Log("R3 PREVIEW POSE " + state + " leg=" + animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).localEulerAngles);
                    camera.transform.position = model.transform.position + new Vector3(3, 2.1f, 4);
                    camera.transform.LookAt(model.transform.position + Vector3.up);
                    Capture(camera, state + "_Front");
                    camera.transform.position = model.transform.position + new Vector3(0, 3.6f, -6.5f);
                    camera.transform.LookAt(model.transform.position + new Vector3(0, 1.15f, 3.2f));
                    camera.fieldOfView = 58; Capture(camera, state + "_Follow"); camera.fieldOfView = 48;
                }
                Debug.Log("R3 PREVIEW: six sampled Humanoid clips rendered, no scene saved.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        };
    }
}
