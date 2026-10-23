# Running path and token layout — 2026-10-16

Applied to the actual four realm scenes; verified byte-for-byte against the Unity validation copy. Scene differences are exclusively road transform scales and token position overrides. No objects/components, IDs, colliders, input, progression, gateway/trophy or runtime scripts changed.

- Jungle road: 7 → 8 m; Ruins/Cave: 8 → 9 m; Hidden City main promenade: 9 → 10 m. Existing ground supports widened surfaces; bridge geometry and 1.2 m gap unchanged.
- 36 tokens: Jungle 10, Ruins 8, Cave 8, City 10. Offsets are -2/0/+2 m perpendicular to the existing route. Short rows and alternating patterns; one optional 1.8 m-high accent per realm (other tokens 1 m above the floor). Existing generous collection radius unchanged.
- Layout validation: 3,485 supported corridor/body-clearance samples, token solid/visual overlap checks, unique IDs/counts; bridge 252 no-floor rays and 30 landing checks passed.
- Unity compilation: no C# warnings/errors. P12.5 journey 191 checks passed; P13 final QA 133 passed; P14 references/dependencies and Jungle road validation passed.
- Tests ran in the existing isolated Unity copy because the actual project would not open in a second Unity instance. Only validated scenes were copied back, after checking the actual files had not changed meanwhile.

Manual: stop Play Mode and reload scenes from disk. Inspect the widened single road and token patterns; run Tools > Lost Worlds > Layout > Validate Running Paths and Tokens. Use Tools > Lost Worlds > P13 > Play Final Test for controller-free collection/progression checks. Verify the first three locked/unlocked gateways and automatic Hidden City trophy finale.

Person-1 checks remain: actual lateral displacement, turning, speed, jump arc, elevated-token pickup and bridge-gap clearance. No runner was created or tested; the 2 m token offsets are layout targets, not movement configuration. No Git push or Windows build.
