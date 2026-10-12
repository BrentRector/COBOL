## Entry 1858 — 2026-10-04 05:54 PDT — Train 1011: wave 1011 groups A, C, E, B2 (+B) — STRING/UNSTRING operand screens, OO BY VALUE method formals and ACTIVE-CLASS crossing, report layout rules, SAME/REDEFINES/RENAMES/CONSTANT RECORD entry rules; GAP 497 → 426

Four clusters in one landing, in manifest order A, C, E, B2. B2 merged its same-file predecessor B (wave 1011 group
B, PB1948/PB1219/PB986/PB1304/PB608) before it ran, so B lands inside B2's commit. A and C were SPLIT at a note
boundary and land only what their reports call complete.

**Cluster A — PB1180, PB1181, PB1182 landed; PB1527 discharged; PB244 resized (wave 1011 A).** STRING and UNSTRING
syntax rules were enforced at some of the operand positions they name and not at others. Re-probe on the base: every
form compiled clean and ran (STRING ALL SPACES as a sender, a zero-length or numeric DELIMITED BY literal, a `PIC 9V9`
STRING sender, a `PIC 99P` UNSTRING receiver, a bit or strongly-typed group as UNSTRING INTO, a constant-name as the
UNSTRING sender). Fix shape: one predicate per rule, asked at every position — `BeginsWithAllWord` (the bound node
loses the word ALL for `ALL SPACES`, so the operand's first token is asked too), `IsZeroLengthLiteral`, the numeric and
boolean literal arms of `UnstringSenderCategory`, `Sr8Offence` for §14.9.43.3 SR8, `HasPScaling` for §14.9.48.3 SR4.
`PicInfo.TrailingPScaling/LeadingPScaling/HasPScaling` is now the one reader of P positions (`OperandText.PExpand` uses
it). The file's 17 diagnostic messages read `DataBinder.WrittenText` instead of `GetText()` (PB1313 shrinks; it stays
open). PB1527 did not reproduce as written: an object-reference or pointer leaf is legal only under a STRONG group, and
STRING over one is already a compile error, so it was discharged with that evidence. PB244 shape (b) (DISPLAY of a
group that combines an ODO table with a DYNAMIC LENGTH leaf, and a runtime-length leaf under a fixed OCCURS) still
aborts at run time and stays open for a finisher. Goldens: `85/pb1181_string_unstring_screen_controls`,
`2002/pb1182_constant_name_literal_positions`, `2002/pb1527_object_reference_group_image`, four negatives, 24 xUnit
rows. Rows: 7 to CONFORMS (SR-14.9.43.3-2/-3/-8, SR-14.9.48.3-1/-4/-8, DOC-A.1-214).

**Cluster C — PB1251, PB1497, PB1051 landed; PB1167 not started (wave 1011 C).** PB1251: a factory or instance
definition's LINKAGE and LOCAL-STORAGE sections compiled clean. New `Binding/OoDefinitionRules.cs` is one table of
(definition kind × data-division section), asked once per definition, which absorbed the method arms; COBOLNET1519 is
now the descriptor `DiagnosticCatalog.OoDataDivisionPlacement` (same code). PB1497: an ACTIVE-CLASS formal in an
interface method prototype had no containing class (COBOLNET1924, cascading 0841). The descriptor is now owner-less
there; `SameDescriptionAs` ignores the containing class (§9.3.8.2.3 rule 2 d)); an ACTIVE-CLASS formal crosses the
method ABI as `CobolObject?` (`OoEmitter.OoFormalCrossingType`), one signature for the method, its overrides and the
prototype, narrowed by a cast at copy-in and copy-out. Sibling fixed: an inherited ACTIVE-CLASS method invoked on a
subclass generated CS1503. PB1051: a BY VALUE method formal was COBOLNET0899. `OoFormal.ByValue` is threaded
transitively (§14.2.3 GR4); `ProcedureHeaderScreen.ByValueClass` is the one §14.2.2 SR2 screen for the program and
method arms; INVOKE takes its argument's mode from the formal (§14.9.23.4 GR6 b)), refuses SR5 a)/b) mismatches and
SR15 (new COBOLNET2741, by class, not category); the callee skips copy-out (§14.2.3 GR10); a universal invocation never
matches a BY VALUE formal (§9.3.6 rule 3 a)) and ends in EC-OO-METHOD; method conformance compares the passing mode per
formal (§9.3.8.2.3 rule 1). PB1112 keeps only its universal leg (its override row closed under PB1497). Rows: 24
(GAP 490 → 467). PB1167 (ANY LENGTH RETURNING) stays open for a finisher with the plan in its note.

