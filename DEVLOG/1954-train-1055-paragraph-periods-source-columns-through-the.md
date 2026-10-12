## Entry 1954 — 2026-10-10 23:48 PDT — Train 1055: paragraph periods, source columns through the origin map, universal OBJECT REFERENCE returns, I/O commit order, TYPEDEF clause resolution, the not-located reason

Train 1055 carries six wave-1055 compiler clusters in one landing (A, B, C, D, E, F), each a finisher that
cherry-picked an earlier wave's unlanded branch onto main 316a9d731 and gated leg 1 only (batched gating, PB2515).
`train_apply.py` brought them in, one commit each. The first lander stopped at its 220-turn cap after gate 1 and the
review; a fresh lander resumed from its STATUS.md and landed the train. This is the night-stop train (owner,
2026-10-10 21:30 PDT: "stop new work. Get everything in progress to main"), and it carries no other branch.

**A — PB2727 + PB2729 + PB2773 + PB2588: one closing period per paragraph, float significands, locale-collation
failures, AS constant-names.** I-O-CONTROL, SPECIAL-NAMES and REPOSITORY each print ONE separator period inside the
outer bracket (§12.4.6.2, §12.3.7.3 SR31, §12.3.8.2), and `(entry DOT?)*` under-rejected both ways at every
edition. The grammar keeps the punctuation superset on the paragraph rule so a misplaced period is refused by NAME:
`Frontend/Cst/ParagraphPeriods.cs` is the one table and sentence splitter, and `ClosedFormatPass.ScreenParagraphPeriods`
the one refusal — COBOLNET3407 (an interior period; the 2023 APPLY COMMIT sentence boundary is licensed) and
COBOLNET3408 (no closing period). `ParagraphPeriodDriftTests` holds every `.g4` rule that writes `DOT?` to a row.
72 per-entry periods were deleted in 52 corpus sources and 14 inline REPOSITORY sources, with no emitted-C# change.
The CHANNEL / RESERVE half (carried from w1053b) refuses those vendor entries (negatives
`pb2727-special-names-{channel,reserve}-entry`); `2023/pb1689_vendor_channel_clause_still_parses` is removed with
it. PB2729: a float significand whose decimal point is its rightmost character (`3.E2`) crashed the backend in a
VALUE and was silently accepted in COMPUTE; now refused (four negatives, `2002/pb2729_float_significand_forms`).
PB2773: a malformed site CLDR file or a stale `.tailor` now has a failure contract instead of an exception at
compile or start-up. PB2588: a REPOSITORY AS position admits a constant-name (§13.10.3 rule 2). The review found
`CollationEngine.IsKnownLocale` answering true for a sound CLDR file under a stale site tailoring, so IS LOCALE ran
in the failed resolution's root order silently; it now asks `ResolveLocale(tag).Failure` first (regression
`LocaleCollationFailureTests.ASoundCollationFile_UnderAStaleSiteTailoring_IsNotAvailable`), and a grammar comment
naming the wrong code was corrected. Three inventory rows re-witnessed (all CONFORMS), GAP unchanged.

**B — PB1688 + PB1671 + PB1801: diagnostics name the SOURCE column.** The §6.5 logical conversion's map carried an
origin LINE per resultant line and no column, so every fixed-form diagnostic printed 7 columns left of the text
(relative to column 8) and every COPY/REPLACE diagnostic printed column 1. PB1801's design is built as one
mechanism: `LineOrigin` origin spans are born in `ReferenceFormatProcessor.LogicalLineBuilder#TextSite`,
`MappedText.LocateAt` / `PointAt` replace `OriginAt`, and every `SourceLocation` producer is swept. The review found
the sibling the sweep missed: `>>SOURCE FORMAT` diagnostics (COBOLNET1911 / COBOLNET0900) still named column 1, and
now name the directive indicator's physical column (`ReferenceFormatProcessor.DirectiveIndicatorIndex`; regression
`DiagnosticPositionTests.TextManipulationAndDirectiveDiagnostics_NameTheirColumn`). It also found the `--nist`
placeholder substitution rewriting columns after the map is built: filed as PB2986 and named as the one exception in
`DESIGN-cc-in-copy.md` §3.5. Negative `pb1688-fixed-form-diagnostic-column`. No ISO clause governs a diagnostic's
position; the golden cites §6.5 5) and its NOTE for the conversion.

