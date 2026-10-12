## Entry 1600 — 2026-09-13 11:05 PDT — Landing train 32 — FIVE clusters in one landing: PB415 (INITIALIZE's category-name is a SET of thirteen words, and the braced choice is mandatory) + PB394 (the sending value is evaluated ONCE, over one mechanism) + PB355 (START's temporary key area is cut from the RECORD AREA) + PB526+PB525 (the LINAGE violation becomes an exception condition, with an I-O status of its own) + PB646 (a national-form item IS its DISPLAY twin composed with one byte transform), GAP 2517 → 2501

**PB415 — INITIALIZE's `category-name` is a SET of thirteen words, and the braced choice was never optional.**
The printed general format (PDF p667 / folio 637, rendered rather than read from the OCR) puts `category-name`
in BOTH phrase slots under choice indicators that §5.2.6.4 reads as "one or more, each only once" — so the
grammar's single-word `initializeCategory` was a scalar where the rule names a SET. It is now
`initializeCategory : initializeCategoryName+` with an `InitializeCategorySet` bitmask carried in both slots of
`InitializeSpec`, which is what makes `REPLACING NUMERIC ALPHANUMERIC DATA BY …` compile — at `--std 85` as
well. Four new lexer tokens plus their `cobol-words.json` rows (each a user word below its §8.9 reservation, so
gating is per word: `initialize-category-2002/-2014/-2023`, with the MESSAGE-TAG row left `pending` because the
A.4 message-control facility is declined). The braced `{ALL | category-name}` in the VALUE phrase had been
`?`-optional, defended in a comment by a "§14.9.20.2 note 2" the clause does not contain and pinned green by
`2002/initialize_phrases.cob`; it is now required (COBOLNET1981) and that golden spells `ALL TO VALUE` with its
`.out` unchanged. §14.9.20.3 SR3 (a pointer/object/message-tag category in REPLACING requires identifier-2)
became COBOLNET1982 and SR4 (the SET/MOVE validity of the pairing) COBOLNET1983, both at bind time; PB418's
staged-LOUD GR6b arm turned out to be the real `InitializeSetFrom` and is answered here. Sibling swept: the hard
`EDITED` lexer token — not an ISO reserved word at any edition, so `01 EDITED PIC X.` was COBOLNET0001
everywhere — is DELETED along with the non-standard two-token spelling it served. A drift test binds grammar ↔
enum ↔ `reserved-words.json` with both failure branches fired. Ten fixtures (85 · 2002×3 · 2014 + five
negatives). Rows: FMT-14.9.20.2, GR-14.9.20.4-1/-2/-5/-6 and SR-14.9.20.3-3 close; SR-14.9.20.3-8 stays PARTIAL
on PB418's open GR7 owner determination, so the note is `status: half`. What did NOT hold on today's tree: two
of the note's four defects had already landed with PB695 family 2, and the note is re-scoped to say so.

