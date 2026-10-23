# Jungle lever auto-run pause hooks

## Behavior

Jungle's existing lever now has `PortalAutoRunPause`. Its existing nonblocking interaction volume spans X=-6..6, Z=93..113, Y=2.63..6.63. A bound player reference entering the volume requests an auto-run pause before the lever at Z=103 and the exit. The zone is checked before default-order Update scripts. It does not need a Rigidbody, tag, name or collision event, and adds no solid obstacle.

The pause request stays latched without a timer, penalty or automatic cancellation on leaving the volume. The existing “Press E to Activate Portal” prompt and E/P10 ACTIVATE path remain. After successful one-shot activation, the lever/portal reveal finishes (0.8 seconds, or immediately with reduced motion), then the zone releases its request. Repeated activation cannot pause or animate again. P11 still blocks portal entry before activation and remains the only scene loader. Journey Tokens are unaffected.

This is an integration request, not a player controller: **without a Person-1 adapter consuming the hook, it cannot physically stop an auto-running player**. It never changes transforms, velocity, CharacterController, time scale or input architecture.

## Person-1 connection

- Keep calling `IntegrationSession.BindPlayer(playerReference, holdPoint)`.
- Add an adapter on that reference or one of its parents implementing `LostWorlds.Person2.Integration.IAutoRunPauseReceiver.SetAutoRunPaused(bool paused)`. BindPlayer discovers it automatically.
- Alternatively call `session.AutoRunPause.BindReceiver(adapter)` after BindPlayer, or subscribe to `PauseChanged` and immediately read `IsPauseRequested` to synchronize late binding. Rebind after each realm/player spawn; P11 clears the player binding at realm entry.
- Treat true as a separate reason to suspend forward auto-run. Keep ACTIVATE input, the interactor, hardware adapter and UI alive. On false release only this pause reason; do not override other controller pauses, menus or stop conditions.
- Apply the pause before advancing the player each frame. The zone uses execution order -100; explicitly poll `IsPauseRequested` before locomotion if Person 1 uses earlier execution or FixedUpdate. Keep the bound reference inside the configured vertical volume at normal ground height.
- Verify stopping distance, momentum, actual auto-run speed and reference height with the real player. The authored request begins 10m before the lever; the adapter should halt forward movement promptly and keep the player within the forgiving activation range. No controller was provided/changed in this checkout.

Reusable API: `RequestPause(owner)`, `ReleasePause(owner)`, `IsPauseRequested`, `PauseChanged`, and `BindReceiver(receiver)`. Separate owners are tracked, so one system releasing its request cannot cancel another. Duplicate requests are ignored. Scene changes clear obsolete requests; disabling/removing a zone releases its own request. A newly bound receiver receives the current state immediately. Add `PortalAutoRunPause` alongside a configured `PortalUnlockMechanism`/BoxCollider to opt another realm into this behavior later; other realms have not been changed.

## Controller-free test

1. Stop Play, reload the saved Jungle scene, then **Tools > Lost Worlds > Jungle > Play Portal Unlock Demo**.
2. Click **Focus lever for E / hardware ACTIVATE (test)**. This binds a stationary reference. Journey Demo should show **Auto-run: PAUSE requested — waiting for activation**. No player movement is fabricated.
3. Wait as long as desired. Portal/frame stay hidden and exit tests remain blocked.
4. Focus Game view and press **E**, or use **Activate portal lever (controller-free test)**. Alternatively send processed ACTIVATE through P10 as documented in ../PortalUnlock/VALIDATION.md.
5. The lever tilts and portal appears. After the reveal the pause status clears. Repeated E does not replay or pause.
6. **Enter portal / skip remaining tokens** loads Ruins through the existing P11 flow.
7. **Tools > Lost Worlds > Jungle > Run Portal Unlock Checks** runs the automated hook/E/entry tests. Real controller movement and stopping distance require Person 1's adapter.

No Git push or Windows demo rebuild.
