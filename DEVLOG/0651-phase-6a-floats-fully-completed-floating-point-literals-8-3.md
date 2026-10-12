## Entry 651 — 2026-07-07 00:21 PDT — Phase 6a floats FULLY completed: floating-point LITERALS (§8.3.3.3.3) + the 9-finding adversarial review, all fixed

Two threads landed together. **(A) Owner directive — full float support ⇒ floating-point LITERALS now.** The
exponent form was a deferred sub-leg in 650; the owner directed completing floats fully, so it is implemented: a new
`FLOATLIT` lexer token `( [0-9]+ '.' [0-9]* | '.' [0-9]+ ) 'E' [-+]? [0-9]+` (significand SHALL include a decimal
point §8.3.3.3.3 r2; `'E'` not `[eE]` — the lexer is case-insensitive so `[eE]` folds to a duplicate-char ANTLR
error) placed BEFORE `DECIMALLIT` (maximal munch keeps `1.5E3` one token, not `DECIMALLIT "1.5"` + `IDENTIFIER "E3"`),
a `FLOATLIT` alt in `numericLiteralCore`, and `EmitText.UnscaledLit` returning a `Real` NumX for the E-form (a COBOL
float literal is already valid C# `double` syntax). A float literal is a floating-point operand (§8.8.1) → its
expression evaluates in IEEE binary64. **The lexer/parser grammar is SHARED with the frozen legacy oracle** (the
legacy CLI loads `Cobol.Net.Frontend.dll`), so this ran the FULL legacy guard (the DEVLOG-619 shared-.g4 discipline);
the token is purely additive (the no-space `<decimal>E<digits>` form was previously always a parse error).

**(B) Adversarial review of 650 (wf_145d8cc9-0b6, 4 reviewers → verify): 14 candidates, 9 confirmed, ALL FIXED.** The
Phase-6a `NumX.Real` integration had missed several paths (none covered by the 6 initial goldens):
- **[HIGH] float → a NUMERIC-EDITED receiver (MOVE + COMPUTE) → CS1503** — a raw `double` reached `CobolEdit.Format`/
  `CobolNum.Rescale` (both take Int128). Now lands via `CobolFloat.ToScaled` at the MASK fraction scale (`CobolEdit.
  MaskScale`, NOT `pic.Scale` — a numeric-edited item's Scale is 0). (Two sites: ConvertSource + StoreArith.)
- **[HIGH] NEAREST-TOWARD-ZERO truncated every value** — `MidpointRounding.ToZero` is DIRECTED rounding, not a
  tie-break (2.7→2). Now an explicit `NearestTowardZero` (nearest; exact ½ ties toward zero → 2.7→3, 2.5→2).
- **[HIGH] a fractional level-88 VALUE on a float truncated to scale 0** (exact-inverse membership: `88 V VALUE 0.5`
  emitted `==0L`). Now a native-double membership branch (`FloatMembershipValue`).
- **[MED] a fixed-only expression into a float receiver evaluated at receiver-scale 0** (`COMPUTE f = 10/3` → 3).
  New `EmissionContext.TargetReal` flag (set when EVERY arithmetic target is float, reset at the condition-render
  entry — the H1 staleness discipline) widens `Combine`/`Power` to Real → `10/3` = 3.3333333333333335.
- **[MED] PROHIBITED on an inexact float silently truncated** (no SIZE ERROR) — `ToScaled` truncated before the
  store could judge inexactness. New `CobolFloat.InexactAtScale` gate raises SIZE ERROR + leaves the receiver
  unchanged (§14.7.5 r7).
- **[MED] a transcendental intrinsic into a float receiver quantized to 9 digits** (SQRT(2)→1.414213562). `RenderFloat`
  now returns a `Real` NumX when `TargetReal` → full binary64 (1.4142135623730951); a fixed receiver keeps the
  established scale-9 quantize (no regression to the intrinsic goldens).
- **[LOW]** corrected the inaccurate NaN→0 "latches EC-SIZE" comment. (The suspected VALUE-ZERO-on-float bug was
  REFUTED — the figurative path handles it before `RawValueAsFloat`.)

New goldens (GreenfieldOnly): `float_literal` (exponent form → -1500/0.025/0010/300), `float_edited` (float→ZZ9.99 →
123.45/124.45), `float_intrinsic` (SQRT + ÷ into COMP-2, full binary64), `float_88` (fractional singleton + THRU on a
float). Battery: **1977 greenfield conformance (+4) · 213 unit** GREEN, zero regressions (the numeric-pipeline changes
are gated on float-ness / TargetReal, so the non-float corpus is byte-identical); the shared-grammar FLOATLIT change
passed the FULL legacy guard.

