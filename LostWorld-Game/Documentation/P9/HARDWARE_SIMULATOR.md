> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P9 — Hardware Command Simulator

## Play and controls

**Tools > Lost Worlds > P9 > Play Hardware Simulator Demo** opens the dedicated P9_HardwareSimulatorDemo scene and starts Play. Click Game view to give it keyboard focus.

| Key | Clean command |
| --- | --- |
| A / Left Arrow | LEFT |
| D / Right Arrow | RIGHT |
| Space | JUMP (event only; no movement) |
| G / R | GRAB |
| E | ACTIVATE |

F1 selects the environmental interaction test station; F3 selects the reusable route-choice station. F2 is retired. Page Up/Page Down/Backspace retain the existing P8 effort test controls.

Commands publish on a fresh press, not every held frame. Opposing LEFT/RIGHT input is ignored while both sides are held. Aliases pressed together publish a single command. Distinct commands pressed together are sent in direction, JUMP, GRAB, ACTIVATE order. Repeated fresh presses are separate commands; gameplay acceptance/duplicate protection remains in existing systems.

The latest-command display and Console report SENT, not successful gameplay. Out-of-range GRAB, already-used interactions and an already-selected route can reject the command while SENT still updates. P7's existing success/choice feedback shows acceptance separately. JUMP has no gameplay receiver and changes no player transform.

Stop and reopen with the Play menu for a fresh session. Fast Play Mode project settings remain unchanged. Original P4–P8 demos and controls are untouched; their keyboard components are disabled only in the separate P9 scene to prevent duplicate delivery.

## Architecture

Keyboard simulator → CommandChannel → independent receivers → existing P4/P6 systems

- CommandChannel.cs defines HardwareCommand (LEFT, RIGHT, JUMP, GRAB, ACTIVATE), ICommandSource (CommandSent event), and ICommandSink (Send). A scene-owned channel avoids global cross-scene subscriptions.
- HardwareCommandSimulator only reads temporary keyboard controls and publishes commands. It has no gameplay references.
- InteractionCommandReceiver calls the bound P4 InteractionInteractor.TryGrab/TryActivate.
- RouteCommandReceiver calls the bound P6 RouteChoiceZone.TryChoose with its existing reference. The core owns range/zone and one-choice guards.
- CommandDebugFeedback subscribes for last-command text/Console output.
- P9SimulatorDemo is test-only station/camera selection and binding. It is not a final GameInputManager, player controller or scene progression system.

All scripts are under Assets/Scripts/Hardware in a new Person 2 Hardware assembly. Assets/Prefabs/Hardware/P9/HardwareCommandSimulator.prefab supplies the channel, simulator and optional receiver components. Bind receiver references in the Inspector or via Bind. Disable receivers while rebinding/integrating if they should not receive commands. They subscribe on enable and unsubscribe on disable/rebind.

## Future processed hardware boundary

A future P10 producer can replace/disable HardwareCommandSimulator and call the same ICommandSink.Send(HardwareCommand) on Unity's main thread. Gameplay receivers need not change. Future subscribers can listen through ICommandSource.CommandSent; JUMP is exposed for Person 1 to consume later. Send returns whether publication occurred, not whether gameplay accepted it.

The channel rejects undefined enum values, disabled/inactive delivery, calls outside Play and nested publication during callbacks. Callback exceptions are isolated so one listener cannot prevent others from receiving the event. Runtime command history resets each Play session; no persistence, networking, device adapter or raw parsing/processing is implemented.

Unity receives clean processed commands only. Raw EEG, EMG, hand and exoskeleton/biosignal processing stays outside Unity. P9 does not implement P10, a medical interpretation, or an effort estimator.

## Editor and manual verification

- **Tools > Lost Worlds > P9 > Setup Hardware Simulator Demo** creates missing prefab/demo assets and preserves existing generated ones.
- **Tools > Lost Worlds > P9 > Run Simulator Checks** runs the P9 Play Mode checks.
- Confirm all five commands appear in the latest-command area. Hold keys to check they do not repeat.
- F1: test GRAB/ACTIVATE and their P7 success feedback. F3: select one route; repeated/opposite commands must not override it.
- Space should report JUMP without moving anything. During P8 Rest Needed, commands remain available.
- Check text at 1280x720 and 1920x1080 and check the Console for errors. Restart via the menu to reset one-shot stations.

All four environment scenes, including their P8.5 jump obstacles, remain separate and unchanged.

Unity 6000.6.1f1 generated/saved the demo and passed 54 P9 checks. P4/P5/P6/P7/P8 regression suites passed 22/31/34/66/57 checks; P8.5 passed 400 geometry checks. All 442 pre-existing Unity files are unchanged. No generation step remains.
