# Hidden City and Journey Complete finale

**Superseded finale interaction (2026-10-16):** ACTIVATE now completes directly, with the existing trophy at the retired portal position. The portal/frame remain disabled. Use [the corrected manual steps](../HiddenCityCorrection/README.md). The original portal-entry notes below are historical.

HiddenCity_Prototype retains its original plazas, wide routes, ten optional tokens and four jump obstacles. Local PBR stone/paving, bronze details, six monumental towers, guardian statues, arch details and sunset light distinguish it from the other realms. The golden trophy is a visible, non-interactive reward display on a decorated pedestal beyond the final portal. All added decoration is collider-free; no other realm scene is modified.

## Final console

The console at (5, 0, 78) clones the already-tested Ruins mechanism, rebinding it to Hidden City's original portal and SanctuaryColonnade frame. Both start inactive. Existing E/P10 ACTIVATE, PortalUnlockMechanism, PortalAutoRunPause and P11 exit gating handle the entire interaction. Wait indefinitely; one accepted activation reveals the portal and releases the pause after the gentle reveal. No tokens, GRAB, keys or precision interaction are required. Completion still comes solely from P11's final-portal API.

## Finale and controls

JourneyFinale observes IntegrationSession.Completed, awards its trophy once, and creates the completion overlay with the requested title, encouragement, 4/4 realms and actual token snapshot X/36. It uses a locally generated trophy icon, eight lightweight UI spark images and the existing quiet chime. Gentle trophy scaling/sparks respect Reduced Motion; sound respects Mute. The default volume is 0.06. No extra world particle systems, lights, camera or realm are added.

- **Replay Journey** calls the new completion-only IntegrationSession.ReplayJourney API, using its existing scene loader. ReplayStarted resets JourneyLedger with reentrant-collection protection. The single persistent session loads a fresh Jungle, with zero tokens, reset completion and a locked starting portal; PlayerReference is cleared for normal Person-1 rebinding. Effort feedback is retained, never used as a penalty.
- **Finish Session** calls completion-only FinishJourney. It is idempotent, marks the session finished, holds the auto-run pause and shows a safe-to-close message. It deliberately does not kill Unity or the application. Replay remains available afterward.
- Premature or duplicate replay/finish calls are rejected. Trophy presentation is never awarded by console activation or token collection. Replay unloads the completed scene and resets the scene-local reward for a new journey.
- Controller-free build-demo controls hide while the completion overlay is displayed, avoiding overlap; they return after replay. The existing Editor Journey Demo remains available.

## Exact manual Unity steps

1. Stop Play, reload HiddenCity_Prototype, and choose **Tools > Lost Worlds > Hidden City > Play Finale Demo**.
2. Inspect the city/trophy. Click the shared **Focus lever for E / hardware ACTIVATE (test)** button; it focuses the console. Confirm pause requested and hidden portal/frame. Try entering: completion must remain blocked.
3. Wait, then focus Game and press **E**. Alternatively use **Activate portal lever (controller-free test)** or send processed ACTIVATE through the existing P10 adapter. Confirm reveal and pause release. Repeating activation has no effect.
4. Collect any number of tokens using the existing test buttons, then **Enter portal / skip remaining tokens**. Confirm “YOU DID IT! — JOURNEY COMPLETE”, trophy, encouragement, 4/4 and the correct token count (direct City demo can have fewer than 36).
5. Test mouse/keyboard button navigation. **Finish Session** shows the closing message and is accepted once. **Replay Journey** then loads Jungle with zero tokens and its portal locked. Repeat clicks must not create extra loads or rewards.
6. Also test a full journey from Jungle using the existing controller-free demo: activate each realm mechanism, then enter its portal. Missing tokens never block completion.
7. Run **Tools > Lost Worlds > Hidden City > Run Finale Checks**, then **Tools > Lost Worlds > P13 > Run Final QA**. Inspect Console, 720p/1080p layout, mute/reduced motion and target-PC performance.

The city and icon are already generated/saved. **Validate Final City** checks authored wiring; **Apply Final City Upgrade** preserves an already-generated scene.

## Person-1 integration

No runner/controller/camera/jump code was created or modified. Person 1 must consume the existing IAutoRunPauseReceiver request, bind after RealmReady, and manage actual stopping/arrival/spawning. Finale also holds a pause at completion. ReplayStarted and SessionFinished are optional lifecycle notifications; CommandsReady remains false on completion/finish. Actual running, braking distance, jump/collider fit and physical hardware were not tested. No Git push or Windows build.
