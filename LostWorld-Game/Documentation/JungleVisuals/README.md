# Jungle natural visual upgrade — 2026-10-15

Jungle-only presentation pass. The seven-metre continuous road, collision geometry, ten token placements/IDs, four obstacles, environmental interactions and exit portal retain their existing gameplay setup. No relics or Jungle route fork were restored.

## Authored changes

- Nine Jungle-only URP Lit surface materials, with small seamless 256×256 albedo/normal maps, mipmaps and nonmetallic roughness. Earth, forest floor, bark, foliage, moss, limestone and rocks have subdued natural variation.
- Visual-only ground/path mesh copies add consistent world-scale UVs. Original ground collider mesh remains untouched.
- Shared smooth organic crown/rock meshes, tapered trunks and tapered buttress roots. Existing tree placement/size variation retained; crown silhouettes vary further. Shared source prefabs are unchanged.
- Lightweight curved leaf mesh replaces Jungle's sphere-based fern leaves. Twelve existing-prefab planting clusters frame the road and portal, outside the seven-metre road. No added colliders.
- Conservative distant foliage culling on 84 trees. Shared materials support instancing; existing URP batching settings are preserved.
- Warm soft-shadow sunlight, balanced ambient light, scene-local subtle grading, procedural sky and gradual distance fog. Existing PC renderer SSAO is reused unchanged; no new renderer features, package installs, bloom or costly volumetrics. The lower Mobile quality retains its existing capabilities.
- Portal energy surface, particles, triggers, runtime scripts, camera placement and integration APIs remain unchanged. The camera now renders the sky and scene-local grading.

## Performance scope

Authored MeshFilter triangle instances decrease from **754,132 to 504,372 (33.1%)**, despite the twelve added planting clusters. This is a scene inventory, not measured GPU frame time. Nine material texture pairs are 256×256 with mipmaps. Particle budget remains four systems / 76 maximum particles; lights and colliders are unchanged. No runtime Update script was added. No Terrain conversion or duplicated multi-resolution tree geometry was necessary.

The new assets live in `Assets/Art/Environments/Jungle/NaturalVisuals`. `Assets/Editor/JungleVisualUpgrade.cs` provides idempotent authoring and validation; reapplying an already upgraded scene validates without duplicating objects.

## Manual Unity checks

1. Stop Play Mode. Open `Assets/Scenes/Person2/Jungle_Prototype.unity`; reload it from disk if it was already open before these changes. Do not overwrite the newly saved scene with an older in-memory scene.
2. Use PC quality to inspect existing SSAO. Inspect Scene view with scene lighting/effects enabled, then Game view. Check ground detail, tree silhouettes, soft shadows and the clear road/portal approach at 1920×1080 and 1280×720. The scene is already generated: no Apply step is required.
3. Run **Tools > Lost Worlds > Jungle > Validate Natural Visuals**.
4. Run **Tools > Lost Worlds > P13 > Play Final Test** for the existing controller-free demo. Use its existing interaction/token/exit test actions; confirm G/E environmental interactions, ten Jungle tokens/HUD, effort feedback and the portal remain readable. Actual player movement is still Person 1's responsibility.
5. Run **Tools > Lost Worlds > P13 > Run Final QA** and **Tools > Lost Worlds > P14 > Validate Demo Build** if local regression confirmation is desired. Do not choose Build Windows Demo for this task.
6. In Play Mode, inspect Console and the Unity Profiler/Game Stats on the intended demonstration PC. Confirm target frame rate and distant foliage culling are acceptable; batch renders cannot establish hardware-specific FPS.

The Windows demo was deliberately not rebuilt and still has the previous Jungle visuals. No Git push.
