## Entry 883 — 2026-07-17 18:01 PDT — PHASE-12 waves 4+5 — pointer §-fixes + the >>PROPAGATE introduction gate + the TYPE TO re-anchor (Steps 8-11)

Two lighter waves combined into one commit — the pointer residues (Step 8), the `>>PROPAGATE` directive gate
(Step 9), the `TYPE TO` re-anchor (Step 10), and the doc sync (Step 11). The re-scout had already shown Step 8's
pointer work was mostly DONE (PROGRAM-POINTER live since P10, FUNCTION-POINTER correctly staged loud), so it
reduced to the corrections below rather than the plan's full implementation.

**Step 8 (pointers) — the residue corrections:** fixed the three §8.5.2.7→§8.5.2.15 code-comment errors the
re-scout flagged (program-pointer category is §8.5.2.15; §8.5.2.7 is function-pointer category) at
`ProgramPointer.cs:6`, `PicInfo.cs:41`, `PicInfo.cs:96`. The genuine remaining pointer work — the FUNCTION-POINTER
runtime carrier + SET Format 8, the restricted PROGRAM-POINTER `TO`-prototype form, and the ISO `ADDRESS OF
PROGRAM/FUNCTION` spellings — stays STAGED LOUD (COBOLNET0899, correct, not silently wrong) and is documented as a
residue (it dovetails with the P13 function-prototype work).

**Step 9 (`>>PROPAGATE`) — the introduction gate:** the re-scout inverted the plan's "re-edition to ≤2014"
premise — §7.3.21 is LIVE in the 2023 spec (Annex E lists no removal), so the correct action is an INTRODUCTION
gate, not a top-end span. Implemented via the proven `>>TURN` pattern: `PropagateDirectiveProcessor` (a new
line-count-preserving preprocessor stage) recognizes `>>PROPAGATE ON|OFF`, edition-gates it below its
introduction edition with **COBOLNET0883** (">>PROPAGATE requires --std 2002+"), validates the ON/OFF operand
(§7.3.21.2), and blanks the line. Provisional COBOL-2002 (the roadmap decision-1 policy — the 2002-vs-2014 edge
cannot be pinned from the 2023 text or the VCR; §7.3.21 is 2002-era EC-directive facility, same era as `>>TURN`).
`ConditionalCompilationProcessor` gained a `leavePropagateDirectives` flag (the `leaveTurnDirectives` twin) so the
directive survives to the new stage — PROPAGATE STAYS in `KnownIgnoredDirectives` so a legacy caller (no flag)
keeps consuming it (verified: the legacy `CobolSharp.Compiler` references `Cobol.Net.Frontend`, so it shares the
stage and handles `>>PROPAGATE` identically). **The RUNTIME propagation SEMANTICS (GR1/GR2 — actually driving EC
propagation) remain the deferred PHASE-13 EC-remnant work** — this wave delivers recognition + the edition gate,
resolving the P9/P13/P12 ownership question: P12 owns the gate, P13 owns the semantics.

**Step 10 (`TYPE TO`) — the re-anchor:** the re-scout showed plain `TYPE TO` is the TYPE clause's OPTIONAL word
(§13.18.57.2 Format 1, `TYPE [TO] type-name`, already covered by the live TYPE clause), NOT a pointer form. The
genuine "pointer-target" deferral is the RESTRICTED data-pointer `USAGE POINTER TO type-name` (§13.18.60.2 / Annex
D.9.2.2). Catalogued it as the pending matrix row `usage-pointer-to-type-2014` (never a silent gap), and fixed the
mislabel + the swapped TYPE(§13.18.57)/TYPEDEF(§13.18.58)/SAME-AS(§13.18.49) citations in the three docs
(`ISO2023_CONFORMANCE_PLAN.md`, `COBOLNET_DATA_MODEL_DESIGN.md`, the P12 §7 table).

**Step 11 (doc sync):** stale hardcoded battery counts (3166/281) in `DESIGN-codegen-backend.md` /
`DESIGN-data-model.md` replaced with count-free phrasing; the plan STATUS + step checkboxes updated.

Tests: `tests/conformance/2014/propagate_directive` (byte-compared; NOT GreenfieldOnly — the legacy shares the
Frontend and handles it identically, verified by a legacy compile); `PropagateDirectiveTests` (6: recognized at
2002/2014/2023, COBOLNET0883 below 2002, malformed-operand reject). Full battery green + FULL legacy guard (NIST
353 MATCH — shared preprocessor changed). Deferred residues (documented): the external-float `E` PICTURE
(§13.18.40.4 GR13b, staged 0899), the FUNCTION-POINTER runtime + restricted PROGRAM-POINTER + `ADDRESS OF`
spellings (staged 0899), and the `>>PROPAGATE` runtime semantics (P13). Next: wave 6 = the adversarial review +
phase close (Steps 12-13).

