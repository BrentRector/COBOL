## Entry 982 — 2026-07-22 13:11 PDT — §24 fix-queue: EC-seam batch #3a — V4 EC-RANGE-SEARCH-INDEX + EC-RANGE-SEARCH-NO-MATCH LANDED (SEARCH scan restructure + latent <1 bug fix)

Increment A of the V4 EC-RANGE family (the batch's final finding; a 4-agent design-validation Workflow — `wf_a0883513-5f3`
— produced the seam spec + confirmed the blast radius before any code). The serial/SEARCH-ALL scan
(`ControlFlowEmitter.EmitSearchScan`) had ONE loop-top `if (idx > bound)` check that (a) never set an EC and (b) checked
only the UPPER bound. **Restructured** to a spec-shaped GR4/GR6/GR9 form: an INITIAL-index guard BEFORE the loop label
(serial: `idx < 1 || idx > bound` → EC-RANGE-SEARCH-INDEX; SEARCH ALL, index already forced to 1 by GR9: only the empty
table `idx > bound` → EC-RANGE-SEARCH-NO-MATCH), the WHEN loop, then an ADVANCE-past-end check after the increment →
EC-RANGE-SEARCH-NO-MATCH; both failure sites route to ONE shared `__searchAtEnd` label (the AT-END body emitted ONCE,
not duplicated). `BoundSearch` gains `CheckSearchIndex`/`CheckSearchNoMatch` (bind-time `Turn.Enabled` capture in
BindSearch; BindSearchAll hard-codes CheckSearchIndex=false per GR9); the ECs are nonfatal `ExceptionState.Set(...,false)`
(the F10/CONTINUE template), gated so the OFF path emits no `Set`.

**Latent-bug fix (mandated, no-deferral):** the `< 1` guard is emitted UNCONDITIONALLY — the old loop-top `> bound` check
let a zero/negative initial index fall through and read a PHANTOM scratch occurrence 0 (undefined COBOL that could
spuriously satisfy a WHEN). §14.9.37.4 GR4 makes a `<1` initial index immediately unsuccessful → AT END. This is the ONE
OFF-path runtime-behaviour change in the whole EC-seam batch (verified spec-correct; the comprehensive GnuCOBOL+NIST
differential must confirm no OTHER program's output moves — a real corpus program always SETs the index ≥1). Only the
`Set` calls are checking-gated.

Gate: golden `2023/ec_range_search` (successful search no-EC · SEARCH-INDEX from initial 0 AND initial >max · serial
NO-MATCH · SEARCH ALL NO-MATCH — all via `>>TURN EC-RANGE CHECKING ON` [the level-2 parent enables both level-3 names]
+ FUNCTION EXCEPTION-STATUS) · characterization **33/33** with EXACTLY ONE re-baseline (`char_occurs.85.g.cs.txt` — the
sole snapshot with a real `__search` label; runtime output byte-identical, it SETs IX TO 3 and finds occ 4) ·
SEARCH/occurs/ec_range Conformance **52/52** · greenfield Unit **571/571**. Observability note: a nonfatal SEARCH range
EC sets the last-exception status (GR1b takes AT END when present) but does not itself invoke a USE declarative —
consistent with the other inline nonfatal sets (a documented known gap). ⏭ Increment B = EC-RANGE-PERFORM-VARYING
(fatal); C = EC-RANGE-INVALID. The comprehensive gate runs before the EC-seam batch merges.

