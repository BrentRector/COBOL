## Entry 539 — 2026-06-10 10:23 PDT — N1: THE Int128 CARRIER — the whole expression pipeline + runtime engine is Int128-monomorphic (numeric design D1+D2 shipped)

**The owner directed: numerics properly implemented for each language version, end to end. Wave N1 of 4.**
The numeric design's central hardening (D1) is now real: NO numeric intermediate anywhere computes in `long`.
- **Runtime:** every `CobolNum` kernel is Int128-typed — `Rescale`, `Store`, `TryStore` (out Int128),
  `MulChecked` (now the >38-digit ESCAPE check, checked Int128), `Divide`/`DivideOrThrow`/
  `DivisionLosesPrecision`, `FormatDisplay`/`FormatDisplaySigned`/`FormatUnsignedDisplay`, `ParseDisplay`,
  `FromAlphanumeric`; `CobolEdit.Format` takes Int128; `Pow10Wide` (10^0..38). Storage stays narrow native
  (long ≤18 digits) — the wide STORAGE tier (PIC 9(19..31), 2002+) is wave N2.
- **Renderer:** `Combine` forces wide math on every operation — `((Int128)(a) * (b))`, additive
  `((Int128)(x) ± (y))` after alignment; `Real()` casts explicitly for the float path (Int128 has no implicit
  double conversion). **D2 SHIPPED:** nested/higher-precision divisions now carry `DivGuardDigits = 14`
  (clamped: alignment exponent ≤ 20 keeps 18-digit dividends ≤ 38 digits); the outermost division keeps the
  exact single-rounding-at-the-receiver path. Generated programs gain `using System;`.
- **Emitter:** ONE width-aware `Narrow()` at the store boundary — a ≤18-digit receiver takes `(long)(…)`, a
  19+-digit receiver (N2) will take the Int128 straight; SET/VARYING/index temps cast at their long boundaries;
  DIVIDE-REMAINDER's subsidiary quotient is an `Int128` temp.
- **Doc-sync (rule #4):** D1 carries a SHIPPED note recording the ONE deliberate refinement — `CobolInt.Scale`
  resolved at COMPILE time (Int128-typed expressions + the renderer's static scales + monomorphic kernels)
  instead of a runtime struct field, with the rationale; D2's note records the clamp and that the corpus holds.
**Verification: the ENTIRE net is byte-identical under the new engine — 440 conformance (incl. all 54 NC
byte-matches) + 15 unit green.** `COMPUTE c = a*b` on two PIC 9(18) operands (36-digit product) is now exact.
The NC17x diffs are unchanged — their causes are beyond intermediates (per-program maps in hand); next waves.

