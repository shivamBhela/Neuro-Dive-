> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# Jungle — final single-road design

The Jungle now uses one seven-metre-wide straight main road (+Z, X=0). The former ravine is filled with matching solid sloped ground. The broken bridge remains an off-road landmark. GRAB and ACTIVATE retain their accessible side clearings.

Removed from the Jungle scene: P6 route-choice station, zone, signs/arrows, RouteSplit location, BranchPathBinding and its left/right waypoint objects, old winding/bypass/fork road surfaces. RealmBindings.Branch is intentionally null. Its mainPath points remain available and now follow X=0. No runtime API or Person-1 system changed.

Preserved: four solid P8.5 obstacles (centre Z=16.2, 27.2, 60, 102), both P4 interactions, ten original token IDs, portal and P11 exit. Tokens lie on the main road at Z=8, 23, 36, 41, 48, 54, 69, 77, 85 and 95. Historical left/right token ID strings remain stable for persistence; they no longer designate branches.

LEFT/RIGHT/JUMP still pass through P9/P10 and the optional P11 player bridge. Jungle does not consume LEFT/RIGHT for route selection or show a route-choice prompt. Hidden City and the reusable route framework remain unchanged. P6's standalone test instantiates a temporary fixture during Play and never saves a fork into Jungle.

## Manual verification

1. Stop Play Mode. Reopen Assets/Scenes/Person2/Jungle_Prototype.unity from disk; do not save an older cached scene over it. Allow Unity to finish importing.
2. In Scene view, select Jungle_Prototype and press F. Inspect the straight road, filled ground, off-road bridge, four low obstacles, ten tokens and exit portal. Check both interaction clearings remain accessible.
3. Run Tools > Lost Worlds > Jungle > Validate Continuous Road, then Tools > Lost Worlds > P11 > Validate Integration Readiness and Tools > Lost Worlds > P14 > Validate Demo Build.
4. Run Tools > Lost Worlds > P13 > Run Final QA. Let it finish and check the Console for the passing result and absence of runtime errors/warnings.
5. Run Tools > Lost Worlds > P13 > Play Final Test. In the Journey demo window, collect remaining realm tokens: Jungle should reach 10. Enter the portal and repeat across the realms; total should reach 36 and Journey Complete should occur once. Missing tokens must still allow completion. Stop and restart to reset.
6. Test existing GRAB/ACTIVATE actions through their unchanged demo controls. Jungle should never ask for LEFT/RIGHT route selection. Optional reusable route tests are now Tools > Lost Worlds > P6 > Play Standalone Route Test.
7. Person 1 must still verify actual player collider/jump height, camera and auto-running against the straight mainPath and obstacles. No controller was created or changed.

Use Tools > Lost Worlds > P14 > Build Windows Demo when a refreshed executable is needed.
