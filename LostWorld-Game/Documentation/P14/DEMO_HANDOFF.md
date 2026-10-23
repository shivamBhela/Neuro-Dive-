> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# Lost Worlds — final Person 2 SIH demo handoff

Unity **6000.6.1f1**, URP **17.6.0**, Windows x64. Person 1's controller is intentionally absent. No movement/jump/auto-run is supplied by this demo.

## Run

- Unity: open `C:\UnityProjects\Lost Worlds`, allow imports to finish, then **Tools > Lost Worlds > P14 > Validate Demo Build**. **P13 > Play Final Test** retains the existing Editor Journey Demo.
- Windows: run `Builds\LostWorldsSIH\RunDemo.cmd`. Keep the entire build folder together (EXE, UnityPlayer.dll, data and runtime folders). The launcher uses a 1600x900 window and `-person2-demo`.
- `LostWorlds.exe -person2-demo` also enables the controller-free demonstration panel. Without this flag the new harness is absent; original realm/P11 behavior is retained for later integration.
- Rebuild: **Tools > Lost Worlds > P14 > Build Windows Demo**. Windows Build Support must be installed for this Unity version. The build explicitly includes Jungle, Ruins, Crystal Cave and Hidden City in that order. The existing SampleScene entry is preserved in project settings but excluded from this demo artifact.

## Controller-free controls

The standalone panel uses temporary stationary test references and existing public APIs; it does not move or animate a player.

| Action | Control |
| --- | --- |
| Select an interaction station | Focus GRAB / Focus ACTIVATE |
| Perform selected action | G or R = GRAB; E = ACTIVATE |
| Select route zone | Focus route, then A/D or Left/Right Arrow |
| Emit future player jump command | Space (event only, no movement) |
| Effort feedback | Page Up / Page Down / Backspace |
| Collect optional token | Collect next Journey Token (test) |
| Use original realm exit | Enter portal / skip remaining tokens |
| Test external input | Enable hardware listener; select an appropriate station first |
| Calm visual setting | Reduced Motion |
| Exit | Quit demo or Alt+F4 |

Follow Jungle → Ruins → Crystal Cave → Hidden City → Journey Complete. After a realm transition, select a new test station to rebind the reference. The 36 optional Journey Tokens retain their count across these transitions; restarting the application resets the session. Missing tokens do not gate the existing completion flow. Jungle is a straight road. Hidden City retains its optional branch token placements; a single City branch offers 34 of the 36 total tokens.

Environmental Editor test menus remain available; P5 now validates removal and F2 relic views are retired, including P9 simulator and P10 TCP client tests. Run P13 **Run Final QA** for the combined automated journey; P12.5 tests cover portal cache-loss and missing optional visuals.

## Processed hardware messages

Enable the adapter using the demo panel (default listener `127.0.0.1:55010`) or the existing P11 Editor menu. Send ASCII JSON, one object per newline, over TCP:

```json
{"command":"LEFT"}
{"command":"RIGHT"}
{"command":"JUMP"}
{"command":"GRAB"}
{"command":"ACTIVATE"}
```

Append a newline after each object. Invalid/unknown/oversized messages are rejected; P9 keyboard input remains available. The parser and P9 CommandChannel are unchanged. Unity accepts processed commands only, never raw EEG/EMG/biosignals. Real hardware on another computer requires an explicitly configured listener address and appropriate local network permissions; this demo does not change firewall settings or expose a listener automatically.

## Person 1 integration

- Keep `IntegrationSession`, `RealmBindings`, `PlayerCommandBridge` and branch APIs unchanged.
- Subscribe to `RealmReady`; spawn/position the player at the realm's `ArrivalPose`, then call `IntegrationSession.BindPlayer(playerTransform, holdPoint)`.
- Bind `PlayerCommandBridge.PlayerCommandRequested` / existing UnityEvents to LEFT, RIGHT and JUMP implementations. At a route zone, P6 owns the one accepted choice; consume `IntegrationSession.BranchSelected` and its world-space path points for branch following.
- P11 owns scene loading, arrival rebinding and duplicate transition/completion guards. Existing portals call that same API. Do not add a competing loader or token gate.
- Launch without `-person2-demo` when using the real player. Person 1 supplies the camera and disables/replaces prototype cameras as appropriate.

## Remaining checks

Actual jump/collider fit, auto-run speed, branch following, camera comfort, arrival placement/rebinding, target-device frame time and live hardware compatibility require Person 1/hardware-team integration. The existing primitive art, open cave cutaway and silent audio slots are intentional prototype limitations. Test the EXE's normal rendering on the SIH display/GPU; Editor capture results alone do not establish build performance.

Automation flag `-person2-smoke` runs the existing interaction/route/token/portal APIs through a full standalone journey and exits with status 0 on success; it is test scaffolding, not a second progression system. Its realm screenshots go to Unity's persistent data folder (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\Lost Worlds` with current project identity).

No Person 1 systems were changed and no Git push is part of this handoff.
