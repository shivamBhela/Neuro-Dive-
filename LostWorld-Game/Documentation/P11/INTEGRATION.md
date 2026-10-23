> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P11 — Person 1 integration contract

P11 adds integration contracts and realm flow only. No player, movement, jump, camera-follow, checkpoint, adaptive difficulty or P12 feature is implemented. Existing P4–P10 sources and demo scenes are preserved.

## Runtime ownership

Each realm has a `P11_Integration/RealmBindings` component. Opening any realm creates one persistent `IntegrationSession` prefab, with the existing P9 channel, optional P9 simulator/P10 adapter, P4 interactor, P7 HUD and P8 effort display. Journey Tokens use their separate persistent JourneyLedger. The simulator and TCP adapter start disabled; enable the desired producer explicitly. No socket opens automatically.

Jungle is the first Build Settings scene, followed by Ruins, Crystal Cave and Hidden City. SampleScene remains available. The four realms stay separate.

## Person 1 binding

Wait until `IntegrationSession.Instance` and its `CurrentRealm` are available. Subscribe to:

- `session.PlayerBridge.PlayerCommandRequested(HardwareCommand)` for LEFT, RIGHT and JUMP outside route zones. Inspector UnityEvents are also exposed on PlayerCommandBridge. Choose one binding method to avoid duplicate actions.
- `session.BranchSelected(RealmBindings, RouteChoice, Vector3[])` for an accepted branch. These are ordered world-space waypoint snapshots, not movement instructions implemented by Person 2. Hidden City exposes both branches. Jungle is a straight main road; Jungle, Ruins and Cave have no required branch selection.
- `session.RealmReady(RealmBindings)` after a successful scene load. For the initial binding also inspect CurrentRealm, because the initial event may already have fired.
- `session.TransitionStarted` to suspend Person 1 input/movement during a load; `session.Completed` for the one-shot completion hook.

Person 1 must create or retain their own player, position/orient it using `realm.ArrivalPose` with their collider's appropriate vertical offset, attach their camera, then call `session.BindPlayer(playerTransform, holdTransform)`. The integration layer never moves that transform. Supply a hold point for generic grab objects. Existing demo objects may have their own target override.

On every realm arrival the player reference is cleared. Rebind after positioning your player; this avoids stale references or immediate transitions using the old exit position. Person 1 decides whether their prefab persists or is respawned. Unsubscribe callbacks when the subscribing component is destroyed. Disable prototype cameras/listeners when the actual player camera is attached.

`realm.GetMainPath()` gives a default route (left branch in split realms). An accepted branch overrides the split section, then rejoins at the final branch waypoint. Person 1 must implement path following and rejoining. Do not apply an extra lateral command for an accepted route: P11 consumes directional commands inside a choice zone, including repeated inputs after the choice locks. JUMP remains an event only. Outside the zone LEFT/RIGHT go to the player callback.

GRAB/ACTIVATE call the unchanged P4 interactor using the bound reference. P7 automatically tracks nearby eligible targets and the active route zone; P8 remains feedback only. Binding integration disables legacy P4/P6 keyboard testers at runtime to prevent duplicate delivery, including subsequent realms. Standalone earlier demo workflows remain available.

## Scene flow and session state

Flow: JungleExit → RuinsEntrance; RuinsExit → CrystalCaveEntrance; CrystalCaveExit → HiddenCityEntrance; City FinalDestination_LocationOnly → completion.

With a bound reference, entering the exit's configurable geometric volume requests the next scene. No tag, collider or Rigidbody is required for detection. `TryAdvance(currentRealm, referenceInsideExit)` is also available as an explicit trusted exit hook. The transition locks before callbacks/loading, rejects repeated requests, gates commands while loading and releases only when the next realm is ready. Final completion fires once and stops further integration commands. The completion status displays Journey Complete. No collectible is a completion gate.

The persistent session retains the independent JourneyLedger for 36 optional tokens. Token IDs prevent duplicates across scene unloads; stopping/starting Play starts a fresh session. No disk save is added. The former relic-only IntegrationSession.Relics member is removed; player binding, commands, branch, arrival, transition and completion APIs are unchanged.

## Manual tests

1. Run **Tools > Lost Worlds > P11 > Validate Integration Readiness** in Edit mode.
2. Run **Tools > Lost Worlds > P11 > Play Integration Preview**. This reopens Jungle for reliable testing with the existing fast Play Mode settings. No player is expected.
3. During Play select a stationary reference/empty GameObject near an interaction or branch, then run **Tools > Lost Worlds > P11 > Bind Selected Reference + Enable Simulator (Play Mode Test)**. This binds a test position only; it does not create a controller. Click Game view. A/D or arrows = LEFT/RIGHT; Space = JUMP event; G/R = GRAB; E = ACTIVATE. Move the test reference manually in Scene view to inspect range/exit behavior. Keep it near the ground and avoid binding a moving/grabbed object itself.
4. **Tools > Lost Worlds > P11 > Advance From Exit (Play Mode Test)** requests the real transition using the realm's exit anchor. It requires no player and intentionally serves as a manual journey test. In each next realm rebind a test reference to test interaction range. Use the Journey Demo token buttons to confirm Journey Token counts survive each transition.
5. In City, test both branch choices in separate fresh runs, then advance at the final destination. Confirm calm completion and no duplicate loads/events.
6. Optional: **Tools > Lost Worlds > P11 > Enable Hardware Adapter (Play Mode Test)** opens the existing P10 adapter. Send P10's documented newline-delimited JSON, e.g. `{"command":"JUMP"}` plus newline, to the configured endpoint. Both producers use the session's one CommandChannel. Stop Play to close the adapter.
7. Check HUD at 1280×720 and 1920×1080; verify prototype cameras and later player camera do not compete. Earlier P4–P10 demo menus and controls are unchanged. P8 effort can be supplied through the prefab's existing EffortModel API; no raw biosignals are processed.

Setup has already run when the generated assets are delivered. **Tools > Lost Worlds > P11 > Setup Integration Readiness** only adds missing integration roots/prefab and preserves existing P11 scene edits. Re-running it does not overwrite authored paths.

## Remaining Person 1 verification

Actual jump clearance/collider fit, auto-run speed, branch steering, camera ownership and arrival offsets cannot be verified without Person 1's prefab. P8.5 geometry/colliders are unchanged. The path points are authoring guides, not navigation or collision avoidance. Keep hardware processing external and use P10's existing validated command transport.
