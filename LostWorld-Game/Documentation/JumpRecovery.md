# Jump recovery and forward controls

Keyboard is the default gameplay mode. Hold **W** to run at the existing 3.1 m/s maximum; release it to stop forward travel immediately. The original acceleration remains unchanged. A/D or arrows, Space and E retain their existing meanings. Releasing W in the air stops horizontal travel, not gravity. GRAB remains supported by the interaction framework; no removed collectible gameplay was restored.

On the persistent **IntegrationSession** component, select **Input Mode**:

- **Keyboard:** W-held forward movement.
- **Simulator:** original auto-run plus P9 test keys.
- **Hardware:** original auto-run; keyboard movement/interaction commands are ignored. P10 commands and processed effort input remain available.

Integration code can call `IntegrationSession.SetInputMode(GameplayInputMode.Hardware)` (or Keyboard/Simulator). Mode persists across realm loads and Replay. The existing command format is unchanged; no raw biosignals are processed.

## Recovery

Only the bound physical runner is monitored. Authored gap/bridge fall volumes and realm-specific world bounds trigger “Let's try that path again!”, block commands, wait 1.5 seconds, then use the existing P11 loader to reload the current realm. Decorative water is not automatically hazardous.

The persistent token ledger retains already-earned IDs, including tokens earned in the retried realm. Reloaded collected tokens remain hidden and cannot reward twice. Local gates and route encounters reset with the scene. Replay still clears the entire token session. Keyboard recovery requires releasing W and pressing it again; Simulator/Hardware resume their original auto-run behavior. Pending TCP frames, including partially received frames, are invalidated at both recovery boundaries. An external device must also discard its own delayed command backlog; Unity cannot identify old commands newly sent by a device without protocol timestamps.

## Authored challenges

All new gaps are 1.2 m long, full road width, with nonblocking approach inlays 6 m and 4 m before takeoff, wide landing edges and one gentle success message. Coordinates are world Z:

| Realm | Gap intervals | Road width |
|---|---|---|
| Jungle | Existing 71.4–72.6 unchanged | Existing bridge |
| Ruins | 33.4–34.6, 56.4–57.6 | 9 m |
| Crystal Cave | 28.4–29.6, 64.4–65.6 | 9 m |
| Hidden City | 65.4–66.6 | 10 m |

Hidden City has one new gap: a second could not retain the chosen clearance from existing obstacles, tokens and route-choice area. Nothing was moved to force another challenge.

Controlled Play Mode measurements at 30/60 fps: approximately 0.70 s airborne, 2.17 m forward travel, and 1.23/1.29 m peak rise. Jump accepts only grounded input, with no double-jump or jump queue while airborne; LEFT/RIGHT retain their existing 0.18 s repeat guard. Physics, animation and obstacle dimensions were not retuned.

Tests inject provisional 0.1/0.3 s command delays from a controlled approach and check physical landing in all three positions. These are software test assumptions, **not measured EEG/EMG latency or clinical validation**. W allows a keyboard user to stop and prepare; hardware auto-run still needs device/patient-specific timing assessment before supervised use. A cue is advance notice, not an instruction to jump immediately on the first stripe.

## Verification menus

- `Tools > Lost Worlds > Jump Recovery > Validate Geometry`
- `Tools > Lost Worlds > Jump Recovery > Run Focused Play Mode Checks`
- Existing R5 full-journey regression explicitly selects Simulator mode; the automated W journey entry point is `Realmwalker.Authoring.RealmwalkerR5Validation.WJourneyBatch`.

Manually start from MainMenu, check W release/resume, stop before a gap, and hold W + press Space to cross. Deliberately miss a gap, confirm the friendly retry and preserved count, then release/repress W. Test each gateway with E nearby; finish at the Hidden City trophy and check Replay. Select Hardware mode on IntegrationSession and verify connected-device commands, actual latency and comfort separately. No Windows executable was rebuilt.

## Completed validation

Unity 6000.6.1f1 Play Mode, using the existing isolated validation project because the actual project was open:

- 171 combined keyboard, hardware-mode, gap, fall-recovery, persistence and stale-input checks passed.
- 29 existing R2 checks passed, plus all six R3 animation states, upright pose and follow camera.
- 214 MainMenu/full-journey checks passed with W held through the four realms, including physical traversal, gate activation, route callbacks without confirmation popups, 36-token summary, trophy, Finish and Replay. Replay without W remained stationary.
- 56 P8 effort/HUD checks passed, including processed effort input and the 55 cap.
- 61 P10 checks passed through actual localhost TCP, including all five commands, GRAB/ACTIVATE framework behavior, route selection, invalid messages, reconnect and keyboard fallback.
- 1,356 empty-gap rays, 3,410 route-clearance samples, 36 unique supported tokens, unchanged Jungle bridge checks, and P11/P12/P12.5/P14 structural/dependency validation passed.
- Final compilation passed. Runtime warning/error monitors passed. Rendered gap images were visually inspected; physical EEG/EMG hardware and patient comfort were not tested.

Validated scene/mesh files were copied to the actual project; their hashes and all changed source hashes matched the tested copy. Earlier test failures were an editor-test type mismatch and screenshot-stall timing; both were corrected and the affected suites passed afterward. No jump-physics defect or physics retuning was needed.

## Files changed in this task

- Runtime: `Assets/Realmwalker/Runtime/RealmwalkerRunner.cs`, new `RealmFallSafety.cs`, new `RealmJumpChallenge.cs`.
- Integration/input/UI: `Assets/Scripts/Integration/IntegrationSession.cs`; `Assets/Scripts/Hardware/HardwareCommandSimulator.cs`, `ProcessedHardwareAdapter.cs`, `TcpCommandTransport.cs`; `Assets/Scripts/UI/Person2Hud.cs` (exposes existing feedback method).
- Scenes: `Assets/Scenes/MainMenu.unity` (W instruction only), and the four `Assets/Scenes/Person2/*_Prototype.unity` scenes. Jungle adds safety only; its geometry is unchanged.
- Generated geometry: `Assets/Realmwalker/JumpChallenges/` and Unity metadata.
- Editor/validation: new `Assets/Realmwalker/Editor/FallRecoveryValidation.cs` and `RealmJumpAuthoring.cs`; updated `RealmwalkerR2.cs`, `RealmwalkerR5Validation.cs`; `Assets/Editor/RunningPathPolish.cs`, `RuinsUpgrade.cs`, `CrystalCaveUpgrade.cs`, `MainMenuBuilder.cs`.
- Evidence: new logs and gap renders in `Assets/Realmwalker/Validation/`; this document.

Pre-existing route-confirmation changes in P7/P11 validation and `PlayerCommandBridge.cs` were retained, not reimplemented. No commits, pushes, Windows builds or existing build-directory modifications were performed.
