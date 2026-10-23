# Ruins sandstone realm

Ruins alone receives a warm sandstone visual pass: golden cracked paving, normal-mapped stone, ochre architectural trim, four roadside temple/arch groups, collapsed masonry, dry grass and sparse sage shrubs. Dense prototype canopy trees and ferns are removed together with their colliders. Existing boulders, broad road, waterfall, pillars and monuments remain, with sandstone materials. Location-only marker renderers are hidden; their integration anchors remain.

Uses existing Jungle stone normal/albedo maps and canopy mesh, plus a small generated paving texture, standard URP Lit materials and a scene-local procedural sky. No downloads, new shaders, new runtime systems or extra particle emitters. Existing slow motes are tinted as faint warm dust; soft sun and long-distance fog preserve readability. Added decoration has no colliders and lies outside the road. Existing road, jump obstacle and token geometry is retained.

## Pedestal and pause

The stone pedestal at **(5, 0, 80)** reuses Jungle's PortalUnlockMechanism, PortalUnlockKeyboardInput and PortalAutoRunPause, cloned with their serialized references rebound to Ruins. Its stone press plate tilts gently on activation; the original quiet chime is reused. Entire existing portal and CaveThreshold frame are disabled at startup. Accepted E or processed ACTIVATE reveals both once. Existing P11 exit gating blocks all premature exits; no new loader exists.

The nonblocking 12m × 4m × 20m activation volume requests pause on approach (road begins entering it at Z=70), waits without a deadline and releases after reveal. Person 1 must implement IAutoRunPauseReceiver and bind the player with IntegrationSession.BindPlayer. These hooks do not move, brake or resume an unconnected controller. Verify player height, execution order, stopping distance and jump collision with the actual controller; see ../PortalPause/README.md.

Eight Journey Tokens remain optional and automatically collected. Relics and GRAB gameplay are not added. P9/P10 and all runtime integration APIs are unchanged.

## Unity verification

1. Stop Play, reload **Ruins_Prototype** from disk, then run **Tools > Lost Worlds > Ruins > Play Sandstone Demo**. Inspect Scene/Game views: warm sandstone, sparse dry planting, clear road and existing obstacles/tokens.
2. In Journey Demo use the existing **Focus lever for E / hardware ACTIVATE (test)** button (shared label; it focuses the Ruins pedestal). Confirm pause requested and the portal/frame absent. **Enter portal / skip remaining tokens** must be rejected before activation.
3. Wait as long as desired; focus Game and press **E**, or click **Activate portal lever (controller-free test)**. The pedestal tilts, portal/frame reveal, prompt changes, and the pause request clears after the gentle reveal. Repeated activation must do nothing.
4. Choose **Enter portal / skip remaining tokens** and confirm **CrystalCave_Prototype** loads exactly once with the token count retained. Missing tokens never block progression.
5. Restart to test hardware: focus the pedestal reference, enable the existing ProcessedHardwareAdapter in Play Mode, then send UTF-8 `{"command":"ACTIVATE"}` plus a newline to its configured TCP address/port (default 127.0.0.1:55010). Do not save temporary adapter settings into its prefab. No physical hardware was connected during automated tests.
6. Run **Tools > Lost Worlds > Ruins > Run Portal and Pause Checks**, and **Tools > Lost Worlds > Ruins > Validate Sandstone Realm** outside Play. Check the Console. Verify text readability at 720p/1080p and performance on the demo PC.

Editor authoring menu **Tools > Lost Worlds > Ruins > Apply Sandstone Upgrade** preserves the existing upgrade when rerun. The scene is already generated/saved; no setup is required after reloading.

No other realm scenes, Person-1 controller or hardware runtime code changed. No Git push or Windows demo rebuild.
