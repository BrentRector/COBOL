## Entry 189 — 2026-05-28: INSPECT multi-counter TALLYING + signed-numeric de-sign — NC216A 100%

Completes NC216A (5→0 FAIL*) on top of the single-pass engine (Entry 187). Two fixes,
the first an approved grammar change:

**Multi-counter TALLYING (grammar, approved).** `inspectCountPhrase`'s adjective is
optional because ALL/LEADING are transitive across following bare operands (GR 10) —
`FOR LEADING "S" "S" "T"` is three operands under one counter. That bare form collides
with the next counter in `c1 FOR ALL x  c2 FOR ALL y`: the parser greedily swallowed `c2`
as a pattern of `c1`, so every tally landed in `c1`. (My first attempt — making the
adjective mandatory — was wrong: it rejected the valid transitive form NC216A itself uses.)
Resolved with a semantic predicate `{IsBareInspectOperand()}?` on the bare alternative:
a data-name immediately followed by `FOR` is the next counter, so the bare alternative
declines it and the count-phrase loop ends. Fixed F3-19.01/.03/.04 and F1-27.

**Signed-numeric de-sign (runtime, spec-mandated).** ISO 14.9.22 GR 4d: a signed numeric
item is inspected "as though moved to an unsigned numeric item" — operational sign removed,
absolute digits examined. `S9(5)` `-12345` is stored with a trailing overpunch (`1234N`),
so `TALLYING ... FOR ALL "5"` found 0; the spec requires it to see `12345` → 1.
`InspectRuntime.ReadInspectTarget` now de-signs a signed DISPLAY identifier-1 via
`DecodeNumeric`/`FormatNumericForDisplay` (abs value, TotalDigits wide); the stored sign is
untouched. `TallyingPass` takes the target PIC; the emitter passes it. Fixed F1-23.02.

NC216A: 57/57, 0 FAIL*. (REPLACING-on-signed de-sign, with sign retention on write-back,
is not yet implemented — no NIST coverage; noted for later.)

---

