> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P12 — Calm realm polish

P12 adds presentation only. All P4–P11 source code, Person 1 integration APIs, P9/P10 architecture, P11 entrance/exit anchors and existing obstacle geometry remain unchanged. The four realm scenes remain separate.

## Realm changes

| Realm | Presentation |
| --- | --- |
| Jungle | Warm, lighter ochre trail against muted green foliage; darker log/rock silhouettes; soft green atmosphere and sparse leaf-coloured motes off the route. |
| Ruins | Warm limestone path against cooler rubble; cool water-coloured fog; sparse, slow mist at the existing waterfall and distant motes. Water remains prototype geometry. |
| Crystal Cave | Brighter blue-grey path, darker blue obstacle silhouettes, readable cool ambient fill; existing crystal lights kept steady with capped intensity and no local shadows. |
| Hidden City | Warm ivory/gold presentation, clear promenade and darker debris; restrained gold motes and an open ceremonial destination arch. It remains traversable and uses the same P11 completion point. |

New path/obstacle materials are scene overrides. Original shared materials and world prefabs are preserved. Thin static rings identify existing functional interactions/relics; rings and destination decorations have no colliders. Existing positive HUD messages and successful-object visuals are retained.

## Effects and feedback

`RealmPolish` observes the scene's existing `InteractableBase.OnSucceeded` events. GRAB, ACTIVATE and P5 relic collection emit five softly fading motes at the accepted target, with no camera shake, flashing light, repeated pulse or gameplay effect. Rejected/duplicate actions do not emit feedback. Event subscriptions are removed on disable/unload.

Each realm has two ambient systems capped at 24 particles each and one success system capped at 16: **64 maximum live particles per loaded realm**. Ambient emission is 1.5 particles/second per system, with slow movement and smooth opacity envelopes. Particles use one small, procedurally generated soft-disc texture and a shared transparent URP particle material. They do not collide, cast shadows or use additional lights, trails or post-processing. The original lighting count is unchanged.

`RealmPolish.ReducedMotion` stops/clears both ambient and success particles without affecting interactions. `Muted` controls ambience and success audio. Both controls are also Inspector fields on each realm's `P12_Polish` object; values may be adjusted before or during Play. These are realm-local settings; this phase does not introduce a persistent settings menu.

## Audio placeholders

No audio clips were available. Four RealmPolishProfile assets under `Assets/Audio/P12` contain optional ambience and success-sound slots, initially empty. **The delivered realms are silent.** No copyrighted audio was imported or generated.

Assign your own licensed, gently looped realm ambience and optional short success cue before Play. Ambience fades in at 0.06 gain units/second toward its configured level; default gain is 0.10 and the profile caps it at 0.18. Success defaults to 0.07, capped at 0.12, with a 0.5-second retrigger interval. Mute stops audible output immediately. Sources are scene-owned and stop on unload; full crossfading between different clips is not implemented.

These are conservative relative mix levels, not a guarantee of physical playback loudness. Audition actual recordings on the intended device at a comfortable system volume. Use an appropriate AudioListener on the eventual Person 1 camera. The P12 Editor preview supplies a temporary listener when none exists. No audio asset is required for successful gameplay or validation.

## Unity menus and manual review

- **Tools > Lost Worlds > P12 > Validate Polish** checks all realm setups, particle budgets, collision-free decorations, audio defaults, fog visibility and retained P11 bindings.
- **Tools > Lost Worlds > P12 > Play Polish Preview** reopens the currently selected realm (or Jungle if another scene is open), then enters Play. It temporarily selects the original overview camera in each visited realm; saved P4 test camera settings remain intact.
- **Tools > Lost Worlds > P12 > Setup Polish** adds missing P12 roots/assets only. Generation is complete; re-running it preserves existing P12 authoring edits.
- Use P11's **Advance From Exit (Play Mode Test)** to visit all realms through the actual scene-flow path without a player. Existing P11 reference binding and P9 controls remain available for inspecting interaction/relic feedback.
- Use the original P4–P10 demo menus for their existing focused tests. Those dedicated test scenes have not been restyled.

Inspect the realms from both overview and a near-ground Scene view. Confirm path/obstacle contrast, cave readability, gentle waterfall mist, clear collectible markers and the City destination. Test GRAB/ACTIVATE/collection success and repeated-input rejection; confirm the HUD and 3/3 state across P11 transitions. Toggle Reduced Motion and Muted. Check the normal URP view at 720p and 1080p and measure frame rate on the intended hardware. Actual Person 1 jump/collider and camera fit still require his prefab.

No P13 functionality is included.