**PB394 — the SENDING VALUE is evaluated ONCE, and it is one mechanism, not five.** §14.9.25.4 GR1 (MOVE) and
§14.9.13.4 GR3 (EVALUATE) say the same thing about different verbs, so they get one implementation:
`Binding/Procedure/SendingValueTemp.cs` over the existing `DataBinder.CreateCompilerTemp` and the `PendingPreOps`
list drained at `BindStatement` — which is simultaneously GR1's "immediately before the first receiving operand"
and GR3's "at the beginning of the execution of the EVALUATE statement". The temp's description is the operand's
OWN (an identity move, §8.4.3.3.4 GR6, with §8.5.1.10.4 as the carrier for a ref-mod and §15.4's temporary for a
function result); literals and figuratives are deliberately NOT materialized, because §8.3.3.6.4 GR2 makes them
unable to vary. Before: `MOVE A(B) TO B, C(B)` answered C=00009 where the rule requires 00005;
`MOVE FUNCTION RANDOM TO R1, R2` gave two different numbers; `EVALUATE FUNCTION RANDOM` consumed four. The
sweep found four SET formats pasting the sender's TEXT into every receiver (§14.9.39.4 GR12/14/16/18), fixed
through `EmitContext.SendOnce`; arithmetic, UNSTRING and INSPECT were verified correct, and INITIALIZE
REPLACING, the abbreviated relations and SEARCH are correct BY RULE (each rule quoted at the site). One
regression was introduced and measured out rather than shipped: an ODO group sender materialized at MAX extent,
wrong against a JUSTIFIED receiver — excluded, with a golden line pinning the exclusion and GR1's length-once
sentence recorded as a lead with its exact fix. The negative `pb17-function-subscript-evaluate-subject` is
RETIRED: it pinned COBOLNET1509 on `EVALUATE W-E(FUNCTION INTEGER(1))`, a rejection of legal source that existed
only because the subject was re-bound per WHEN, and it is replaced by a positive at four editions.
`DESIGN-binder-bound-tree.md` grows §3.7, because the note's proposed repair ("hoist into one C# local") is not
implementable — each receiver CONVERTS from the value, so the thing that must be hoisted is the VALUE, in an
intermediate result item. Rows: GR-14.9.25.4-1 and GR-14.9.13.4-3 move DIVERGES → PARTIAL with each residual
named on its row, so the note is `status: half`.

**PB355 — START's temporary key area is cut out of the RECORD AREA, not out of the operand.** §14.9.41.4 GR17 a)
says the key "is set up by moving the relevant parts of the record area into a temporary data area" — the record
area, not data-name-1's own content padded with spaces, which is what `KeyedIoEmitter.EmitStart` was sending.
`START … KEY IS EQUAL TO IX-GEN WITH LENGTH 4` answered '23' where the record area held the key 'AB01', and
`START … > GN-PFX WITH LENGTH 4` silently positioned on the wrong record. The emitter now sends
`OperandText.RecordAreaImage(refs.RecordArea(file))` and `IndexedConnector.Start` builds the area as
`KeyOf(Fit(image), keyIndex)[..compareLength]` — the SAME extraction that random READ (§14.9.30.4 GR32) and
DELETE (§14.9.10.4 GR3) already perform, so the three verbs cannot drift apart. A new
`ReferenceResolver.RecordArea(FileModel)` replaces five hand-written copies of
`file.AreaRecord is {} ar ? ResolveItem(ar) : null`. Three START arms were separated: FIRST/LAST take no
operand, RELATIVE's key lives outside the record (GR10) and stays numeric, INDEXED is the one fixed.
`StartTemporaryKeyAreaDriftTests` was measured with the defect restored — 3 of 4 red. And NINE sites were found
carrying a PARAPHRASED §13.4.2 citation the standard does not contain (`--check` FAIL; the real clauses are
§13.18.33.4 GR3 and §13.18.43.4 GR5 a)) — all nine corrected here. What did NOT hold on today's tree: the note's line numbers and
its quoted emitter call had both moved under PB327 (the defect it describes was intact; the code it quoted was
not), its "the national branch is unreachable — a national key is refused COBOLNET0899" was STALE (the national
leg compiles and is now witnessed by a golden of its own), and its "PB355 and PB354 part 4 land together" no
longer applies — PB354 is landed on main, and its SR6 screen is exactly what makes reading the key out of the
record area sound. GR-14.9.41.4-17 moves DIVERGES → CONFORMS.

