## Entry 1581 — 2026-09-09 14:27 PDT — Golden round for PB245: thirteen of fourteen §15 intrinsic rows close on spec-derived goldens; RV-15.4.1-4 restated on its surviving residue

kb/Work PB245 measured that fourteen §15 intrinsic rows of the P14 traceability inventory were verdicted
NOT-IMPLEMENTED or PARTIAL on forensic notes describing a tree that no longer existed — seven later landings had
closed the mechanisms and nothing re-verdicted the rows. This entry is the GOLDEN ROUND the owner ordered for
them. ⛔ "Looks closed" was not accepted as a verdict anywhere in it: for every row the rule was quoted from
`specs/ISO_COBOL.md` and `--check`ed, the expected value DERIVED from the clause before anything was run, a
spec-derived golden written to pin it, and the golden run on a build of this tree. Thirteen rows closed CONFORMS
on the new witnesses; one did not and is re-stated. **GAP 2726 → 2713 of 4,348.**

WHAT WAS DERIVED, WHAT WAS MEASURED, ROW BY ROW.

· **AR-15.40.3-5** (§15.40.3 r5, |argument-4| ≤ 1439). §15.40.4 r3 makes an OFFSET format reflect argument-3
  directly in the time portion and argument-4 directly in the offset portion, so the derivation needs no
  arithmetic: ±1439 render `2021-06-16T12:34:56±23:59`, 0 renders `+00:00` (§15.3.3.6.1's own receiving-field
  paragraph fixes the sign), and ±1440 set EC-ARGUMENT-FUNCTION. All six lines measured identical.
  `2014/l1_formatted_datetime_offset_magnitude`, checking ON, matched pairs one minute apart on each side.
· **AR-15.40.3-6** (§15.40.3 r6, argument-4 barred for a LOCAL time portion). Compile-time decidable because r1
  makes argument-1 a literal. Measured: COBOLNET1633 at 2014 and 2023 —
  `negative/l1-formatted-datetime-offset-local-format`. The converse arm (r7's omitted argument-4) is required
  green by the positive golden above, so a widened screen turns that one red.
· **AR-15.43.3-2** (§15.43.3 r2, standard binary usages under STANDARD-DECIMAL). Derived as a TABLE over two
  axes — the MODE and §3.166's usage family — because the guard the row was opened on failed on both:
  STANDARD-DECIMAL × FLOAT-BINARY-64 must reject (COBOLNET1516, measured), NATIVE × FLOAT-BINARY-32/-64 must
  ACCEPT (§11.9.5.2 GR4; measured, bracketed at seven digits), STANDARD-DECIMAL × COMP-1/COMP-2 must ACCEPT
  (pb122). `negative/l1-highest-algebraic-standard-binary-usage` + `2014/l1_algebraic_standard_binary_usage_mode_bar`.
· **AR-15.67.3-4 · AR-15.68.3-7 · AR-15.69.3-3** (the NUMVAL-family caps). The standard-BINARY sentence of each
  rule is unreachable by construction — PB198's documented §4.2.6 decline of the mode — and the
  standard-DECIMAL sentence caps at 34. Derived and measured as three matched pairs one digit apart in one run,
  including NUMVAL-F's significand discriminator (34 significand digits inside a 35-digit string must be
  ACCEPTED). `2014/l1_numval_family_standard_decimal_cap`; seven lines, all as derived.
· **RV-15.83.4-2** (§15.83.4 r2 on a float carrier). ⚠ The pre-existing witness bracketed binary64 as
  `> 0 and < 5.0E-324`, which any positive number below the subnormal satisfies — the right test for the
  ADMISSION arm, not for this row's VALUE. The new golden pins 2^-149 and 2^-1074 inside windows one unit wide
  in the TWENTIETH significant digit, and each window was proved discriminating by shifting it one unit and
  watching the probe go red. `2023/l1_smallest_algebraic_float_increment`.
