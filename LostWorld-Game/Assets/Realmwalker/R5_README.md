# R5 — all-realm Realmwalker integration

Play: **Tools > Lost Worlds > R5 > Play Full Journey**. A/D or arrows change position, Space jumps, E activates. Existing processed P10 commands remain unchanged. Speed remains 2.8 m/s.

Jungle, Ruins and Crystal Cave now share a gateway-local trigger 5.2 m wide and 1.2 m deep, centered 3.2 m before their exit anchor. The configurable activation margin is 0.2 m. All three positions can approach; auto-run waits inside the same reachable zone. ACTIVATE outside it is rejected. Existing lever/pedestal animation and P11/P12.5 transitions remain authoritative. Controller-free TestActivate uses a dedicated reference inside the zone.

Each scene contains one instance of the existing Realmwalker prefab. The old scene's runner is removed by P11 scene loading and the next runner binds to the persistent session. The follow camera reuses that realm's existing camera. Token counts persist (10/8/8/10); 0.45 m pickup spheres use R4's physical capsule overlap. Tokens remain optional.

Hidden City retains automatic trophy completion, the existing victory/star UI, Replay and Finish; no ACTIVATE or portal was added there.

Hair changes affect only the rear/crown backing mesh and reuse the existing brown material, head weights and bindposes. Face, beard, swept locks and rig remain unchanged.

Validation menu: **Tools > Lost Worlds > R5 > Validate All-Realm Journey**. The automated test places the capsule at token test positions, then resets to the entrance and drives continuously through each road using jump/ACTIVATE commands. It is not a human playthrough or real hardware test. Manual review is still required for camera comfort, precise jump timing, terrain foot sliding and optional token-route choices. No foot IK, checkpoint/recovery or R6 polish is added.