**PB526+PB525 — the LINAGE exception condition becomes an exception condition, and gets an I-O status to be
selected by.** The note said to route the two LINAGE value rules through the EC subsystem; re-probing showed
routing alone cannot make a format-1 `USE AFTER STANDARD ERROR` fire, because a format-1 declarative is selected
by the I-O STATUS (§14.9.49.4 GR6) and §9.1.13.1's status→EC map has no EC-I-O-LINAGE entry at all. So the
violation is given I-O status **'90'**, §9.1.13.11's implementor-defined slot, fatal under this compiler's
existing determination and Table 13 — a new `docs/CONFORMANCE.md` §7 row DOC-A.1-110, and a DETERMINATION the
owner may want to overturn: GnuCOBOL answers '57', which is NOT followed here because its first digit is a
nonfatal class raising EC-I-O-RECORD-OPERATION, a contradiction rather than a latitude. The exception NAME now
travels beside the status (`FileConnector.IoConditionName`, cleared by the single `Status` setter;
`FileRegistry.IoConditionName` the one combiner; the generated hook changed by one line), so
`>>TURN EC-I-O-LINAGE CHECKING ON` reads EXCEPTION-STATUS = EC-I-O-LINAGE and no longer EC-I-O-IMP. PB525 was
FOLDED IN by measurement rather than by plan: GR6 b) 2's "greater than zero and not greater than the page size"
had nothing to validate while 0 still encoded "FOOTING absent" — same evaluator, same rule, the same two rows.
After: the declarative fires with FS=90, execution continues after the WRITE, LINAGE-COUNTER stays 0 until
CLOSE, later WRITEs re-raise, and a fresh OPEN OUTPUT restores. Sibling fixed in the same pass: `EmitWrite` ran
the AT / NOT AT END-OF-PAGE branches with NO success guard (§14.9.51.4 GR27/GR28) — both write arms are latched
now. Three goldens (2023 format-1, its 85 twin, and the `>>TURN` path), five `LinageConformanceTests` including
a discriminating footing-sentinel pair, one unit fact. GR-13.18.34.4-6 and -3 move PARTIAL → CONFORMS and
DOC-A.1-110 → CONFORMS.

**PB646 — a national-form item IS its DISPLAY twin composed with the one national byte transform.** Data-model
design **D-N7**: `ByteForm` answers `Zoned` for both usages, `GroupImageCodec` COMPOSES the byte transform over
the carrier codec, and `NationalWindow.PositionsOf` collapses to the single test `Pic.Usage is National`. The
§13.18.60.3 SR12 staging behind COBOLNET0899 ("Phase 4a residue") turned out to be FOUR arms —
`AnalyzeFloatEdited` and `AnalyzeLocaleEdited` each carried a private copy that was also the only SR3 screen its
path ran — and all of them now call the one screen. Five defects the note did not have were found and fixed: SR7
asked at the wrong time (moved into a declared pass); §13.18.8.4 GR2 conditioned on DISPLAY, so
`BLANK WHEN ZERO` on a national item stored `000`; `SignKindFor` sent every non-DISPLAY usage to BinaryMinus
against §13.18.52.3 SR2's "display or national"; §15.50.4 r2 re-derived the position count; and ref-mod had a
two-arm `RefModExclusion`/`RefModView`. The finisher found two MORE arms of the same shape: §13.18.49.4 GR5's
SAME AS sign transform kept a DISPLAY-only guard (extracted as `DataBinder.ApplyEffectiveSign`, the twin of
`ApplyEffectiveUsage`), and §13.18.63.3 SR14 screened an alphanumeric group where a national one was in front of
it — `GroupValueSlicer` was dropping a GROUP-USAGE NATIONAL group's whole VALUE distribution, a silent wrong
answer. Five tests that pinned the staging flipped to behaviour (NAT1 → a positive edition-gate theory plus
`NoDataSkeleton_StagesAt0899`; `NumericByteFormDriftTests(National)` → Zoned; `DisplayUsageUnionDriftTests` →
two counts per file, DISPLAY-only versus the `Display or National` pair; `LoudGuardTests` split;
`ConstructRegistryDriftTests` regenerated — 229 rows in the implementer's tree, 232 after the merge with train
31's own construct rows). Seven implementer goldens plus the finisher's legs,
the 0899 descriptor re-texted, DIAGNOSTICS regenerated, PHASE4_RECONCILIATION updated. What did NOT hold: the
implementer's own self-review recorded `GroupValueSlicer`'s arms as unreachable "by rule" on a §13.18.63.3 SR14
that answers a DIFFERENT question — a national group is not an alphanumeric group — and that wrong closing
sentence IS the finisher's defect B, shipped green (feedback_reachability_is_measured_not_deduced, and the
closing sentence getting the least scrutiny again). Six §13.18 rows move PARTIAL → CONFORMS and the note lands.

