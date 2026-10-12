## Entry 423 — S4: OCCURS slice 2 (numeric tables) — BLOCKED on the byte path's quirky VALUE-on-OCCURS init; refactor kept

Started numeric OCCURS tables (`long[]`/`decimal[]`). The codegen side was trivial — the three value-access
primitives index generically (slice 1), so the emitter/resolver already handle numeric elements; only the Binder
OCCURS branch needed extending. Factored the numeric-classification core out of the two standalone predicates into
`ClassifyTypedNumeric` (DRY; both `IsTypedUnsignedInteger`/`IsTypedDecimal` now delegate — byte-identical, the
numeric flip tests confirm) and pointed the OCCURS branch at it.

Then a `--typed-fields` probe caught a **byte-identity divergence that is the byte path's, not the flip's**: the byte
engine's **VALUE-on-`OCCURS` init is quirky and self-inconsistent**. `05 N PIC 9(3) OCCURS 3 VALUE 7` DISPLAYs `000`
(the value *ignored*, zero-filled) on the byte path; the same item *without* VALUE DISPLAYs spaces (empty); a scaled
`OCCURS … VALUE` element is inconsistent across occurrences (one shows `000`, another behaves as if `1.5` were
applied). A typed `long`/`decimal` cannot be initialized to match that incoherent behavior. Per `feedback_diff_is_a_bug`
this is a byte-engine `OCCURS`+VALUE **correctness** question to settle *before* the numeric-table flip — so numeric
OCCURS is **deliberately not flipped** (the Binder branch stays char-only with a comment; the divergent numeric arm
was reverted). Char tables are unaffected and proven byte-identical: the byte path inits `OCCURS` char to spaces,
which the typed spaces-init matches exactly (verified uninitialized DISPLAY → empty on both, and `IF C(1) = SPACES`
→ true on both).

Net: kept the `ClassifyTypedNumeric` refactor; OCCURS stays char-only (slice 1); design §9.5 updated with the blocker.
19 flip tests, all flag-on≡flag-off. Guard **ALL GREEN: 1196 / 500 / 364**. NEXT options: (a) reconcile/fix the
byte-path `OCCURS`+VALUE init (unblocks numeric tables), or (b) move to nested groups / Stage-4 pointers/OO while
numeric OCCURS waits — owner steer welcome.

