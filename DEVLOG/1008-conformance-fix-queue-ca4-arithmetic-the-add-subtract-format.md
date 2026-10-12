## Entry 1008 — 2026-07-23 03:11 PDT — Conformance fix-queue: CA4 (arithmetic) — the ADD/SUBTRACT Format-2 composite EXCLUDES the GIVING resultants (ISO §14.9.2.3 SR1b / §14.9.44.3 SR1b), no longer a spurious COBOLNET0805

**CA4 (CONFORMANCE-FIX-QUEUE, MAJOR/S) landed** — the first of the arithmetic batch. ISO §14.9.2.3 SR1b (ADD
Format 2) and §14.9.44.3 SR1b (SUBTRACT Format 2) define the composite of operands as "all of the operands in the
statement excluding the data items that follow the word GIVING"; §14.7.7 rule 2 superimposes only those operands.
`ArithmeticBinder.BindAdd` (ADD GIVING) passed `givingRecv` and `BindSubtract` (SUBTRACT GIVING) passed `recv` as the
`receivers` argument to `StatementValidation.CheckComposite`, which unconditionally shapes every numeric non-float
receiver into the composite. So a wide GIVING resultant's digit positions were wrongly counted against the 31-digit
cap and a CONFORMING program was rejected with COBOLNET0805.

**The fix.** Pass `[]` (no receivers) at the two GIVING call sites — the resultants are excluded from the composite;
the receiver lists are still used unchanged to build `BoundAddGiving`/`BoundSubtractGiving`, so codegen is identical.
Scope verified exactly ADD + SUBTRACT: MULTIPLY (§14.9.26.3 SR4 counts the GIVING receiver) and DIVIDE (§14.9.12.3 SR4
excludes only the REMAINDER, which the code already omits) are correct and left untouched. Edition-invariant.

**Leniency change (the inverse of a strictness fix)** — it makes MORE programs compile, never rejects a new one, so
no corpus-rejection sweep is needed. The one existing COBOLNET0805 test (`StandardDecimalTests.
CompositeOverThirtyOne_RejectedEverywhere`) is UNAFFECTED: its 40-digit composite comes from the OPERANDS
(9(20)V9(11) + 9(11)V9(20) → maxInt 20 + maxFrac 20 = 40), not the 9(18) GIVING receiver, so it still rejects
correctly. Golden verified end-to-end (`feedback_verify_demo_output`): `PIC 9(25) A=1 B=2 D=5`, `ADD A B GIVING C`
and `SUBTRACT B FROM D GIVING C` into `PIC 9(6)V9(10)` → composite 25 ≤ 31, both compile, C = 3.0 each →
`0000030000000000` twice (CLI `--run` byte-confirmed).

**Gate.** Conformance filter (Corpus incl. the new golden + StandardDecimal + Arithmetic + Compute) 400/400 ·
characterization 33/33 byte-identical (validation-only, bound nodes unchanged). Golden
`conformance/2023/ca4_giving_composite` (positive, .out-verified). **18 landed / 28 fix-ready remain.** NEXT = CA5
(division rounding / PROHIBITED intermediate — a two-directional defect; the careful one), then CA6 (§14.7.7 rule 2b
binary-N operand exclusion).

