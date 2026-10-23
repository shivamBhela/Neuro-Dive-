# Lost Worlds — Main Menu

Open `Assets/Scenes/MainMenu.unity` and press Play. START JOURNEY enters Jungle with a new session, zero Journey Tokens and the beginner effort cap of 55. No runner or gameplay session exists on the menu. Repeated START clicks are blocked. The four existing realms, Replay and Finish retain their existing behavior.

HOW TO PLAY opens instructions; BACK returns and restores keyboard selection. Mouse and keyboard UI navigation use the existing Input System. EXIT GAME stops Play Mode in the Editor and calls Application.Quit in standalone.

The enabled build order starts MainMenu, Jungle, Ruins, Crystal Cave, Hidden City. The P14 build command now includes MainMenu and its launch script no longer automatically enables the controller-free test overlay. No executable was built. Existing optional test menus remain available.

Automation: Tools > Lost Worlds > Main Menu > Validate Menu and Journey. Authoring: Create or Open (opens an existing menu without overwriting it). Runtime: MainMenu.cs; authoring: MainMenuBuilder.cs. Menu-only background materials are under Assets/Art/Materials/MainMenu.

Manual checks: inspect 1280×720, 1600×900, 1920×1080 and 4:3; click all buttons; test Tab/arrows/Enter; confirm instructions fit; confirm EXIT stops Editor Play Mode; complete a journey and test Replay/Finish. Actual Windows process exit and executable startup remain unverified until a build is authorized.

Validation: 206 automated Play Mode checks passed, including menu navigation, duplicate START protection, fresh zero-token/55-cap session, full journey, completion, Finish and Replay. No runtime warnings/errors. A 1600×900 rendered menu was visually inspected. Actual mouse/keyboard UI navigation, Editor EXIT and other aspect ratios remain manual checks. Tested scene/source files match the actual project.

Cinematic polish: MainMenuPolish adds 18 lightweight golden UI motes, a slow title-outline pulse, 0.22-second button color transitions with at most 2.5% scale, and a 0.6-second pre-load fade. Reduced Motion (Inspector) disables mote movement, title pulsing and scale feedback. Optional Ambience accepts a suitable loop at volume 0.06; no ambience is assigned because the existing sound is a portal one-shot. No new audio dependency. Manual review: hover and keyboard selection, HOW TO PLAY/BACK, fade pacing and Reduced Motion. Runtime layout captures cover 1280×720, 1600×900 and 1920×1080; continuous motion and any future assigned audio still need human review.

Polish validation: 209 Play Mode checks passed plus text-fit/render checks at all three requested resolutions. Fade starts before gameplay, selection scaling is restrained, menu navigation/fresh session/full journey/Replay/Finish passed; no runtime warnings/errors recorded. Actual scene and source hashes match the tested copy.
