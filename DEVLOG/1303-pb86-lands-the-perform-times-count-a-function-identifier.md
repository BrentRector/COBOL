## Entry 1303 — 2026-08-18 03:25 PDT — PB86 lands: the PERFORM … TIMES count — a function-identifier evaluates, SR2 is enforced, and every integer-identifier read lands by VALUE (PB87 found under it: ADVANCING ZERO advanced one line)

**PB86 — the count.** `PERFORM COUNT-IT INTEGER(3.7) TIMES` (the keyword-omitted intrinsic under FUNCTION ALL
INTRINSIC) ran the body ONCE: `ControlFlowEmitter.CountExpr` had arms for a literal, a field and an error operand and
a `_ => "1"` default that swallowed the computed operand. `PERFORM COUNT-IT FUNCTION INTEGER(3.7) TIMES` was a
parse error — `performTimes: (integerLiteral | dataReference) TIMES` had no `functionCall` arm, alone among the
identifier slots the grammar admits a function-identifier in. And §14.9.28.3 SR2 "Identifier-1 shall be an integer"
was not enforced at all: `PERFORM P X TIMES` with `X PIC 9V9 VALUE 1.2` compiled and iterated the item's UNSCALED
digits — 12 times.

- **Grammar:** `(integerLiteral | functionCall | dataReference) TIMES` — functionCall first (it begins with the
  FUNCTION token; the DISPLAY / INSPECT / FROM slots have the same shape). §8.4.3.2.4 GR1: a function-identifier is
  an identifier referencing a temporary item; GnuCOBOL (`id_or_lit_or_func TIMES`) and IBM evaluate it.
- **Binder:** `CountOperand` binds the function through `IntrinsicBinder` and enforces SR2 for EVERY shape through
  the ONE integer classifier — `IntrinsicResultType.IsIntegerOperand`, made public: a scale ≤ 0 numeric elementary
  item that is not USAGE INDEX (a trailing-P picture has no digit position right of the decimal point — it IS an
  integer, and the first cut rejected `PIC 9P` before the probe caught it), or a function whose RESOLVED type is
  integer (§15.2 type 5). **COBOLNET1646** `perform-times-count-not-integer`, all four editions.
- **Emitter:** the count (GR7 — determined once) is a literal verbatim or, for every other operand,
  `(long)(NumericRenderer.Align(num.AsNum(count), 0))` — the ONE integer landing, so a P-scaled or unsigned-wide
  item counts by VALUE (`PIC 9P VALUE 20` → 20, not its stored digit 2). No default.
- **The sweep — every "integer identifier read raw" site:** the GO TO … DEPENDING selector (`(int)(x.Expr)`), WRITE
  … ADVANCING identifier LINES (`LinesExpr`, which ALSO had a `_ => "1"` swallow), and the STRING/UNSTRING POINTER
  and TALLYING initial reads all land through `Align(…, 0)`; the swallows are gone (an error operand is loud).

**PB87 — what the sweep surfaced (found, fixed, registered).** With `LinesExpr` evaluating its operand, NIST SQ101M
went red by ONE blank line: WRT-TEST-GF-25's `WRITE PRINT-REC BEFORE ADVANCING ZERO` — the figurative constant,
admissible where integer-1 is a numeric literal (§8.3.1.2.3 r1a) — used to fall to the `_ => "1"` default and
ADVANCE ONE LINE, and `tests/nist/valid/SQ101M.txt` line 249 encoded exactly that. §14.9.51.4 GR (c): "If integer-1
or the value of the data item referenced by identifier-2 is zero, no repositioning of the representation of the
printed page is performed"; the CCVS's own text ("SHOULD BE 1 LINES BELOW AND 1 LINES ABOVE THE BRACKETING WRT-TEST
LINES"; "THIS TEST ADVANCES THE PRINT POSITION 2 LINES, TO LINE 28") says no gap. The golden loses the blank line,
`corpus.tsv`'s SQ101M row (already `divergent` over the identifier-operand legacy holes DEVLOG 573 re-baselined)
carries the citation, and the note is `kb/Work/PB87` (landed). The lesson is the swallow's: a `_ => "1"` in an
emitter switch reads as a sensible default and is a wrong answer for every operand it did not name; the golden then
pins it, and only evaluating the operand can find it.

**Measured** (golden `pb86_perform_times_function_count`): INTEGER(3.7) → 3, INTEGER-PART(2.9) → 2, LENGTH("ABCD") →
4, MOD(7, 3) → 1 over a THRU range, an inline INTEGER(2.5) → 2, `N3` 3, `PIC 9P VALUE 20` → 20, a COMP-5 4, the
literal 2, and `GO TO L1 L2 DEPENDING ON SEL` with `SEL PIC 9P VALUE 10` falling through (value 10 is out of range —
the raw digit 1 would have selected L1). Negatives `pb86-perform-times-non-integer-item` (PIC 9V9 — rejected at
85/2002/2014/2023) and `pb86-perform-times-non-integer-function` (SQRT). Verdicts: SR-14.9.28.3-2 and GR-14.9.28.4-7 →
CONFORMS (GAP 4047 → 4045).

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|Negative|Nist|Perform|ControlFlow|Intrinsic|String|GoTo|SpecTraceability|Write|Sequential|VersionMatrix|Linage`
**3387/3387** · Unit `SpecTraceabilityInventory|Intrinsic|Grammar|Parser|Registry|Diagnostic|Manifest|Perform|Guard`
**196/196** · CLI probes.
Docs: COBOLNET_CONTROL_FLOW_DESIGN.md D5 (the TIMES-count paragraph), DIAGNOSTICS.md regenerated, kb/Work PB86 →
landed, PB87 new (landed), plan §0. Battery owed for the PB84+PB85+PB86+PB87 batch. Next: PB70.


