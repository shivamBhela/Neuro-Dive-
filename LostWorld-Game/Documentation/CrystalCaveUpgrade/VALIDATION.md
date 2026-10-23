# Crystal Cave validation — 2026-10-15

Unity 6000.6.1f1 compiled and tested the saved sources in an isolated copy while the actual project remained open. Preview captures use a temporary pipeline clone with batching disabled, restored afterward; shared render settings are not changed.

| Check | Result |
| --- | --- |
| Cave E/pause/unlock/Hidden City entry | 30 passed; Verified-E.log |
| Cave P10 parser ACTIVATE/pause/unlock/entry | 32 passed; Verified-Hardware.log |
| Ruins activation regression | 29 passed; Ruins-Regression.log |
| P11 integration/full flow | 65 passed; P11.log |
| P12 polish/mute/reduced motion | 71 passed; P12.log |
| P12.5 tokens/portals/persistence | 200 passed; Final-P125.log |
| P13 full journey | 133 passed; Verified-P13.log |
| Cave support/body clearance | 126 samples passed; Emission-Final.log |
| P8.5 cave obstacles | Four prefab obstacles, eight solid colliders, 100 centre-lane collision/side-bypass checks; P11.log |
| Missing references/P14 readiness | Eight scenes/all prefabs, 36 tokens/four portals retained; Final-References.log |

The final E/hardware tests verify inactive portal/frame/triggers, locked exit rejection, pause latch with no timeout, single resume after reveal, rejection of repeated activation, persistent unlock, pedestal illumination after Unity reimport, and actual P11 loading of Hidden City. The hardware test uses the real P10 parser/channel entry point, not a physical rehabilitation device. P12.5 verifies automatic bound-reference token collection and persistent counts; tokens remain optional.

The initial editor compilation found an obsolete GetInstanceID call, replaced by deterministic position-based color selection. Visual review caught URP disabling emission on reimport; cave materials now retain the emissive flag, with an explicit activation test. No compile warnings/errors or runtime Console warnings/errors in final monitored portal/P13 runs. Historical failed Build.log is retained for traceability. Host licensing/shutdown diagnostics are outside the gameplay monitor.

No new runtime scripts, dynamic lights or particle emitters. The four existing particle systems retain the 76-particle cap. New decorative geometry has no colliders; existing road/obstacle colliders are preserved. All new materials use standard URP Lit and reuse project textures. No light bake, downloads, Git push or Windows build.

Changed: CrystalCave_Prototype.unity; new CrystalCaveUpgrade.cs/meta, seven cave-local materials and three render meshes; PortalUnlockValidation.cs extended to Crystal Cave and its next-realm exit policy; PROJECT_STATUS.md and this documentation. The actual saved cave matches the tested scene hash. Jungle, Ruins and Hidden City match their original hashes in BeforeScenes.json. The final copied mineral materials retain their emission keywords.

Remaining manual checks: actual Person-1 receiver binding, auto-run stopping distance, player reference height, real jump/collider fit, physical hardware/network deployment, target-PC frame times and visual/audio comfort. A pause hook cannot stop an unconnected player controller. No controller was created or changed.
