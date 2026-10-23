# Targeted Hidden City finale validation — 2026-10-16

Unity 6000.6.1f1 / URP. Tests ran against an isolated copy of the actual Assets/Packages/ProjectSettings; only the tested Hidden City scene and new trophy material were copied back. No environment generator or Windows build ran.

| Check | Result |
| --- | --- |
| Console E / one-time trophy / pause ownership / actual token UI / Replay / Finish | 38 passed |
| P10 processed ACTIVATE via P9 channel | 40 passed |
| P11 player hooks and full realm flow | 67 passed |
| P12 feedback, mute, reduced motion and progression | 75 passed |
| P12.5 tokens, persistence, duplicate guards, portal recovery (first three realms), optional-token completion | 191 passed |
| P13 full journey, 36 tokens, interactions, hardware command callbacks, effort/rest | 133 passed |
| P14 ordered scenes, dependencies and missing references | Passed; no executable built |

Final compilation reports no C# warnings/errors. Targeted finale and P13 monitored runtime checks report no warnings/errors. The retired City portal is intentionally excluded from active portal-recovery tests; it remains referenced but disabled for compatibility, with its collider disabled. No new scene objects were added/deleted. Only the trophy group's transform moved, from (0,0,92) to the exact old portal origin (0,0.15,86).

The console wait request releases once. The existing finale immediately requests a separate completion pause; this is intentional and prevents a future runner continuing behind the screen. Replay clears the pause and token session; Finish is idempotent.

Player locomotion, actual brake distance, collider/jump fit, physical hardware and built-player behavior were not tested. Tests use reference-only probes, the existing simulator/parser and P11 APIs.

See individual logs in this folder. Current manual steps are in README.md.

## Actual project application

All 14 changed source/scene/material files were compared byte-for-byte with the tested copy (AppliedFiles.txt). Jungle, Ruins and Crystal Cave scene hashes remain identical to their pre-task hashes. The additional actual-project batch run could not start because that project is already open in Unity; the open Editor was not stopped. Reload HiddenCity_Prototype and run the menu checks manually. Automated results above are from the matching isolated copy, not an actual runner.
