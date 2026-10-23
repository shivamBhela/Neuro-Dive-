# Realmwalker R1B

Open Preview/Realmwalker_Preview.unity. The existing prefab now has 24 bones (including three cape bones), 103 SkinnedMeshRenderers, a Unity-valid Humanoid Avatar, and an Animator with an empty AwaitingAnimationClips state. Root motion is disabled. No movement/camera/input added.

97 source mesh internal names corrected to filenames; source geometry and GUIDs unchanged. Skinned derivatives preserve the original vertex/triangle data and add normalised weights/bind poses. Default A-pose appearance preserved to 0.000000135 m maximum measured vertex error.

Validated in Unity 6000.6.1f1: saved prefab/Avatar/skin/material references, weight sums, bind pose, Humanoid muscle set/get, finite posed geometry, and front/side/rear rendered shoulder/elbow/hip/knee/spine/head/cape poses. Final compilation has no C# errors/warnings; naming audit reports zero remaining mismatches. Validated assets copied to the actual project. No unrelated files changed.

Tools > Lost Worlds > R1B > Validate Rig reruns focused checks and writes RigPose/RigRest previews. It opens the preview scene; save your own work first. Do not run the R1A generator on the finished rig.

Ready for R2 integration work and R3 Humanoid animation trials, not production animation sign-off. Actual idle/run/jump/landing clips, root-motion conventions, contact/foot sliding, extreme shoulder/hip poses and runner collision remain untested. Mitten hands have no finger bones or facial rig. Cape has weighted bones, not cloth simulation. Separate capped joint meshes can show seams at extreme bends; 103 skin renderers should be consolidated before performance sign-off. No guaranteed clipping-free motion for arbitrary future clips.
