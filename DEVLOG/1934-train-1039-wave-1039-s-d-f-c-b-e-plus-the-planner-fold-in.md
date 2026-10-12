## Entry 1934 — 2026-10-08 01:29 PDT — Train 1039: wave 1039's D, F, C, B, E plus the planner fold-in — BASE's factory, the decimal-point mode read off the item, the edited categories told apart, GR9/GR10 at the activation boundary, SPECIAL-NAMES constant-names, the reservation gate without exclusions, classConditionName, signed comma-decimal list literals (PB2489, PB2554, PB850, PB2549, PB667, PB1941 part, PB1942, PB845, PB2501, PB2292, PB2506, PB2562, PB2575)

**D — PB2489, PB2554 (both landed).** Both re-probed before any edit. PB2489: `INVOKE BASE "NEW"` failed Roslyn
compilation with CS0117, because every emit site names `{Class}__FACTORY.__Instance` and the runtime `BASE` /
`BASE__FACTORY` pair, both abstract, had no such member (§16.2.1.2 GR1: New "allocates storage for an object").
The fix is one shape for every factory: `BASE__FACTORY` is concrete with the run-unit `__Instance` singleton and a
virtual `__Create`, `BASE` is concrete with a virtual `__FactoryOfClass`, and a generated factory with ANY superclass
hides the accessor with `new` (`OoEmitter.EmitClassUnit`; it was COBOL superclasses only). PB2554: a class compiled
under DECIMAL-POINT IS COMMA and INVOKEd from a driver without it printed `[  0,12]` for 12.5, because every edit,
de-edit, receiver-scale and edited-image consumer read the EMITTING unit's mode (§12.3.7.4 GR14 b)).
`PicInfo.DecimalPointIsComma`, read off the PICTURE's clause identity, is now the only source; `ReceiverScale`,
`RuntimeApi.MaskScale`, `AlgebraicRanges.Of`/`OfFunctionReturnedValue` and `EditedImageOfNumericValue` lost their
bool parameter and every caller changed. `DecimalPointModeDriftTests` holds CodeGen and the procedure binders to three
justified readers of the unit's own mode. Five goldens (three positive, two negative); no row, no code.

**F — PB850 (landed); PB2496, PB2497 (re-probed, left to a finisher).** A rule worded in a category admitted an
alphanumeric-edited or national-edited item, because the one category screen read only `PicCategory`, which folds
the edited categories into their base. §8.5.2.1: "Use of the name of a data class or data category in the rules of
COBOL refers to the category unless class is specifically indicated", and Table 2 lists them separately.
`ItemCategory.Admits` now reads the edit axis, so RECORD and ALTERNATE KEY, FILE STATUS, ASSIGN USING, READ/RETURN
INTO, SET ADDRESS OF PROGRAM, INVOKE identifier-2 (§14.9.23.3 SR8) and START SR6 b) 2. refuse an edited item, while a
reference-modified view of one stays admitted (§8.4.3.3.4 GR6). `ItemCategory.CategoryName` is the one spelling of a
Table 2 category: five private maps route through it and two (`ClausePlacement.CategoryWords`,
`ActivationDescriptions.ElementaryCategory`) are deleted. Ten goldens (two positive, eight negative). Batch
`pb850-witnesses` re-applied on the merged tree: 8 rows, all already CONFORMS, +10 test references. PB2496 and
PB2497 still reproduce as run-time aborts of programs the compiler accepts; their notes already say so.

