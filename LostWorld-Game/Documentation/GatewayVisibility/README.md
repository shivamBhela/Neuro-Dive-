# Permanent stone gateway visibility — 2026-10-16

Root cause: each of the first three scenes placed its stone frame in PortalUnlockMechanism.revealObjects. Awake therefore hid the frame together with the locked portal.

Fix: remove only that frame reference from the serialized revealObjects list in Jungle_Prototype, Ruins_Prototype and CrystalCave_Prototype. All three gateway GameObjects were already enabled. No geometry, transform, material, collider, trigger, shader, animation or runtime script was edited. The existing portal reference still hides the vortex and rejects transitions until ACTIVATE. Hidden City is untouched.

PortalUnlockValidation now expects a permanent visible frame in the first three realms and checks it remains visible during both the locked wait and activation animation. Hidden City's separate frame policy is unchanged.

## Manual Unity checks

1. Stop Play and reload Jungle_Prototype from disk. Use **Tools > Lost Worlds > P12.5 > Play Journey Enhancement Demo**.
2. Before activation, inspect the exit: stone gateway visible, opening empty. The existing portal-entry test must reject entry.
3. Click **Focus lever for E / hardware ACTIVATE (test)** and press E in Game view, or send processed `{"command":"ACTIVATE"}` through the existing adapter. Check the frame never disappears and the golden/amber vortex reveals fully.
4. Enter the portal using the existing demo controls. Repeat in Ruins (ice-blue/violet) and Crystal Cave (royal-purple/electric-blue). Confirm transitions to the intended next realm.
5. Check Reduced Motion and run **Tools > Lost Worlds > Portal Visuals > Validate Gateway Vortexes**. Hidden City must retain its existing trophy finale.

Changes are already saved; no setup/regeneration command is required. Automated tests use an isolated copy of the same project, not an actual runner. No Git push or Windows build.
