## Entry 1295 — 2026-08-17 23:28 PDT — PB58 lands: the argument screen's predicate list, and every catalogued function has a row

**The shape, as the note demanded.** The §15.3 argument screen was a per-position CLASS column, and the 08-09
sweep measured five predicate kinds the standard's argument rules need that a class column structurally cannot
carry, plus whole functions with no row at all. Now: `ArgRule` carries a `Predicates` list — `ArgPredicate`
kinds `MinWidth` / `ExactWidth` (in character positions, decided on a STATIC width only), `DataItemOrLiteralOnly`
(§15.37.3 r3's "an integer data item or integer literal", narrower than §15.3 type 6) and `NotStrongGroup` (the
MAX/MIN-family r1's "nor shall it be a strongly-typed group item"), each with its own clause;
`ArgSchema.WithPredicate/WithTailPredicate` attach them; `PredicateViolation` decides them beside the class kind
in `CheckArgumentClasses` (which passes `KnownWidth` in); `Violation` takes the RULE so the 'p' message names the
row's clause (it said §15.71.3 for MAX's §15.59.3 r1); `IntegerViolation` gained the nonzero-fraction LITERAL arm
(`MOD(7.5 2)`); `Admissible` gained CONCAT's `'c'` (all classes but index/object/pointer); and the rules a class
kind cannot carry at all sit beside the schema on the `StaticUsageOf` axis (`CheckConcatArgs` — §15.18.3 r2's
usage agreement and r3's usage-display + unsigned-integer conditions) or in the function's own binder (SUBSTITUTE's
per-pair §15.87.3 r3 widths; INTEGER-OF-FORMATTED-DATE's "shall be a DATA ITEM" half of §15.48.3 r3).

**The rows.** ORD ExactWidth 1 (§15.70.3 r1's "one character position" — its width half was unscreened);
LOWER-CASE / UPPER-CASE / REVERSE MinWidth 1 (§15.57.3 / §15.97.3 / §15.78.3 r1's second half); ORD-MAX / ORD-MIN
gain the AllSameClass cross rule MAX and MIN had (r3 — the two-arm-dispatch scar, one arm fixed on 08-03) and
the strong-type predicate, MAX / MIN the strong-type predicate; FIND-STRING's argument-3 the shape predicate;
ANNUITY becomes `Schema(['n','i'])` (r3's integer period count); SECONDS-FROM-FORMATTED-TIME and
INTEGER-OF-FORMATTED-DATE become two-position schemas with `MatchArgument1` for their "same type as argument-1"
rules; TRIM (arg-2 ExactWidth 1 + MatchArgument1), SUBSTITUTE, TEST-FORMATTED-DATETIME, CONCAT `'c'`, the DATE
family (DATE-OF-INTEGER, DAY-OF-INTEGER, DATE-TO-YYYYMMDD, DAY-TO-YYYYDDD, YEAR-TO-YYYY, TEST-DATE-YYYYMMDD,
TEST-DAY-YYYYDDD), NUMVAL / NUMVAL-F / TEST-NUMVAL / TEST-NUMVAL-F / TEST-NUMVAL-C, SUM / VARIANCE /
STANDARD-DEVIATION, six zero-argument rows, and twelve `DeliberatelyUnscreened` reasons (LENGTH, DISPLAY-OF,
NATIONAL-OF, SMALLEST-ALGEBRAIC, MODULE-NAME, EXCEPTION-FILE(-N), the four LOCALE functions, STANDARD-COMPARE) —
every one read at its own clause and `cite.py --check`ed (36 checks in one pass). The new drift guard
`EveryCataloguedFunction_HasARow` holds `Verified ∪ DeliberatelyUnscreened ⊇ catalog`, so "no row, so
`CheckArgumentClasses` returns at its `TryGetValue` guard" — the class of gap the sweep found on six functions —
is dead structurally. The date/time FORMAT arm admits `ALL "hh:mm:ss"` (§8.3.3.6.3 SR1: a figurative constant
"may be used whenever 'literal' appears"; it re-binds as the string literal per §8.3.3.6.4 GR3c) — AR-15.79.3-1's
over-rejection.

**Measured.** 37 illegal shapes probed and each rejected with its rule; the legal golden
`pb58_argument_predicates_legal` (24 shapes: one-character ORD and ref-mod views, TRIM with a one-character
argument-2, FIND-STRING with an integer item and an integer literal, MAX over an ordinary group, same-class
ORD-MAX/ORD-MIN lists, ANNUITY(0 2), the date family, the NUMVAL family, SUM, CONCAT over display items + an
unsigned integer item + an unsigned integer literal and an all-national list, INTEGER-OF-FORMATTED-DATE with a
data-item argument-2, `SECONDS-FROM-FORMATTED-TIME(ALL "hh:mm:ss" …)` = 3723, SUBSTITUTE with an empty
argument-3) compiled and ran; 49 `pb58-*` negatives, one per rule instance, reject at their editions. **One
correction to the corpus:** §15.48.3 r3 says argument-2 "shall be a DATA ITEM of the same type as argument-1", the
row's own refuter had recorded "(iii) 'shall be a DATA ITEM' is not enforced either: a literal argument-2 is
accepted, and BOTH shipped tests do exactly that", and enforcing it failed those tests — `formatted_datetime`
(2014), `pb23_week_date_beyond_integer_date_form` (five sites) and the inline `IntegerOfFormattedDate_Ordinal_2014`
now carry the date in a data item, outputs unchanged. Inventory: all 40 PB58 rows → CONFORMS (GAP 4129 → 4089).
Gates: Conformance 882/882 (Intrinsic|Corpus|Function|Argument|SpecTraceability|Concat|Date|Format), the negative
corpus 270/270, Unit 100/100 (Intrinsic|SpecTraceability|Diagnostic), characterization 33/33, SpecTraceability
10/10. `COBOLNET_INTRINSICS_DESIGN.md`'s argument-shapes section carries the schema model. PB58 is CLOSED.

**Recorded, not glossed.** `IntegerViolation` still fails open on an arithmetic EXPRESSION — "always results in an
integer value" is not syntactically distinguishable in general (§4.2.2's discretion), so `MOD(A / 2, 3)` over
integer items binds; the fractional RESULT is RV-15.64.4-1's. And the width predicates decide static widths only —
a runtime-width operand (ANY LENGTH, a dynamic-length item, a computed ref-mod length) is never rejected.

