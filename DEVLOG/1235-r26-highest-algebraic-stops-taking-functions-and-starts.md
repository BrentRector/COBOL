## Entry 1235 — 2026-08-08 16:27 PDT — R26: HIGHEST-ALGEBRAIC stops taking functions and starts taking the counters — both arms of one guard

Two defects in one guard, landed together as the note demanded. `COMPUTE R =
FUNCTION HIGHEST-ALGEBRAIC(FUNCTION MY-FN)` folded +9999 from the UDF's caller-temp PICTURE —
§15.43.3 r1 admits a DATA ITEM and "shall not be an integer function or numeric function", an
exclusion the standard must WRITE because §8.5.2.12 items 6/7 make functions category-numeric too,
and the temp wears the same bound shape as declared data. The fix is a structural discrimination, not
a shape test: DataItem.IsCompilerTemp, set only in CreateCompilerTemp (THE ONE synthesized-temp
constructor — property temps covered identically), tested in BindAlgebraicFold and nowhere else,
because every other verb legitimately needs the temp→operand mapping.

The mirror arm: §8.5.2.12 items 3/4/5 make LINAGE-COUNTER, PAGE-COUNTER and LINE-COUNTER
category-numeric DATA ITEMS — r1 ADMITS them — and they drew COBOLNET1516 "not a data item". The
registers now fold: LINAGE-COUNTER's capacity is §8.4.3.14.4 GR1's own tie to the page size (the
LINAGE literal directly; a data-name operand's all-nines maximum), and the report counters — which
carry NO size in the standard (§8.4.3.15.4 GR1) — take the documented implementor shape PIC 9(18),
the all-nines of the runtime's long carriers, recorded as CONFORMANCE.md item 216 (the same
one-constant-not-two discipline as item 202). All three are unsigned: LOWEST 0, SMALLEST 1.

Goldens: algebraic_counter_registers (2023 — six register×function folds; the report counters fold as
compile-time constants, no INITIATE) + negative algebraic-udf-argument (reject-at 2002 2014 2023).
Gate: Conformance Intrinsic|Corpus 658/658 (+2 = exactly the new cases) · the algebraic set 31/31 by
name · Unit incl. drift 3197/3197 · characterization 33/33. R25 and R26 landed back-to-back per the
owner's sequence; R34 (recursive COPY REPLACING) heads the register next.

