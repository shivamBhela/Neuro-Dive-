# Validation results — 2026-10-16

| Realm | E-key checks | Processed hardware ACTIVATE checks |
| --- | ---: | ---: |
| Jungle | 29 passed | 31 passed |
| Ruins | 29 passed | 31 passed |
| Crystal Cave | 30 passed | 32 passed |

182 targeted checks passed. Each fresh realm confirms the stone gateway is visible, portal renderers/triggers are inactive and both portal/P11 entry paths reject premature entry. Frame visibility is checked throughout the locked wait and reveal animation. E or the actual P10 parser/P9 ACTIVATE path reveals the vortex; repeat activation is rejected and bound-reference entry advances to the expected next realm. Runtime warning/error monitoring passed.

- Existing RealmVortex validation passed for all three destinations, full-opening coverage and one visible surface per unlocked portal.
- P12.5 complete-journey regression: 191 checks passed, including token persistence/duplicates, optional-token completion, portal recovery and Reduced Motion.
- Final compilation: no C# warnings/errors.
- Byte-level reconstruction against the pre-task scene hashes proves the sole scene change in each realm is removal of its one stone-frame reference from revealObjects.
- Hidden City's scene hash remains unchanged. No runtime script, shader, material, mesh, trigger or player API was edited.

Tests ran in the isolated Unity validation copy. The three actual scenes and updated PortalUnlockValidation.cs match those tested files byte-for-byte. No environment generator, Git push or Windows build ran. No physical hardware or actual runner testing is claimed.

P14 reference/dependency/scene-order validation also passed; no build was produced.
