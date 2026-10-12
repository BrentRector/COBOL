## Entry 1630 — 2026-09-21 23:31 PDT — Landing train 44: the report group's second axis, the class-condition table read three ways, and the intrinsic argument intake finished — three clusters, nine notes, GAP 2310 → 2288

**Three clusters, nine `kb/Work` notes to `landed`, no cluster dropped.** All three implementer groups were
wave-43 survivors of the 18:00 PDT API-529 outage: each merged its killed predecessor's branch and re-measured
every probe, citation and row on its own build before believing any of it. The train was brought in from the
three BRANCHES, gated ONCE on the whole tree, and landed through `scripts/push-main.sh`.

**Cluster 1 — the report group entry repeats on EITHER axis (PB484 + PB429 + PB541 + PB565).** PB484:
`ReportWriter.PresentLine` travelled to the line it was handed instead of to the PAGE line the LINE clause
names, and §13.18.35.4 GR7 existed as FOUR copies of `LineCounter + l.Value` — body, page heading, page
footing, report heading/footing. `SubsequentTarget` was extracted BEFORE the Step arm was added, so all four
arms moved together; asking "which arm did I fix" is the cheapest question in this codebase and it has now
paid five times. PB429: PAGE-COUNTER was a read-only pseudo-item, so `MOVE 1 TO PAGE-COUNTER` was refused
where §8.4.3.15.3 permits it, while LINE-COUNTER — which SR3 DOES forbid as a receiving operand — was not;
eight receiver sites now route through one chokepoint and COBOLNET2197 carries SR3. PB541: §13.18.60.3 SR7
admits DISPLAY *or* NATIONAL in one screen (COBOLNET2198), which is what makes §13.18.60.4 GR1's inheritance
of a report GROUP entry's usage unambiguous. PB565 is the one that grew: the re-probe reproduced COBOLNET0899
on `03 LINE PLUS 1 OCCURS 3 TIMES.`, and with the 0899 lifted the SAME conforming program was refused AGAIN by
COBOLNET2021 — the horizontal landing had read §13.18.38.3 SR25c and SR25d as ONE subtree scan for an absolute
COLUMN clause, where c) is about the entry's OWN clause and d) adds §13.18.38.4 GR10b/GR12b's qualifier. SR25
is four legs with three predicates now, and its absolute-LINE legs a/b went live having never been reachable.
The fix shape came out of the standard rather than out of the code: §13.18.35.4 GR9 says a multiple LINE
clause "is functionally equivalent to a LINE clause with a single operand, together with a simple OCCURS
clause whose integer is equal to the number of operands of the LINE clause", so `ReportRepetitionOf` BUILDS
that reduction and there is ONE repetition mechanism, not two. The axis became a FIELD —
`ReportOccursSpec.Axis`, read only through `Shift(axis)`/`Undisplaced(axis)` — rather than a test at each
site, and a drift test asserts a frame's STEP becomes a displacement in exactly one place. Both COBOLNET0899
stages are deleted with a retirement comment. Three goldens, six negatives, rows GR-13.18.35.4-6,
SR-14.9.25.3-4, SR-8.4.3.15.3-1/-3, SR-13.18.60.3-7 and PB565's fifteen.

**Cluster 2 — the class-condition alternative list, written down four times (PB571 + PB590 + PB649).** Filed as
three defects, one root. §8.8.4.4.2's list lived in the `className` grammar rule, in a SECOND grammar rule
`classCondition` serving EVALUATE — which offered ALPHANUMERIC, a phrase the general format does not print,
and omitted BOOLEAN, class-name-1 and alphabet-name-1 — and in a kind-decode in each of two binders, while the
§8.8.4.4.3 operand screen was a fifth partial that returned early unless the operand was boolean. SR1 was
therefore asked of nothing at all, and `IF IX IS NUMERIC` over a USAGE INDEX item compiled clean and printed
TRUE. The stated scope was an estimate and it was wrong (CLAUDE.md rule 5): the answer is the extraction.
`ClassConditionModel` is the one table, a row per offered alternative carrying the rules that name it, and
SR1 — "Identifier-1 shall not reference a data item of class index, message-tag, object, or pointer" —
delegates its class half to the two class definitions that already existed rather than writing a third. The
duplicate grammar rule is gone and EVALUATE names `className`. PB649 is the same disease one phase later:
`ConditionRenderer.RelationCategories` now makes a relation's comparison class ONE decision from BOTH
operands, where the figurative leg used to derive the pair's class from the anchor alone and hand that answer
back to itself, so `IF ALL N"AB" < XA` answered 0 while `IF NB < XA` over identical values answered 1. Three
codes (COBOLNET2200/2201/2202), three goldens, six negatives, two drift tests each proved RED before they were
trusted green. ⚠ **A BEHAVIOUR CHANGE worth announcing: `EVALUATE X IS ALPHANUMERIC` no longer parses.**
ALPHANUMERIC is not one of §8.8.4.4.2's fourteen alternatives; it was a non-ISO extension of the deleted
duplicate rule, and only the EVALUATE spelling ever had it. And `className`'s alternative ORDER is
load-bearing — BOOLEAN stays AFTER `cobolWord`, because that IS its COBOL-2002 edition gate, measured both
ways: at `--std 85` a user-defined `CLASS BOOLEAN` still works, at `--std 2002` the same source is
COBOLNET0901.

