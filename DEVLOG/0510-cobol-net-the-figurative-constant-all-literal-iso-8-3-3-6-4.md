## Entry 510 — COBOL.NET: the figurative constant ALL "literal" (ISO §8.3.3.6.4) — NC211A is 50/51

After the de-signing fix (Entry 509) NC211A FAILed FIG-TEST-1: `VALUE ALL "ABC"` on a `PIC X(6)` stored the raw text
`ALL"AB` instead of the repeated "ABCABC". The figurative `ALL literal-1` (§8.3.3.6.4 Format 6) was unimplemented —
loud in operand contexts (`FigurativeOperand` returned a `BoundOperandError` for any `ALL`), raw-stored in a VALUE
clause. The spec is precise: **§8.3.3.6.4 GR2** (a width-specified context — VALUE, a fixed-length receiver, a level-88
VALUE, a compared-with operand) — the literal is "repeated character by character until the size … is greater than or
equal to the number of character positions … then truncated from the right" to that width; **GR3c** (a
length-unspecified context — DISPLAY) uses the literal once.

**Implemented from the spec, complete across every figurative context** (one shared helper `EmitText.RepeatToWidth`):
- VALUE init (`FieldEmitter.InitializerFor`) and level-88 VALUE (`ConditionRenderer`) detect `ALL "literal"` raw text
  and repeat to the item / conditional-variable width.
- A new `BoundAllLiteral(Literal)` operand (`FigurativeOperand` binds `ALL STRINGLIT` to it; `ALL ZEROS`/`ALL SPACES`
  now also bind to the single-character `BoundFigurative`, lifting their former operand-context loud-reject) renders:
  MOVE-to-alphanumeric (`ConvertSource`) → repeated to the receiver width; comparison (`RenderRelational` →
  `RenderFigurativeRelational`) → repeated to the OTHER operand's width; DISPLAY (`OperandText.AsString`) → once (GR3c).

**Tests — SPEC-ANCHORED** (`AllLiteralDifferentialTests`, +7): each result derived from §8.3.3.6.4 (e.g. `X(7) VALUE
ALL "ABC"` → "ABCABCA"; `C6 "ABCABC" = ALL "ABC"` is EQ but `= ALL "AB"` is NE; `DISPLAY ALL "XY"` → "XY") and
cross-checked against the legacy. Conformance 272 → 279; 14 unit; the 7 green NC golden programs unaffected.
Greenfield-only.

**NC211A is now 50 of 51 tests — diff-9, ONE failure left: GF-48 MIXED CONDITIONS.** (FIG-TEST and the NEXT-SENTENCE
paths now pass.) GF-48 is the last blocker to the first NC-nucleus *conditional* program going fully green.

