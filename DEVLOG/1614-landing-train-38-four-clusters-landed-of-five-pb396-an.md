## Entry 1614 — 2026-09-13 20:54 PDT — Landing train 38 — FOUR clusters landed of five: PB396 (an imperative-statement the general format prints unbracketed is REQUIRED, at nine positions in four verbs) + PB411 (an alphanumeric group HAS a usage, and a method's GOBACK reads every phrase of its format) + PB420 (a stale deferral kept INITIALIZE's implicit MOVE out of every float receiver) + PB391 (§14.9.25.3 SR8 and SR9 are asked under CORRESPONDING, through one entry), GAP 2435 → 2423 — and ⛔ PB425 DROPPED by the train's own `~CorpusRunner` leg on eight goldens main already carried

**PB396 — one quantifier, four verbs, nine positions.** Every USE of `statementBlock` in the control-flow grammar
was written `statementBlock*`, restoring the zero case ISO §5.2.6.2/§5.2.6.3 forbid: an imperative-statement a
printed general format leaves outside brackets, or stacks inside braces, is REQUIRED, and §14.9.19.3 SR1 says the
same for IF a second way. So `IF X = 1 END-IF`, a WHEN phrase with no body, `PERFORM UNTIL … END-PERFORM` with no
body and a SEARCH WHEN with no body all compiled in silence at every edition; `WHEN OTHER` written twice printed
the second and left the first's imperative dead. All nine positions now read bare `statementBlock` (= `statement+`),
each with its rendered page and printed folio in the `.g4` (648/665/712/750), and `WHEN OTHER` moved out of the
repeated EVALUATE clause into the single trailing phrase §5.2.7 says it is — in the greenfield binder AND in the
legacy oracle, which shares the generated parser and had to move with it. ⛔ The re-probe contradicted the note on
two points and both are recorded: §14.9.13.4 GR3's subject evaluation was NOT the emitter's only-WHEN-OTHER
shortcut but `EvaluateBinder`'s LAZY subject slot, which survives the grammar fix (`EVALUATE R (9) WHEN ANY` still
ran its arm until `Bind` was changed to force every subject slot before any arm binds, `SubjectUsage` narrowing the
no-hoist exemption to one use in the FIRST arm); and the row's other two divergences were already closed by earlier
landings. A ROOT FIX came out of the work: `Frontend.Parse` attached the error listener across BOTH passes of the
SLL(bail)→LL strategy, so every syntax error was reported twice in two wordings and an SLL-only failure became a
FALSE error — the listener is attached to the LL pass only now, which is why `pb331-read-key-before-lock.err` and
`pb712-write-two-advancing-operands.err` re-pin from the duplicate's COBOL0001 to the LL pass's COBOL0307.
COBOLNET2072 (empty required consequent) and COBOLNET2073 (WHEN OTHER repeated or out of position) re-code the
resulting syntax error structurally rather than from a table of verbs. The drift test is the thing that makes the
next general format automatic: no `.g4` may contain `statementBlock*` or `statementBlock?`, population-asserted.
Golden `85/pb396_required_imperative_85` plus two all-edition negatives; the four `pb440_perform_empty_range.cob`
inline twins gained a `CONTINUE` body (§14.9.9, "a no operation" — output unchanged) and `PerformFormat3ParseTests`
traded its empty-imp-2 pin for a body plus a new `EmptyImp2_IsRejected`. FMT-14.9.13.2 DIVERGES → CONFORMS and
GR-14.9.13.4-5 PARTIAL → CONFORMS; GR-14.9.13.4-3 stays PARTIAL with its code-location repaired. ⚠ DETERMINATION
(owner-overturnable): the two IF formats stay ONE grammar rule, so `IF X = 1 NEXT SENTENCE END-IF` is still
ACCEPTED per §14.9.19.4 GR4.

