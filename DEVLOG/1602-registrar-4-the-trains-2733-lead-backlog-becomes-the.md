## Entry 1602 — 2026-09-13 12:10 PDT — Registrar #4: the trains 27–33 lead backlog becomes the register — 36 notes filed (PB835–PB870), 10 owning notes extended, PB499 re-scoped to its re-verdict and PB420 re-measured as still live

Trains 27 through 33 landed 22 implementer clusters and recorded their leads in DEVLOG entries 1590-1600 and
nowhere else - the train-32 lander said so in as many words ("None of train 30's, 31's or 32's leads is in
`kb/Work/` yet - no registrar has run since train 29"). A known defect outside `kb/Work/` is invisible to the
ranker, so this pass reads every "new defects" / "leads" / "for the register" section of the 40 reports under
the three scratchpads, de-duplicates them BY MECHANISM, and files them.

**Method.** Every lead was rebuilt as a probe and RE-RUN on this worktree's own `dotnet build CobolSharp.sln -c
Debug` - first at `b5ca78fb`, then again at `e13ef810` after train 32 landed under the pass; 44 probes, all
reproducing identically at both heads except the one noted below. Every citation was re-derived with
`python scripts/spec/cite.py --check` and the command line is printed beside the quotation in the note; three
inherited clause numbers did not survive that check and were replaced (§13.4.2 -> §13.18.33.4 for the record
area; "§13.18.63 GR10" -> SR10; §13.18.44.3 SR14 vs SR12 separated). One derivation MOVED a finding: PB415's
`MOVE "Q" TO <PIC ZZZ9>` lead was reported as a wrong VALUE, and Table 16 makes the move VALID while
§14.9.25.4 GR6 d) 1 makes the missing EC-DATA-INCOMPATIBLE the defect - measured with the condition explicitly
TURNed on, over a literal, an item sender and a numeric receiver alike.

**Clustered by mechanism, not by report** (owner lever 1): the SortEmitter USE hook, the checking-flag leak and
the paraphrase-audit hole were each reported by two or three implementers and are one note each; EVALUATE's
leads split into TWO mechanisms (a condition-1 subject that is unmodelled - grammar and bound tree - versus two
operand shapes that only a resolved SYMBOL can disambiguate); PB829's four unmodelled general-format fragments
went to PB829 itself, which already owns their four FMT rows.

**Filed (36, PB835-PB870).** PB835 NUMVAL-C/TEST-NUMVAL-C ignore `mon_grouping` (silent wrong answer through a
validator) - PB836 an FD's implicit redefinition is bound by SYNTHESIZING a REDEFINES clause, so REDEFINES-clause
rules refuse a legal strongly-typed record - PB837 a SORT USING retrieval failure is reported with the implicit
CLOSE's status, so no declarative runs - PB838 both citation audits are blind to a PARAPHRASED citation (the
mechanism behind nine wrong §13.4.2 sites with every gate at zero) - PB839 `filter_population.py` calls a
matched corpus case DEAD and an unfiltered term INERT under the same banner - PB840 a named SUM COUNTER is not
published to the procedure division, so §13.18.54.4 GR12 cannot be exercised - PB841 the ambient checking flags
leak across a CALL - PB842 §14.9.13.2's condition-1 selection subject is unmodelled (only a class condition
parses; the bound tree has no condition-to-boolean bridge) - PB843 EVALUATE's `THRU ... IN alphabet-name` and its
bare class-name object are decided by ANTLR alternative order - PB844 an alphanumeric sender into a numeric
receiver never sets EC-DATA-INCOMPATIBLE - PB845 the NATIONAL/LENGTH/BIT `cobolWord` exemption, positional or
total (adjudication) - PB846 `SORT table-name` with the KEY phrase omitted is a parse error - PB847 a
function-pointer can be built and never called - PB848 the unstressed `TO` in the three pointer USAGE phrases,
and the optional-word audit that cannot see the shape - PB849 §13.16.2's four condition-name formats are
modelled as one - PB850 `IsAlphanumericOrNational` admits both edited categories (adjudication) - PB851
`GR-13.18.38.4-1` had no claimant - PB852 the SOURCE clause's arithmetic-expression and ROUNDED have no surface
(five rows with no population) - PB853 §13.15.3 SR10 unenforced, and the fabricated operand now ABORTS the run
unit - PB854 EC-I-O-EOP / EC-I-O-EOP-OVERFLOW are never set to exist (measured beside the END-OF-PAGE phrase,
which fires) - PB855 the extended-editing placement rules have no site - PB856 INSPECT refuses a NATIONAL
operand while quoting the rule that admits it - PB857 the BOOLEAN class condition has no grammar token - PB858
§13.18.43.3 SR6 cannot be enforced until `DataItem` knows its section - PB859 §5.5's nonzero `integer-n` is
enforced nowhere (`OCCURS 0 TIMES` reaches the user as raw Roslyn errors) - PB860 a sharing refusal answers '30'
where §9.1.13.9 requires '61' - PB861 a vacuous drift-test assertion - PB862 §12.3.7.3 SR8's unspecified
implementor name set (adjudication; `SPECIAL-NAMES. WIBBLE WOBBLE.` compiles) - PB863 the bound tree loses a
literal's category - PB864 CloseCore's unconditional print-stream terminator - PB865 LINAGE on a RELATIVE or
INDEXED FD (adjudication) - PB866 COBOLNET1658 refuses an IS-form EDITING phrase the FOR-form ban does not reach,
with the two-items-labelled-a) transcription hazard that makes its citation pass `--check` - PB867 RAISE with
checking not enabled (adjudication) - PB868 docs describing deleted members - PB869 nine drift tests parse
`IntrinsicCatalog.cs` as text - PB870 the START FIRST/LAST edition band on the RELATIVE twins.

**Extended rather than duplicated (10).** PB394 (the §14.9.25.4 GR1 length freeze for a run-time-extent group
sender - re-measured AFTER train 32 landed PB394's own fix and still `Z=[1    ]` where `[125  ]` is required),
PB415 (the REPLACING category list, DISCHARGED by train 32's landing and recorded as closed), PB212 (SET's
SR3/SR4 screen, measured: `SET N TO 2.5` compiles and stores 0000), PB507 (BLANK WHEN ZERO - both spellings
measured, and they disagree), PB491 (`PicInfo.Length` under-counts a staged EDITING literal), PB551 (SR25's
format-4 leg is vacuous and the row must say so), PB318 (§14.9.27.3 SR2 measured: `OPEN EXTEND` on a LINAGE file
compiles), PB829 (the four unmodelled general-format fragments, all four re-measured), PB736 (the ROOT CAUSE of
the intermittent GrammarDiagram red: a constant shared temp path six lines from the GUID form that fixes it),
PB541 (the report NATIONAL item, re-measured).

**Re-scoped.** `kb/Work/PB499` - the INITIALIZE lane's blindness to the Format-2 VALUE carrier - is DISCHARGED
by PB418: all five populations (working-storage, linkage, external, based, pointer) re-measured and every one
now produces the value the note derived from the standard. It is `half`, not `landed`, because its three rows
are still verdicted against the old measurement and flipping it terminal would leave them unowned; the remaining
work is one golden per population and a `record_verdicts` batch. `kb/Work/PB420` was re-measured the same way
and is NOT discharged: one COMP-2 leaf still aborts the whole INITIALIZE at run time while the identical
explicit MOVE runs. Both notes' references to the deleted `InitializeSender` are re-pointed, as is PB415's.

**Dropped with a reason** (in the registrar report): six leads already owned by a live note (§14.9.27.3 SR2 ->
PB318, SET SR3/SR4 -> PB212, BLANK WHEN ZERO -> PB507, the report NATIONAL item -> PB541, `GR-9.1.15-2` ->
PB833, the class-pointer argument carrier -> PB834), the `CodeSetConversion` raw-throw lead (site confirmed,
reachability probe did NOT reach it - recorded as not reproduced rather than filed), and three "no test is named
for this predicate" observations, which are coverage facts rather than defects.

Gate: `dotnet build CobolSharp.sln -c Debug` 0 errors; the filtered unit leg
(`SpecTraceabilityInventory|DefectiveRowCoverage|DerivedVerdict`) 47 of 47; `work.py check` 903 items all
well-formed; `audit_doc_citations.py --check` 432 citations, 0 MISFILED; `gen_conformance_notes.py` regenerated
(4348 items, 2517 GAP - unchanged, this pass files defects and moves no verdict).

