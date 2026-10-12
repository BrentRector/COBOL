## Entry 504 — COBOL.NET: figurative constants in numeric / VALUE contexts (ZERO in arithmetic, ALL ZEROS init)

Two figurative-constant gaps from the corpus map, both of which emitted INVALID C# (a word rendered as an identifier)
rather than a value or a loud guard:
1. **Figurative ZERO in an arithmetic expression** (NC118A/119A/175A/177A): the binder's numeric-literal path
   (`BindExpr` / `BindOperandExpr` / `FindLeaf`) rendered a `literal` node's text verbatim, so a figurative `ZERO`
   reaching arithmetic became `BoundNumLiteral("ZERO")` → `ZEROL` (CS0103). A new `NumLiteral` helper maps a figurative
   ZERO (incl. `ALL ZEROS`) to `0` (ISO §8.3.1.2 — ZERO is a valid numeric operand) and makes a non-numeric figurative
   in a numeric context a loud error, not a raw identifier.
2. **`ALL <figurative-word>` in a VALUE clause** (NC201A): `FigurativeInitializer` keyed on the exact word, so
   `ALL ZEROS` (raw `ALLZEROS`) fell through to the literal path → `ALLZEROSL` (CS0103) for a numeric item, or the
   stored text `"ALLZEROES"` for an alphanumeric one. It now strips an `ALL` prefix when the remainder is a figurative
   word (`ALL ZEROS` ≡ `ZEROS`); the factored `FillCharFor` is shared. (`ALL "literal"` — repeating a multi-character
   literal — remains a separate later form.)

Cleared 5 of the 8 remaining NC compile-errors (NC118A/119A/175A/177A/201A now compile; the other 3 are distinct
issues — a group qualification, another figurative path, a REDEFINES name). +7 `FigurativeDifferentialTests`
(ZERO in ADD/COMPUTE/SUBTRACT/as-a-leaf; `ALL ZEROS`/`ALL SPACES` VALUE init) pinned to the legacy. Conformance
235 → 242; 14 unit; the 7 green NIST programs unaffected. Greenfield-only.