**PB411 — the termination-status operand, wrong in both directions, fixed as one classifier and one dispatch.**
§14.9.18.3 SR6 and its word-for-word twin §14.9.42.3 SR2 admit "a data item with usage display", and §8.5.2.1 says
an alphanumeric group item *is* one — "treated as though it had a usage of display". The shared screen asked
`DataItem.OperandPic`, which is null for such a group, and read that null as "no usage at all", so
`STOP RUN / GOBACK WITH ERROR STATUS <group>` was rejected on both verbs with a diagnostic quoting the very rule
the operand satisfies — a rejects-legal-source defect held open by a green InlineData row, a green doc comment and
a negative fixture, all three now flipped (`negative/pb169-status-group-identifier` is DELETED: it pinned the
rejection of legal source). The repair is `ItemCategory.UsageOf`, THE ONE §8.5.2.1 usage reader, placed beside the
ONE spelling of §3.11's definition-by-exclusion so a strongly-typed or variable-length group — for which the
standard states no usage — answers null rather than an invented display; two sibling sites that had written the
same rule their own way (`SendingValueTemp.UsageOf`, `RecordLayout.UsageOfItem`, the latter answering BINARY for
`01 G USAGE COMP.` against §13.18.60.4 GR1) now read it, and `IntrinsicArgumentRules.StaticUsageOf`, which
deliberately asks about REPRESENTATION instead, says so in its own comment. In the other direction,
`CallBinder.BindGoback` forked to the OO binder inside a METHOD and handed it the whole parse node, which
re-decided which phrases existed and read only two of the three: the 2023 status phrase was discarded in silence,
so SR6/SR7/SR8 and the COBOL-2023 introduction gate never ran there at all. `DecodeGobackPhrases` now reads the
rule ONCE into a `GobackPhrases` record before the §14.9.18.4 GR2/GR4 fork and `OoBindMethodGoback` takes that
record, never the context; the status is screened and then inert in a method, which is the standard's own division
between §14.9.18.3's unqualified syntax rules and GR7–GR10's "in a main program". EXIT PROGRAM/METHOD/FUNCTION
carry no status phrase (§14.9.14.2), so there is no third arm. Golden
`2023/pb411_goback_status_operand_and_method_arm` plus two negatives, a decode drift test whose failure branch was
fired twice by re-introducing the defect, and a parity test running the same statement in a program and in a
method. Six rows close CONFORMS — the three GOBACK rules and the three STOP twins that were at GAP and claimed by
no note.

**PB420 — INITIALIZE's implicit MOVE reaches a floating-point receiver, through the one seam.**
`InitializeEmitter` carried an arm that intercepted every store into a COMP-1/COMP-2/FLOAT-* receiver and emitted a
loud "float MOVE path deferred". The premise was true when written; PB271 then hardened `MoveEmitter`'s
float-receiver store, which presupposes that path, and nothing noticed — so `INITIALIZE G REPLACING NUMERIC DATA
BY 1` over a group with one float leaf ABORTED THE RUN UNIT while `MOVE 1 TO F`, the statement §14.9.20.4 GR4 says
the implicit one IS, compiled and ran in the same data division. The arm is DELETED rather than replaced: GR4
exempts only the five pointer-ish categories, a float item is category numeric (§8.5.2), and the receiver now takes
`ConvertSource`'s float arm with its §14.9.25.4 GR6 d)4.a overflow check and its windowed IEEE re-encode intact — a
Tier-B REDEFINES window receives bytes identical to the explicit MOVE's, the GR6 c) ZEROES default included. No
float arm was added anywhere. The same stale premise sat on `AcceptDisplayEmitter`'s Format-2 (temporal) arm, where
§14.9.1.4 GR6 says outright that the transfer is "to the data item specified by identifier-2 according to the rules
for the MOVE statement", and it is fixed the same way; the Format-1 device arm stays, because §14.9.1.4 GR1 leaves
that conversion to the implementor and no determination exists — recorded as a lead, ADJUDICATION-first.
`StaleDeferralDriftTests` now pins the exact inventory of "deferred" loud arms across `CodeGen/` — exactly one —
and INITIALIZE's single store path, both proven red against a wrong pin first. Goldens at 85 and 2002 plus a
negative; GR-14.9.20.4-4 and GR-14.9.20.4-L2.3 both flip DIVERGES → CONFORMS.

**PB391 finished — the half of rule 2 that is not Table 16.** §14.7.6 rule 2 sends CORRESPONDING's pairing decision
to *"the rules for the MOVE statement"*, plural, and §14.9.25.3 SR10 — the rule that routes to Table 16 — governs
only *"all other cases not described in Syntax rules 8 and 9"*. The first PB391 landing deleted the private
Table-16 copy `CorrespondingBinder.CorrMoveValid` and left the filter asking `MoveTable16.Refusal` alone, which
skipped both rules SR10 defers to; both were measured wrong on this tree. SR8 gave the same two-answers shape the
note was written about: a `BINARY-LONG` namesake paired with a `PIC X(5)` one and overwrote it (`K2=[00000]`),
while the identical written `MOVE K OF G1 TO K OF G2` was refused COBOLNET0819 by the same compiler. SR9 was worse
than a wrong answer — a variable-length-group namesake paired with an elementary one and the implied MOVE asked for
a whole-group character image the pair can never have, so the program reached RUN TIME as
`NotImplementedCobolFeatureException` where the written MOVE is COBOLNET1931. Both are now asked through ONE
composite entry, `MoveTable16.DataItemRefusal` — §14.9.25.3's validity question over two data items in SR order —
so the CORRESPONDING filter is a single call and the next MOVE syntax rule reaches it without an edit. Neither rule
was written a second time: SR8's item-keyed test became a `ShapeRefusal(DataItem, Table16Operand)` overload that
PB416's bound-operand entry delegates to, and SR9's relation moved out of
`StatementValidation.CheckVariableLengthMove` into `MoveTable16`, leaving that method holding only the MOVE
statement's framing with its COBOLNET1931 message bytes re-measured identical. All three CORRESPONDING verbs were
answered rather than assumed: rule 3's "both of the data items are numeric" is strictly stronger than SR8's
receiver requirement, so ADD and SUBTRACT were already right, measured before and after. PB391's own original
content rides with it — rule 4 via `ItemCategory.IsIndexMessageTagObjectOrPointer` (§13.18.60.3 SR4/SR14's readers
MOVED out of `DataBinder`), and the unsourced `>= 2002` de-editing gate DELETED. Five goldens across 85/2002/2014/
2023 plus two negatives and a negative control; `CorrespondingRule2DriftTests` grew two positions that are
deliberately NOT Table-16 rows and was verified to fail 18 of 40 against the pre-fix filter. SR-14.9.25.3-13 and
GR-14.9.25.4-11 close at CONFORMS.

