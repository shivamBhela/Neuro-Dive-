# R2 — Jungle runner

Open **Tools > Lost Worlds > R2 > Play Jungle Runner**. Focus Game view.
A / Left Arrow and D / Right Arrow change one position; Space jumps; E activates the endpoint lever while stopped. Existing P10 processed commands use the same P9 channel. No new input listener or hardware parser.

Defaults: +Z forward 2.8 m/s, acceleration 10 m/s², lane centers -2/0/+2 m, lateral speed 7 m/s, directional debounce 0.18 s, jump height 1.35 m, gravity 22 m/s². CharacterController: radius 0.32 m, height 1.85 m, step offset 0.18 m. Existing geometry, Humanoid, skins and materials are preserved.

The P11 pause receiver stops motion while waiting at the lever, during transition and completion. ACTIVATE reveals the gate and releases the pause; jump the final obstacle or use a side position. The runner exists only in Jungle; Ruins remains controller-free. A missed bridge jump stops the fallen runner below y=-6; restart Play Mode to retry. No checkpoint or recovery system was added.

Animator parameters prepared for R3: ForwardSpeed, LateralSpeed, VerticalSpeed, Grounded, Paused. No animation clips or follow camera are provided; the existing overview camera remains.

Focused automated Unity Play Mode checks cover lanes/input limits, P10 parser commands, gravity, jumping/landing, the physical bridge gap, fall stopping, lever pause/ACTIVATE and portal transition. This is not a human full-course playthrough or a real hardware test. Run **Tools > Lost Worlds > R2 > Run Focused Movement Checks** only when needed. **Setup Jungle Runner** is an idempotent repair/setup tool, not required for the saved scene.

Manual checks: steer both directions, jump logs and bridge gap, release controls at the lever to confirm indefinite pause, press E, clear the final obstacle and enter Ruins. Check Console. Character remains in its R1 pose until R3 supplies clips.

R2 posture correction: the empty Humanoid Animator state shifted the hips downward and bent the knees. Animator evaluation is disabled on the prefab to retain the authored upright R1 rest pose; Avatar, controller, bones and skinning remain intact. R3 must supply a verified animation state before re-enabling Animator. No rest-pose-forcing script or animation clip was added. At the slower pace, jump near the bridge's takeoff edge (approximately z=71.1), rather than at the old earlier launch point.