**C — PB2549 (landed); PB2097 (not started).** A CALL whose activating element holds no signature of the activated
program (a §12.3.8.4 GR10 c) prototype, a CALL by data-name, a CALL through a program-pointer) adopted a BY CONTENT
argument's image unconverted: 1234 into a specified program's PIC 9(3) formal arrived 123 where §14.2.3 GR9's "COMPUTE
statement without the ROUNDED phrase" gives 234, and BY VALUE overflow under EC-SIZE-TRUNCATION checking did not
raise. Every unit now registers each numeric formal's profile and its typed `CarrierLanding<T>`, a signature-less site
states its program-specifiers and EC-SIZE-TRUNCATION state, and `ProgramTable.LandArguments` lands through the
compile-time lane's own `CobolArgAdapt.LandForFormal<T>`. One golden, four unit facts (`BoundaryItemLandingTests`);
batch `batch-pb2549-witnesses` re-applied (GR-14.2.3-9/-10, already CONFORMS). The implementer found that GR9's MOVE
leg is missing on every lane (PB2587).

**B — PB667 (discharged), PB1942 (landed), PB1941 (main shape landed, residual arms open), PB1722 (not started).**
PB667 did not reproduce: PB666 (861110338) already refuses it, and the stale witness header was fixed. PB1941: a
constant's LENGTH OF operand that is a later entry of a record still being bound now binds ahead of the walk and
attaches at its position (w1032a's fa4e414b9, cherry-picked); COBOLNET0899 now names only an operand subordinate to
the referencing entry, or an unreached report group. Batch `w1032a-pb1941` re-applied (SR-13.10.3-4 stays CONFORMS).
PB1942: a constant-name in a SPECIAL-NAMES literal position was COBOL0305; §13.10.3 SR2 puts it in every literal
position "of the class and category of constant-name-1". One grammar rule (`specialNamesLiteral`), a left-edge
predicate (`classOperandAhead`) that continues a CLASS list only for a word the token stream declares a constant, and
one binder reader (`DataBinder.SpecialNamesConstant`) hold the constant's literal-1 to the CURRENCY, CLASS, ALPHABET,
LOCALE and ORDER TABLE rules; CONFORMANCE items 181 and 184 record that a declared constant-name wins over a
same-spelled code-name or locale name. Four goldens; semgrep's raw-diagnostic-code count fell 271 → 268, and the
baseline was locked in this cluster's commit.

**E — PB2506, PB845, PB2501, PB2292 (all landed).** All reproduced. PB2506: under DECIMAL-POINT IS COMMA,
`ABS(-1,5)` was split into two arguments; the comma fixed-point shape gains a lexer body and its `FN_SIGNED` twin, and
`SignedLiteralShapeDriftTests` now requires a body for every parser-assembled shape. PB845 and PB2501: the reservation
gate in `gen-cobol-words.ps1` exempted function names, so LENGTH, NATIONAL and BIT were user words where §8.9 reserves
them, while an unnamed `05 NATIONAL PIC N(3).` was refused. The exemption is gone, LENGTH joins SIGN and SUM as a
keyword-omitted reserved intrinsic (§8.3.2.1 5)), and `entryNameHere()` gives the entry-name slot to the clause
reading when the grammar parses the entry that way (`ReservedGatedWord` is the one "gated and reserved here" test).
Determination: migration-blind, so under `--permissive` `05 BIT PIC 1(8).` is the 2023 USAGE BIT item. PB2292: the
class condition's operand is `classConditionName` and `GrammarRuleUniquenessDriftTests` holds every rule name to one
definition. Nine goldens; no row, no code.

**Fold-in — PB2562, PB2575 (planner tooling, landed).** Branch `plan-wave-split-finishers` (29da61ccb, 2f28f9d0d),
cherry-picked: `plan_wave.py next_wave` counts `Train NNNN` labels as taken wave numbers, and a SPLIT report on an
unlanded branch now makes a finisher instead of waiting for a landing. `test_plan_wave.py` covers both.