**⛔ THE FIFTH CLUSTER WAS DROPPED BY THE TRAIN'S OWN GATE, AND ONLY `~CorpusRunner` COULD SEE IT.** PB425
(§14.9.25.4 GR2/GR3's zero-length-literal substitution) turned the union gate RED on **eight goldens main already
carried**, every one of them a NUMVAL / float / decimal program no cluster wrote:
`Failed!  - Failed: 8, Passed: 3451, Skipped: 0, Total: 3459 - Cobol.Net.Tests.Conformance.dll` —
`pb65_dec_move_channel` (2023), `pb60_numval_standard_decimal` (2023), `pb60_numvalf_native_channels` (2023),
`pb251_numval_native_value_fixed` (2023), `pb253_prose_float_standard_container` (2002),
`pb253_prose_float_sdidi_container` (2023 and 2014) and `l1_numval_family_cap_discriminators` (2023). Bisected by
cluster, by compiling and running `tests/conformance/2023/pb65_dec_move_channel.cob` against each checkpoint's own
build: at the PB420 boundary `MOVE FUNCTION E TO EW` prints `MOVE-E=2718281828459045235360287471352`, the golden's
value; one commit later, with PB425, it prints `MOVE-E=2718281828000000000000000000000`. THE MECHANISM:
`MoveClassifier.NeedsLengthFreeze` answers true for `BoundComputedOperand { Expr: BoundIntrinsicCall }` against ANY
numeric or numeric-edited receiver, so `MoveBinder.BindMoveOf` now materialises EVERY function-result MOVE into
`SendingValueTemp`'s carrier and the STANDARD-DECIMAL / SDIDI value is truncated on the way. The freeze is there for
§14.9.25.4 GR1, whose sentence reads "If **identifier-1** is a zero-length item, it is as if literal-1 were
specified as a zero-length literal" — identifier-1, and a zero-length item is §8.5.1.10's, which a numeric function
result can never be. The predicate fires on a shape the rule cannot reach, and pays for it with precision. The
cluster is DROPPED whole — patch, goldens, manifest entries, the D-B2 determination and its two verdict records —
and returned to its implementer with this attribution; it is NOT repaired here, because repairing another agent's
classifier from outside its evidence is how a second defect gets landed on top of the first. ⚠ And the lesson is
the one train 37 wrote down and this train inherited: PB425's own gate ran `~ZeroLengthLiteralMove|~Move|~Drift|
~EditionGate` — 533 Conformance cases and **no `~CorpusRunner`** — so its 2,218-case complement, which is where all
eight reds live, was never measured. An implementer gate that does not run `CorpusRunnerTests` has measured only
what the change's author thought to name.

**THE TRAIN.** Four clusters landed — PB396 · PB411 · PB420 · PB391+finisher — four cluster commits plus this one.
Every cluster was based BEFORE the trains between it and main — PB396 at `0b1b48f9` (before trains 34–37, and a
GRAMMAR change), PB411 at `f8fa6452`, PB420 at `ccf03eed`, PB391+finisher at `9cfab584` — so all were three-way
merged onto train 37's head and every conflict was resolved by reading BOTH sides against the spec, never by taking
a side. Five conflicts, all keep-both: `ControlFlowBinder.BindPerform` keeps PB403's
`EnclosingConstruct.InlinePerform` frame with PB396's singular `[p.statementBlock()]`; `EcBinder.ExceptionPerform`
keeps PB403's `BindHandler` helper and its `PerformFinally` frame with the same arity change;
`CallBinder.BindGoback` and `OoBinder.OoBindMethodGoback` take PB411's decoded `p.Raising` with main's PB408
`EcRaiseSite.Goback`; and the `COBOLNET_FILES_DESIGN.md` bullet carries PB411's `ItemCategory.UsageOf` paragraph AND
PB391's `IsIndexMessageTagObjectOrPointer` sentence. ⛔ **NO generated or list file was three-way merged at all** —
a conflict INSIDE one JSON element is silently lossy — so `tests/version-matrix/traceability-inventory.json`,
`docs/DIAGNOSTICS.md` and all five conformance `manifest.json` were EXCLUDED from every cluster patch and
reproduced mechanically: the manifests by a script that edits the lists TEXTUALLY from the base→branch set
difference and asserts each list's element COUNT, then re-parses the JSON and asserts that nothing else moved
(85 89 → 92, 2002 283 → 285, 2014 97 → 98, 2023 602 → 604, negative 1121 → 1127, with
`pb169-status-group-identifier` REMOVED because it pinned the rejection of legal source);
`docs/DIAGNOSTICS.md` by `scripts/gen-diagnostics-doc.ps1`, which reproduced exactly the two PB396 rows; and the
inventory by re-applying the batch files IN ORDER. The two PB391 batches collide by design —
`pb391-finisher.json` SUPERSEDES `pb391-rule2-rule4.json` on `SR-14.9.25.3-13` and `GR-14.9.25.4-11`, and
`record_verdicts.py` refuses both in one run — so they were applied as two runs in order: **GAP 2435 → 2425 →
2423** with the denominator unchanged at 4,348, sixteen records over six batch files. (With PB425 it would have
been 2421.)

The union gate ran on the merged tree over **15 terms** the first time and **14** after the drop —
`~RequiredImperativeStatement|~Evaluate|~Perform|~Search|~Drift|~EditionGate|~Nist|~CorpusRunner|~Goback|~Initialize|~Accept|~StaleDeferral|~Move|~Corresponding`
— every one measured by `filter_population.py`: 12 LIVE (NIST 358, `CorpusRunner` 2218, `~Drift` 287, `~Move` 264,
`~EditionGate` 125, `~Accept` 93, `~Perform` 93, `~Goback` 81, `~Search` 49, `~Evaluate` 47, `~Initialize` 13,
`~Corresponding` 10) and **2 INERT and named rather than believed** — `~RequiredImperativeStatement` and
`~StaleDeferral` select nothing in Conformance because both classes live in the Unit assembly, which this gate runs
UNFILTERED. `~ZeroLengthLiteralMove` left the union with PB425. `~Nist` and `~CorpusRunner` were in the union from
the start, and the drop above is what they were for.

The four legs, on the merged tree:
```
  Conformance          3445 /  3445      (run 2, after the drop — run 1 was 3451 / 3459)
  Unit (UNFILTERED)   24162 / 24162
  Characterization        33 /    33
  legacy Integration     503 /   504      (one skipped — the committed baseline)
```

Semgrep: no rule increased; `cobolnet-raw-diagnostic-code-literal` fell **419 → 417** and the baseline is TIGHTENED
to 417 this time (the two previous trains left it untightened). The three citation audits report **0 findings**
each; the non-gating rule-ordinal arm of `audit_code_citations.py` measures **203**, unchanged from trains 36 and
37. `work.py check` = **910 items, all well-formed**. The gate FETCHED the GnuCOBOL corpus (gnucobol-3.2, sha256
verified), so the two `ExternalCorpusPopulationDriftTests` are real passes and not the environmental red. Five
citations were re-checked with `cite.py --check`, one per cluster, all OK.

**TWO diagnostic codes are claimed by the train, COBOLNET2072 and COBOLNET2073** (both PB396's); COBOLNET2074 and
COBOLNET2087–2095 were allocated to clusters that did not need them and are returned FREE. §0 still says the next
free code is **COBOLNET2099**, because the orchestrator's central allocation runs to COBOLNET2098 —
`session-probe.ps1`'s mechanical answer (catalog max + 1 = 2098) is one lower and must not be taken.

⚠ ONE repair the train made itself, and it is the kind a report cannot catch: the new `CobolControlFlow.g4` comment
pinned its own drift guard under the name `GrammarRequiredImperativeDriftTests`, a class that does not exist — the
test is `RequiredImperativeStatementDriftTests`, which is what `DESIGN-frontend-grammar.md` already calls it. A
cross-reference you cannot follow is not a pin, so it is corrected here.

⚠ ONE claim in the train manifest was NOT borne out and is recorded rather than inherited: it said
`docs/CONFORMANCE.md` §3 would gain "PB420's and PB411's notes" beside PB425's D-B2; neither cluster's diff touches
that file, and with PB425 dropped §3 is unchanged by this train.

Notes flipped to `landed`: **PB396, PB411, PB420, PB391** (PB217 and PB392 updated in place; PB392 stays `open`).
**PB425 stays whatever its note said before the train — it did NOT land.** ⛔ The worklist is `kb/Work/` —
`python scripts/spec/work.py next` — never this entry (rule 8).

