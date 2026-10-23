# Final Jungle portal verification — 2026-10-15

The actual Jungle_Prototype scene already contained the wooden lever, P11 gate reference, original portal and hidden-on-start stone frame. No scene or runtime changes were needed for this verification. Only PortalUnlockValidation.cs was extended, plus this report/status documentation.

Unity 6000.6.1f1 ran the saved project sources in an isolated temporary copy because the actual project was open in Unity. This verifies the on-disk project, not unsaved changes in an already-open Editor scene. No Windows executable was rebuilt.

## Results

- E path: **22 checks passed** (Verify-E.log). Injected Input System E event through the existing fallback and P9 channel; verified the fallback defers when P9 simulator is enabled.
- Hardware path: **24 checks passed** (Verify-Hardware.log). Fresh Jungle, valid `{"command":"ACTIVATE"}` through the actual P10 ReceiveMessage/parser and P9 channel. This targeted test uses the parser boundary, not a physical hardware device or network connection.
- Both runs verified invisible portal renderers, disabled portal triggers, inactive stone frame, locked entry rejection, generous approach range, single accepted activation, finished lever pose/reveal, prompt/audio configuration, mute, persistent unlock, frame reveal and actual P11 loading of Ruins when a bound reference enters the exit zone. Token count survives; Ruins retains its ungated exit.
- **P12.5: 192 checks passed**, including controller-free activation/entry, tokens and portal regressions (Verify-P125.log).
- **P13: 129 checks passed**, full Jungle → Ruins → Crystal Cave → Hidden City → Journey Complete, duplicate protection and existing systems (Verify-P13.log).
- Missing-reference validation: eight scenes and all prefabs; 36 tokens/four portals retained, no retired relic references.
- Final structural pass also confirmed Jungle's continuous road and 10 tokens, bridge gap (252 no-floor rays/30 landing checks), P11 anchors/scene order and P14 build readiness (Verify-References.log). This did not build an executable.
- No C# compilation errors/warnings or runtime Console errors/warnings recorded by the monitored portal/P12.5/P13 tests. Unity licensing/network and shutdown diagnostics outside the runtime monitor are not gameplay test failures.
- All four actual realm scenes remained byte-for-byte unchanged during this verification; see VerificationSceneHashes.json. No other realm was authored, no Person-1 changes, no Git push.

## Exact Unity Play Mode steps

1. Stop Play Mode. Open/reload `Assets/Scenes/Person2/Jungle_Prototype.unity` from disk. Do not overwrite the saved scene with stale unsaved Editor content. Clear the Console.
2. Choose **Tools > Lost Worlds > Jungle > Play Portal Unlock Demo**. In Journey Demo, confirm the portal and stone frame are absent at the endpoint. **Enter portal / skip remaining tokens** must not advance while locked.
3. Click **Focus lever for E / hardware ACTIVATE (test)**. This binds a stationary test reference and frames the lever. Click Game view, then press **E**. Expect the lever to tilt, the portal/frame to appear gently, a quiet chime and “Portal ready — proceed.”
4. Press E again and wait. The portal must remain visible/unlocked; animation/audio must not restart.
5. Click **Enter portal / skip remaining tokens**. Expect Ruins to load once. The existing P11 **Advance From Exit (Play Mode Test)** is also valid after activation. Missing tokens never block entry.
6. Stop and restart using the same menu. Without any player, click **Activate portal lever (controller-free test)**, then **Enter portal / skip remaining tokens**. Expect the same unlock and Ruins transition.
7. For actual TCP input: restart the demo, click **Focus lever for E / hardware ACTIVATE (test)**, enable the existing **ProcessedHardwareAdapter** component on the persistent IntegrationSession in Play Mode, and connect a local TCP client to its configured address/port (default `127.0.0.1:55010`). Send UTF-8 `{"command":"ACTIVATE"}` followed by a newline. Expect the same reveal. Do not save this temporary component-enable change into the prefab. See the P10 guide for the sender example.
8. Run **Tools > Lost Worlds > Jungle > Run Portal Unlock Checks** for the automatic E/entry checks, or **Tools > Lost Worlds > P13 > Run Final QA** for the journey regression. Confirm a passing result and no new runtime Console warnings/errors.

Physical player trigger/collider behavior at actual auto-run speed, real hardware/network deployment and subjective sound/visual comfort still need manual verification with Person 1's controller. No player movement was implemented here.