**Cluster 3 — the intrinsic argument intake, finished and verified (PB620 + PB636).** COMBINED-DATETIME has
THREE carriers and the binary64 one had no argument rule at all, so `COMBINED-DATETIME(1, F)` with a COMP-2 F
holding 86 400 printed 1.8639999999999 in silence while the identical VALUE in a `PIC 9(5)V9(8)` item
terminated the same run unit under the same armed `>>TURN EC-ARGUMENT-FUNCTION CHECKING ON` — §15.17.3 r2 is a
rule about a VALUE, and §15.5.5 bounds that form by MAGNITUDE, so the item's USAGE cannot decide whether §15.3
applies. Both rules now live only in `CobolDate.CombinedDatetimeOutOfRange`, asked by all three carriers: one
message, one raise site. Separately the equivalent arithmetic expression of §15.17.4 — "The equivalent
arithmetic expression is as follows" — is evaluated on the SDIDI, so under STANDARD-DECIMAL the expression
form no longer loses three digits against the literal form; and `SecondsOutOfStandardForm`'s deleted
`86 400 × 10^secScale` scaling had WRAPPED past secScale 33. PB636: RANDOM's seed is a TOTAL argument, so
`Random(long)` became `Random(Int128)` — the carrier IS the totality claim — and the narrow renderer arm took
the wide intake with it. FOUR DEFECTS IN THE PREDECESSOR'S UNVERIFIED CHECKPOINT DID NOT SURVIVE: a drift test
expecting `186399999990000` where §15.17.4 r1 gives the scale-10 unscaled 18 639 999 999; a `CobolDec` compared
by REPRESENTATION where the rule asks about VALUE; a `<summary>` orphaned onto the new Real body by the
insertion; and §8.3.3.3.2 (fixed-point LITERALS) cited for a 31-digit data ITEM, whose clause is
§13.18.40.3 r14 — a real clause answering a different question, caught by `audit_code_citations`. An
UNVERIFIED checkpoint is a hypothesis, and this is the second wave running in which verifying one found real
defects inside work that was otherwise correct. Both notes land with `closes_rows: []` and a measured reason:
§15 reached zero GAP before either defect was found, and both live in the SHARED intake no single function's
rows can see, so the landing STRENGTHENED seven already-OK rows instead.

**The train.** Brought in from the three branches in manifest order. Cluster 1's content was taken as
`git diff b25f4dc59 <branch>`, NOT from the manifest's 8e321d13b base: that branch had already merged the
PB950 landing, so the manifest base would have re-applied `push-main.sh` and `kb/Work/PB950.md` on top of
themselves — the base a manifest names is the base the implementer STARTED at, not necessarily the merge base
today. Four conflicts, every one a whole list element or a whole table row, resolved by UNION and then
VERIFIED rather than assumed: the four corpus manifests were re-parsed and asserted to carry all
twenty-five new goldens exactly once with no duplicate, `ConstructRegistry.g.cs`/`Constructs.g.cs` were
regenerated from `constructs.json` (239 rows, byte-identical to the merged result) and `docs/DIAGNOSTICS.md`
was regenerated from `DiagnosticCatalog` (byte-identical to the union), so the generated files were never
resolved by hand. The traceability inventory was NOT merged: the seven `record_verdicts` batches were
re-applied in landing order on the merged tree, and because two of cluster 3's batches both record
`AR-15.17.3-2` the shape guard refuses them in one run, so they went in as separate runs, later over earlier.
The per-commit history was then rebuilt so each cluster's commit carries its own inventory rows, and the
reconstructed inventory was byte-compared against the first pass. The landing gate was the WHOLE
`Cobol.Net.Tests.Conformance` assembly UNFILTERED plus the whole Unit and Characterization assemblies and the
legacy integration suite — never a union of the implementers' filter terms, which is by construction blind to
CI's `rest` shard. GAP 2310 → 2288. Six codes claimed (COBOLNET2197–2202) and two returned (2203, 2204), so
the next free code is COBOLNET2205.

