# P4 — GRAB and ACTIVATE

This phase adds only Person 2 interactions and temporary keyboard testing. There is no player/controller, movement, jump, inventory, relic/progression logic, scene loading, route logic, HUD, effort/rest, hardware adapter, simulator, or final GameInputManager.

## Run the Jungle demo

1. Let Unity finish importing/compiling, then use **Tools > Lost Worlds > P4 > Open Jungle Interaction Demo**.
2. Press Play and click inside the **Game** view so keyboard input reaches Unity.
3. Press **G**: in the left close-up view, the amber practice block moves from its stone stand to the blue target stand and turns teal.
4. Press **E**: in the right close-up view, the pedestal orb turns teal and a larger steady beacon appears.
5. Further presses do not repeat completed actions. To reset reliably with the existing fast Play Mode settings, stop, use **Open Jungle Interaction Demo** again, then press Play.

No player movement is needed: two explicitly named stationary test references sit near the existing Jungle GrabPoint and ActivatePoint. Each uses a 3.5m interaction range. The original anchors, world geometry, lighting, prefabs, and overview camera are preserved. New cameras render over the original overview only while `Jungle_Prototype/P4InteractionDemo_TEST_ONLY` is active. Disable that root to return to the original Jungle overview. These cameras and separate references are temporary test scaffolding, not player cameras or a controller.

For a range test, while playing move `GRAB_Station_G/GrabReference_TEST_ONLY` or `ACTIVATE_Station_E/ActivateReference_TEST_ONLY` away from its demo object in the Scene view and press the relevant key. Outside range, nothing succeeds. Move it back before trying again. Enable **Debug Feedback** on its InteractionInteractor for optional Console messages; select the component to see its range gizmo.

## Reusable framework

All runtime source is in `Assets/Scripts/Interaction`, namespace and assembly `LostWorlds.Person2.Interaction`. The assembly definition references the already-installed Input System for the temporary adapter; no Person 1 assembly or controller is referenced.

| Type | Responsibility |
| --- | --- |
| `IInteractable` / `InteractableBase` | Command type, eligibility, closest interaction point, range guard, one-shot completion, optional success material and UnityEvent. |
| `InteractionInteractor` | Query colliders on a command, select the nearest eligible matching target, enforce range and success cooldown, and hold one grabbed object. Owns no input or movement. |
| `GrabInteractable` | Move an object to its configured Target Point or the interactor's Hold Point. Parent it to that point, make attached rigidbodies kinematic, and disable held colliders so it cannot push the future player. |
| `ActivateInteractable` | Enable an optional visual and invoke the inherited success event once. Can be reused for switches, pedestals, crystals, and other world-object activation. |
| `P4KeyboardTestInput` | Temporary Input System adapter: G calls TryGrab; E calls TryActivate. Can be disabled independently. |

The entry points for later approved integration are `InteractionInteractor.TryGrab()` and `TryActivate()`, returning whether an action succeeded. Both can be called without keyboard code. The public target method also checks eligibility and range, preventing an out-of-range direct call. These methods are not a GameInputManager or a hardware transport; processed hardware commands can be connected in a later authorized phase.

## Inspector setup in any environment

- Add an InteractionInteractor to a reference object. Assign **Reference** if range should follow another Transform; otherwise its own Transform is used. It never moves that transform. Set **Range**, **Interaction Layers**, **Success Cooldown**, **Debug Feedback**, and **Draw Range** as needed.
- Assign a **Hold Point** for GRAB. Alternatively assign an individual GrabInteractable's **Target Point** and optional local hold position/rotation. Missing targets and parenting cycles are rejected. One held item per interactor is intentional; no inventory/drop/reset progression system is included.
- Add GrabInteractable or ActivateInteractable to a world object with an enabled Collider on itself or a child. Child colliders resolve their parent target. Ordinary colliders and trigger zones are supported; the reference does not need a Rigidbody or controller.
- Detection is forgiving: no aiming/facing requirement, collider-surface distance rather than pivot-only distance, no timed gesture sequence, and no per-frame physics search. Non-convex mesh zones use bounds as an approximate forgiving distance fallback. Prefer box/sphere zones for predictable authoring.
- An optional **Interaction Point** overrides distance measurement, but the object must still have a collider inside the query sphere for discovery. Keep that point inside its authored zone.
- Assign **Feedback Renderers**, **Success Material**, an optional Activate **Activated Visual**, and/or **On Succeeded** UnityEvent in the Inspector. Success changes the renderer's material reference; it does not mutate the source material asset. Do not place the interactable itself under a visual that starts inactive.
- Reusable prefabs are `Assets/Prefabs/Interactions/P4/GrabDemoObject.prefab` and `ActivateBeacon.prefab`. Drag them into any of the four scenes and configure the local reference/hold point. Ruins, Crystal Cave, and Hidden City have not been modified in P4.

## Editor tools and validation

- **Tools > Lost Worlds > P4 > Setup Jungle Interaction Demos** adds missing demo content and saves Jungle. It refuses an unsaved/dirty Jungle and preserves an existing demo rather than overwriting edits.
- **Tools > Lost Worlds > P4 > Validate Jungle Demo Setup** checks wiring and missing scripts.
- **Tools > Lost Worlds > P4 > Run Play Mode Interaction Checks** opens Jungle after the normal save prompt, enters Play mode, validates behaviour using queued Input System keyboard events, and returns to Edit mode. Test interactions are never saved into the scene. Temporary test devices/fixtures are removed. The runner temporarily permits unfocused synthetic keyboard input and restores those Input System settings before returning to Edit mode; no settings asset is saved.

The original P2 validator intentionally forbids all runtime scripts because it validates an environment-only phase. After P4, use the P4 checks for Jungle; the P2 builder/validator source was preserved. P3 validation remains unchanged for the other three scenes.

`PLAY_MODE_VERIFICATION.txt` records the actual executed check count. `Grab_Before.png`, `Grab_After.png`, `Activate_Before.png`, and `Activate_After.png` are actual Unity captures from that Play Mode run. As in P2/P3, captures use an unsaved temporary URP clone with batching disabled; project rendering settings remain unchanged.

## Integration boundary

Person 1 still owns the player, movement, jumping, animations, checkpoints, adaptive difficulty, and main progression. Future integration should replace the two stationary test references with an agreed player/reference transform and disable/remove the P4 keyboard adapter and close-up cameras. EEG/EMG/exoskeleton processing remains outside Unity. No raw-signal processing or hardware protocol is implemented.
