## Entry 1323 — 2026-08-18 15:37 PDT — PB99: the floating-point numeric literal — its form rules checked, its exponent range documented and diagnosed (no more CS0594), and its value EXACT in every operand position and under STANDARD-DECIMAL

**What landed.** kb/Work **PB99** (found probing PB66's literal legs; MAJOR — a crash class and a silent wrong
answer): `01 F USAGE FLOAT-LONG VALUE 1.0E12345.` and `MOVE 1.0E+400 TO F` failed in the BACKEND — Roslyn's
CS0594 "Floating-point constant is outside the range of type 'double'" — with no COBOL diagnostic; §8.3.3.3.3's
form rules (SR2 a 1–36-digit significand, SR3 at most four exponent digits, SR4 the zero form) were not checked at
all; and — the part the crash hid — a floating-point literal in an OPERAND position was a binary64: `MOVE
1.2345678901234567890123E+3 TO N1` stored 1234.56789012345685803008, the same literal into `PIC 9.9(25)E+99`
defeated D21's exact channel at the door, `PERFORM VARYING N1 FROM 1.0E+3` set a 20-decimal item to
999.99999999999999916 (the binary64 1E+23 product), and under ARITHMETIC IS STANDARD-DECIMAL a 23-digit significand
kept 17 digits.

**The spec.** §8.3.3.3.3 SR1–SR4 (form); r3 "The maximum permitted value and minimum permitted value of the
exponent is implementor-defined" — never documented here; GR5 the literal's value is significand × 10^exponent,
exact. §8.8.1.3 makes NATIVE arithmetic the implementor's method for an arithmetic EXPRESSION / STATEMENT — that,
and only that, is where numeric design D16's "a float literal is a binary64 operand" latitude lives; a MOVE, a
relation, a PERFORM VARYING FROM value, a function argument are not native arithmetic, so the literal's exact value
governs there; §8.8.1.5.2 r1 converts every operand of a standard-decimal expression to the decimal128
intermediate — the literal's exact value, not a binary64's shadow.

**As built.**
- `NumericLiteral.CheckFloatingPointForm` (Frontend Common) at the ONE normalizer both funnels share
  (`DataBinder.NormalizeNumericLiteral`) → COBOLNET1661 for a 40-digit significand, a five-digit exponent, `0.0E+5`,
  `-0.0E+0`, `0.0E-0` — in a VALUE clause, a level-88 and a statement alike. `NumericLiteral.TryParseExact` is the
  ONE exact parser (the binder's range checks, the renderer and `ValueInitializer.TryParseFloatLiteral` use it);
  `FitsBinaryFloat` the binary64 / binary32 fit.
- The implementor-defined exponent range, documented (CONFORMANCE.md §7 A.1 item 82; the A.3 item-1 row is now
  Claimed for all four abilities) and enforced at compile time: `ExpressionBinder.CheckLiteral` (the ONE expression
  chokepoint) diagnoses a native arithmetic literal beyond binary64 and, under STANDARD-DECIMAL, one beyond
  decimal128 (`CobolDec.FromParsed` in a try — the same funnel NUMVAL-F uses); `DataBinder.ValidateValueCategory`
  (the item-VALUE + level-88 funnel) diagnoses a VALUE beyond a FLOAT-SHORT (binary32) / FLOAT-LONG (binary64)
  subject's range (§13.18.63.3 SR2). A VALUE on a fixed-point or floating-point numeric-edited item keeps the exact
  value — `PIC 9E+9999 VALUE 1.0E+9999` seeds `1E+9999`.
- The renderer: `NumericRenderer.LiteralOperandNum` renders a `BoundNumericLiteral` (every operand position) as
  the EXACT `CobolDec.FromParsed(sig, exp10, mode)` on the Dec lane — every consumer already accepted a Dec operand
  (PB84's landing / store funnel; `Real()` converts where a binary64 body wants a double); `RenderOperandLike` does
  the same for PERFORM VARYING FROM / BY (the format admits only identifier / index-name / literal, the binder types
  them BoundExpr); `LiteralNum` (a `BoundNumLiteral` inside an arithmetic expression / statement) is exact under
  STANDARD-DECIMAL and stays D16's binary64 natively (`ADD 1.5E+3 TO I` is still binary64 arithmetic). D16's
  paragraph in COBOLNET_NUMERIC_DESIGN.md now says exactly this.
- Goldens `2023/pb99_floating_literal_extremes` (a 36-digit significand and a 4-digit exponent into floating-point
  numeric-edited VALUEs; the binary64 / binary32 edges), `pb99_floating_literal_operand_exact` (MOVE / relation /
  EVALUATE / VARYING / function argument), `pb99_floating_literal_standard_decimal` (`1.0E+400 / 1.0E+398` = 100);
  negatives `pb99-floating-literal-form`, `pb99-floating-literal-range`. Inventory SR-8.3.3.3.3-1..4 /
  GR-8.3.3.3.3-5 → CONFORMS. COBOLNET1661 claimed; DIAGNOSTICS.md regenerated.
- Not this entry: `COMPUTE N1 = 1.5E+3 * 2` into a 20-decimal item lands 3000.000000000000008388608 — the
  in-carrier binary64 product of the float→fixed landing, the CONFORMANCE.md §3 conversion-manner determination
  whose open analysis is kb/Work PB90.

**Battery #18** (the PB66 + PB97 + PB98 batch, tree `684ec6f3`, recorded in §0 with `ef9cbc17`): Conformance
4788/4788 · Unit 4238/4239 — the one red was `ConstructRegistryDriftTests` catching that `ConstructRegistry.g.cs`
still carried the row's pre-correction citation after the post-generation `§13.18.40.3 SR13 b)` → `§13.18.40.4
GR13 b)` fix (regenerated; the drift test did its job) · Characterization 33/33 · NIST 353/0 audit-clean ·
differential 1323 cases, one attributed FIX flip: GnuCOBOL's `run_fundamental:72` ("DISPLAY literals,
DECIMAL-POINT is COMMA" — `DISPLAY 1,23E0`) WE_REJECT_THEY_ACCEPT → AGREE_ACCEPT, the PB98 comma-decimal floating
literal — baselined. GREEN.

**Gate (PB99, wave-local).** Characterization 33/33; unit (literal / numeric / float / Dec / drift / inventory)
3408/3408; conformance (arithmetic / float / literal / value / move / perform / intrinsic / standard / condition /
evaluate) 1496/1496 + the corpus runner / manifests / version matrix: 2771/2771 (9 m 54 s). Battery #19 follows.

