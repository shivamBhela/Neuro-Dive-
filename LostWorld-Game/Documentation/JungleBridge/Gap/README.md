# Jungle bridge jump gap — current design

Supersedes the continuous bridge and fallen-log design in the parent/JumpLog documents.

- Real opening: **1.2m**, world Z=71.4–72.6, across the 8.4m-wide bridge. Three planks and the fallen log removed. Longitudinal supports and railings split at both ends of the opening.
- Two level landing sections: X=-4.2–4.2; Z=67–71.4 and 72.6–77; deck top Y=2.10. Flush contrasting edge timbers make takeoff/landing visible.
- Deck collision is split; bridge-area road render mesh, terrain render mesh and terrain collision are cut underneath. Water remains visual-only. No solid catch floor was introduced beneath the gap. Collisions outside this local opening are preserved.
- The scene keeps ten tokens at their existing positions, including Z=69 and 77 outside the gap. Existing interactions, other obstacles, stream, portal, lighting, hardware and integration APIs are unchanged. No Player/controller, jump or recovery behavior was added.

## Validation

Unity compilation succeeded. 252 downward rays across the opening find no solid collider for 30m below the bridge. Thirty landing support/body-clearance samples and sixty other bridge samples pass. Jungle road validation now checks the rest of the road normally and separately verifies the intentional opening. P13 full-journey regression: 122 checks passed, with no runtime Console warnings/errors; includes existing interaction, token/persistence, portal, effort and command integrations. No missing-reference failures. Preview rendered and inspected.

The saved scene, three new mesh assets and editor tools match the validated copy by SHA-256 (12 files including metadata). Only the Jungle scene, bridge/road editor validation and new bridge-gap assets/tools changed among project assets; other realms/runtime scripts/shared settings remain unchanged. FILES_CHANGED.txt lists assets; PROJECT_STATUS.md and this guide are documentation updates. No Git push or Windows build.

## Manual Unity verification

1. Stop Play Mode. Reload `Assets/Scenes/Person2/Jungle_Prototype.unity` from disk; do not overwrite it with an old open-scene copy.
2. Frame `Environment/JungleScenicPolish/WideWoodenBridge/JumpGap_1p2m`. Inspect the missing section and both landing edges. In the Physics Debugger, confirm the two separate deck colliders and no ground/floor across the opening.
3. Run **Tools > Lost Worlds > Jungle > Validate Scenic Bridge**. The saved scene is already generated; no creation command is needed.
4. With Person 1's actual controller, test jumps from both directions at the intended slow running speed and verify the full collider lands clear of both edges. A 1.2m gap is a conservative prototype choice, not a certified jump distance: this checkout does not contain the actual controller/jump arc. Falling/recovery must use existing Person-1 behavior; no hidden floor or new reset system was added.
5. Run **Tools > Lost Worlds > P13 > Play Final Test** to inspect the unchanged controller-free token/portal flow. This harness does not simulate player movement or prove jumpability.

No reference images were attached to this request; the opening follows the existing bridge's materials and proportions. Preview: Bridge.png. Logs and FINAL_QA.txt accompany this guide.
