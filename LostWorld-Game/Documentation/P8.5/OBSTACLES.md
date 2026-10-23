> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P8.5 — Environmental obstacles

World geometry only: eight reusable obstacle prefabs and four placed instances per realm. Existing realm geometry and P4–P8 components are retained. No movement, jumping, damage, physics-driven motion, interaction triggers or progression logic was added.

| Realm | Added instances | Existing style reused |
| --- | --- | --- |
| Jungle | 2 low fallen logs, 2 rounded boulders | Bark, cut wood and jungle rock |
| Ruins | 2 broken-block clusters, 2 fallen pillar/rubble clusters | Sandstone, limestone and moss |
| Crystal Cave | 2 low crystal clusters, 2 rocks | Existing faceted crystal mesh, teal/violet crystals and cave rock |
| Hidden City | 2 low stone barriers, 2 debris clusters | Ivory, patina and city paving |

## Find and adjust

Each realm scene remains separate under Assets/Scenes/Person2. Expand **Environment > P85_EnvironmentalObstacles**. Prefabs live under **Assets/Prefabs/World/P85Obstacles**. Select an instance and press F in Scene view to frame it.

The existing 16 instances are now centred directly on the running routes. Logs/pillars/barriers span across the route rather than following its edge. Solid colliders intersect the centre lane: a runner continuing straight encounters an obstacle. Side walking passages remain open, so these are not full-width walls. No automatic movement or jumping was implemented.

Each collider envelope is 0.6m high and approximately 0.7m deep along travel. Width is 2.4m in Jungle and 2.8m elsewhere. Root Transform scale adjusts the existing reusable prefab geometry and colliders together; the instances intentionally have scale overrides. Ground allowance is 0.02m. Final tuning must use Person 1's actual jump height, step offset and player collider.

### Obstacle locations

World X/Z coordinates below describe the route crossing centre (an asymmetric prefab root may be slightly offset to centre its geometry). Heights follow the existing ground.

| Realm | Type and crossing centre (X, Z) |
| --- | --- |
| Jungle | Log (-0.25, 16.20); rock (1.80, 27.20); log (0, 60); rock (0, 102) |
| Ruins | Blocks (1.05, 23.95); fallen pillar (1.05, 46.05); blocks (0, 67.30); fallen pillar (0, 81.70) |
| Crystal Cave | Crystals (-1.05, 20.25); rock (-1.95, 35.60); crystals (1.65, 54.80); rock (1.05, 72.40) |
| Hidden City | Barrier (0, 18); west-branch debris (-10, 46); east-branch barrier (10, 55); shared-exit debris (0, 77) |

Jungle has four encounters along its straight main road. Hidden City has one encounter before the plaza split, one on each branch and one after rejoining: either city route encounters three obstacles. Spacing remains at least 8m. Environmental interactions and route-choice zones are unchanged.

## Editor tools

- **Tools > Lost Worlds > P8.5 > Validate Obstacles**: checks all four saved realms and restores the previous scene setup. Save current scene edits when asked so validation checks the current version.
- **Tools > Lost Worlds > P8.5 > Apply Main Route Jump Layout**: explicitly reapplies the documented positions/scale to the 16 P8.5 instances. This resets manual placement edits in that group only.
- **Setup Obstacles** creates missing groups/prefabs, then applies this main-route layout. No setup action is required for the delivered scenes.

Validation checks active prefab links, enabled solid colliders, ground height, low obstacle height, spacing, materials and absence of runtime obstacle scripts/Rigidbodies. Three forward ray tests per obstacle confirm collision on the running centre line and at +/-0.3m. Sampled body-clearance checks confirm side passages against all solid scene colliders. Protected locations and P4 test references retain 3m clearance; the full P6 zone remains clear. These are geometry checks, not validation of an unprovided player controller.

## Manual Unity checks

Open each realm and inspect its P85 group in perspective Scene view. Walk the route visually, particularly near the Jungle main road and glade, Ruins viewing approach, cave chambers and city plaza branches. Confirm silhouettes and spacing feel calm/readable and that interaction markers remain easy to see and reach.

Run Validate Obstacles after any size/position edit. Use the existing P4–P8 demo menus to check their controls. Final player collision and jump-fit testing must wait for Person 1's actual prefab; retain a walking bypass regardless of jump tuning.

No P9, hardware systems or Person 1 changes are included.

Revised validation completed: 400 centre-line collision/side-passage checks and 28 solid colliders. P4/P5/P6/P7/P8 regression suites passed 22/31/34/66/57 checks. All assets are generated and saved; no setup action remains.
