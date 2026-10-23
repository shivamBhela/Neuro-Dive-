> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P14 build validation and handoff evidence

## Windows artifact

- Unity 6000.6.1f1, Windows x64, non-development player; four scenes explicitly supplied in Jungle → Ruins → Crystal Cave → Hidden City order.
- Build succeeded with **0 errors and 3 warnings**. See BUILD_RESULT.txt for exact output and size.
- Actual artifact: `Builds/LostWorldsSIH/LostWorlds.exe`; use `RunDemo.cmd` for the opt-in controller-free panel. `DEMO_HANDOFF.md` is included beside it. The Unity-generated `LostWorlds_BackUpThisFolder_ButDontShipItWithYourGame` directory is backup/debug data and can be omitted from a distribution copy; retain the EXE, player/data/runtime folders and DLLs.
- The build was authored in the isolated Unity validation copy and copied to the actual project. Unity automatically rewrote shader-prefilter/default-volume/global settings in that copy during building; these generated changes were not copied into the project. Original source settings were restored in the validation copy before regressions.

## Build warnings and diagnostics

1. Generated `JungleGround` mesh has triangle pre-baking disabled. Unity 6.6 automatically baked it for this build; future Unity versions may require explicit baking configuration. Existing solid-ground geometry and obstacle tests remain unchanged.
2. `Hidden/Core/DebugOccluder` was stripped because it has no matching pipeline tag.
3. `Hidden/Core/DebugOcclusionTest` was stripped for the same reason. These are URP debug shaders, not the portal energy shader; actual portal materials/shaders were exercised in the player.

The batch Editor also emitted its licensing-refresh message and an internal JobTempAlloc diagnostic at shutdown. These are recorded rather than described as a warning-free Editor build. The executable's successful smoke run reported **zero runtime warnings/errors**. No C# compiler errors/warnings or shader compilation errors were found in the final build log.

## Standalone verification

`LostWorlds.exe -person2-smoke` passed **98 checks**, exited 0, and used the actual built scene-loading code. It covered startup, all four realms, GRAB/ACTIVATE and all three relics through P9, route locking, 36 unique tokens with duplicate prevention and HUD/persistence, portal materials, Reduced Motion, duplicate transitions and exactly one Journey Complete.

Automated smoke mode alone sets runInBackground so a hidden window continues updating. Initial backbuffer screenshots were black because the test window was hidden; final captures use an explicit render request with the **unchanged built render pipeline**, not the Editor batching workaround. P14_Realm0.png through P14_Realm3.png are real standalone captures; all four were visually reviewed. The controller-free IMGUI panel is intentionally omitted during the automated smoke run and remains a manual UI check.

## Final regressions

Fresh P4/P5/P6/P7/P8/P9/P10/P11/P12/P12.5/P13 runs passed **22/31/34/66/57/54/65/78/71/212/129** checks respectively: **819**, plus **400** P8.5 collision/bypass samples. All exited 0. No C# warnings/errors, shader compilation errors or runtime exceptions were found in these successful test logs. P10 included actual localhost TCP, malformed messages and reconnect/cleanup; P12.5 included all-four-realm PortalAtmosphere cache/missing-reference recovery. No regression was skipped.

After tests, every actual Assets/Packages/ProjectSettings file matched the regression copy, and every Windows build file matched the tested artifact. Only the handoff guide was added beside the copied artifact. The actual project retains its original configuration and frozen content. The standalone 98-check result is recorded in PLAYER_SMOKE.txt; it is additional to the Editor regressions above.

## Frozen content

New files are P14DemoBuild.cs, the Demo assembly/Person2BuildDemo.cs and their metadata, handoff/build/test documentation and ignored build artifacts. The opt-in harness only calls existing public methods; it does not implement movement, physics control, another scene loader, relic/token state or a hardware transport. No saved scene, prefab, P11 API or existing P4–P13 runtime script was edited for P14. Build Settings already had the correct first four realms; SampleScene remains in the project but is excluded from this artifact.

Editor menus: **Tools > Lost Worlds > P14 > Validate Demo Build** and **Build Windows Demo**. Existing P13 and earlier test/demo menus are retained. See DEMO_HANDOFF.md for manual controls and remaining player/hardware checks.
