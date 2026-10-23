# Straight running roads

Ruins and Crystal Cave now have straight 9 m roads; Hidden City has a straight 10 m road, all centred on x=0. Existing rectangular road meshes/materials are reused in adjacent, aligned segments. Rounded joins and superseded side-road render objects are inactive. Decorative circular courts remain beside the road; the Hidden City monument remains with its side plaza. Endpoints and elevation are unchanged.

Main-path and existing branch waypoint transforms match the single road. Existing obstacle prefab transforms and token positions were realigned, preserving IDs, colliders, references and automatic collection. 36 tokens remain (10/8/8/10). Jungle is unchanged, including its bridge gap. No runtime scripts or gameplay systems changed.

Validation: straight edges, exact 9/9/10 m widths, joined segment ends, full-width floor support, corridor clearance, token counts/IDs/clearance and missing references. P12.5 journey: 191 passed; P13: 133 passed; P14 reference/dependency validation passed. The final viewing-court side adjustment was checked with the layout/reference suite. No C# errors/warnings. Tests ran in the isolated Unity copy; tested scenes are copied back only after checking the actual scenes remain unchanged during testing.

Changed assets: Ruins_Prototype.unity, CrystalCave_Prototype.unity, HiddenCity_Prototype.unity; existing Editor-only RunningPathPolish.cs and P85ObstacleBuilder.cs (layout validation adapted to authored straight paths). No new runtime components or assets.

Manual: stop Play Mode and reload each scene from disk. Inspect the parallel road edges and side courts in Scene view. Run Tools > Lost Worlds > Layout > Validate Running Paths and Tokens, then P13 > Play Final Test for collection/progression. Check lever-activated gateways and Hidden City's automatic trophy completion. Actual lane displacement/jump reachability require Person-1's runner; no runner testing is claimed.

No Git push or Windows build.
