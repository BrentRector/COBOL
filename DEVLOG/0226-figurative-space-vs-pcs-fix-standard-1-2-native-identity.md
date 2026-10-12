## Entry 226 — Figurative-SPACE-vs-PCS fix: STANDARD-1/2/NATIVE identity + normalize-identity-to-null

Unblocks Gap 2. The prior session reverted Gap 2 because making CHAR/ORD honor the program collating
sequence regressed 8 NIST tests; this entry root-causes and fixes the underlying defect (collating
correctness), regenerates the contaminated baselines, and leaves the guard genuinely green.

Two real bugs, both fixed:

1. **STANDARD-1/2/NATIVE built an all-255 collating table.** They are dedicated lexer tokens forming
   their own `alphabetDefinition` alternatives, NOT an `alphabetEntry`/cobolWord, so
   `SemanticBuilder.BuildAlphabetCollatingSequence` saw zero entries for `ALPHABET x IS STANDARD-2`
   and fell through to the user-defined branch with an all-255 weight table — under which EVERY string
   compares equal to every other (`"ABCD" = SPACE` vacuously true). Fix: detect
   `alphaDef.NATIVE()/STANDARD_1()/STANDARD_2()` first → return the native identity table.

2. **An identity program collating sequence still took the weight-table comparison path**, which
   differs from native on trailing spaces (`CompareAlphanumericWithSequence` pads with 0x20 and
   weights it, no trim; native `CompareFieldToString` TrimEnd()s both). Fix: `Compilation.
   BuildSemanticModel` normalizes an identity sequence to null (`IsIdentityCollation` helper) so
   STANDARD-* programs use the proven native path. Genuinely reordered alphabets are non-identity and
   stay honored.

Verified with a minimal repro (`PROGRAM COLLATING SEQUENCE IS x / ALPHABET x IS STANDARD-2`):
`"ABCD" = SPACE` is now FALSE / `NOT = SPACE` TRUE (was the reverse).

**Baselines corrected (they encoded the bug).** 8 guarded tests had been self-captured WHILE the
all-255 bug was present. Their CCVS boilerplate does `IF P-OR-F = "FAIL*" PERFORM FAIL-ROUTINE ELSE
PERFORM BAIL-OUT`; under all-255, `"PASS " = "FAIL*"` was vacuously true, so passes wrongly ran
FAIL-ROUTINE and emitted spurious `*** INFORMATION ***NO FURTHER INFORMATION` lines. (Confirmed: only
the 3 PCS-declaring IF tests carried those lines; all 39 non-PCS IF baselines had none.) Regenerated
7 baselines (NC114M, IF105A, IF119A, IF123A, IF127A, IF128A, IF129A) from corrected output after
verifying each diff removes only blank + spurious-INFORMATION lines (no result/value changes).
- **NC214M dropped from the guard** (deleted its baseline): it is an `ACCEPT FROM DATE/DAY/TIME/
  DAY-OF-WEEK` "CHECK VISUALLY" test whose output is inherently live/non-deterministic. The all-255
  bug had masked the live values with the constant "NO FURTHER INFORMATION" string, making it
  accidentally deterministic; with the bug fixed it emits today's date/time and cannot be a stable
  baseline. Removed from NIST_TESTS.

Guard ALL GREEN: 1000 unit, 340 integration (+1 unrelated skip), **148 NIST baselines** (was 149;
NC214M removed) 0 FAIL*. Next: re-apply CHAR/ORD (Entry 227).

