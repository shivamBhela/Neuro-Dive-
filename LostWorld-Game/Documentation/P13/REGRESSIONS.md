> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P13 regression results

Final revalidation includes the latest PortalAtmosphere fix and all-four-realm missing-reference/cache-recovery tests. Existing P13 menus and safe optimizations were reused; no further runtime changes were required. The table below records the complete fresh P4–P13 sweep, not a mixture of earlier results.

Unity 6000.6.1f1, 2026-09-23. Fresh runs on the existing isolated validation copy, followed by file comparison against the actual project. All tests exited with code 0. Only the new QA report/metadata needed transferring; runtime changes were already in the actual project. No realm scenes, prefabs, packages or settings changed in P13.

| Suite | Passed checks |
| --- | ---: |
| P4 interactions | 22 |
| P5 relics | 31 |
| P6 routes | 34 |
| P7 HUD | 66 |
| P8 effort/rest | 57 |
| P9 simulator | 54 |
| P10 adapter | 65 |
| P11 integration | 78 |
| P12 polish | 71 |
| P12.5 journey/portal visuals | 212 |
| P13 combined final QA | 129 |
| **Total** | **819** |

The unchanged P8.5 validator also passed its 400 obstacle collision/bypass samples. These do not establish jump feasibility for an absent Person 1 controller.

P10 covered actual localhost TCP delivery for all five commands, malformed messages, framing, reconnect/cleanup, disabled channel and simulator fallback. P4–P9 exercised original temporary keyboard controls. P11 covered shared commands, branch data, optional player references, relic persistence and duplicate scene-transition protection. P12/P12.5 verified VFX lifecycle, portal visuals, Reduced Motion, duplicate token protection, missed-token completion and HUD layouts. P13 independently collected all 36 tokens and 3 relics together while retaining Rest Needed and reached completion once through the four existing portals.

No C# compiler warnings/errors, shader errors or runtime exceptions were found in the successful regression logs. P13's Console listener also observed no warnings/errors during its audit/Play test. Unity batch startup logged `Access token is unavailable; failed to update`; immediately afterward licensing resolved the entitlement and updated the license successfully. This launcher/licensing message is recorded separately from project Console/compiler results. The first P13 runner attempt failed on an empty batch starting-scene setup; that QA-tool issue was fixed before the successful final run.

Detailed combined results and scene inventories: [FINAL_QA.txt](FINAL_QA.txt). Manual checks, optimizations and profiling limits: [FINAL_TESTING.md](FINAL_TESTING.md).

These are Editor/Play Mode regressions, not a packaged-player performance certification. Actual player jump/collider fit, branch-following, camera comfort, target-device frame time and live hardware compatibility remain manual/integration checks. P14 has not started; no Git push was performed.
