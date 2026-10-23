> Current design (2026-10-14): the three-relic system is retired. F2/relic tests and counters no longer exist; Journey Tokens, environmental interactions and completion remain. See [current cleanup and testing guide](../RelicRemoval/README.md). Earlier validation counts below are historical.

# P12.5 — Journey enhancements

Portals and optional Journey Tokens add no player controller, movement, jump, animation or branch following. Existing P4–P12 scripts, P11 integration APIs, P9/P10 architecture remain unchanged.

## Portals

Each realm's `P125_Journey` child has a reusable open-arch portal at its existing P11 exit. Its BoxCollider is a trigger sized to the existing P11 exit volume; the visual arch and bases have no colliders. The visual refinement adds a two-sided transparent cyan/blue energy surface and one gentle emitter capped at 12 particles. No extra lights or colliders are added. A slow approximately 18-second pulse and broad drifting bands remain low contrast; P12 Reduced Motion freezes the energy and clears the motes. PortalAtmosphere handles presentation only.

`RealmExitPortal.TryEnter(Transform reference)` delegates to `IntegrationSession.TryAdvance(realm, reference)`. P11 alone owns loading, transition locks and final completion. The portal never reads the token count. P11's existing automatic bound-reference exit polling remains active, so it works without a Rigidbody. The trigger also accepts colliders belonging to the bound reference without checking names or tags. Collider entry and reference polling converge on P11's same duplicate guard.

Public `RealmExitPortal.TestEnter()` uses the existing exit anchor as a trusted test reference. P11's **Advance From Exit (Play Mode Test)** still works unchanged. The sequence remains Jungle → Ruins → Crystal Cave → Hidden City → Journey Complete.

## Tokens and placement

| Realm | Shared path | LEFT branch | RIGHT branch | Total |
| --- | ---: | ---: | ---: | ---: |
| Jungle | 10 | 0 | 0 | 10 |
| Ruins | 8 | 0 | 0 | 8 |
| Crystal Cave | 8 | 0 | 0 | 8 |
| Hidden City | 6 | 2 | 2 | 10 |

There are 36 optional tokens across the four scenes. Jungle has one main road; following one Hidden City branch offers 34 on a normal journey. Exploring both City branches may collect all 36. Neither the HUD nor completion demands a total.

Tokens sit about one metre above the grounded path, use a steady coin/diamond visual, and have a forgiving 2.4-metre trigger radius. They have no solid colliders. Both branches have matching token counts and positions at equivalent route distances. Placement avoids the existing obstacle collision surfaces by at least 2.8 metres horizontally.

Authored Z positions (X follows the P11 path; inspect transforms for exact grounded positions):

- Jungle main road: 8, 23, 36, 41, 48, 54, 69, 77, 85, 95. Historical left/right IDs remain stable.
- Ruins: 8, 17, 31, 39, 54, 60, 73, 78.
- Crystal Cave: 8, 14, 26, 42, 48, 62, 68, 81.
- Hidden City shared: 6, 12, 23, 26, 71, 82; both branches: 39, 63.

Each token has a stable serialized ID (`jungle.shared.1`, etc.). Keep IDs stable when moving tokens; give duplicated/new tokens a new ID. The validator rejects duplicate IDs.

## APIs and persistence

`JourneyRealm` automatically samples P11's `PlayerReference`. Normal short position segments are swept against token zones so passing through between frames still collects. A new binding or a teleport longer than eight metres samples only the endpoint, avoiding a trail of accidental collections during repositioning. It does not move the reference. Inactive/unbound references cannot collect automatically. Physics triggers accept only colliders associated with the bound reference; unrelated objects cannot collect tokens.

- `JourneyToken.TryCollect(Transform reference)`: explicit trusted proximity test.
- `JourneyToken.TryPass(Vector3 from, Vector3 to)`: explicit trusted swept-proximity test.
- `JourneyToken.TestCollect()`: controller-free test at the token centre.
- `JourneyToken.Collected`: event after successful collection.
- `JourneyLedger.Count`, `Contains(id)`, `CountChanged`, `TokenCollected`: session tally and callbacks. Its registry API is trusted; gameplay callers should use the token's proximity methods.

One JourneyLedger is added to P11's existing persistent session root. The HUD receives a small event-driven `Journey Tokens: X` label. A fresh Play session starts at zero; scene loads preserve the count and collected IDs. Reloaded/duplicated IDs restore their collected appearance and cannot count twice. No disk saving or new-game reset API is added.

Routes, effort/rest, hardware commands and completion do not depend on this ledger. Collection hides only the token's visual and trigger. It emits three gentle motes through the existing P12 success system, respecting Reduced Motion and its existing particle cap. No new audio or flashing effects are added.

## Controller-free Unity demo

1. Run **Tools > Lost Worlds > P12.5 > Validate Journey Enhancements** in Edit mode.
2. Run **Tools > Lost Worlds > P12.5 > Play Journey Enhancement Demo**. It opens Jungle with the existing P12 overview camera and a small Editor-only Journey Demo window.
3. Click **Collect next token (test)**. Confirm a coin disappears and `Journey Tokens: X` increments. **Collect remaining realm tokens (test)** exercises the same public token methods for the remaining coins.
4. Click **Enter portal / skip remaining tokens** to use the actual portal/P11 flow. Skip tokens deliberately and confirm the next realm still loads and the previous count persists.
5. Repeat through Hidden City; its portal must show existing Journey Complete, regardless of missed tokens. Repeated transition attempts are ignored.
6. Stop and reopen the demo for a fresh count. Original P4–P12 demo menus remain available.

To inspect automatic collection without a player controller: during Play create/select an empty test reference, use P11's **Bind Selected Reference + Enable Simulator (Play Mode Test)** menu, then move that reference through a token in Scene view. No token button press is needed. Move it into the portal exit volume to test automatic progression. Rebind a new reference on arrival as P11 requires. A collider/Rigidbody is optional for geometric detection; use a suitable collider on the bound reference to test physics contact.

Person 1 later supplies the real player/camera and calls the existing `IntegrationSession.BindPlayer` after arrival positioning. No new name/tag convention is required. Person 1 still owns movement, jump and branch following; their actual collider dimensions and camera readability need manual verification. No P13/P14 work or Git push is included.
