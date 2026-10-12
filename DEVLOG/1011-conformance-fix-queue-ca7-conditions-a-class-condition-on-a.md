## Entry 1011 — 2026-07-23 03:42 PDT — Conformance fix-queue: CA7 (conditions) — a class condition on a ZERO-LENGTH operand is FALSE (ISO §8.8.4.4.4 GR1), not vacuously true

**CA7 (CONFORMANCE-FIX-QUEUE, MAJOR/S) landed.** ISO §8.8.4.4.4 GR1: "If the data item referenced by identifier-1
is a zero-length item, the truth value of the class condition without the word NOT is false"; GR2 reverses under NOT;
GR3 applies the per-class membership rules only when the item is NOT zero-length. So EVERY class test (ALPHABETIC/
-UPPER/-LOWER, alphabet-name, user class-name, NUMERIC, BOOLEAN) on a zero-length operand is false. `CobolClass`
guarded `IsNumeric`/`IsNumericZoned` with `IsNullOrEmpty => false` (so NUMERIC was accidentally spec-correct) but
`IsAlphabetic`/`IsAlphabeticUpper`/`IsAlphabeticLower`/`IsInClass` only null-checked then iterated — an empty string
fell through the char loop and returned TRUE (IsInClass's own doc even asserted "an empty value is true vacuously").

**The fix.** Change the `if (s is null)` guard to `if (string.IsNullOrEmpty(s))` in all four predicates (matching the
numeric ones), and correct the IsInClass doc. `ConditionRenderer.RenderClass`'s existing `Negated ? !(test) : (test)`
wrapper then delivers GR1 (false, no NOT) and GR2 (true, with NOT) with no renderer change. A zero-length operand
renders as `""` via `OperandText.AsString` (a length-N≥1 field always renders N chars), so the empty guard is exactly
GR1. Collateral-free: `ConditionRenderer` is the sole caller of these greenfield predicates (the legacy
`CobolSharp.Runtime` PicRuntime *Class methods are a separate engine).

**Blast radius.** A runtime behavior change (the generated C# is unchanged — characterization 33/33 byte-identical);
it shifts output only for a class test on a zero-length operand, which is 2002+ and rare (a DYNAMIC-LENGTH item, an
ODO group with count 0, or ref-mod X(1:0) under >>REF-MOD-ZERO-LENGTH). Corpus + class/condition 478/478 (the other
dynamic-length programs run unchanged); full Conformance gate running.

**Gate.** Conformance Corpus+Class+Condition 478/478 · characterization 33/33 · full Conformance (runtime gate).
Golden `conformance/2014/ca7_class_zero_length` (2014+ — DYNAMIC LENGTH): after `MOVE ""` the item is zero-length →
`IS ALPHABETIC` false (GR1) → `NOTALPHA`, and `IS NOT ALPHABETIC` true (GR2) → `NOT-ALPHA-TRUE` (CLI `--run` verified;
before: `ALPHA`). **21 landed / 25 fix-ready remain.** NEXT = CA36 (tables-refmod — SEARCH EC-RANGE handler dispatch).

