# Ruins upgrade results — 2026-10-15

Unity 6000.6.1f1 compiled and ran saved sources in the temporary validation copy while the actual project remained open. Only the validated Ruins scene, local art folder and new editor metadata were copied back. Source and saved-scene hashes matched. The other three realm scene hashes match BeforeScenes.json.

| Check | Result |
| --- | --- |
| Ruins E/pause/unlock/Crystal Cave entry | 29 passed; Final-Ruins-E.log |
| Ruins P10 parser ACTIVATE/pause/unlock/entry | 31 passed; Ruins-Hardware.log |
| Jungle existing activation regression | 29 passed; Jungle-Regression.log |
| P11 commands/binding/flow/duplicate protection | 63 passed; P11.log |
| P12 feedback/mute/reduced motion | 67 passed; P12.log |
| P12.5 token/portal/persistence journey | 196 passed; Final-P125.log |
| P13 full journey/effort/HUD/interactions | 131 passed; Final-P13.log |
| Road support/body clearance | 105 samples passed, authored jump obstacles excluded; FinalBuild.log |
| P8.5 obstacles | All four realms passed existing collision/bypass checks; P11.log |
| References/P14 readiness | Eight scenes/all prefabs, 36 tokens/four portals, no missing references; Final-References.log |

Portal tests verify hidden renderers/triggers/frame, locked P11 exit rejection, E through existing channel, hardware through the actual P10 parser/channel, one pause request, no timed expiry, one resume after reveal, repeated-input rejection and actual Crystal Cave scene loading. Hardware test was before the final landmark-material correction; runtime code and pedestal behavior did not change afterward. Final E and all journey tests used the corrected visuals.

No C# compilation warnings/errors or runtime Console warnings/errors in monitored portal/P12.5/P13 runs. Unity host licensing/shutdown diagnostics outside gameplay monitoring are not claimed absent. Preview rendering temporarily disables batching on an in-memory pipeline clone and restores it; project render settings are unchanged.

Visuals inspected in Overview.png and Unlocked.png. Added decorative geometry has no colliders. Existing gameplay obstacle colliders remain; removed tree/fern visuals were removed with their colliders rather than leaving invisible blockers. Eight tokens remain optional and retain existing automatic collection logic. No runtime source files were changed.

Changed files: Ruins_Prototype.unity; new RuinsUpgrade.cs and metadata; PortalUnlockValidation.cs generalized to use the same checks for Jungle/Ruins; P11IntegrationValidation.cs now activates a realm gate before expecting exit acceptance; local SandstoneUpgrade art assets; PROJECT_STATUS.md and this documentation. No other realm scene or shared prefab/material was copied/edited.

Person-1-dependent checks: actual movement, stopping distance, controller reference height, jump clearance and collider fit. A pause callback alone cannot stop an unconnected controller. No physical hardware/device test or target-PC frame-time benchmark was performed. No Windows demo rebuild or Git push.
