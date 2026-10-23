# R6 final QA — 18 October 2026

## Automated result
Unity 6000.6.1f1 compiled successfully. R6 preflight passed: Windows x64 support installed, first four enabled scenes in correct order, all four scenes checked for missing scripts/broken serialized references, star boundaries 0/17/18/29/30/36 correct. Existing journey regression extended to 200 passing Play Mode checks, with no runtime warnings/errors recorded.

Covered fresh Jungle start, four-realm traversal, upright pelvis, one runner/camera per realm, single persistent session/ledger, single active simulator/adapter, LEFT movement and outer clamp, P10 RIGHT, obstacle/bridge jumps, all 36 physical token pickups and duplicate/neighbor/vertical rejection, all three gateway approach positions, distant ACTIVATE rejection, pause/unlock/resume, Hidden City automatic completion, 36-token/three-star summary, Finish and Replay/reset. Tests position the capsule for token checks then drive the route with scripted commands; this is not a human collection playthrough.

Validation ran in the existing isolated Unity copy. Tested scene/prefab/runtime files matched the actual project; QA source files were hash-checked after compilation. Log: `Validation/R6QA.log`. No runtime, environment, character, HUD, input or build-setting changes were necessary. No executable was generated.

## Demo checklist
1. Open the actual project, allow import/compilation, and check Console.
2. Select a 16:9 Game view (1920×1080 or 1280×720), then **Tools > Lost Worlds > R5 > Play Full Journey**.
3. A/D or arrows: change position; Space: jump; E: ACTIVATE. Confirm nearby-position tokens remain uncollected and jump pickups require contact.
4. Cross Jungle's intentional bridge gap. Approach each gateway from any position; confirm safe pause, visible prompt, E unlock and automatic transition. Distant E must do nothing.
5. Check HUD/effort readability, camera comfort, hair, animation, lighting and portal effects during actual play. Batch screenshots were not produced, so no new rendered HUD/layout approval is claimed.
6. Reach Hidden City's trophy without ACTIVATE. Check actual token count, stars, Finish and Replay. Tokens never block completion.
7. Optional processed hardware smoke test: send `{"command":"LEFT"}`, `RIGHT`, `JUMP`, or `ACTIVATE` through the existing P10 transport. No raw biosignals are processed. Real hardware was not tested in this pass.
8. Repeat automated QA using **Tools > Lost Worlds > R6 > Run Final QA** (enters Play Mode and drives the journey).

## Build/performance readiness
Windows x64 support and scene order are valid. Jungle is first, then Ruins, Crystal Cave and Hidden City; the existing optional SampleScene remains fifth. Input System is enabled. Current standalone defaults are 1024×768 with fullscreen-window mode; use a 16:9 display or explicit 1600×900 window settings for the eventual demo. Existing P14 builder explicitly includes only the four realms.

No configuration blocker was found, but executable startup, shader behavior, deployment hardware and real device transport remain unverified until a build is authorized. Human visual/comfort review remains separate from automated QA. Cached camera collision buffers and event unsubscription were reviewed; no measured bottleneck justified an optimization. Target-machine FPS, GC allocations and GPU/VFX cost still require Profiler measurement. No Git push or Windows build.
