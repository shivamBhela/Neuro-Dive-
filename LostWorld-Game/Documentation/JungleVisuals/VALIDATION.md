# Validation — final Jungle visual pass

Unity 6000.6.1f1 / URP 17.6.0. Authoring, compilation, renders and tests executed in the existing isolated validation checkout. The finished scene, new Jungle-only assets and editor tool were copied to the actual project; all 94 copied/authoring files match the tested copy by SHA-256. Only the Jungle scene changed among pre-existing Assets/Packages/ProjectSettings files. Other realms, runtime scripts, shared prefabs, packages and settings match Before.json.

| Check | Result |
| --- | --- |
| C# compilation | Passed; no compiler errors/warnings in final runs |
| Jungle visual and continuous-road checks | Passed: 5,725 grounded vertices, 327 body-clearance samples, 10 unique tokens, no fork/branch |
| Missing references / retired relic audit | Passed: eight scenes and all prefabs; 36 tokens, four portals, no relics |
| P8.5 obstacle checks | Passed: 400 samples across four realms; four Jungle obstacles preserved |
| P11/P12/P14 structural/dependency checks | Passed; P11 APIs/anchors, polish, ordered realms and build readiness |
| P13 full journey | 122 checks passed; Jungle → Ruins → Crystal Cave → Hidden City → Journey Complete |
| P12.5 runtime regression | 188 checks passed; automatic token/portal tests, duplicate protection, persistence, portal null-cache recovery |
| Runtime Console monitoring | No warnings/errors during P13; portal missing-reference recovery tests also passed without warnings/errors |
| Rendering | Oblique road and low portal-approach previews visually inspected |

Performance inventory: 754,132 → 504,372 mesh-instance triangles, 1,515 → 1,599 renderers (12 seven-leaf planting clusters), 628 colliders and two lights unchanged. Four particle systems / 76 cap unchanged. 84 foliage-only distant culling groups; no extra runtime script. Small shared normal/albedo textures have mipmaps. This inventory does not establish FPS; profile the intended demo computer.

Batch logs contain Unity licensing-service access-token notices and occasional Curl shutdown messages outside gameplay; all five final processes exited 0. Git whitespace checks report Unity's generated YAML empty-value trailing spaces (and prior changes); serialized assets were not reformatted. No Windows build was run. Captures temporarily disable SRP batching/GPU Resident Drawer on an in-memory pipeline clone for Unity render-request compatibility, then restore it; saved pipeline assets remain unchanged.

Evidence: `Final-*.log`, `FINAL_QA.txt`, `Before.json`, `FILES_CHANGED.txt`, `Jungle.png`, `Portal.png`. The file manifest also lists generated .meta files. PROJECT_STATUS.md and this documentation are additional documentation-only changes.
