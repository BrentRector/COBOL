## Entry 573 — 2026-06-11 01:07 PDT — Phase 1 Wave 2 (two parallel agents): the LINAGE subsystem + the IC residue → 11 programs locked (1007 conformance; 314 NIST; legacy guard 353 MATCH + 11 DIVERGENT, 0 regressions)

**Phase 1D — LINAGE (ISO §13.18.34 + §8.4.3.14 + §14.9.51 EOP; 5 programs).** The logical-page counter state
machine landed in the sequential connector (the GR7 advancement rules ported from the legacy's proven
algorithm), with ONE closure-based page evaluator covering the literal and data-name forms (GR6a/GR6b) —
re-evaluated at exactly OPEN OUTPUT / WRITE ADVANCING PAGE / page overflow, applying to the NEXT page
(GR6b1–3); the END-OF-PAGE vs page-overflow discrimination per GR26a/b (footing-zone EOP vs the overflow
advance, incl. the c==body ⇒ footing case); `BoundLinageCounterRef` as the register channel (qualified
`LINAGE-COUNTER OF file` + ambiguity diagnostics COBOLNET0860–0864); the WRITE AT/NOT-AT EOP bind (the silent
phrase-drop the scout found was a loud-failure violation). 18 per-GR spec-pinned facts in
`LinageConformanceTests`. **Three goldens re-baselined over verified legacy holes** (the twice-verified
evidence package at `/e/tmp/phase1-out/linage/`; legacy==old-golden byte-confirmed independently): SQ101M —
the legacy text-parsed WRITE ADVANCING identifier bytes (S99 overpunch/COMP → advance 0; §14.9.51 GR25a says
the VALUE governs); SQ208M/SQ210M — the legacy evaluated data-name LINAGE operands ONLY at OPEN (the GR6b
re-evaluation hole; pages stuck at 66). PASS/FAIL/summary rows identical to the old goldens in all three;
every changed line classified.

**Phase 1F — the IC residue + NC105A (6 programs; the IC suite is COMPLETE at 23/23 goldens).**
(A) the COBOLNET0881 gate was WRONG: CALL … ON EXCEPTION is ANSI X3.23-1985 Format 2 (CCVS-85 IC222A tests
it) — lifted; (B) §14.9.25.4 GR4 group-sender MOVE arms (NC105A's last rows); (C) the §14.2.3 GR8
full-allocation ODO CALL carrier (IC207A); (D) EXTERNAL FD run-unit-shared connectors — the `::EXT::`
registry band, one connector per run unit whose open mode/position persists across activations
(§13.18.22.4 GR4a; IC227A); (E) cross-program GLOBAL USE — GLOBAL FDs merge into contained programs'
FilesByName (§13.18.30) and `__IoCheck` falls through to the containment chain's `__RunGlobalUse`
(§14.9.49 GR4b; IC233A/IC234A); (G, owner-approved) the cross-assembly dynamic-CALL sibling-assembly probe —
IC117M→IC118M and IC109A→IC110A→IC111A resolve their separately-compiled callees (those five stay
no-golden/run-only by NIST design). IC113A diagnosed as a CONFORMING infinite loop (EXIT PROGRAM in a main =
CONTINUE per §14.9.14 GR2 over zeroed LINKAGE) — no fix, documented. IC401M needs the one FRONTEND fix
(programIdParagraph) — queued for the guard-gated frontend pass.

**Locked: SQ101M/SQ201M/SQ208M/SQ209M/SQ210M + IC222A/NC105A/IC207A/IC227A/IC233A/IC234A. 1007 conformance +
16 unit green; 314 NIST locked. Legacy guard: 353 MATCH + 11 LEGACY_DIVERGENT, 0 regressions, ALL GREEN.**

