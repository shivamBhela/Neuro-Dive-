> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P13 — optimization and final testing

P4–P12.5 remain feature-frozen. P13 adds Editor-only QA and two small internal optimizations. No player controller, movement, jumping, branch following, new gameplay, scene content, hardware protocol or public API was added or changed.

## Changes

- Portal error follow-up: `PortalAtmosphere` now lazily restores its nonserialized MaterialPropertyBlock and polish reference, on enable and before Update. The previous Awake-only initialization could leave the cache null after a Play-mode script reload, producing repeated NullReferenceExceptions. Two regression checks deliberately clear the cache and verify recovery without Awake and on re-enable. Transitions, triggers and visual settings are unchanged.

- `ProcessedHardwareAdapter.Pump` compares connection/listening/error/drop values directly instead of constructing an unchanged status string every frame. Queue limits, parser, delivery, status events, transport lifetime and P9 fallback remain intact.
- `PortalAtmosphere` avoids resending an identical shader property block while Reduced Motion or a paused clock keeps the surface static. Pulse timing, visual appearance and particle lifecycle remain unchanged.
- `P13FinalQA` audits scene wiring, shaders, particle budgets and original P8.5/P12/P12.5 validators, then exercises a full journey with all three relics and all 36 tokens in one persistent session. It creates only temporary test references, restores the previous scene setup and writes `Documentation/P13/FINAL_QA.txt`. Console warnings/errors during this run fail the result.
- An initial fresh-batch test exposed a missing starting-scene setup in the new QA runner. The runner now opens Jungle before validators require a restorable scene setup and guarantees batch shutdown. This was a QA-tool issue, not a gameplay defect.

## Menus

**Tools > Lost Worlds > P13 > Run Final QA** runs the combined final scene/journey audit. Save any prompted scene changes first; do not edit scenes or interrupt Play during the automated test. It checks GRAB/ACTIVATE through P9, 3 relics and 36 tokens, repeated input/collection/transition guards, both route choices in fresh encounters, HUD tally, effort/rest independence, P11 optional-reference binding and full portal progression. It is not a replacement for the standalone keyboard/TCP regression suites listed in REGRESSIONS.md.

**Tools > Lost Worlds > P13 > Play Final Test** opens the existing controller-free P12.5 Journey Demo and Jungle overview; it does not create a new scene or controller. Use **Collect next token (test)**, **Collect remaining realm tokens (test)** and **Enter portal / skip remaining tokens**. The final button follows the existing P11 path through Ruins, Crystal Cave, Hidden City and Journey Complete. Stop and reopen to reset the session.

## Manual checks

1. Let Unity finish importing; run **Run Final QA** and confirm its final PASS report and a clean Console.
2. Run **Play Final Test**. Inspect every realm and portal, confirm gentle translucent effects and path visibility, collect/skip tokens, and verify persistent count and one completion. All 36 tokens include both optional branches; a normal single-branch journey offers 32 without penalty for missed tokens.
3. Toggle P12 Reduced Motion: all realm/portal particles should clear and the portal surface should remain steady. Check mute; audio remains intentionally silent until licensed clips are supplied.
4. In the existing P7/P9 demo, check G/E interactions; A/D or arrows at the route station. F1/F3 select the existing test stations. Space emits a JUMP command only. P8 Page Up/Page Down/Backspace changes/resets effort; REST must remain feedback only.
5. In **Tools > Lost Worlds > P10 > Play Hardware Adapter Demo**, test F4 connect, F5–F9 commands, F10 invalid message, F11 disconnect; check status and simulator fallback. P13 does not automatically open a real hardware connection.
6. Check Game view at 1280x720, 1600x900 and 1920x1080. Inspect the final player camera separately once available; the overview camera is test scaffolding.

For automatic token/portal contact without a controller, bind a temporary Transform using P11 **Bind Selected Reference + Enable Simulator (Play Mode Test)** and move it through zones in Scene view. Rebind after arrival. P11 **Advance From Exit (Play Mode Test)** remains available. Test references do not constitute player movement code.

## Performance audit and limits

| Realm | Renderer components (including inactive) | Colliders | Lights | Maximum particles |
| --- | ---: | ---: | ---: | ---: |
| Jungle | 1564 | 636 | 2 | 76 |
| Ruins | 582 | 223 | 1 | 76 |
| Crystal Cave | 216 | 124 | 7 | 76 |
| Hidden City | 321 | 140 | 1 | 76 |

Each realm has three P12 systems (64 combined maximum particles) and one portal emitter (12). Portals add no dynamic lights, solid colliders or post-processing requirement. Single-scene progression unloads the old realm; one session owns persistent state. Hardware processing stays outside Unity; the receiver retains its bounded 64-message queue and 16-message frame drain. Interaction proximity queries remain uncapped to avoid silently omitting valid targets; their existing 0.15-second context sampling is preserved.

The runtime-component review retained existing demo scaffolding to preserve earlier test menus. The P11 session starts the simulator, adapter and unbound interactor disabled; binding a reference suppresses legacy P4/P6 test inputs. The final preview enables only the overview camera in each realm. No redundant session is allowed by the final journey test. No additional component/object removal was justified without affecting those workflows.

Renderer counts are an inventory, not measured draw calls or an FPS guarantee. Jungle is the heaviest prototype and should be profiled on the intended target hardware with Person 1's camera/player. No speculative mesh merging, collision simplification, package/settings changes or visual degradation was performed. Batch-render previews use the existing temporary capture workaround; normal player-build GPU performance is not established by these tests.

## Person-1-dependent checks

- Actual jump height, capsule dimensions and clearance over the 0.6m obstacles.
- Auto-run speed, stopping behavior, branch waypoint following and arrival positioning/rebinding.
- Real player trigger/contact behavior and camera framing through portals, including motion comfort.
- End-device frame time, GPU/CPU profiling and real hardware timing/calibration.

Existing Person 1 integration callbacks, P9/P10 command architecture and P11 progression APIs are unchanged. No P14 work or Git push.
