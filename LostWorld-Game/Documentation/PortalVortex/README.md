# Realm gateway vortex visuals

Visual-only upgrade, 2026-10-16. No gameplay, trigger, frame, input, transition or finale script changed.

| Scene | Destination colors | Gateway opening |
| --- | --- | --- |
| Jungle | Ruins: golden / amber | 6.20 × 5.65 m |
| Ruins | Crystal Cave: ice blue / violet | 12.00 × 7.20 m |
| Crystal Cave | Hidden City: royal purple / electric blue | 12.00 × 7.20 m |
| Hidden City | No change; no portal restored | Existing finale preserved |

## Implementation

The existing PortalAtmosphere component still supplies _MotionTime and respects Reduced Motion. One new URP unlit shader draws layered rotating spiral energy and a bright core on a shared four-vertex, two-triangle plane. Three materials set destination colors. The existing TranslucentEnergy renderer is fitted to each original gateway's inner pier bounds and lintel underside. The rectangular energy field extends to every edge; original stonework occludes the corners naturally. Old portal arch/base/mote renderers are disabled in those three scene instances only. Their prefab and Hidden City remain unchanged.

No new runtime objects, scripts, lights, textures, particles, packages, physics or rendering features. Existing optional motes retain their serialized component/reference for compatibility, with their renderer disabled. No bloom dependency, screen-texture sampling or post-processing changes. Transparent fill cost is limited to one surface per visible portal.

Assets added: Assets/Art/Materials/RealmVortex/RealmVortex.shader, GatewayPlane.asset, RuinsAmber.mat, CaveIceViolet.mat, CityRoyalBlue.mat, associated metadata; Assets/Editor/RealmVortexVisuals.cs and metadata. Scene overrides: Jungle_Prototype, Ruins_Prototype, CrystalCave_Prototype only.

## Unity checks

1. Stop Play Mode and reload the saved realm scene from disk to see the applied override. No generator run is required.
2. Use the existing controller-free journey demo. Focus the realm's activation mechanism, then press E (or use the existing ACTIVATE test). The original locked/unlocked behavior is unchanged.
3. Inspect the whole gateway from the main approach: correct colors, full opening coverage, slow continuous spiral motion, no old outer ring or floating portal decorations. Try Reduced Motion; the surface should become still.
4. Use the existing portal-entry test: Jungle → Ruins → Crystal Cave → Hidden City. Confirm each destination remains unchanged. Verify Hidden City's existing trophy finale and replay/finish behavior; no portal has been restored there.
5. Run **Tools > Lost Worlds > Portal Visuals > Validate Gateway Vortexes**. Existing **P12.5 > Validate Journey Enhancements** and **P13 > Run Final QA** remain available.

**Apply Gateway Vortexes** is an idempotent maintenance command for the existing visual components, not an environment generator. It skips Hidden City. Existing scene edits should be saved before running it.

Automated checks use an isolated Unity copy; applied assets are hash-compared with the tested version. No actual runner or physical hardware testing is claimed. No Windows build or Git push.
