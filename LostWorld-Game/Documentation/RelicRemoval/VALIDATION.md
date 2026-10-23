# Validation — collectible retirement (2026-10-14)

Unity 6000.6.1f1 / URP 17.6.0. All final suites exited 0. Tests ran in the isolated Unity project, then the validated scenes/prefabs and deletions were applied to the actual project. All delivered scripts/scenes/prefabs match tested files. Unity build preprocessing changed six temporary settings files; those automatic changes were not copied back. Actual packages, project/render settings, P4 core, token/portal implementations and prefabs, P9 channel/P10 transport, PlayerCommandBridge and realm/branch data APIs remain unchanged.

| Suite | Passed checks |
| --- | ---: |
| P4 interactions | 22 |
| P6 reusable routes | 34 |
| P7 HUD | 39 |
| P8 effort/rest | 47 |
| P9 simulator | 44 |
| P10 adapter (actual localhost TCP) | 61 |
| P11 integration and progression | 60 |
| P12 polish | 59 |
| P12.5 tokens, persistence, portals and optional completion | 188 |
| P13 combined final journey | 122 |
| Total Editor behaviour checks | 676 |
| P14 actual Windows player | 91 |

Also passed: all eight realm/demo scenes and all prefabs free of retired objects/UI and missing scripts/object references; 36 authored tokens and four portals; 400 obstacle collision/bypass checks; Jungle 5,725 grounded vertices and 327 body-clearance samples; P14 ordered-scene/dependency validation. The three relic-only decorative focus rings were caught and removed by the structural audit.

P11 completes without collecting anything. P12.5 deliberately misses one token and reaches completion at 35; Completion_OptionalTokens.png documents this. P13 and the real Windows player collect all 36 with persistence/HUD and duplicate guards. Exactly three realm transitions and one Journey Complete remain. Portal cache-loss/missing-visual recovery and Reduced Motion still pass.

No C# warnings/errors or runtime exceptions in the successful regression logs; P13 and Windows-player Console monitors report zero runtime warnings/errors. Earlier assertions specific to deleted relic functionality were replaced or retired, so current check counts are intentionally lower.

## Windows build

Builds/LostWorldsSIH/LostWorlds.exe: succeeded, zero errors, three warnings. All 207 delivered build files were hash-verified against the tested build. Two obsolete files from the previous build were removed, including the retired collectible assembly output. RunDemo.cmd remains the launcher.

Build warnings are the existing categories: one generated ContinuousGround mesh lacks explicit collision pre-baking; Hidden/Core/DebugOccluder and Hidden/Core/DebugOcclusionTest were stripped. The build compiler also emits Editor shutdown JobTempAlloc diagnostics. These are recorded in P14.log and Reports/BuildResult.txt; the actual player smoke test has no runtime warnings/errors. No unrelated pipeline or mesh-import settings were changed to suppress them.

Actual player movement, jump/collider fit, camera ownership and hardware-team integration still require Person 1. No performance/FPS guarantee is inferred from the component inventory.