**C — PB2463: a universal INVOKE returning an OBJECT REFERENCE into a typed receiver of an unrelated class.**
§9.3.6 rule 6 now asks the SET compatibility the returning pair must satisfy (§14.9.39.3 SR8/10/12/14), so the pair
§14.9.39.3 SR12 refuses finds no matching method and the search ends in EC-OO-METHOD, where it matched and raised
EC-OO-UNIVERSAL later. The table moved to the runtime `ObjectReferenceSet.Refusal`; `OoClassTable.SetOperand`
derives the class facts once and activation descriptions carry them as `ObjectClasses` / `ObjectInterfaces`; four
arms use it (rule 6 `MaySet`, CALL BY CONTENT, CALL RETURNING per §14.8.3.3 rule 1, BASE's FactoryObject
description). The whole-population gate showed `StandardClassBaseTests` asserting the old reading; under this rule
BASE's New returns an ACTIVE-CLASS reference contained in BASE, so a universal New into a subclass-typed receiver is
EC-OO-METHOD. The FactoryObject test now receives into universal items and was renamed, and its old name is RETIRED
from `GR-16.2.2.2-1` through `record_verdicts.py` (gate 2's WITNESS LOSS audit caught the first lander's hand edit
of the test-ref). Goldens `2002/pb2463_universal_returning_set_match`, `2002/pb2463_call_object_reference_set_conformance`
and a below-2002 negative. PB2984 (a drift-guard hole) and PB2985 (§14.8.3.3 rule 2's ACTIVE-CLASS sender, a silent
wrong answer, extended by the review) are filed.

**D — PB2848 + PB2834: four I/O findings and the line-sequential REWRITE rule order.** `CobolFile.ResolveHostPath`
is total and never throws; an OUTPUT writer under record locking falls back to a write-only handle, so a write-only
file answers 00 rather than 37 (§14.9.27.4 GR3); `_writerReads` keeps `ReleaseRecord` from counting the medium; and
`FileConnector.CommitGate` makes the lock ceiling (§12.4.5.9.4 GR7) the last check at all eight WRITE/REWRITE commit
points, pinned by `CommitGateDriftTests`. PB2834: line-sequential REWRITE tests §14.9.35.4 GR17 b) ('44', the record
size) before d) ('71', no byte image). The lander made `LineSequentialEncoding.ChannelLength` and `Put` read one
`Utf8Length` (the report's lead), updated `pb1760`'s X2 to 44 (GR17 b) correctly first), rewrote a CONFORMANCE.md
citation of `DOC-A.1-104` outside its row key, and made `FileAuthorityPresenceTests`' 40,000-character path case
accept the Linux outcome (Linux makes it absolute). Goldens `2023/pb2834_ls_rewrite_size_before_charset`,
`2002/pb2848_lock_ceiling_after_record_rules`, negative `pb2848-lock-clause-below-2002`.

**E — PB2548 + PB2833: TYPEDEF templates go through every clause resolver; one reader of a variable component's
maximum.** `DataBinder.ClauseResolutionForest()` (roots, type declarations, template items) is the one forest the
RENAMES, OCCURS KEY, DEPENDING ON and OCCURS DYNAMIC resolvers and the REDEFINES strong screen walk, so an unused
template's RENAMES operand or KEY data-name that names nothing is refused (COBOLNET1655, COBOLNET2353);
`InTypeDeclaration` gates the rules that read the placement. A DEPENDING ON counter the declaration does not define
stays legal in an unused template (§13.18.57.4 GR1: a use site may supply it). PB2833: `OccursSpec.MaxCapacity` /
`DataItem.VariableComponentMax` is the one reader of a variable component's maximum (`DynMaxOf` and
`GroupImageCodec`'s copy deleted), so a dynamic table's maximum is the implementor ceiling (§8.5.1.9.1), not 0, and
a contiguous image stored into a cell-backed group keeps the table's occurrences. The review found `OdoResolve`
judging a template's DEPENDING ON counter against ANY item of that name, so an unused template beside unrelated CNTs
drew COBOLNET1639 or COBOLNET0852 and two use sites each supplying their own CNT were rejected; the declaration arm
now keeps only candidates inside the declaration (goldens `2002/pb2548_template_counter_per_use_site`,
`_unused_elsewhere`). Its aside — a TYPEDEF whose dynamic table names `CAPACITY IN` cannot be used by two TYPE
entries (COBOLNET1523 on the second expansion, legal per §13.18.38.3 SR30) — predates the train and is PB2988.

