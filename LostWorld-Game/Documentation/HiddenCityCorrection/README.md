# Hidden City: ACTIVATE-only finale correction

The existing trophy/pedestal now sits at the former portal origin **(0, 0.15, 86)**. The final portal object, portal component and colliders are disabled; SanctuaryColonnade (its frame) stays inactive. Other gates, plaza and city scenery remain. All ten City tokens remain optional.

Hidden City alone opts into `RealmBindings.CompletionOnActivation`. `TryAdvance` rejects this realm even after activation, so neither exit polling nor portal/test entry can complete it. The existing console animates and softly illuminates the trophy, then calls the guarded P11 `TryCompleteActivation` API after its accepted reveal finishes. The existing completion observer awards once and displays actual X/36. No second loader or reward framework.

The console's pause owner releases before completion. The existing finale immediately holds a separate completion pause, preventing an integrated runner from continuing behind the screen. Replay clears these requests; Finish retains the session stop. Other realms' portal logic is unchanged.

## Manual Unity checks

1. Exit Play Mode and reload HiddenCity_Prototype. Run **Tools > Lost Worlds > Hidden City > Play Finale Demo**.
2. Confirm the trophy/pedestal is visible at the old portal location, with no portal/frame. In Journey Demo, collect any desired number of optional tokens **before** activating.
3. Click **Focus lever for E / hardware ACTIVATE (test)**. Confirm the console prompt and pause request; wait as long as desired without completion.
4. Focus Game and press **E**, or click **Activate trophy / complete journey (test)**. The console animates, trophy illuminates, and Journey Complete appears automatically. There is no final Enter Portal button/action.
5. In a fresh run, enable the existing P10 adapter and send `{"command":"ACTIVATE"}` after focusing the console. Expect the same result. Repeat commands must not award twice.
6. Check the golden trophy, title, encouragement, 4/4 realms and actual collected X/36. Verify 720p/1080p, Mute and Reduced Motion. Missing tokens have no penalty.
7. Click **Finish Session**: the safe-to-close message appears once. Click **Replay Journey**: Jungle loads with zero tokens and its original locked portal. No application exit is forced.
8. Run **Tools > Lost Worlds > Hidden City > Run Finale Checks** and **Tools > Lost Worlds > P13 > Run Final QA**. For a full controller-free journey, use the first three realms' normal ACTIVATE + portal entry, then only ACTIVATE in Hidden City.

Person 1 must still bind the player reference and consume pause/resume hooks. Actual runner movement, stopping distance and jumping are not claimed as tested. No Windows executable was rebuilt and nothing was pushed.
