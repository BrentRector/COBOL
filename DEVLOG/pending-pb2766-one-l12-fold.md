## PB2766: one L12 fold for the locale snapshot and the NUMVAL-C LOCALE argument

PB2766 (an R2 duplication finding of the PB1754 review) said DETERMINATION L12's character equivalence was written
twice with different contents: `LocaleFacts.NormalizeLocaleText` removed every Unicode Cf character and folded
U+00A0, U+202F and U+2009 to the plain space, while the NUMVAL-C / TEST-NUMVAL-C LOCALE matcher (`MatchAt`) restated
only the three spaces and never skipped Cf. `NormalizeSpacing` was a one-line alias of `NormalizeLocaleText`.

**Re-probe.** On this worktree's build, under `LOCALE AE IS "ar-AE"`, TEST-NUMVAL-C returned 14 for ar-AE's own
formatted currency string (`1,234.56 د.إ.` followed by U+200F; the snapshot strips that U+200F from the symbol), 9
for a U+00A0 between the number and the symbol (`MatchAt` accepted U+00A0 against a normalized separator, but the
token-adjacency skip took only U+0020), and 1 for a leading U+200F. NUMVAL-C of the first raised
EC-ARGUMENT-FUNCTION and returned 0. L12 already said "recognition matches through the same equivalence", so the
two arms disagreed about what conforms.

**Fix.** `LocaleFacts.TryFoldLocaleChar` is now the one per-character L12 fold. `NormalizeLocaleText(string)`
normalizes every snapshot string through it, and a new overload `NormalizeLocaleText(string, out int[]? origins)`
folds a text that is matched against locale data and records each kept character's ordinal. `NvScanLocale` folds
argument-1 once, scans the folded text, and maps every reported position (the structural one and the digit-cap
one) back to the original argument, so §15.94.4's positions are unchanged for an argument with nothing to fold.
`MatchAt` no longer restates the set. `NormalizeSpacing` is deleted, and `DateFormat`, `TimeFormat` and
`CobolLocaleTests` call `NormalizeLocaleText`. Decision: a Cf character in argument-1 is ignorable. That is the
only reading under which both an ar-* culture's raw formatted string and its L12-normalized form are "a format
consistent with the specifications of locale category LC_MONETARY" (§15.68.3 r5 b). It is recorded with L12 in
docs/CONFORMANCE.md and in DESIGN-locale-facility §4.6.

**Not behavior-neutral.** This unification fixes a wrong answer: arguments carrying a Cf character, or a no-break
space where a space is admitted, were rejected and are now accepted. Golden `2002/pb2766_numvalc_locale_l12_argument`
covers a leading Cf, Cf inside parentheses, a no-break-space adjacency, §15.94.4 r1 b.1's example shifted by a Cf,
and r1 c's LENGTH + 1 with a trailing Cf. Unit tests: `NumvalCLocaleScanTests.L12Equivalence_*`, and
`EveryHostCulture_RecognizesItsOwnFormattedCurrencyString`, which requires every host culture's own
`ToString("C")` to be conforming and to evaluate to its value. mn-Mong-MN is excluded because its mon_thousands_sep
equals its mon_decimal_point, which is the §8.2.1 invalid-content case. `compare_oracle.py` against 32f372674:
1 of 8021 cases differ, the ADDED golden; no existing case's emitted C# or diagnostics moved (the change is in the
runtime library).

**Sweep.** The L12 character set now appears only in `LocaleFacts`. The format-2 de-edit stays ordinal, because
§14.6.13.2 r4 admits only "a possible result" of the edit, and the edit's output is already folded. The
`MatchAt` in `CobolIntrinsics.Text.cs` is the unrelated SUBSTITUTE matcher. No inventory row was claimed or closed.
