## Entry 541 — 2026-06-10 11:10 PDT — N3+N4 COMPLETE: per-edition numerics END TO END — OPTIONS/MODE-IS gates, the composite check, and the STANDARD-DECIMAL (decimal128) engine

**The owner's numerics directive is delivered across all four waves.** This entry closes N3-rest and N4:
- **N3-rest — the remaining edition gates:**
  - The OPTIONS paragraph is ISO-2014+ (§11.9): at `--std 85|2002` it REJECTS with COBOLNET0804 naming the
    edition, and the implied defaults (native arithmetic, nearest-away-from-zero bare ROUNDED) apply — exactly
    the pre-2014 semantics.
  - `ROUNDED MODE IS` (the 8-mode set, §14.7.4) is 2014+: COBOLNET0803 below; bare ROUNDED stays the single
    nearest-away-from-zero rounding at 85/2002. The PROHIBITED differential facts moved to a 2014 harness.
  - **Composite of operands (§14.7 rule 2, native mode, ADD/SUBTRACT/MULTIPLY/DIVIDE only — COMPUTE exempt per
    §8.8.1.2 r7):** compile-time COBOLNET0805 past 31 digits (maxInt + maxFrac superimposition; float operands
    excluded per r2b; DIVIDE's REMAINDER excluded). ⚠ FINDING: a COBOL-85-specific tightening to 18 was planned
    and REFUTED by the conformance corpus itself — CCVS-85 NC101A deliberately composes 21 digits in a MULTIPLY
    SIZE ERROR test (`MULTIPLY A06THREES-DS-03V03 BY WRK-DS-18V00`), so the cap is 31 at every edition; the
    '85 18-digit figure governs only PICTURE/literal capacity (those gates stand).
- **N4 — ARITHMETIC modes applied (§11.9.5):**
  - **NATIVE** (the default): the documented implementor-defined technique IS the exact Int128 engine (D1/D2) —
    nothing new to run, now formally documented (D3 doc-sync).
  - **STANDARD-DECIMAL — IMPLEMENTED** (`CobolDec.cs`): the SDIDI as `(Int128 Sig, int Exp)` — decimal128
    semantics per §8.8.1.5: exact computation (a 256-bit hi:lo scratch for products/quotients; Math.BigMul limb
    multiply; shift-subtract 256÷128 division, clarity-first until profiled) with ONE per-op rounding to 34
    significant digits under the INTERMEDIATE ROUNDING mode (§11.9.11 — default NEAREST-AWAY-FROM-ZERO;
    NEAREST-EVEN; TRUNCATION; PROHIBITED ⇒ EC-SIZE-TRUNCATION as the size error until the EC model). The
    renderer routes Combine/comparisons through `CobolDec` when the mode is on (`NumX.Dec`); receivers store
    through `CobolNum.Store/TryStore(CobolDec,…)` — the §14.7 NOTE-1 final transfer carries the statement's
    ROUNDED; edited receivers convert at the mask scale. Spec-pinned against a decimal128 reference: the
    signature case `2/7×7 = exactly 2` (per-op NAFZ rounding) vs native's truncated 1.99999; `1/3×3 ≠ 1`
    (34 nines); TRUNCATION and PROHIBITED modes.
  - **STANDARD-BINARY**: documented-unsupported (COBOLNET0806 — spec-obsolete §8.8.1.4.1 NOTE 1; binary128 has
    no exact .NET carrier). **Plain `STANDARD`** (the 2014 mode): dropped by ISO 2023 — COBOLNET0807 with
    per-edition wording.
- **Matrix/tests:** 3 new constructs.json rows (options-arithmetic-native, rounded-mode-is,
  arithmetic-standard-decimal — all introducedIn 2014; 64 matrix cells green), `StandardDecimalTests` (10
  spec-pinned facts incl. every gate), the OPTIONS/ROUNDED differential files retargeted to their 2014 home.
- **Doc-sync:** NUMERIC design D3 superseded-note — the owner directive LIFTED the owner-gate; records that the
  "incompatible with the locked substrate" rationale was wrong (an Int128 significand carries decimal128's 34
  digits exactly; only the `decimal`-type ban stands).
**474 conformance + 15 unit green (all 54 NC byte-matches hold).** Per-edition numerics now hold end to end:
storage (long/Int128 by digits), intermediates (exact Int128 native / decimal128 SDIDI), gating (18/31/never;
OPTIONS+MODE-IS 2014+; composite 31), and diagnostics that name the required edition — the four-compilers
contract, delivered for the numeric subsystem.

