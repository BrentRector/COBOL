## Entry 057 — 2026-03-15: CategoryCompatibility Matrix — ISO MOVE/Arithmetic/Compare Rules

### The Change

Created `CategoryCompatibility.cs` — single authoritative source for COBOL
category compatibility rules per ISO/IEC 1989:2023:

**MOVE matrix** (HashSet-based): Numeric→anything, NumericEdited→NumericEdited
+ alpha/national, all others→alpha/national families only. Exactly matches the
ISO truth table.

**Arithmetic**: Operands must be Numeric or NumericEdited. No alphanumeric/national
in arithmetic.

**Comparison**: Same-family (including edited variants). Numeric↔NumericEdited,
Alphanumeric↔AlphanumericEdited, National↔NationalEdited. Cross-family illegal.

Public API: `IsMoveLegal()`, `IsArithmeticOperand()`, `IsArithmeticResult()`,
`IsComparisonLegal()`, `IsNumericFamily()`, `IsAlphanumericFamily()`,
`IsNationalFamily()`.

### Result

NC101A: 243 lines, 20/59 pass — identical output. Matrix ready for binder
diagnostics and lowering dispatch.

---

