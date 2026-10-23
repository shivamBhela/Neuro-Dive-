# Jungle prototype — P2 environment only

## Open and build

Scene: `Assets/Scenes/Person2/Jungle_Prototype.unity`.

Use Unity **6000.6.1f1** and the existing URP configuration. The editor-only tool is **Tools > Lost Worlds > Build Jungle Prototype**. On first use it creates and saves the scene plus reusable materials, meshes, and prefabs. On subsequent uses it opens the existing scene additively, without rebuilding or overwriting your edits. Other open scenes remain loaded and unchanged. Use **Tools > Lost Worlds > Validate Jungle Prototype** after opening the scene.

The builder does not run automatically on import. It does not modify build settings, packages, input actions, SampleScene, player objects, or other developers' scenes. It uses a fixed local random seed for repeatable initial vegetation placement. Existing P2 materials, meshes, and prefabs are reused rather than overwritten. If you want a fresh generated variant later, first agree how to preserve hand edits; the tool intentionally has no destructive rebuild command.

The delivered scene has already been generated and saved by Unity. If an unnamed scene is open, save it under its own name before using the additive build menu; the builder safely stops rather than discarding it.

## Layout and orientation

The playable ground footprint is approximately 64 m wide by 128 m long. Forward travel is **+Z**; left/right are **-X/+X**. Ground climbs gradually from Y=0 to Y=4 through two gentle slopes. The ground is a generated mesh with a solid, non-convex MeshCollider, not Unity Terrain.

| Along route | Environment |
| --- | --- |
| Z=0–12 | Open starting clearing and first canopy corridor |
| Z=12–28 | Winding dirt trail with a west exploration glade |
| Z=32–50 | Broken timber bridge over a recessed ravine; continuous west bypass around X=-10 |
| Z=50–64 | Open stone courtyard east of the path, four columns, lintel, low wall, moss, and empty dais |
| Z=70–96 | Two visible dirt branches around a central tree/rock island, rejoining before the exit |
| Z=96–110 | Uphill approach to the stone jungle exit threshold |

The exit points toward a future Waterfall/Ruins environment. Only the threshold exists in P2; no waterfall, later world, loading, or progression is implemented.

The bridge is deliberately broken and its direct approaches have visible rubble barriers. Use the west bypass to inspect a continuous route; P2 does not assume jumping, climbing, or bridge repair. Perimeter berms, rocks, tree trunks, and canopy create route framing. Decorative foliage, path ribbons, and marker discs have no colliders. Ground, structural stones, bridge pieces, roots, trunks, and boulders have solid colliders. There are no triggers, rigidbodies, navmeshes, or runtime behaviours.

## Future integration locations

All locations are empty transforms under `Jungle_Prototype/FutureGameplayPoints`, with small visual-only child discs. Positions are authoring anchors, not spawn/checkpoint/interaction logic. Forward orientation is +Z. Marker origin is approximately 0.15 m above local ground; adapt to Person 1's agreed prefab pivot and collider.

| Transform | Position (X,Y,Z), metres | Intended future use |
| --- | --- | --- |
| StartPoint | (0, 0.15, 0) | Person 1's future Player integration anchor |
| RouteSplit | (0, 2.15, 70) | Future left/right decision location only |
| GrabPoint | (-7, 0.15, 20) | Future GRAB location in exploration glade |
| ActivatePoint | (9, 2.15, 56) | Future ACTIVATE location in ruin courtyard |
| JungleExit | (0, 4.15, 108) | Future connection toward Waterfall/Ruins |

Teal discs identify start/split/exit; amber discs identify future interaction locations. No collectible, interaction component, command receiver, or route-choice logic is attached.

## Reusable assets and hierarchy

- `Assets/Prefabs/World/JunglePrototype`: CanopyTree, FernCluster, MossyBoulder, RuinColumn, BrokenBridge.
- `Assets/Art/Materials/JunglePrototype`: earthy ground/trail/wood, varied foliage, rock/moss/ruin stone, dark ravine, teal/amber location colors; all use URP/Lit.
- `Assets/Art/Environments/Jungle/Prototype`: generated ground and path/clearing mesh assets.
- `Assets/Editor/JunglePrototypeBuilder.cs`: authoring, static validation, optional batch overview capture; no runtime scripts.

The scene has one `Jungle_Prototype` root. `Environment` contains `Terrain_Ground`, `JunglePath`, `Vegetation`, `Rocks`, `Obstacles`, and `Landmarks`; landmarks contain `BrokenBridge` and `AncientRuins`. `FutureGameplayPoints` and `Lighting` are siblings under the root.

Lighting uses a warm directional sun, cool green ambient fill, and gentle linear distance fog. A solid-color camera background avoids dependencies on external sky assets. The fixed `PrototypeOverviewCamera_NoController` is an inspection camera only, with no controller, MainCamera tag, or AudioListener. Disable/remove it when Person 1 integrates the actual player camera. Review Jungle on its own for visual validation; other loaded scenes can contribute cameras/lights to an additive editor view.

## Verification boundaries

The editor validator checks the expected hierarchy, material/shader references, solid colliders, missing scripts, and absence of runtime scripts, triggers, rigidbodies, character controllers, and animators. It also raycasts the ground at roughly metre intervals at the center and 1m to each side of each intended route. The delivered scene passes 627 grounding samples and contains 613 solid colliders. These checks establish the P2 boundary and continuous route support, not compatibility with an unknown future player controller. Person 1 must later verify clearance, slope limits, collision response, and camera behaviour with their prefab. P2 adds no gameplay or P3+ features.

`Jungle_Overview.png` is an actual Unity-rendered overview. The optional `BuildAndCapture` batch entry point uses a temporary in-memory URP clone with batching disabled to avoid first-frame capture artifacts, then restores the original pipeline. The saved project configuration is unchanged. Normal Editor Scene/Game rendering remains part of user verification.