· **AR-15.95.3-1** (§15.95.3 r1, TEST-NUMVAL-F's class screen). The row's open half was class ALPHABETIC, which
  the model could not express; PB124 wave 5 gave `CobolClass` its member and routes a PIC A item to it through
  `PicInfo.IsAlphabetic`. Measured: PIC A(6) and PIC 9(6) both rejected COBOLNET1627 at 2002/2014/2023, while an
  alphanumeric literal, a PIC X item, a PIC ZZ9.99 numeric-edited item, a PIC N(8) national item and a group item
  all evaluate — the admit side is fixtured deliberately, because a screen narrowed to CATEGORY would close the
  hole and over-reject conforming source invisibly.
· **RV-15.96.4-1 · RV-15.96.4-2** (TRIM LEADING / TRAILING with ≥2 argument-2). Read alone, r1's "does not
  contain ANY argument-2" is a set union; r5 and its NOTE make r1/r2 rules about one argument-2 per nesting
  level. ⚠ No fixture had pinned the TRAILING shape with multiple argument-2 at all, and the §15.96.4 NOTE's own
  example agrees under both readings. `2023/l1_trim_leading_trailing_multi_arg2` writes the union answer beside
  every line and carries ORDER-CARRYING PAIRS that hold the argument-2 SET fixed.
· **AR-15.100.3-3 · AR-15.100.3-6 · RV-15.100.4-1** (YEAR-TO-YYYY). The golden evaluates the standard's OWN
  expressions at run time — r5's `FUNCTION NUMVAL(FUNCTION CURRENT-DATE(1:4))`, r1's maximum-year and r2a/r2b —
  so nothing is pinned to a calendar year, and it compares four argument-1 values straddling the r2a/r2b split.
  ⚠ Neither r6 boundary had been pinned by any fixture on any function; both matched pairs are now written on
  YEAR-TO-YYYY itself with argument-3 satisfying r4 so a raise is attributable to r6 alone.
  `2002/l1_year_to_yyyy_defaults_and_sum_window` (13 lines) + `negative/l1-year-to-yyyy-float-item`.

THE FLOAT-CARRIER RESIDUE OF THE THREE YEAR-TO-YYYY ROWS, MEASURED RATHER THAN ASSUMED. All three recorded their
open half as `FUNCTION YEAR-TO-YYYY(F)` over a COMP-2 F. At HEAD that reference is REJECTED at bind (COBOLNET1627
— PB248 screens a floating-point item at every §15.3 type-6 position), and a float-bearing arithmetic expression
is rejected with it; only `--permissive`'s documented coercion reaches the Real lane, where the measured answers
are 2076 / 1976 / 2976 with the clock at 2026 — every one the derived value, because PB119 split
`YearToYyyyReal` into the two overloads the exact core has. Both halves are now fixtured: the rules on the lane
conforming source reaches, the rejection on the lane it does not.

⚠ **RV-15.4.1-4 DID NOT CLOSE, AND THE ROUND NARROWED WHAT CLOSING IT NEEDS.** Two of its three recorded costs
are dead — PB125 landed FACTORIAL's standard-decimal EAE arm, PB198 recorded the standard-binary decline — and
the third survives: §15.4.1 NOTE 1 item 4 has no located code and no test. Re-measured here, NOTE 1 scopes item 4
to *the result of an equivalent arithmetic expression*, and §15.75 (RANDOM), §15.80 (SECONDS-PAST-MIDNIGHT) and
§15.38 (FORMATTED-CURRENT-DATE) carry no equivalent arithmetic expression at all — so three of the four
instances the row's own note lists are the wrong functions for this rule, and E/PI's native
"implementor-defined approximation" is already covered by NOTE 1 item 1. What is owed is an enumeration of the
functions that have an EAE **and** whose returned value the standard makes explicitly implementor-defined under a
STANDARD mode; that is not an agent's to assume, so the row keeps its PARTIAL and PB245 keeps claiming it.

TWO OF MY OWN CITATIONS WERE WRONG AND `--check` CAUGHT THEM BEFORE THEY LANDED, which is the whole reason the
rule exists. §15.43.4 r2 does not read "the greatest finite magnitude that can be represented in the data item
referenced by argument-1" (the real text is "The value returned is equal to the positive algebraic value of
greatest finite magnitude that may be represented in argument-1"), and §13.18.60.4 does NOT pin FLOAT-SHORT and
FLOAT-LONG to binary32/binary64 — GR13 and GR21 leave their representation to the implementor, and it is
`docs/CONFORMANCE.md` item DOC-A.1-207 that determines them. The SMALLEST-ALGEBRAIC golden's derivation now runs
through that determination instead of through a clause that says the opposite. A stale class remark in
`IntrinsicArgumentRules.cs` — still asserting that a `CobolClass.Alphabetic` member could not exist, beside the
member — was corrected in the same change set.

Also landed: `kb/Work/PB245.md` rewritten, `inventory_rows` shrunk to the one row still defective (a closed row
must not stay claimed), and plan §0's live-state GAP number moved to 2713. **No diagnostic code was claimed.**
