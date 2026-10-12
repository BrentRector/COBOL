## Entry 634 — 2026-07-06 19:12 PDT — Phase 5: SMALLEST / HIGHEST / LOWEST-ALGEBRAIC (§15.83 / §15.43 / §15.58) — the PICTURE-metadata fold family

Three functions, ONE mechanism: a **compile-time PICTURE fold**, exactly the discipline LENGTH uses (§15.50 /
`BindLengthFold`). The value derives entirely from the argument data item's PICTURE/USAGE — digit count, scale,
sign, container width — so `BindAlgebraicFold` returns a `BoundNumLiteral` at bind time; no bound-node property,
no renderer case, no runtime body. (Design from the recon workflow wf_840f8070-fdf.) Implemented all three
together (they share the metadata mechanism — not scoped to one).

- **SMALLEST-ALGEBRAIC (§15.83, 2023)** = the smallest positive increment = 10^(−scale), independent of digit
  count / sign / container (`S999`→+1, `S9PP`→+100 [scale −2], `99V9(3)`→+.001, `S9(4) COMP`→+1). Category
  numeric DATA ITEM only (§15.83.3 r1).
- **HIGHEST/LOWEST-ALGEBRAIC (§15.43 / §15.58, 2002)** = the greatest / lowest representable value; numeric OR
  numeric-edited DATA ITEM. Three truncation disciplines keyed on USAGE: **all-nines** (10^Digits−1) for a
  DigitCount item (`S9(4)`→+9999); the **two's-complement container range** for a BinaryCapacity item
  (COMP-5 / BINARY-CHAR family — `8·StorageWidth` bits, signed 2^(b−1)−1 / unsigned 2^b−1); the **mask capacity**
  for a numeric-edited item, reusing the ONE canonical `CobolEdit.MaskCapacity` (made `public` — singular pattern;
  `$**,**9.99`→+99999.99). LOWEST negates the magnitude for a sign-representable item, else 0 (Annex D.32 —
  `99V9(3)` unsigned → 0, `$**,**9.99` no-sign mask → 0). BigInteger throughout (8-byte binary exceeds `long`);
  `Decimalize` renders the scaled literal ('.' radix — a C#-facing literal, not COBOL source).
- **Diagnostics** — a non-data-item / group / ref-mod / index / nested-function argument → **COBOLNET1516**
  (§*.3 rule 1). A floating-point argument (COMP-1/COMP-2) → COBOLNET1516: the native-arithmetic usage
  restriction is implementor-defined (§15.83.3 r4) and COBOL.NET defines no PICTURE algebraic range for IEEE
  floats; under STANDARD-DECIMAL it is barred by rule 2. (The float guard is in place; it is exercisable once
  float DATA ITEMS land — Phase 6 — so the differential battery tests the fixed-point + edited legs, which are
  the whole realizable surface today.) Edition gates ride the existing COBOLNET1502 (2002 / 2023).

Goldens `tests/conformance/2002/highest_lowest_algebraic.cob` + `tests/conformance/2023/smallest_algebraic.cob`,
both byte-exact FIRST RUN (every spec example incl. P-scaling, the edited mask, unsigned LOWEST), **GreenfieldOnly**.
+8 `IntrinsicFunctionDifferentialTests`. Battery: **1925 conformance (+8) · 216 unit · 116 corpus goldens GREEN**;
greenfield-only (catalog Deferred→Fold ×3, the BindAlgebraicFold path, `MaskCapacity` made public). Phase-5
intrinsics live: CONCAT/BASECONVERT (628), TRIM (629), FIND-STRING (630), SUBSTITUTE (631), CONVERT (632),
MODULE-NAME (633), the ALGEBRAIC fold family (634); residue = the 2014 date family, to be done in full next.