**The train.** Every cluster was based on `a1de916e`, eight commits behind `origin/main`, so three of the five
needed real conflict resolution. PB394's `EvaluateBinder` met train 31's PB398: main had added the
partial-expression arm, PB394 had replaced the per-WHEN subject bind with a once-per-statement `SubjectSlot`, and
the merge is NOT either side — the partial-expression arm is kept and MOVED ABOVE the `slot.Value` read, because
§14.9.13.3 SR8 splices the subject INTO the condition (where the relation side binds it) and there is no
receiver to convert the slot's value into; touching `slot.Value` first would materialize the intermediate result
item AND bind the operand again inside the rewrite, two activations for one written reference. The residue stage
is therefore narrowed to that one arm and its text says so, which is exactly why GR-14.9.13.4-3 lands PARTIAL
and not CONFORMS. PB355's two READ INTO sites met train 31's PB337: PB355's `ctx.Refs.RecordArea(file)` with
PB337's `IntoPhraseRules.Read` signature. Six conformance manifests were unioned. The five verdict batches were
re-applied with `record_verdicts.py` on the merged tree in manifest order (21 records, 21 rows changed, 17
re-adjudicated) rather than letting five patches fight over `traceability-inventory.json`; the generated
artifacts — `DIAGNOSTICS.md`, `Constructs.g.cs`, `ConstructRegistry.g.cs`, `CobolWords.g4`,
`CobolLexerWordSet.g.cs` — were regenerated on the merged tree and reproduced the clusters' bytes exactly. Codes
claimed: 1981–1983, all PB415's, inside its assigned range and colliding with no other cluster's. GAP 2517 →
2501. Union gate filter: `~Drift|~EditionGate|~Initialize|~Corpus|~VersionMatrix|~Nist|~Move|~Evaluate|
~SendingValue|~Set|~Unstring|~Inspect|~Arithmetic|~Pointer|~Justified|~FileIo|~Io|~KeyedOffsetSpec|~Start|
~Linage|~Sequential|~National|~Usage|~DataSkeleton|~Sign|~SameAs|~Value`, 27 terms all live, plus the full
`Cobol.Net.Tests.Unit` assembly, `Cobol.Net.Tests.Characterization`, and the legacy
`CobolSharp.Tests.Integration`. The FIRST run of that gate was RED on exactly one case, and it is worth the
paragraph: `UdfInvocationTests.PerEvaluationResidueOperands_1509(UDFT6H)` pinned COBOLNET1509 on
`EVALUATE FUNCTION UDFDBL(2)` under the doc comment "this lowering re-binds subjects per WHEN — a hoist would
over-activate". That premise is the DEFECT PB394 removed, and PB394's report retires the twin pin in the
negative golden `pb17-function-subscript-evaluate-subject` — it simply never ran this one, because no term of
its gate filter selects `UdfInvocationTests`. Proven by probe before the test was touched: the source compiles
and activates the function EXACTLY ONCE per statement, at one WHEN (rendered inline) and at three
(materialized). The row is replaced by a positive Fact and the surviving negative's "three windows" comment
corrected to two — a test-side sweep of the implementer's own decision, so the cluster was NOT dropped. The
re-run was GREEN: Conformance 6537/6537, Unit 23874/23874, Characterization 33/33, the legacy Integration
assembly 503/504 (1 skipped), and the citation audits at 0 findings / 0 MISFILED / 0 unmarked claims. Semgrep
moved `cobolnet-raw-diagnostic-code-literal` 416 → 420 and every one of the four was attributed per added line:
PB415's three had descriptors in `DiagnosticCatalog` and emitted RAW LITERALS anyway — converted to
`Error(DiagnosticCatalog.…)`, which `EditionContext` implements as `Error(descriptor.Code, message)` so no
golden moved — and PB646's one is the twenty-sixth site of `COBOLNET0898`, a BAND code with no descriptor and
no prospect of one under the catalogue's one-code-one-rule invariant; that one is re-baselined to 417 with the
disposition written into `kb/Work/PB175`, which owns the baseline. No cluster was dropped.

