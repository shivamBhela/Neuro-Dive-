> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P6 — LEFT / RIGHT route choices

This phase adds only Person 2 route-zone selection and world feedback. Person 1 still owns actual player movement. Selecting a route does not move, rotate or teleport any participant, close a path, start a timer, collect a relic, load a scene or advance progression.

## Try both choices

1. Allow Unity to finish importing.
2. Run **Tools > Lost Worlds > P6 > Play Jungle Route Test**. Save any personal scene edits if prompted.
3. The Editor opens Jungle and starts Play with a close-up view of its existing split. Click the **Game** view.
4. Press **A** or **Left Arrow** for LEFT, or **D** or **Right Arrow** for RIGHT.
5. The selected arrows/sign become teal and its steady beacon appears. The other path stays open and neutral. The Console reports the accepted choice.
6. Further input cannot change the selection. Stop and rerun the menu to test the other choice in a fresh encounter.

The test reference is stationary inside the zone. There is no Player prefab, controller or movement script. P6 test input and its camera are disabled in the saved scene; the Editor test menu enables them only during Play. It temporarily changes camera visibility without saving those changes. The existing P4/P5 controls and tools remain available unchanged.

Opposing keys pressed together are ignored without choosing a default. Release them and press a single direction. A key held before entering a zone or starting a new encounter must be released and pressed again. There is no timeout or speed requirement.

## Jungle placement and paths

The saved `Jungle_Prototype/P6_RouteChoice` prefab instance sits at the existing `FutureGameplayPoints/RouteSplit`, on ground at approximately (0, 2, 70). Original anchors and path geometry are retained.

- The BoxCollider choice zone is a trigger, 12m wide × 4m high × 10m deep, centered 1.5m above its ground origin.
- Both existing branches are 4.5m wide. LEFT follows X=-10/-8; RIGHT follows X=10/8. Both rejoin around Z=96.
- World-space LEFT/RIGHT signs and ground arrows clarify each route. New markers have no solid colliders, gates, hazards or penalties.
- 234 ground/body-clearance samples passed along both branches over a 3m-wide sampled corridor. Final fit testing still belongs with Person 1's actual player prefab.

## Reusable components

All runtime code is under `Assets/Scripts/World`, in assembly/namespace `LostWorlds.Person2.World`.

| Component | Role |
| --- | --- |
| `RouteChoiceZone` | Checks a reference against the configured trigger box and accepts at most one LEFT/RIGHT selection per encounter. |
| `RouteChoiceFeedback` | Updates world-marker materials and steady selection beacons; restores neutral feedback for a new encounter. |
| `P6RouteTestInput` | Temporary A/D/arrow-key adapter, disabled by default. No movement or final input manager. |

Reusable prefab: `Assets/Prefabs/World/Routes/P6RouteChoice.prefab`. It can be placed in another environment and its box, signs and arrows repositioned for that local split. Other environments have not been modified.

The zone uses exact local box containment, including rotated/scaled boxes. The reference requires no Rigidbody, Collider or Player tag. Optional debug feedback and a selected-object zone gizmo are available in the Inspector. A disabled zone/box, inactive or missing reference, invalid direction, or out-of-zone command cannot select.

## Integration contract and encounter lifecycle

An approved input adapter can call:

```csharp
bool accepted = routeZone.TryChoose(RouteChoice.Left, agreedReferenceTransform);
```

Read `Selection`, `HasChoice` and `EncounterNumber` for current state.

- `SelectionAccepted(RouteChoiceZone zone, RouteChoice choice)` is a C# event.
- `On Selected` is an Inspector UnityEvent carrying the accepted choice.
- `EncounterBegan(RouteChoiceZone zone)` and the Inspector `On Encounter Began` event notify explicit resets.
- Subscribe/unsubscribe with consumer lifecycles and read current state on subscription.

The first encounter is ready when the scene/session starts. It remains locked after a successful choice even if the reference leaves/re-enters, the component is disabled/re-enabled, or more commands arrive. This avoids accidental resets near the boundary.

Only a future authorized encounter owner should call `BeginNewEncounter()` when a genuinely new encounter is intended. That method clears feedback and increments the encounter number; it does not move anyone. P6 provides this lifecycle API but implements no progression owner. Callback reentrancy cannot reset or replace an accepted choice. A fresh Play/application session resets state, including with fast Play Mode settings.

LEFT means the authored left branch when looking along Jungle's +Z travel direction. Passing that choice to actual player movement remains Person 1's later integration work. No movement receiver is connected in this phase.

## Editor tools and validation

- **Tools > Lost Worlds > P6 > Setup Jungle Route Choice** creates missing assets/setup and saves Jungle. It requires saved, clean scenes and preserves existing P6 content on repeated runs.
- **Tools > Lost Worlds > P6 > Validate Jungle Route Setup** checks the loaded Jungle's wiring.
- **Tools > Lost Worlds > P6 > Run Route Play Mode Checks** runs input/zone/feedback tests, verifies a second fresh Play session, then returns to Edit mode.
- Generation has already completed in Unity 6000.6.1f1; no manual setup execution remains.
- `PLAY_MODE_VERIFICATION.txt` records 34 passing checks. `PATH_VERIFICATION.txt` records 234 route samples. The regression report records the existing P5 checks, including P4 G/E compatibility.
- `Route_Before.png`, `Route_Left.png`, and `Route_Right.png` are actual Unity renders. The capture helper uses the same temporary unsaved URP batching workaround as prior phases, restoring the existing pipeline afterward.

The isolated batch Editor still logs an internal SearchDatabase startup exception. P6 compilation and checks completed. Inspect normal Scene/Game views in the original Editor.

No Person 1 systems, HUD, effort/rest, hardware, GameInputManager, scene progression or P7 functionality was added.

