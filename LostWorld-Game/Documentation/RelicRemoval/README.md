# Final design — Journey Tokens only

The Verdant Seed, Sun Seal and Amethyst Prism system has been removed. There is no relic tracker, persistence, collection logic, prompt, counter, demo view or focus-ring effect. The three relic prefabs, five dedicated materials, tracker asset, three runtime scripts, collectible assembly and P5 authoring/collection tests are retired.

## Preserved functionality

- 36 optional Journey Tokens: Jungle 10, Ruins 8, Crystal Cave 8, Hidden City 10. IDs, transforms, trigger geometry, prefab, ledger behaviour, HUD and duplicate protection are preserved.
- Environmental GRAB/ACTIVATE and success feedback; all four realms, obstacles, route framework, portals, effort/rest and hardware commands.
- P9 CommandChannel and P10 parser/transport are unchanged. LEFT, RIGHT, JUMP, GRAB and ACTIVATE still use the same contracts.
- Person-1 binding, player-command callbacks/UnityEvents, branch data, realm arrivals and transition/completion APIs are unchanged. The retired relic-only IntegrationSession.Relics property is intentionally removed. No controller or movement/jump code was added or changed.
- Completion is still Jungle → Ruins → Crystal Cave → Hidden City → Journey Complete, with no collectible requirement. Tokens remain optional and are the only collectible tally.

## Optimizations

Removed three realm collectible stations (including dedicated references, cameras and input components), twelve collectible demo stations across P7–P10, three decorative focus rings and the relic panel from the shared HUD. The world inventory drops by 22 renderers and six colliders. The unused collectible assembly and four assembly dependencies are removed. HUD/session code no longer subscribes to or stores the retired tracker; demo drivers no longer poll or select relic views. Shared P4 interactions, token effects and portal particles are retained.

## Manual Unity verification

1. Stop Play, let Unity recompile/import, then reopen Jungle_Prototype from disk. Do not save an older cached scene over the delivered scene.
2. Run **Tools > Lost Worlds > P5 > Validate Relic Removal**. This scans all eight realm/demo scenes and all prefabs for retired objects, UI, missing scripts and missing object references. It also verifies 36 tokens and four portals.
3. Run **Tools > Lost Worlds > P13 > Run Final QA**, then **Tools > Lost Worlds > P14 > Validate Demo Build**. Check Console results.
4. Run **Tools > Lost Worlds > P13 > Play Final Test**. No 0/3 counter or relic prompt should appear. In the Journey Demo window, use **Collect remaining realm tokens (test)** and **Enter portal / skip remaining tokens**. The tally should reach 10 → 18 → 26 → 36 and survive each transition. Journey Complete must appear once. Restart and skip tokens to confirm they never gate completion.
5. In **P7 > Play HUD Demo**, click Game and use **F1**, **G**, **E** for environmental actions; **F3**, **A/D or arrows** for the standalone route station. F2 is retired. Repeated interactions/choices should not produce duplicate feedback. Check 1280×720 and 1920×1080 layouts.
6. **P8 > Play Effort/Rest Demo** retains Page Up/Page Down/Backspace. REST feedback must not block interactions. **P9 > Play Hardware Simulator Demo** retains A/D/arrows, Space, G/R and E; F1/F3 select its test stations.
7. **P10 > Play Hardware Adapter Demo**: F4 connects the local tester; F5–F9 send LEFT/RIGHT/JUMP/GRAB/ACTIVATE, F10 invalid input, F11 disconnects. Use F1 for interaction tests and F3 for route tests. The external format remains newline-delimited JSON such as {"command":"GRAB"}.
8. Inspect all four realms and portals; no relic geometry or focus ring should remain. Jungle stays one straight main road. Hidden City retains its route choice. Actual player collider/jump/camera checks still require Person 1.

P5's old setup/collection menus are removed. The new removal audit also runs inside P13 and P14. Older phase validation reports/screenshots are historical; use this folder for current evidence.

## Results and build

676 Editor behaviour checks plus 91 actual Windows-player checks passed, along with structural/obstacle validation. See [validation details](VALIDATION.md) and [files changed](FILES_CHANGED.md). Build succeeded with zero errors and three documented Unity build warnings; final runtime tests reported zero warnings/errors.

Run the refreshed `Builds/LostWorldsSIH/RunDemo.cmd`. Its demo panel has environmental GRAB/ACTIVATE, routes, tokens, portals and effort controls; no relic button/counter. All 207 build files match the tested artifact. No Git push.
