## Entry 650 — 2026-07-06 23:38 PDT — Phase 6a: floating-point USAGE (FLOAT-SHORT/LONG/EXTENDED + COMP-1/2) LIVE — the implementor-defined float facility (D16)

Opened Phase 6 with floats (the readiest — the grammar already parses the trio, a golden was written-but-PENDING).
Recon workflow wf_9de26ab6-3a8 (4 readers → xhigh synthesis); all load-bearing claims verified myself (§13.18.60.4
GR13 :22824 verbatim — the trio are implementor-defined signed numerics with the short⊆long⊆extended nesting; the
`SkeletonUsage` gate PicInfo.cs:663, the `(long)` truncation stub NumericRenderer.cs:100, the `NumX` carrier). Design
recorded as numeric-design **D16** (extends D7 to the full trio). Implemented in the 6 designed increments:

- **Declaration.** `FLOAT-SHORT`→`float`, `FLOAT-LONG`/`FLOAT-EXTENDED`→`double` (no .NET quad — conformant via the
  GR13 subset nesting), `COMP-1`/`COMP-2` synonyms. Un-gated `ParseUsage` (was `SkeletonUsage`→0899); `IsFloat` +
  new `IsSingle` fold in the trio; `ClrType`/`DefaultInitializer` arms; a new PICTURE-less `PicInfo.FloatItem`
  factory (a float item is picture-less §13.18.60.2 — a PIC with a float usage is **COBOLNET1521**, since the whole
  08xx band is exhausted; DataBinder synthesis arm). **This also lit up COMP-1/COMP-2, which were declared-live but
  every operation over them was stubbed (pic=null NRE + a (long) truncation of the fraction) — a pre-existing bug.**
- **Arithmetic (D16).** A `bool Real` flag on the `NumX` carrier: any expression with ≥1 float operand evaluates
  ENTIRELY in IEEE binary64 (a float leaf → a `Real` NumX, replacing the `(long)` truncation; `Combine` gains a
  `Real`-first branch before the StandardDecimal path — COBOL float is IEEE binary, never decimal; `Real`/`Negate`/
  `Power` Real arms).
- **Stores/MOVE/DISPLAY/compare.** A store INTO a float receiver is a native cast (holds the algebraic value,
  §14.6.8.3 GR1; no SIZE ERROR — IEEE overflow is Inf); a float SOURCE lands into a fixed receiver via the new
  `CobolFloat.ToScaled(double,scale,mode)` (double→unscaled Int128, mode-aware round, saturate ±Inf/overflow so the
  existing capacity check fires SIZE ERROR) then the ordinary store funnel. DISPLAY = `CobolFloat.Display`
  (invariant-culture shortest round-trip, §14.9.11 GR1 implementor-defined — replaces the `.ToString()` placeholder).
  Compare = a native IEEE double comparison when either operand is `Real` (§8.8.4.2.4; NaN-unordered / ±0-equal fall
  out of C#). ONE new runtime file `CobolFloat.cs`.

**Goldens.** `float_usage` un-gated (S=0021/L=0401/X=0501) + `comp1_comp2` (C1=0021/C2=0401) are LEGACY-SHARED — the
frozen oracle also supports the trio + COMP-1/2 and agrees byte-for-byte. `float_move` (float↔float, S=1.5…),
`float_neg` (negative/fractional literal → float), `float_rounded` (float→PIC truncate=10 vs ROUNDED=11, 2.5→03),
`float_compare` (EQ/GT) are GreenfieldOnly (our DISPLAY form / float rounding). **Deferred sub-leg (documented, loud
not silent):** the float LITERAL exponent form (`1.5E3`, §8.3.3.3.3) is a distinct frontend feature not yet lexed — a
loud COBOL0001 parse error; a float holds a fixed-point literal directly (`float_neg_exp`→`float_neg`).

Stale skeleton assertions retargeted (making a gated feature live invalidates its "not implemented" guards):
`LoudGuardTests` (the float trio now `ParseUsage_FloatTrio_LiveAt2002` + a live storage-mapping fact; the empty
not-implemented theory dropped) and `DataSkeletonEditionTests` (the trio rows removed; the external-float PICTURE `E`
row stays — a 6b leg). Battery: **1973 conformance · 213 unit · legacy integration 86 GREEN**, zero regressions
(the non-float corpus is byte-identical — every float behavior is gated on float-ness). NEXT Phase-6: OCCURS DYNAMIC,
then TYPEDEF; the 6b IEEE family (FLOAT-BINARY/DECIMAL) stays loud.

