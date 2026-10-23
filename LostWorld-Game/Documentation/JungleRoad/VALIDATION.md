> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# Validation — Jungle single-road refinement

Unity 6000.6.1f1. Checks run in an isolated Unity copy; the delivered Assets, Packages and ProjectSettings were compared byte-for-byte against it (618 files, zero differences). No runtime script or Person-1 API changed. Ruins, Crystal Cave and Hidden City scenes remain byte-for-byte unchanged in the actual project.

- Jungle authoring: saved by Unity, 5,725 road vertices grounded, 327 body-clearance samples, zero Jungle route zones/branch binding, ten unique original tokens on the main road.
- P8.5: 400 collision/bypass samples across four realms; four low Jungle obstacles retained.
- P11: 80 checks, including direct Jungle LEFT/RIGHT callbacks, Hidden City branch callbacks, relic persistence and completion.
- P12: 71 polish checks.
- P12.5: 212 checks, including all 36 authored tokens, duplicate protection, persistence, HUD, optional collection and portal cache/missing-reference recovery in every realm.
- P13: 128 combined QA checks; three relics, 36 tokens, exactly three transitions and one completion, hardware commands/adapter, effort feedback, interactions, portal visuals and Console monitoring.
- P14: Windows build support, scene order, dependency and P11/P12/P12.5 structural checks passed. Existing executable not rebuilt by this layout-only task.
- P6: 34 reusable route checks passed in a temporary standalone fixture; Jungle no longer needs a fork.

No C# warning/error, shader compile error or runtime exception was reported by the successful regression checks. P13's Console monitor passed with no runtime warnings/errors. Fresh batch import also emits Unity package diagnostics (unused terrain shader dependency/URP Deprecated.cs import messages) and normal shutdown memory telemetry; these are retained in the raw logs, not hidden or treated as gameplay failures.

Manual player/collider/jump/camera verification remains Person 1's responsibility. Ground/body samples use a test capsule, not a claim that the absent controller was tested. See README.md for exact manual steps.
