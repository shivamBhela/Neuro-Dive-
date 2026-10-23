
# P8 — Effort and rest feedback

## Test

Use **Tools > Lost Worlds > P8 > Play Effort/Rest Demo**. Unity represents the slash as an Effort submenu containing Rest Demo. Setup is also available at **Tools > Lost Worlds > P8 > Setup Effort Rest Demo** and preserves existing generated assets.

Click the Game view:
- Page Up: increase effort by 10 per press.
- Page Down: decrease effort by 10 per press.
- Backspace: reset effort to zero.
- F1: existing G/E interactions.
- F3: existing A/D or Left/Right Arrow route choice.

Stop and launch the menu again for a fresh demo, including original one-shot interactions/routes. Existing fast Play Mode settings are preserved.

## Behavior

The reusable model accepts finite values, clamped to 0–100. Its default cap is 80, with Near Limit at 80% of that cap (64). Normal is below the near threshold, Near Limit is below the cap, and Rest Needed begins at the cap. Decreasing effort immediately returns to the corresponding state. The recommendation never pauses, locks controls, deducts progress or causes game-over. There is no automatic exertion calculation, recovery timer or clinical interpretation.

Configure cap (1–100), near-limit fraction (0.1–0.99) and initial value in the model Inspector before Play. Runtime callers use Configure(cap, fraction), which updates state and HUD immediately. These are game feedback settings, not medically determined limits.

The panel shows a numeric value, cap, text state, calm recommendation and restrained colored bar. State remains readable without relying on color. No flashing or alarm audio.

## Reuse

- Assets/Scripts/UI/EffortModel.cs: independent model. SetEffort(processedValue), ResetEffort(), Configure(cap, nearFraction), Value/Cap/State, Changed and StateChanged callbacks.
- Assets/Scripts/UI/EffortHud.cs: subscribes only while enabled, reads current state on enable and supports Bind(model).
- Assets/Prefabs/UI/P8/EffortPanel.prefab: drop under the existing P7 Canvas. Includes its own default model and display; no test input in the prefab. Bind another model to share feedback across views.
- Assets/Scripts/UI/P8EffortTestInput.cs: isolated keyboard adapter used only in the P8 test scene.
- Assets/Scenes/Test/P8_EffortDemo.unity: separate copy of the dedicated P7 test scene with the panel added. P7 HUD prefab and original scene remain intact; no environment scenes are copied or merged.

Calls must be made on Unity's main thread. NaN/infinity are rejected; repeated values emit no events. Callback-triggered nested mutation is rejected to preserve consistent event snapshots. Subscribers should not mutate the source during notification. Future adapters may supply already-processed effort via SetEffort; none are implemented here. Raw EEG/EMG/exoskeleton processing must remain outside Unity.

Runtime effort resets to the configured starting value on a new Play session, including with domain reload disabled. Disable/re-enable preserves current effort. No persistence or scene progression is introduced.

## Manual verification

Run **Tools > Lost Worlds > P8 > Run Effort Rest Checks** for the automated P8 suite. Check normal/near/rest presentation at 1280x720, 1600x900 and 1920x1080. Confirm the rest text and bar do not overlap the Journey Token display, prompts or positive feedback. While Rest Needed is visible, test G/E and a route choice. Confirm the Console has no new errors.

P8 is rehabilitation/game feedback only, not medical diagnosis. No Person 1 systems, hardware adapters, raw signal processing or P9 work are included.

Validation completed in Unity 6000.6.1f1: P8 57 checks; P4 22, P5 31, P6 34, P7 66 regression checks passed. Generation is complete. All 408 pre-existing Unity files are unchanged.