**Determinations the owner may overturn, recorded here so they are visible.** (1) PB526's I-O status **'90'**
for a LINAGE violation, over GnuCOBOL's '57' — the survey answer is declined on the ground that '57' is a
nonfatal class raising EC-I-O-RECORD-OPERATION, which contradicts the fatality this compiler already determined
for the condition rather than exercising latitude within it (`docs/CONFORMANCE.md` §7, DOC-A.1-110).
(2) PB415's per-word edition bands for the thirteen `category-name` words (`initialize-category-2002` /
`-2014` / `-2023`), with the MESSAGE-TAG row left `pending` because the A.4 message-control facility is a
declined module — a category word gated on the edition that introduced it rather than on the edition that
introduced the SET.

**Leads recorded, unfiled (fourteen).** From PB415: (a) `MOVE "Q" TO <PIC ZZZ9>` yields `"   0"` — the
alphanumeric sender is read as a numeric value and then edited, a SILENT WRONG ANSWER in
`MoveEmitter.ConvertSource` needing a §14.9.25.3 Table 16 derivation; (b) `NATIONAL` is admitted to `cobolWord`
ungated while its §8.9-straddling siblings carry `{userWordHere}?` — exempt by construction in
`gen-cobol-words.ps1` step 4b, but whether the exemption should be positional is established nowhere. From
PB394: (1) §14.9.25.4 GR1's length-once sentence for a run-time-extent group sender — `MOVE ODO-G TO N, Z`
answers `"1    "` where `"125  "` is required, with the fix shaped inside `CreateCompilerTemp`; (2) a CONDITION
selection subject is re-analysed per WHEN (§14.9.13.4 GR3 e)) because the bound tree has no
condition→boolean-operand bridge — the bridge is the fix, after which `UdfStagePerEvaluationResidue` can be
deleted outright. From PB355: (3) `audit_code_citations.py` cannot see a PARAPHRASED citation, which is the
inheritance hole CLAUDE.md rule 1 names and is how nine sites carried a §13.4.2 that does not exist while both
audits sat at zero findings; (4) `build-local.ps1` calls a term INERT when its tests live only in the assembly
the gate runs unfiltered — the message should separate dead from inert-because-unfiltered. From PB526: (5)
`EC-I-O-EOP` and `EC-I-O-EOP-OVERFLOW` are never set to exist — §14.9.51.4 GR27 a) requires both, and there is
no mask bit and no raise site; the channel this landing built is precisely what they need; (6) UNVERIFIED, not a
finding: `CodeSetConversion.cs:106` throws a raw `InvalidOperationException` on an unrepresentable CODE-SET
character. From PB646 and its finisher: (7) the §8.8.4.4 BOOLEAN class condition has no grammar token at all, so
`IF BD IS BOOLEAN` is COBOLNET0901; (8) §13.18.8.3 SR1 and SR2 — BLANK WHEN ZERO's own screens exist for no
usage, and the inherited spelling is refused by a DIFFERENT rule, so the two spellings of one defect disagree;
(9) a usage-NATIONAL printable report item is refused as "not supported" against §13.18.60.3 SR7, with a
citation that does not say what it claims (check Annex A first); (10) INSPECT refuses a NATIONAL identifier,
COBOLNET0847 quoting §14.9.22.3 SR2 as "usage-display" where the rule reads "display or national"; (11) no test
is NAMED for `GroupValueSlicer` — `~GroupValue` is a DEAD filter term, which is part of why the VALUE-slicing
defect could sit there. Two report inaccuracies found and not inherited: PB394's sweep table cites "§14.9.49"
for UNSTRING where §14.9.48 is UNSTRING and §14.9.49 is USE (the code is right everywhere), and PB415's report
§8 says its note goes to `status: landed` where the note itself, correctly, is `half`.

