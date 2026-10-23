> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P11 validation

Unity 6000.6.1f1 / URP 17.6.0. Executed in an isolated source copy; only P11 assets, four realm scene additions and EditorBuildSettings were copied back. No standalone executable build or real Person 1 prefab test was performed.

| Suite | Passed checks |
| --- | ---: |
| P11 journey, commands, branch data, relic persistence, completion and HUD layout | 76 |
| P4 interactions (unchanged suite) | 22 |
| P5 relics (unchanged suite) | 31 |
| P6 routes (unchanged suite) | 34 |
| P7 HUD (unchanged suite) | 66 |
| P8 effort/rest (unchanged suite) | 57 |
| P9 simulator (unchanged suite) | 54 |
| P10 adapter, including real localhost TCP (unchanged suite) | 65 |
| P8.5 route collision/bypass samples | 400 |

Final runs contained no C# compiler warnings/errors or logged test/runtime exceptions. All suites exited successfully. P11's initial test needed to respect the existing 0.25-second interaction cooldown; the corrected test waits before collecting the next relic. A regenerated test prefab reference was corrected before the final run.

P11 verified: optional-player startup; LEFT/RIGHT/JUMP callbacks; no transform movement on JUMP; both branch paths; repeated route input protection; unchanged P4 GRAB/ACTIVATE via the session channel; real collection of each realm's relic through GRAB; 0/3 → 1/3 → 2/3 → 3/3; duplicate rejection; HUD count; entrance rejection; each exit accepted once; input gated during loading; one persistent session; cleared player references on arrival; three arrival callbacks; one final completion callback. Structural validation checks all four scene/anchor/path/prefab bindings and enabled Build Settings entries.

P8.5 validated four obstacles per realm (16 total): Jungle 4 solid colliders, other realms 8 each; protected locations and bypasses remained clear. Physical fit with Person 1's actual jump/collider remains untested.

Completion HUD renders at 1280×720 and 1920×1080 were inspected. Text fits and stays within the Canvas. Capture temporarily used an unsaved URP clone to work around the existing batch-rendering issue; project render settings were unchanged.

Preservation: 480 of 485 pre-existing Assets/Packages/ProjectSettings files are byte-for-byte unchanged. Only the four requested realm scenes and EditorBuildSettings changed. Scene comparison found no removed original serialized objects; existing scene edits were the root's added child reference and Unity's empty-name whitespace normalization. P4–P10 source, prefabs, test scenes and P8.5 obstacle assets are unchanged.

Manual verification: run Tools > Lost Worlds > P11 > Validate Integration Readiness and Play Integration Preview; inspect all four realms, use the exit test menu, bind a stationary reference for command/range checks, and verify normal Editor HUD rendering. Person 1 must later test their own controller, arrival offsets, path following, camera and jump clearance. See INTEGRATION.md for the binding contract.

P11 status: READY FOR UNITY VERIFICATION. P12 not started.
