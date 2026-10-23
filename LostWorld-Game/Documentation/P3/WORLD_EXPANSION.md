# P3 — calm world expansion

P3 constructs three separate Person 2 environment scenes. It does not connect scenes at runtime and does not add gameplay. The original Jungle scene and all its reusable assets remain unchanged.

All three delivered scenes were generated and saved by Unity 6000.6.1f1. No manual scene-generation step remains.

## Viewing the worlds

Open the desired scene in the Project panel under `Assets/Scenes/Person2`, or use:

- **Tools > Lost Worlds > P3 > Open Ruins Prototype**
- **Tools > Lost Worlds > P3 > Open Crystal Cave Prototype**
- **Tools > Lost Worlds > P3 > Open Hidden City Prototype**

The open commands offer Unity's normal save dialog for any unsaved work, open only the selected scene, and frame it at a 3D angle. Switch to the **Game** tab for its static overview camera; no Play-mode input is needed. The Jungle scene does not change when these files are created.

**Tools > Lost Worlds > P3 > Build All World Prototypes** generates missing P3 scenes and assets. It preserves existing scene files, materials, meshes, and prefabs; repeated execution does not rebuild hand edits. It leaves your original scene open. Save unnamed scenes under their own names before using the build command. The builder uses its own scratch scene for prefab creation so temporary objects never dirty your active scene.

**Tools > Lost Worlds > P3 > Validate Open World Prototype** validates the currently active P3 scene. There is no automatic build-on-import behaviour. No package installation or build-settings change is needed.

## Rehabilitation-focused spatial choices

- Continuous level ground; no mandatory jumps, narrow balance paths, timed traversal, or precision crossings.
- Main painted routes are 8m wide in Ruins/Cave and 9m in the City. The waterfall viewing approach is 6m wide.
- The validator samples ground and obstruction clearance across a 4m-wide corridor at roughly 1m intervals. The clearance probe extends from 0.2m to 2.1m above ground. This is an environmental authoring check, not a player controller or a clinical assessment.
- Broad arrival courts, a waterfall viewing court, cave chambers, and a large city plaza provide space for slow controlled interaction later.
- Steady lighting, restrained emission, and static water; no flicker, camera motion, particles, or abrupt effects.
- The Cave uses bright ambient fill, a cool directional light, and steady local crystal lights. It is an open-roof cutaway with sparse high ribs, rather than an excessively dark closed tunnel.
- Decorative paving and location discs do not add collision seams. Solid ground supports the route; structural objects and rock formations have colliders outside the tested traversal corridor.

## Distinct environments

| Scene | Identity and route |
| --- | --- |
| `Ruins_Prototype.unity` | Mossy sandstone colonnade and fragmented walls; warm limestone promenade, jungle arrival gate, scenic cliff waterfall and still pool off the main route, open court, and cave threshold. Water ribbons are static geometry, not a water simulation. |
| `CrystalCave_Prototype.unity` | Slate rock walls and high ribs, pale winding path and three spacious chambers, faceted teal/violet emissive crystals, sandstone arrival portal, and a warm ivory exit toward the City. |
| `HiddenCity_Prototype.unity` | Ivory pavilions, patinated tiered roofs, gold trim, colonnades, gardens, a wide plaza around a central monument, and a level sanctuary court as the final destination. Both plaza promenades are open with no route-choice logic. |

Each scene has one named root containing **Environment**, **Landmarks**, **FutureGameplayPoints**, and **Lighting**. These are independent authoring spaces: forward is +Z and the flat ground surface is Y=0. A static untagged overview camera has no controller or AudioListener; Person 1 can later replace it during integration.

## Transition contract — locations only

| Source | Destination | Coordinates within its own scene |
| --- | --- | --- |
| Existing Jungle `JungleExit` | Ruins `RuinsEntrance` | Jungle anchor preserved at (0,4.15,108); Ruins entrance at (0,0.15,0) |
| Ruins `RuinsExit` | Cave `CrystalCaveEntrance` | Ruins exit at (0,0.15,88); Cave entrance at (0,0.15,0) |
| Cave `CrystalCaveExit` | City `HiddenCityEntrance` | Cave exit at (0,0.15,88); City entrance at (0,0.15,0) |
| City main promenade | `FinalDestination_LocationOnly` | (0,0.15,86) |

Each new scene also has `GrabPoint_LocationOnly`, `ActivatePoint_LocationOnly`, and `RouteChoice_LocationOnly`. Anchors contain only a Transform with a small visual child disc. There are no triggers, interaction components, collectible objects, scene loaders, progression scripts, or command receivers. Coordinates are not global stitched-world positions or an implemented scene-transition API.

## Reuse and ownership

- Existing P2 CanopyTree, FernCluster, and MossyBoulder prefabs are referenced without modification.
- New shared modules live in `Assets/Prefabs/World/WorldExpansion`: SandstoneColumn, IvoryColumn, CaveFormation, CrystalCluster, CityPavilion, and LowBalustrade.
- New materials live in `Assets/Art/Materials/WorldExpansion`; the faceted crystal mesh is in `Assets/Art/Environments/CrystalCave/Prototype`.
- Authoring code is solely `Assets/Editor/WorldExpansionBuilder.cs`. No runtime scripts are created.
- Player movement, jump, animations, checkpoints, adaptive difficulty, and progression remain Person 1's responsibility. Hardware signal processing remains outside Unity, and no hardware/input functionality is added here.
- Person 1 should later verify collider fit, viewing comfort, and clearance using the actual player prefab. There is no hidden fall-recovery or checkpoint system in P3; stay within the defined prototype space during future tests.

## Rendered verification

The explicit `BuildAndCapture` batch entry point builds and validates in an isolated source copy, opens each scene alone, and saves actual Unity renders under `Documentation/P3`. It uses the P2-verified transient URP clone with batching disabled for reliable first-frame capture, then restores the original pipeline. No saved project pipeline settings are changed. Normal Scene/Game rendering in the original project remains part of user verification.

Final verification passed compilation and all 1,065 ground/body-clearance samples: Ruins 321, Crystal Cave 294, Hidden City 450. Their solid collider counts are 204, 105, and 120 respectively. Every scene passed hierarchy, named-anchor, material, missing-script, and no-runtime-functionality checks. SHA-256 comparison confirmed all 236 pre-existing Unity source/configuration files remained unchanged. All three rendered overviews were visually reviewed; they are available beside this document.
