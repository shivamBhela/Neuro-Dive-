# Lost Worlds — Person 2

## Current design update — 2026-10-14

The three-relic system is retired. Journey Tokens (36 across the four realms) are the only collectible tally; all are optional and retain their independent session ledger/HUD. Environmental GRAB/ACTIVATE, portals, obstacles, effort/rest and processed-command architecture remain. Player-command, binding, route-data and transition APIs are unchanged; the relic-only session member is removed. Jungle uses one continuous main road.

Use [the current cleanup/testing guide](Documentation/RelicRemoval/README.md) and [P14 handoff](Documentation/P14/DEMO_HANDOFF.md). The original phase descriptions below are historical and do not authorize recreating relics.

## Project overview

Lost Worlds is a collaborative Unity project. Phase 1 prepares folders, documentation, and local version control only. No worlds or functional systems are implemented in this phase.

The inspected project is `C:\UnityProjects\Lost Worlds`. The requested spelling `C:\UnityProjects\LostWorlds` does not exist; the existing project has not been moved or renamed.

Unity version: **6000.6.1f1** (revision `7efac9f6c10e`). Render pipeline: **Universal Render Pipeline 17.6.0**, assigned by the Mobile and PC quality levels; PC is the current quality level. The default Graphics pipeline slot is empty, but quality-level URP overrides are present and preserved.

## Ownership

| Owner | Responsibilities |
| --- | --- |
| Person 1 | Player character/controller, auto movement, LEFT/RIGHT movement, jump, player animations, checkpoints, adaptive difficulty, gameplay and progression. |
| Person 2 | 3D environments/worlds, environmental obstacles, GRAB/ACTIVATE interactions, relics/collectibles, route-choice triggers, UI/HUD, effort/rest feedback, hardware simulator/adapter, environment audio/VFX/lighting/polish. |

Person 2 work must not implement or modify Person 1 systems. Shared integration contracts should be agreed before future implementation.

## Collaboration rules

- Person 1 mainly works in player scenes and player prefabs.
- Person 2 mainly works in environment scenes and world prefabs.
- Avoid both developers editing the same scene; agree on scene ownership before editing shared content.
- Use modular prefabs and systems for later integration. Coordinate edits to shared packages and project settings.
- Preserve existing Unity files, GUIDs, and `.meta` files. Commit asset changes together with their metadata; move assets through Unity where possible.
- Version `Assets`, `Packages`, and `ProjectSettings`. Do not commit generated caches or local IDE settings.
- Visible Meta Files and Force Text serialization are already enabled and were preserved.
- No GitHub repository, remote, push, or initial commit is created by Phase 1.

## Person 2 folder layout

```text
Assets/
  Art/
    Environments/
      Jungle/
      Ruins/
      CrystalCave/
      HiddenCity/
    Materials/
    Models/
    Textures/
    VFX/
  Audio/
    Music/
    SFX/
  Prefabs/
    World/
    Interactions/
    Collectibles/
    UI/
  Scenes/
    Person2/
    Test/
  Scripts/
    Core/
    Interaction/
    Hardware/
    UI/
    World/
  UI/
  Editor/
```

These are organizational placeholders only. Folder `.meta` files provide stable Unity GUIDs; `.gitkeep` files preserve empty directories in Git and contain no implementation. Existing starter content remains in place.

## Hardware rule

EEG, EMG, and exoskeleton processing happens **outside Unity**. Unity will later receive processed commands, for example:

```json
{"command":"JUMP"}
```

Phase 1 includes no signal processing, hardware adapter, simulator, transport, or command handling.

## Future GameInputManager concept — not implemented

```text
Keyboard/Test ─┐
Simulator ─────┼→ GameInputManager → Person 1 + Person 2 systems
Real Hardware ─┘
```

Future commands may include `LEFT`, `RIGHT`, `JUMP`, `GRAB`, and `ACTIVATE`. The shared manager would provide a common input boundary, with Person 1 consuming movement/jump commands and Person 2 consuming interaction commands. Its ownership, API, routing, and integration require future agreement. The existing template Input System asset is preserved; it is not a GameInputManager implementation.

## Phase boundary

Do not begin Phase 2 without a new instruction. Terrain/worlds, gameplay, player systems, interactions, relics, routes, HUD, effort/rest feedback, hardware systems, and environment polish are outside Phase 1.
