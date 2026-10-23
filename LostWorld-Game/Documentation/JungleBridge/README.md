# Jungle scenic bridge and interaction polish

Jungle-only authoring pass. New assets: `Assets/Art/Environments/Jungle/ScenicPolish`; editor tool: `Assets/Editor/JungleScenicPolish.cs`; scene: `Assets/Scenes/Person2/Jungle_Prototype.unity`.

## Changes

- The yellow GRAB cube was functional, and the blue box was its receiving stand. Their mesh/material overrides now present a mossy stone and weathered stone stands. The ACTIVATE pedestal and button likewise use natural stone forms. Original GameObjects, scripts, colliders, rigidbody, hold points, feedback renderers, success material, beacon and APIs are retained. Shared P4 prefabs are untouched.
- Removed unused visual-only locator discs beneath the future gameplay anchors; preserved the anchors themselves.
- Added an 8.4-metre-wide, ten-metre-long wooden bridge at X=0, Z=67–77. It spans the flat middle road section around Z=72. Twenty-five planks, timber supports, simple posts and rails use existing natural wood materials and scene lighting. One seamless deck collider and two outer railing colliders preserve a clear seven-metre main route. Deck top is Y=2.10, only 4.5cm above the existing road visual; approaches soften the edge. Actual Person-1 collider/step-offset testing remains necessary.
- Added a shallow cross-stream bed, still water surface, bank stones, fourteen small understory clusters, two off-road fallen logs, and natural stone edging around the starting clearing. Static URP geometry only: no water simulation, extra lights, particles or runtime scripts.
- The new ground visual mesh cuts the shallow channel; a separate collision mesh slopes its banks while retaining the original supported main road beneath the deck. Existing ground assets are untouched. No token, obstacle, interaction, portal or integration waypoint was moved.

## Manual Unity verification

1. Stop Play Mode and reopen Jungle_Prototype from disk. The scene is already saved; do not run Apply unless authoring a copy without this pass.
2. Run **Tools > Lost Worlds > Jungle > Validate Scenic Bridge**. This includes existing natural-visual and continuous-road checks.
3. Frame `Environment/JungleScenicPolish/WideWoodenBridge` in Scene view. Inspect the deck/approaches, railing spacing and stream from above and near player height. Confirm tokens remain unobstructed and the road has no fork.
4. Run **Tools > Lost Worlds > P4 > Run Play Mode Interaction Checks**. For manual inspection use the existing controller-free P13 demo's interaction view, then G for GRAB and E for ACTIVATE. Check the stone moves to its receiving stand and activation still shows success feedback.
5. Use **Tools > Lost Worlds > P13 > Play Final Test**. Collect Jungle's ten tokens using the existing test buttons and enter the portal. Check that the HUD/count persists into Ruins. **Run Final QA** tests the complete journey.
6. Inspect Console and target-PC frame time. When Person 1 supplies a controller, walk/run across both bridge approaches and test the rails and interaction reach with the real collider. No player code was added or changed here.

All other realms, lighting/vegetation from the previous pass, portal visuals, gameplay/hardware and P11 interfaces are preserved. No external assets, Windows rebuild or Git push.
