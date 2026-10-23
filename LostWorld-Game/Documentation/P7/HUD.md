> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P7 — Person 2 HUD

The reusable HUD adds interaction prompts, route choices/confirmation, and brief positive feedback. It uses the existing uGUI package and Unity's built-in font. No packages, TMP resource imports, Player systems, scene progression, effort/rest or hardware features were added.

## Test in Unity

Run **Tools > Lost Worlds > P7 > Play HUD Demo**. The menu opens the dedicated demo scene and enters Play; click the Game view.

| View | Controls |
| --- | --- |
| **F1 — Interactions** | **G** GRAB, **E** ACTIVATE |
| **F3 — Routes** | **A / Left Arrow** LEFT; **D / Right Arrow** RIGHT |

These are the original P4/P6 adapters and controls. F1/F3 only switch demo cameras/context, without moving a player or loading environments. Test objects are instances of the existing reusable prefabs. The four environment scenes are not merged or modified.

Stop and rerun **Play HUD Demo** for a fresh session, particularly with the project's existing fast Play Mode settings. This resets completed interactions and allows testing the other route.

Also available:
- **Tools > Lost Worlds > P7 > Setup HUD Demo** — creates missing HUD/demo assets while preserving existing edits. Already executed; no generation remains.
- **Tools > Lost Worlds > P7 > Run HUD Play Mode Checks** — opens the demo, runs automated checks, then stops Play mode.

## Assets and bindings

- HUD prefab: `Assets/Prefabs/UI/P7/Person2Hud.prefab`
- Demo scene: `Assets/Scenes/Test/P7_HudDemo.unity`
- UI source: `Assets/Scripts/UI`, assembly `LostWorlds.Person2.UI`

`Person2Hud` subscribes to P4 `OnSucceeded`, and P6 `SelectionAccepted/EncounterBegan`. It reads current state when enabled/rebound and removes its subscriptions when disabled. Duplicate rejected commands produce no additional toast.

`HudWorldContext` holds explicit target/interactor/route-reference bindings. Existing P4/P6 do not publish proximity-change events, so this isolated read-only adapter samples eligibility/range every 0.15 seconds and emits an event only when availability changes. Accepted selections are entirely callback-driven. It never chooses targets, collects items, selects a route or changes a transform. A future authorized context owner can call `SetContext(...)` directly.

For reuse, place the HUD prefab in a Person 2 scene and assign the context's relevant targets/references and the shared P5 tracker. Customize input hint strings for the future input source. No Person 1 controller type is referenced or integrated. The separate `P7HudDemoControls` component belongs only to the demo; it is not part of the reusable HUD prefab.

## Presentation

- Screen Space Overlay Canvas, pixel-perfect rendering, Scale With Screen Size at 1600×900 with a 0.5 width/height match.
- Anchored margins, dark solid panels, warm-white text and restrained teal accents.
- Core prompt text is 28 reference pixels; counter is 38. Checked at 1280×720, 1600×900 and 1920×1080.
- Only the current context's prompt panel appears. Accepted route status stays clear after the toast disappears.
- Feedback lasts 4.5 unscaled seconds (Inspector configurable); no countdown, flashing, penalties or timed choices.
- Canvas does not block raycasts or consume G/E/R/A/D input. There are no interactive HUD buttons or EventSystem dependencies.

## Verification and manual review

`HUD_VERIFICATION.txt` records the executed HUD checks. Regression reports cover the unchanged P4, P5 and P6 suites. PNGs are Unity renders at the three target resolutions; the capture helper temporarily uses a camera-space Canvas and an unsaved URP clone, then restores Overlay mode and project settings.

Manually verify:
1. GRAB/ACTIVATE prompts match available actions and show one short success message.
2. Each relic increments the counter exactly once, reaching 3/3.
3. Both route keys work in fresh sessions; accepted choices stay locked and visible.
4. Text fits at 720p, 900p and 1080p, with comfortable contrast and no overlap.
5. Success messages clear after a few seconds; existing P4/P6 test menus still work.

The HUD and its demo are generated and saved. No P8 work has started.

