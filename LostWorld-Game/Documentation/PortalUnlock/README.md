# ACTIVATE portal unlock — Jungle only

## Behavior and architecture

Jungle now starts with its existing portal (including trigger, energy/particles and stone threshold) inactive. The ancient wooden lever at **(5, 3.63, 103)** displays **Press E to Activate Portal**. Its nonblocking trigger spans the road generously (12m wide, 20m long, plus the existing interactor range); failed attempts consume nothing and can be retried indefinitely.

An accepted ACTIVATE latches the unlock, enables the original portal, rotates the lever and gently grows the existing portal visual to its authored size over 0.8 seconds. A quiet 0.8-second original synthesized chime plays once at volume 0.06. P12 mute/reduced-motion preferences are respected. Prompt becomes “Portal ready — proceed.” Duplicate input cannot repeat success or relock the portal. State lasts until the realm unloads; a fresh Jungle session starts locked.

- `PortalUnlockMechanism` derives from the existing `InteractableBase` and consumes ACTIVATE only. It has no input parsing or scene-loading code. Optional handle rotation, prompt and audio references allow other visual mechanisms later. `OnSucceeded` remains the success callback; `CanExit` exposes the latched state.
- `IRealmExitGate` is an optional Person-2 integration contract. `RealmBindings.ExitUnlocked` defaults to true without a gate. The existing `IntegrationSession.TryAdvance` checks it before its original transition/duplicate guards. This also blocks automatic P11 exit polling, so hiding a trigger cannot be bypassed by the other entry path.
- Jungle alone references a gate. Other realm scenes are unchanged and remain unlocked by default. P11 entry methods/signatures, player binding, command callbacks, token persistence and completion logic are retained.
- Existing hardware ACTIVATE reaches the mechanism via CommandChannel → IntegrationSession → InteractionInteractor. No P9/P10 transport/parser changes.
- P11's saved session has its keyboard simulator disabled by default. `PortalUnlockKeyboardInput` is a small Jungle-only E fallback: when a player/test reference is bound and in lever range, it sends ACTIVATE through the same channel. It defers entirely when the session's P9 simulator is active. It does not generate LEFT/RIGHT/JUMP/GRAB or change any producer settings. Disable this component if replacing all local keyboard testing with another custom producer.
- `TestActivate()` uses the same interaction/range/one-shot path with an authored test interactor. It is explicitly for controller-free tests, not a distance-free gameplay entry point. `RealmExitPortal.TestEnter()` and P11 **Advance From Exit** still work after unlocking and correctly reject the locked state.

No relics or new GRAB gameplay, player movement, pause/auto-run behavior, or hardware processing was added. Journey Tokens remain optional. The bridge gap and existing Jungle gameplay remain intact.

## Manual Unity test

1. Stop Play and reload Jungle_Prototype from disk if already open. Setup has already been saved.
2. Run **Tools > Lost Worlds > Jungle > Play Portal Unlock Demo**. The existing Journey Demo window opens. Confirm the endpoint portal/frame are absent and “Enter portal / skip remaining tokens” does nothing while locked. P11 **Advance From Exit (Play Mode Test)** must also remain blocked.
3. In Journey Demo choose **Focus lever for E / hardware ACTIVATE (test)**. This binds a stationary reference and frames the lever; it creates no player/controller. Focus Game view and press **E**. Alternatively send the normal processed ACTIVATE command through the connected P10 adapter. Check handle motion, subtle sound, prompt change and portal reveal.
4. Or use **Activate portal lever (controller-free test)** without binding a reference. It exercises the same accepted interaction path. Repeated activation must not replay feedback.
5. Choose **Enter portal / skip remaining tokens**, or P11 **Advance From Exit**. Confirm Ruins loads once, retained token count is correct, and missing tokens never block entry. Other realms retain their original portals.
6. Restart Play: Jungle must be locked again. Check quiet audio and readable prompt at target resolution. Test P12 mute/reduced motion. With the future Person-1 controller, verify the broad early activation window at actual auto-run speed; movement/jump integration remains Person 1's responsibility.

Validation menus:
- **Tools > Lost Worlds > Jungle > Validate Portal Unlock Setup**
- **Tools > Lost Worlds > Jungle > Run Portal Unlock Checks**
- Existing **Tools > Lost Worlds > P13 > Run Final QA** and P12.5 regression tools.

## Reusing in another realm later

Add/configure `PortalUnlockMechanism` on that realm's activation object, retain an enabled nonblocking collider for interaction selection, assign its existing `RealmExitPortal`, optional handle/prompt/audio and `RealmPolish`, and assign the mechanism to that realm's `RealmBindings.exitGate`. Optional `revealObjects` can include a doorway frame. Keep the mechanism outside anything it hides. The mechanism must remain enabled until activation. Leave the other realms unmodified until explicitly authorized. No alternate loader is needed.

The generated chime is original local synthesis, with no downloaded audio. No Git push or Windows build. The existing Windows executable therefore retains its previous behavior until a separately authorized rebuild.
