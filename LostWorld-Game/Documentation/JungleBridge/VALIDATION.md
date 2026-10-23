# Jungle bridge validation — 2026-10-15

Validated with Unity 6000.6.1f1 in the isolated project copy. No Windows build was run.

The final twelve authored/copied asset files were applied to the actual project and matched to the validated copy by SHA-256. Among pre-existing Assets/Packages/ProjectSettings files only Jungle_Prototype.unity changed; all other realm scenes, runtime scripts, shared assets and project settings match this task's baseline. Documentation and PROJECT_STATUS.md were also updated.

- Jungle road: 5,725 grounded vertices and 327 body-clearance samples; continuous straight road, no route fork, ten unique Jungle tokens.
- Bridge: 65 raycast walking-surface and capsule-clearance samples across five lanes and both approaches. Seven-metre route remains clear. Only three new colliders: seamless deck and two outer rails.
- P4: 22 Play Mode checks passed using actual Input System G/E events, including range rejection, held-object following, repeated input and visible success feedback.
- P12.5: 188 runtime checks passed, including token persistence, duplicate protection, automatic collection, portal transitions and null-reference recovery.
- P13: 122 full-journey checks passed with no runtime Console warnings/errors. Full Jungle → Ruins → Crystal Cave → Hidden City → Journey Complete remains supported.
- P11/P12/P14 structural checks and scene/prefab missing-reference checks passed. P8.5's 400 obstacle samples passed.
- Existing serialized scene scripts, rigidbodies and colliders were compared with the original: only the intentional ground bank MeshCollider changed. Interaction component wiring and original interaction colliders are unchanged. All runtime source files and shared prefabs are unchanged.
- Bridge and GRAB station renders inspected. Static water mesh uses subtle normal detail; no water physics, animation, lights or particles were added. Existing lighting/vegetation, four particle systems and 76-particle budget are preserved.

Limits: these checks use test references, not a Person-1 controller. Real controller clearance, jump/step-offset behavior and performance on the demo computer remain manual checks. Batch logs may contain Unity licensing-service/Curl shutdown notices and an asset-import-worker shutdown timeout; these are outside the monitored gameplay QA. No compiler errors or warnings were introduced.

Evidence: Unity logs in this folder, `P4_VERIFICATION.txt`, `FINAL_QA.txt`, `Before.json`, `FILES_CHANGED.txt`, `Bridge.png` and `Interaction.png`. See README.md for exact menus and manual testing steps.