**Cluster E — PB1222, PB1224, PB1288, PB1289 landed (+PB1298); PB1270 stays open (wave 1011 E).** The report group's
layout rules compiled clean in every violating shape (`COLUMN 20` then `COLUMN 10`, `PAGE LIMIT 10000`, a page heading
line below FIRST DETAIL, a TYPE-less level 1 entry, SR16 on a printless group). Each kind of fact got one home: the
written clause (`ScreenReportEntryClausePresence` SR5/SR9/SR12, `ScreenReportColumnClauses` SR10 a)/b),
`ScreenReportPageIntegers` SR5/SR6), the printable line (`ScreenReportColumnArrangement` SR7, SR8 a)–c) over
`NominalPlacements`, the one horizontal walk the width computation also uses) and the group (`ScreenReportGroupLines`,
§13.18.35.3 SR6 a)–e), with `GroupLimits` for §13.18.57.4 GR7/GR8 and `MinimumLastLine` as the one vertical walk). BLANK
WHEN ZERO and JUSTIFIED on a printable item run the data division's predicates. `ReportModel.WrittenEntries` lets the
PRESENT WHEN and name scans read what was written, across every RD. Determination recorded in docs/CONFORMANCE.md:
"subject to a different PRESENT WHEN clause" means each carries a clause the other lacks. New COBOLNET2744/2745; two
COBOLNET0899 descriptors folded into 2247. Five corpus programs that ran past the syntax rules to reach a run-time
EC-REPORT-* were rewritten with items under different clauses. Rows: 15 (467 → 452); PB1270 keeps three PARTIAL rows
and two residues (a relative first line above its upper limit, a relative footing past the page limit).

**Cluster B2 — PB1087, PB1242, PB1262, PB1280, PB1283 landed; with B: PB1948, PB1219, PB986, PB1304, PB608 landed
(wave 1011 B, B2).** B: a constant-name now stands at RESERVE, BLOCK CONTAINS, RECORD CONTAINS/VARYING, DYNAMIC LENGTH
LIMIT, PICTURE LOCALE SIZE and LINAGE (one `integerOperand` grammar rule and the `IntegerOperandValue` reader); a
TYPEDEF's RENAMES is cloned onto every group the type is applied to (`CloneRenamesOnto`, §13.18.58.4 GR1); a RENAMES
window that contains a table tiles one run per outer occurrence (PB986). B2: the SAME clause (new
`DataBinder.IoControl.cs`, SORT and SORT-MERGE one format per SR1, one occurrence table for SR5–SR7, SR2/SR3/SR8–SR10
and §13.18.27.3 SR2 GLOBAL; COBOLNET2738), the CONSTANT RECORD subtree rules moved to the composed forest after
ExpandTypes so TYPE and SAME AS clones are asked (`CheckConstantRecordSubtrees`; PB1262 was a two-arm find), the
REDEFINES entry rules (`DataBinder.RedefinesEntry.cs`: SR1, SR2, SR3, SR8 in bits with the EXTERNAL conjunct, SR13,
SR15; the target is the nearest same-named non-redefinition in its own section, which also removes a COBOLNET1656
false reject; COBOLNET2739) and the RENAMES entry rules (`DataBinder.RenamesEntry.cs`: SR2, SR3, SR5, SR6, SR11 both
halves; a 66 binds to the record this walk last opened, never `_lastRoot`; COBOLNET2740). One documented acceptance
was withdrawn because SR11 controls: `RENAMES A THRU B` where B is a shorter redefinition of an earlier A
(`2023/pb96_renames_span_over_redefines` re-derived). Rows: B 1, B2 25 (452 → 426).

**The train.** Applied cluster by cluster with the inventory excluded from every patch and each cluster's
`record_verdicts` batches re-applied on the merged tree, in order (B2's eight batches one run each: SR-13.18.45.3-3 is
in both PB986's and PB1283's). `docs/DIAGNOSTICS.md` conflicted twice (E, B2: rows appended at the same spot) and was
regenerated from the catalogue. No conflict markers at any checkpoint; manifests and the inventory parse with no
duplicate keys. Codes used: COBOLNET2738–2741, 2744, 2745, in disjoint ranges. Gate:
`=== BUILD-LOCAL GATE: GREEN — Conformance 10,205/10,205 · Unit 30,451/30,451 · Characterization 35/35` (lander mode,
run 20261004T124528Z-073e12); legacy Integration 503 passed, 1 skipped; `=== LINUX GATE: GREEN (legs unit
characterization conformance) ===`; semgrep verify PASS, raw-diagnostic-code literals 287 → 283, nothing rose. Review
of the merged diff: no correctness finding, no cluster dropped; one comment fix (`AllItems` had lost its summary to
`RenamesOwners`). PB986's `closes_rows_reason` said its row "stays DIVERGES under PB1283", which this same train closed;
reworded. Leads filed: PB1949 (VALUE before an OCCURS with TIMES and STEP fails to parse, re-probed), PB1950
(`2002/typedef_same_as` compiles red under the CLI's default fixed format because its line 40 runs past column 72,
re-probed). GAP 497 → 426.

