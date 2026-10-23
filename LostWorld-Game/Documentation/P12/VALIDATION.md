> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P12 validation

Completed Unity validation during the P12 implementation on 20 September 2026, using Unity 6000.6.1f1 and an isolated project copy. The generated assets were copied back and checked against the tested copies. This report records those completed runs; the final documentation update did not change runtime code or scenes.

| Suite | Passed checks |
| --- | ---: |
| P12 polish behavior | 71 |
| P4 interactions | 22 |
| P5 relics | 31 |
| P6 route choices | 34 |
| P7 HUD | 66 |
| P8 effort/rest | 57 |
| P9 command simulator | 54 |
| P10 hardware adapter, including localhost TCP | 65 |
| P11 integration and scene journey | 76 |
| P8.5 obstacle collision/bypass samples | 400 |

All final suites exited successfully. Final compilation/regression logs contained no C# warnings/errors or test/runtime exceptions. An initial preview implementation used an obsolete SceneHandle conversion; it was replaced with direct Scene comparison before the successful final runs.

P12 verified all four realm profiles, bounded particle counts, no added colliders, safe missing-audio behavior, conservative profile volume caps, mute, reduced-motion stop/clear/resume, disable/re-enable subscription lifecycle, success feedback through existing interactions, rejection of repeat feedback, all P11 exits and duplicate guards, old-realm cleanup, persistent session ownership, completion and three collected relics.

Four 1280x720 Unity renders were visually reviewed: Jungle, Ruins, Crystal Cave and Hidden City. The rendering helper temporarily used an unsaved URP clone to avoid the existing batch-rendering issue; project pipeline assets/settings were not changed. Normal Editor rendering and device frame rate remain manual checks.

Preservation verification at delivery: 500 of 504 pre-existing Assets/Packages/ProjectSettings files were byte-for-byte unchanged. Only the four realm scenes changed. Original serialized scene objects were retained, and no original collider records changed. Existing P4–P11 source, Person 1 integration APIs, P9/P10 architecture, world prefabs and Build Settings were unchanged. Copied P12 source/prefab/material/audio assets matched the tested files.

Audio assets remain intentionally empty: no recordings were auditioned, imported or copyrighted audio supplied. Check eventual licensed clips on the intended device at comfortable playback volume. Actual player jump/collider, branch-following and camera fit still require Person 1's prefab.

Manual menus:

- Tools > Lost Worlds > P12 > Validate Polish
- Tools > Lost Worlds > P12 > Play Polish Preview

Use P11's Advance From Exit (Play Mode Test) to visit every realm through the real flow. Inspect lighting/path contrast, gentle effects, interaction/relic feedback, Reduced Motion and Muted controls, and the final destination. See POLISH.md for full configuration notes.

P12 status: READY FOR UNITY VERIFICATION. P13 has not started.
