## Entry 392 — Two spec-fidelity bug fixes surfaced by the data-model review: IS ALPHABETIC range + ROUNDED MODE PROHIBITED size error

The adversarial review (Entry 393) surfaced two pre-existing runtime bugs, independent of the migration and valid
to fix today:

1. **`PicRuntime.IsAlphabeticClass` used `char.IsLetter`** — the Unicode-wide letter predicate, so a byte like
   `0xC9` ('É' in Latin-1) tested TRUE for `IS ALPHABETIC`. ISO §8.8.4.4 defines the alphabetic class as the
   closed set {A–Z, a–z, space}. Replaced with literal range checks. (`IS NUMERIC` was already literal/correct.)

2. **ROUNDED MODE PROHIBITED silently truncated** — mode 5 fell through to `decimal.Truncate` in
   `RoundToIntegerByMode` and never signaled, so `COMPUTE x ROUNDED MODE IS PROHIBITED = 2.25` into a
   one-fraction-digit field quietly produced 2.2 with no error. ISO §14.9.4: PROHIBITED forbids rounding, so an
   inexact result raises the SIZE ERROR condition (EC-SIZE-TRUNCATION) and leaves the receiver unchanged. Added
   `IsInexactAtScale` + a PROHIBITED guard in `StoreArithmeticResult` (sets `status.SizeError`, returns without
   encoding). This also completes M2-ARITH-1 follow-up #2 at the observable level (`ON SIZE ERROR` now fires for
   PROHIBITED; the named-EC exception-object/USE framework remains future work under M2-PROC-4).

   **Refactor in passing (doctrine: centralize).** Several older inline arithmetic ops (Add/Subtract/Multiply and
   their literal forms) duplicated the `ApplyScalingAndRounding → WouldOverflow → EncodeNumeric` sequence and so
   *bypassed* `StoreArithmeticResult` — meaning they also skipped its COMP-1/2 float guard and numeric-edited
   routing. Converged all of them onto `StoreArithmeticResult`, so the PROHIBITED check (and the float/edited
   handling) now applies uniformly to every arithmetic store path.

Regression tests added in the same commit (they fail against the old code): `PicRuntimeRegressionTests` (byte-level
— `0xC9` not alphabetic; PROHIBITED inexact → SizeError + receiver unchanged; exact → no error) and the
conformance test `tests/conformance/2002/rounded_mode_prohibited.{cob,out}`. Guard ALL GREEN
(1052 unit / 481 integration / 364 NIST).