**F — PB2847: the not-located reason on SET … TO ENTRY and ADDRESS OF FUNCTION.** Those two address-identifiers
dropped the reason CALL and ADDRESS OF PROGRAM report, and `ProgramTable._probeFailure` keyed a probe failure by
name only. `ProgramTable.AddressNotLocated(name, function)` is the one wording of an address-identifier miss;
`PtrEmitter` reads the name into a local once and passes it to `EcEmitter.EmitConditionRaise`'s new detail
expression on the fatal path; `ProgramTable.ProbeKey(name, wantFunction)` keys both the probe set and the failure
map. `NotLocatedReasonTests` (7). No golden output changes.

**The train.** Windows lander gate 1 (run `20261011T053150Z-3bb99d`) was RED on six cases, all attributed by the
first lander (C two, D two, one interaction, and `GateSlotDriftTests`), and Linux gate 1 RED on the same plus
`FileAuthorityPresenceTests` (D). Gate 2 on the repaired train (run `20261011T061343Z-dbbcdc`) was RED on two:
the WITNESS LOSS audit (C's renamed witness, above) and `GateSlotDriftTests.SelfTest_DrivesAndPassesEveryArm`
AGAIN — `gate_slot.py --self-test` timed out twice at the test harness's fixed 120 s budget, while the same gate's
self-test phase ran it GREEN in 314.9 s (gate 1: 303.7 s) and it takes 9.2 s alone. A timing failure is a failure
(the owner, 2026-10-10), so the root was fixed in the train as its own commit: the drift test re-ran a self-test the
runner already runs in every lander gate, the Linux gate and CI on both systems, so it now holds the ten arms by
name and the ALL GREEN count from the source and runs nothing (`PB2882: GateSlotDriftTests holds the arms from
source, not a rerun`). PB2882 records both reds and the sibling sweep: `LandingLeaseDriftTests` (106.8 s under load
against the 120 s budget), `GateLegDriftTests` and `SelfTestDiscoveryDriftTests` re-run self-tests of 231-236 s
under load the same way, the next timing reds, left open there. Gate 3 on `01e02b208`: `=== BUILD-LOCAL GATE: GREEN
— Conformance 11,453/11,453 · Unit 33,352/33,352 · Characterization 36/36` (run `20261011T063356Z-1b0a8b`), and
`=== LINUX GATE: GREEN` on the same head. Semgrep counts unchanged. Codes claimed: COBOLNET3407, COBOLNET3408 (A);
every other assigned range returned. GAP 103 → 103. The review (four agents) found five issues and one aside: four
fixed in the train (A two, B one, E one), B's NIST stage filed (PB2986), the aside filed (PB2988); no cluster
dropped. Notes PB2982–PB2986 and PB2988 filed; PB2882 and PB2985 extended.

**The oracle.** `compare_oracle.py`: DIFFERENT on 1,253 of 7,755 cases against `6994ce119e40`, every one attributed
by class (every added line of every C# case, in compare_oracle's 80-line excerpt per case, matched a class): B —
1,075 diagnostic streams whose only change is a position: 1,036 only at +7 (611 NIST, 346 negative, 63 matrix, 16
corpus; a fixed-form position is now counted from column 1 of the line, not of the program-text area) and 39 in
which COPY / REPLACE / directive diagnostics moved from column 1 (or a wrong column) to their operand's or
directive word's source column (e.g. `pb1356-replace-produces-replace`'s parse error from (14,20) to the `=` at
(14,16)); C — 28 C# cases whose activation descriptions gain `ObjectClasses` / `ObjectInterfaces`; E — 77 C# cases
whose dynamic table's maximum is 1073741823 where it was 0 (contiguous layouts, EXTERNAL / BASED groups holding
one); F — 41 C# cases (one shared with C) whose SET … TO ENTRY / ADDRESS OF FUNCTION read the name into a `__ppNm` /
`__fpNm` local and pass it to the not-located detail; ADDED 32 (the train's new goldens:
A 18, B 1, C 3, D 3, E 7) and REMOVED 1 (A's `pb1689_vendor_channel_clause_still_parses`). No diagnostic changed a
code or message. The baseline is re-recorded on the train's tree as the landing's last commit.

