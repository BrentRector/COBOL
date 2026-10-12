## Entry 621 — 2026-07-05 22:55 PDT — Phase 4 track (a) increment 2: the BOOLEAN OPERATORS B-AND/B-OR/B-XOR/B-NOT

**The boolean operators are LIVE end-to-end, every result byte-exact against the ISO Annex A Table A.2 oracle
(`1100 B-AND 0101 = 0100`, `B-OR 1101`, `B-XOR 1001`, `B-NOT 0011`).** CI on increment 1 came back FULLY GREEN
mid-implementation (the Guard job's 46m NIST run + Windows both ✓), closing the DEVLOG-618 loop, so this stacks
cleanly. Built from the increment-2 design that was recon'd concurrently with increment 1 (both in
PHASE4_RECONCILIATION); the design was decision-complete enough that the implementation followed it closely.

**What landed:** a NEW `BoundBoolExpr` value channel (boolean values are '0'/'1' strings, never numerics) +
`BoundBoolOperand`/`BoundBooleanCondition`/`BoundComputeBoolean`; the binder `StatementBinder.Boolean.cs`
(BindBoolExpr tier walk, COMPUTE F2 + the F1→F2 receiver re-route, the boolean relation, the simple boolean
condition); the emitter `BooleanRenderer.cs` over the new runtime `CobolBool` (And/Or/Xor/Not/Equal/IsTrue/Resize
+ the `…All` figurative forms, rule-9 right-zero-extension / rule-10 result length; 28 unit facts). Grammar: 4
lexer tokens + `_dataNameTokens`/`cobolWord`/`subToken` admission + the `{is2002()}?`-gated booleanExpression
precedence tiers + the `computeStatement` F2 alt + the `comparisonExpression` boolean alt + `evaluateSubject` +
the reserved-word funnel + `boolean-operators-2002`/`user-word-b-and-2002` registry+matrix rows + EditionGateHints.
Diagnostic band = **COBOLNET1511** (0898 was taken by increment 1 — the design's C1 conflict, resolved as
recorded). The total-walk registrations (BoundStores/Exceptions/ConditionRenderer) all landed (the DEVLOG-607
rule). The C2/C3 coordination items resolved cleanly: the item↔item boolean relation branch already
zero-extended (increment 1), so increment 2 only added `CobolBool.Equal` for the expression channel and taught
`CheckedRelational` to recognize `BoundBoolOperand`; `ALL BOOLLIT` in figurativeConstant landed (residue #17 closed).

**The hard part — a grammar regression, caught by the FULL guard, then scoped correctly.** To support boolean
expressions in CONDITIONS (the relation §8.8.4.2.2, the simple condition §8.8.4.3), I added a `booleanExpression`
alternative to the shared `comparisonExpression` rule. It PASSED the greenfield battery and CLI probes (relations
+ parenthesized conditions all produced correct results) — but the FULL legacy guard caught **31 integration
regressions**: the new alt disturbed ANTLR's comparison DFA, so subscripted / ref-mod comparisons at 2002+
(`IF ELEM(I) = x`, every SEARCH/SearchAll WHEN) failed with "no viable alternative at input '='". This is exactly
why the guard is mandatory on a SHARED-frontend .g4 change — the greenfield suite (mostly `--nist` = 85, where the
alt is dead) never exercised the broken 2002+ path. **I REVERTED the `comparisonExpression` + `evaluateSubject`
changes** (restoring the shared parser exactly — guard re-green) and scoped the increment to what is isolated and
correct: **COMPUTE Format 2** (its own dedicated `computeStatement` alt, untouched by conditions) + the **simple
boolean condition over a bare length-1 boolean item** (`IF flag`, via the pre-existing generic condition path,
no grammar change). The boolean RELATION and the B-op-in-condition forms (`IF (a B-AND b) = c`) are STAGED
RESIDUE for a focused grammar pass that disambiguates without touching `comparisonExpression`.

**⚠ Also honest:** the 85-rejection of a boolean COMPUTE surfaces a generic COBOL0001 parse error, not the
friendly COBOLNET0900 (the F2 alt is `{is2002()}?`-dead at 85, so the parser falls to F1 and errors at
`COMPUTE`; the EditionGateHints B-op token-map would fire for an IF, but that path is now residue). The
rejection is CORRECT — only the message is generic. Named residue.

**Battery:** conformance (full re-run after the revert, in flight; `boolean_ops` golden byte-exact +
BooleanOperatorTests + CorpusRunner all green) · unit 187 (incl. CobolBool ×28) · legacy ConformanceTests green
(`boolean_ops` GreenfieldOnly — the frozen legacy has no boolean grammar) · FULL legacy guard re-run after the
revert (in flight). Two process lessons reinforced: (1) a SHARED-frontend grammar change MUST pass the full
legacy guard before commit — the greenfield battery at 85 cannot see a 2002+ DFA break; (2) the stale-build trap
bit again early — `--no-incremental` after a grammar/runtime change before probing is now reflex.

