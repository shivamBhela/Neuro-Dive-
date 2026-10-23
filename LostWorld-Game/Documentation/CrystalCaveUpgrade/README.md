# Crystal Cave mineral upgrade

CrystalCave_Prototype alone receives layered blue-grey rock, slate paving, cyan/blue/purple minerals, roadside stalagmites and overhead stalactites. Existing high cave ribs and open-roof cutaway remain so the overview stays readable. Soft cool illumination and faint drifting particles provide depth without flashes. The broad original road, eight Journey Tokens and jump obstacles remain.

The editor builder reuses existing Jungle PBR textures/organic rock mesh and Ruins paving texture, with cave-local materials and UV-adjusted render meshes. A small shared hexagonal mineral mesh supplies new crystal formations. Emissive materials retain URP's required emissive flag on reimport; this does not enable or perform a lighting bake. No downloads, custom shaders, extra dynamic lights, extra particle emitters or runtime scripts are introduced. New decoration is off the road or safely overhead and adds no colliders. Existing gameplay colliders remain unchanged.

## Crystal activation pedestal

At **(5, 0, 80)**, the pedestal reuses the authored Ruins mechanism and all Jungle runtime hooks. Its focus begins softly blue, then illuminates using the existing interaction success-material setting. The focus tilts gently, the original portal reveals and the existing quiet chime plays. P12 mute/reduced motion remain supported.

The complete portal and CityLightPortal frame are inactive at startup. Accepted E or processed ACTIVATE unlocks once; the existing P11 gate rejects premature exit and remains the only scene loader. The generous zone requests auto-run pause, waits without a timeout or penalty, and releases the request after the reveal. Journey Tokens never gate activation or progression.

Person 1 must connect IAutoRunPauseReceiver through IntegrationSession.BindPlayer, as documented in ../PortalPause/README.md. Without a receiver, this is a pause request only: it does not physically brake, move or resume the player. Actual stop distance, reference height, jump/collider fit and execution order remain integration checks.

## Manual Unity steps

1. Stop Play and reload **CrystalCave_Prototype** from disk. Choose **Tools > Lost Worlds > Crystal Cave > Play Mineral Demo**.
2. Inspect the cool rock/crystal palette, clear road, eight tokens and four authored jump obstacles. Check camera readability at 720p/1080p and target-PC frame time.
3. Click the shared **Focus lever for E / hardware ACTIVATE (test)** button; it focuses the crystal pedestal and binds a stationary test reference. Confirm pause requested, hidden portal/frame, and rejected **Enter portal / skip remaining tokens** while locked.
4. Wait as long as desired. Focus Game view and press **E**, or use **Activate portal lever (controller-free test)**. Confirm the brighter crystal, gentle reveal, updated prompt and cleared pause request. Repeat E: no second success or re-pause.
5. Select **Enter portal / skip remaining tokens**. Confirm **HiddenCity_Prototype** loads once and the Journey Token count persists. Missing tokens have no penalty.
6. For TCP testing, restart, focus the pedestal test reference and enable the existing ProcessedHardwareAdapter in Play Mode. Send UTF-8 `{"command":"ACTIVATE"}` plus newline to its configured endpoint (default 127.0.0.1:55010). Do not save temporary adapter enablement to the prefab. Automated verification uses the actual P10 parser/channel; no physical hardware is claimed.
7. Run **Tools > Lost Worlds > Crystal Cave > Run Portal and Pause Checks** for automated Play Mode checks, and **Tools > Lost Worlds > Crystal Cave > Validate Mineral Realm** outside Play. Inspect the Console.

The scene is already generated/saved. **Apply Mineral Upgrade** under the same menu preserves an already-generated upgrade. Other realm scene files and all runtime scripts are unchanged. No relics or GRAB gameplay added; no Git push or Windows demo rebuild.
