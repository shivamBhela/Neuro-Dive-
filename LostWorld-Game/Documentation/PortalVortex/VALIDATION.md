# Portal vortex validation — 2026-10-16

- Unity 6000.6.1f1 / URP compilation: passed, no C# warnings/errors or shader errors in final runs.
- Geometry: existing gateway inner bounds match the energy plane within 0.005 m. Jungle 6.20 × 5.65 m; Ruins and Crystal Cave 12.00 × 7.20 m.
- Exactly one enabled portal renderer per upgraded realm, one existing PortalAtmosphere, four vertices/two triangles. No added collider or light.
- Serialized gameplay components, colliders and their transforms were compared before/after applying the visual override. Original gateway component state also compared unchanged.
- Destination colors checked against the three material palettes. Two rendered times (0 and 8 seconds) showed animation in all three surfaces; approach previews visually inspected.
- P12.5: **191 checks passed**, including token persistence/duplicates, Reduced Motion, portal cache/null recovery, all three transitions and optional-token completion.
- P13: **133 checks passed**, including the complete journey with 36 tokens, interactions, command callbacks, effort/rest and one completion. No monitored runtime warnings/errors.
- Existing Hidden City finale: **38 checks passed**, including pause ownership, completion, token tally, Replay and Finish. Its scene hash is unchanged in both actual project and test copy; no portal or console was restored/created.
- P14 dependency/reference/scene-order validation passed. No executable built.

Tests ran in the existing isolated validation copy. The actual three scenes and all supporting new assets were then copied back and compared byte-for-byte (16 files including metadata; AppliedFiles.txt). Hidden City was not copied. The complete process did not alter any existing runtime script, shared portal prefab, stone frame, destination mapping, trigger or player API.

No physical runner/hardware or target-device frame-rate test is claimed. Reload the saved scenes in the open Editor, exercise the existing ACTIVATE/portal controls and inspect Reduced Motion and approach visibility. See README.md for menus and asset list. No Git push.
