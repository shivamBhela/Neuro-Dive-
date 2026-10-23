# Hidden City/finale validation — 2026-10-15

Unity 6000.6.1f1 compiled and tested the saved sources in an isolated copy while the actual project remained open. No Windows executable was built.

| Check | Result |
| --- | --- |
| Final console E, pause, completion UI, trophy, Finish and Replay | 36 passed; Verified-E.log |
| Same flow via P10 processed ACTIVATE parser/channel | 38 passed; Verified-Hardware.log |
| Crystal Cave activation/transition regression | 30 passed; Cave-Regression.log |
| P11 integration and complete progression | 67 passed; P11.log |
| P12 polish/mute/reduced motion | 75 passed; P12.log |
| P12.5 token/portal/persistence journey | 204 passed; Verified-P125.log |
| P13 complete journey and duplicate guards | 135 passed; Verified-P13.log |
| Hidden City obstacles | Four prefabs/eight solid colliders; 100 collision/bypass samples passed; P11.log |
| Missing references/P14 readiness | Eight scenes/all prefabs, 36 tokens/four portals; References.log |

Finale tests verify inactive portal/frame and locked P11 exit, one accepted ACTIVATE, indefinite pause/no timeout, one resume after reveal, no trophy before completion, one trophy afterward, exact title/4-of-4/actual token text, repeated-completion rejection, real UI Button pointer-handler callbacks, one-shot Finish, guarded Replay, zero tokens after reset, rejected reentrant collection during reset, fresh Jungle and cleared player binding. Full-journey tests exercise all four realms and optional token persistence. Physical device input was not used: hardware verification runs the actual P10 parser/channel boundary.

No compile warnings/errors or runtime Console warnings/errors in final monitored portal/P13/P12.5 runs. Host licensing/shutdown diagnostics are outside the gameplay monitor. Preview capture temporarily uses a cloned render pipeline with batching disabled and restores it; project render settings are unchanged.

No runtime world particle systems or lights added for the finale. Eight UI spark images and a 1.5% slow trophy scale animation respect Reduced Motion; the reused chime respects Mute and plays at 0.06 volume. New environment decoration has no colliders. No fifth scene, duplicate loader or runner code.

Changed: HiddenCity_Prototype.unity and local FinaleUpgrade art; new HiddenCityUpgrade.cs/editor metadata; new JourneyFinale.cs/runtime metadata; IntegrationSession completion-only Replay/Finish APIs/events; JourneyLedger reset listener; Person2BuildDemo hides test overlay during completion; PortalUnlockValidation extends existing tests to the final realm/UI; PROJECT_STATUS.md and documentation.

Remaining manual checks: real Person-1 stop/resume, movement/camera/jump behavior, collider fit, physical hardware/network, mouse/keyboard focus and 720p/1080p UI on the target machine, subjective sound/visual comfort and target-PC frame time. No actual runner testing is claimed. Finish leaves a closing message rather than terminating the application; Replay reuses the current persistent session and retains non-punitive effort feedback.

The actual saved Hidden City scene and changed sources match the validated copy. Jungle, Ruins and Crystal Cave remain byte-for-byte unchanged against BeforeScenes.json. Only Hidden City art/scene and required finale/shared code were applied. No Git push or Windows build.
