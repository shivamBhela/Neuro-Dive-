> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P12.5 completion and validation

Completed 2026-09-23 using Unity 6000.6.1f1.

## Portal runtime error follow-up

The reported old Update line 27 dereferenced the nonserialized `MaterialPropertyBlock properties`, originally allocated only in Awake. A Play-mode script reload can discard this field without rerunning Awake. The prior lazy-initialization fix was confirmed present on disk; the old line number no longer identifies Update in the fixed source. This follow-up retains lazy recovery on enable/update, allocates the cache only when a surface exists, and returns safely when both optional visuals are absent.

All four actual realm scenes and their shared portal prefab matched the validation copy. Unity resolved both authored visual references in every portal, including Hidden City; no missing saved reference required scene/prefab edits. Expanded P12.5 checks deliberately clear the cache, remove the renderer, remove both visuals, restore a surface without particles, and re-enable the component. Each realm passed 180 repeated missing-reference updates with **zero captured warnings/errors**, plus cache recovery checks. Full P12.5 regression passed **212 checks**, Unity exit 0; no C# warnings/errors, shader errors or runtime exceptions appeared in the successful log. Transitions, triggers, tokens and Person 1 code were not modified. P13 was not continued.

Manual verification: stop Play, allow compilation/import to complete (Assets > Refresh if needed), clear historical Console entries, then run the P12.5 Play Journey Enhancement Demo. Inspect all four exits, especially Hidden City, and confirm no new PortalAtmosphere errors. If Unity is configured to defer compilation during Play, the running old assembly cannot use the fix until Play stops and compilation finishes. Existing portal effects and progression should remain unchanged.

## Portal visual refinement

The subsequent visual-only update passed **192 P12.5 checks** (Unity exit 0), including the original transition/token checks plus shared visual presence in all four realms, no visual colliders, a 12-particle budget, Reduced Motion clearing, and resumption. No C# warnings/errors, shader errors or exceptions appeared in the final run. Rendered Portal0.png through Portal3.png are saved alongside this report; Jungle and Hidden City close-ups were reviewed for transparency/path readability.

Only the shared RealmExitPortal prefab gains visual children; all four realm scene files, original trigger, RealmExitPortal transition code, P11 APIs and Journey Token code remain untouched by the refinement. New assets are PortalAtmosphere.cs, QuietPortalEnergy.shader, PortalEnergy.mat, PortalEnergySurface.asset and P125PortalVisualBuilder.cs, with metadata. Existing P125JourneyValidation.cs adds visual checks and captures. No dynamic lights, post-processing dependency, solid colliders, audio or additional progression system.

Manual visual check: run **Tools > Lost Worlds > P12.5 > Play Journey Enhancement Demo**, inspect each exit in Scene/Game view during Play for the translucent surface and slow pulse/motes, and advance through all four realms with the existing demo button. Toggle the realm's P12 Reduced Motion setting to confirm a steady surface and no motes. Check paths remain visible and token/portal behavior is unchanged. The shared prefab update is already applied; **Apply Portal Visual Refinement** is an idempotent authoring utility, not a required generation step.

## Resume audit

The actual project already contained six Journey source/assembly files, three P125 Editor tools, their locally imported metadata, and JOURNEY_ENHANCEMENTS.md. All source matched the validation copy, including the dark token HUD panel. No P125_Journey roots or generated P125 prefabs/materials were yet present in the actual project.

Transferred the four validated realm scenes, two reusable prefabs, three materials, arch mesh and six rendered previews. Transferred the new P125 metadata as well: GUIDs from the authoring copy are required by the saved scenes/prefabs. No earlier asset GUIDs were changed. Working source was not reimplemented.

All original serialized scene records remain. In each realm only one original record changed: its root Transform gained a child reference. All other additions are under P125_Journey. Existing scripts, prefabs, configuration and packages were preserved. After transfer, every actual Assets/Packages/ProjectSettings file matched its counterpart in the tested copy.

## Results

| Suite | Passed checks | Evidence |
| --- | ---: | --- |
| P12.5 | 136 | Fresh final compile/Play Mode run, exit 0 |
| P11 | 78 | Fresh resumed regression, exit 0 |
| P12 | 71 | Fresh resumed regression, exit 0 |
| P4 / P5 / P6 / P7 | 22 / 31 / 34 / 66 | Existing completed P12.5 regression runs retained |
| P8 / P9 / P10 | 57 / 54 / 65 | Existing completed P12.5 regression runs retained |
| P8.5 | 400 collision/bypass samples | Included in integration validation |

No C# compiler warnings/errors or test exceptions were found in the final P12.5, resumed P11/P12, or retained P4-P10 regression logs. Tests ran in the isolated Unity validation copy; the actual project received matching tested assets rather than a competing batch Editor opening its working checkout. Batch entry waits for Unity startup scene-reference resolution. Rendering uses the existing temporary capture workaround and does not change project URP settings.

Validated 36 unique authored tokens (10/8/8/10), nonblocking portals, automatic bound-reference and collider-contact collection, swept pass-through, range rejection, malformed input rejection, duplicate/reloaded IDs, one event per collection, persistent count across all realms, independent relic count, and duplicate transition/completion protection. The journey test deliberately collected 35 and skipped one: completion still succeeded with zero relics, proving neither token total nor relic total gates the existing completion flow.

Reviewed final TokenCloseup.png (1280x720) and Completion1080.png (1920x1080): warm ivory token text is readable against its dark panel in both Jungle and bright Hidden City, without overlapping the effort display. These are rendered checks, not a substitute for actual player-camera comfort testing.

## Manual verification

1. Let the actual project import, then run **Tools > Lost Worlds > P12.5 > Validate Journey Enhancements**.
2. Run **Tools > Lost Worlds > P12.5 > Play Journey Enhancement Demo**. Use its Editor window: **Collect next token (test)**, **Collect remaining realm tokens (test)**, and **Enter portal / skip remaining tokens**.
3. Confirm tokens disappear, the counter increases, and gentle feedback respects P12 Reduced Motion. Traverse all four realms; count persists and Hidden City completes even with tokens skipped.
4. Stop and reopen the demo: the session starts at zero. Check Game view at 1280x720 and 1920x1080.
5. For automatic pickup, bind an empty Transform using P11's **Bind Selected Reference + Enable Simulator (Play Mode Test)**. Move it through token/exit zones in Scene view. Rebind after each realm arrival. No player movement code is provided. P11 **Advance From Exit (Play Mode Test)** remains available.

Person 1 must supply and position their player/camera and call the existing BindPlayer API on realm arrival. Test actual collider dimensions, movement speed and camera framing then. No name/tag dependency, hardware architecture change, P13/P14 work or Git push was introduced.