**The train.** Every cluster's diff applied with `git apply -3` onto ea20c28e3 and, after train 1037t landed (PB2537's hold), rebased onto b6d771c3a; D's own DEVLOG entry was dropped and
folded in here. Conflicts: the 2002, 85 and negative corpus manifests (whole string elements, unioned; net +1, +2, +6
for E, as its patch adds), `docs/DRIFT_RULES.md` (regenerated, 296 drift tests, and again after the rebase onto 1037t, 298); `gen-cobol-words.ps1` re-run
(total=205, no change). The inventory hunks of F, C and B were discarded and their batches re-applied in order, each
in its own cluster's commit: **GAP 145 → 145**. Codes: none claimed; COBOLNET3149–3166 all returned. Gate (lander
mode, whole population): run 1 (20261008T081235Z-b3eef7) RED on one case,
`ConditionValueRecipeDriftTests.TheNumericEditedValueImage_IsPhaseNeutral`, a source scrape that pinned
`EditedImageOfNumericValue`'s old `bool decimalPointIsComma` parameter. Only D's patch touches that method (PB2554
removed the parameter), so D owns it; fixed in D's commit by pinning the new signature, whose rule (no emit context)
still holds. Attribution took about 3 minutes, by the scrape's subject, with no per-cluster run. Run 2:
`=== BUILD-LOCAL GATE: GREEN — Conformance 11,158/11,158 · Unit 32,640/32,640 · Characterization 36/36`, run
20261008T081906Z-f6c93a. semgrep PASS before and after (raw codes 271 → 268, the rest flat). CI audits run locally:
code citations 0, doc citations 0, evidence supersession 0, witness loss GREEN, drift index current, `work.py check`
OK. **Oracle: DIFFERENT, 386 of 7,588 cases, every one classified by a script over the captured blobs:** 29 ADDED
(the train's goldens); 3 DIAGNOSTICS, F's (UNSTRING faces now print Table 2's names, "category numeric" and
"category data-pointer" for "Numeric" and "Pointer"); 354 CSHARP: D's `new` on every generated factory under BASE
(213 cases); C's `formals:` and `Landing:` on the Register line of every unit with a numeric formal, and
`programSpecifiers:` / `siteSizeTruncationChecking:` on signature-less CALL sites (160 cases); D's PicInfo identity
text in activation descriptions gaining `DecimalPointIsComma = False` (9 cases); and D's `commaMode: true` dropped
from `CobolEdit.Format` calls in `nist/NC107A` and `2023/pb490_simple_insertion_comma_mode`, whose masks carry no
period or comma, so the mode cannot change an edited character (both programs' outputs are unchanged at the gate).
No fragment was left unexplained; recorded as `f7ae0509b168`, then re-recorded on the rebased commit as `9803cd950d0f` (compare: IDENTICAL, 7,588 cases). Review of the merged diff (correctness, then the
drift rules, then citations): no correctness finding was confirmed, and no cluster was dropped. One premise was
corrected: F's lead called the PIC A defect "STRING/UNSTRING", and §14.9.43.3 has no category rule, so it is UNSTRING
SR2 only (SR4 admits alphabetic for the INTO receiver). Leads filed: PB2586 (UNSTRING SR2 admits PIC A; owner
decision after the corpus exposure is measured), PB2587 (§14.2.3 GR9's MOVE leg missing on every CALL lane, a wrong
answer; claims GR-14.2.3-9), PB2588 (REPOSITORY `AS` literal positions refuse a constant-name), PB2589 (keyword-omitted
reserved intrinsics compile at `--std 85`). The rebase conflicted only in this file and plan section 0, but it
left `docs/DRIFT_RULES.md` stale (1037t added two drift tests), so the train was re-gated under the landing lease:
`=== BUILD-LOCAL GATE: GREEN — Conformance 11,158/11,158 · Unit 32,644/32,644 · Characterization 36/36`, run
20261008T085200Z-8228f3, with 1037t's self-tests GREEN (60), and `=== LINUX GATE: GREEN` on b2fba2331. A standalone
`python scripts/self_tests.py` before that re-gate had two reds (`test_orchestrate_core.ps1`,
`test_orchestrate_startup.ps1`, both on handoff synthesis) that pass alone and inside the gate, so they are
load-sensitive and not the train's; filed as PB2590.

