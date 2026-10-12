## Entry 1191b — 2026-08-06 14:11 PDT — PB51: COMPUTE gained the F2→F1 re-route mirroring the F1→F2 one it already had, and the ambiguity survey the entry demanded is what made the grammar change unnecessary

> ⚙ **RECONSTRUCTED 2026-08-07 from the primary sources — the commit message (`1838a304`), its `kb/Work/` note and its diff — not written at the time.** The commit landed without a DEVLOG entry; the owner's call was to reconstruct where it can be done accurately. Every fact below is carried from those sources; nothing is recalled. **Lettered rather than renumbered** because entries 1192–1198 are already referenced by pushed commit messages, which cannot be edited.

**`COMPUTE <numeric> = ZERO` was REJECTED** with "a boolean COMPUTE expression shall not consist solely of an ALL
literal (ISO §14.9.8 Format 2 SR3)" — a diagnostic naming a construct the source does not contain, on a statement
§8.8.1.1 makes legal arithmetic: "An arithmetic expression may be an identifier referencing a numeric data item,
a numeric literal, **THE FIGURATIVE CONSTANT ZERO** (ZEROS, ZEROES), …".

⭐ **THE ENTRY PROPOSED RETIRING `ZeroTokenRewriter` FOR A GRAMMAR ALTERNATIVE, AND DEMANDED AN AMBIGUITY SURVEY
FIRST. THE SURVEY IS WHAT MADE THE CHANGE UNNECESSARY.** Sweeping every bare-ZERO position:

```
COMPUTE N = ZERO / ZEROS                                       ⛔ rejected
ADD / SUBTRACT / MULTIPLY ZERO · the GIVING forms · IF · MOVE  ✅ already accepted
COMPUTE N = ZERO + 3 · FUNCTION ABS(ZERO)                      ✅ already accepted
```

Every other arithmetic position already worked — those verbs carry their own operand rules admitting the
figurative. **COMPUTE is the ONE verb whose RHS is a bare `arithmeticExpression`.** So the entry's premise
("primaryExpression cannot express §8.8.1.1's figurative ZERO") is TRUE and was still the wrong basis for sizing
the work: the blast radius is a single construct, not the grammar
(`feedback_validate_the_premise_not_only_the_rule`).

**THE FIX IS THE MIRROR OF ONE ALREADY PRESENT.** `ArithmeticBinder` carried an F1→F2 re-route (`COMPUTE bool =
bool` parses as Format 1 because a sole-identifier RHS predicts the arithmetic alt), documented as "the ANTLR
alternative-order reality precedent". The F2→F1 direction was simply absent. A Format-2 parse whose source is a
BARE ZERO and whose receiver is not boolean is Format 1 with the value 0 — §8.3.3.6.4 GR4, "the numeric value
'0' … depending on context", and a numeric receiver IS that context. No grammar touched, no ambiguity introduced.

⚠ **THE TEST IS ON THE PARSE TREE, NOT THE BOUND NODE, and that is forced.** `BindBoolExpr` normalises a
figurative ZERO to `BoundBoolAll("0")` — the same node `ALL B"0"` produces — so by bind time the two are
indistinguishable, which is exactly why SR3 fired citing a construct the source lacked. `SoleFigurativeZero` asks
what the SOURCE wrote, so `ALL ZERO` and `ALL B"0"` keep their verdicts (§8.3.3.6.3 SR1a — "the only figurative
constant permitted is ZERO … WITHOUT the ALL phrase").

**VERIFIED:** all three spellings (ZERO/ZEROS/ZEROES) → 0; ROUNDED, ON SIZE ERROR and multiple receivers all
preserved through the re-route; `COMPUTE N = ALL ZERO` still rejected; every control (`ZERO + 3`, `ADD ZERO`,
`MOVE ZERO`) unchanged.

⚠ **RESIDUE, RECORDED NOT SILENTLY WIDENED:** `COMPUTE <boolean-item> = ZERO` still reports COBOLNET1511. The
re-route excludes a boolean receiver ON PURPOSE — there GR4's boolean reading is the right one and Format 2
genuinely applies — so that behaviour is unchanged rather than newly decided. But whether SR3's "an ALL literal"
should bar a bare ZERO at all is a separate reading question: §8.3.3.6.2 Format 1 is `[ALL] ZERO`, and SR3's
phrase most naturally means the Format-6 `ALL literal-1`, which a bare ZERO is not. **PB50 (a bare ZERO in a
SUBSCRIPT or ref-mod position) is untouched and remains the one genuinely open member of this family.**

⚖ **EXTERNAL ORACLE:** unlike PB46's INVOKE, this construct IS in GnuCOBOL's reach — the differential ran over
1,323 real programs and reports 0 per-case flips, so nothing that previously compiled changed verdict.

**GATE.** `=== BATTERY: ALL GREEN ===` on ONE whole run — FULL greenfield Conformance **4222/4222** zero skipped ·
Unit **3986/3986** · characterization **33/33** · guard-fast ALL GREEN with NIST 353 MATCH / 0 REGRESSION and
NIST AUDIT CLEAN · GnuCOBOL differential 1323 cases, **0 PER-CASE FLIPS**. Golden
`pb51_compute_figurative_zero` (9 assertions incl. the three controls that already worked).

