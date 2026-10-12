## Entry 1544 — 2026-09-06 03:22 PDT — Landing train 14: PB678 · PB350 · PB692 · PB346 · PB676 · PB688 (GAP 2820 → 2815)

**PB678 — the SORT/MERGE comparator selected by the key's CLASS, and two defects the note did not have.**
§14.9.40.4 GR5 and §14.9.24.4 GR5 resolve TWO collating sequences per statement — the alphanumeric one for keys
of class alphabetic and alphanumeric, the national one for keys of class national, "each separately determined"
— and the compiler carried ONE. PB327 had removed the FD/SD record gate that kept national keys away from it, so
a file SORT with a `PIC N` key silently mis-ordered; the table-sort twin still staged loud behind a comment
citing GR5b (the PROGRAM-collating-sequence precedence step, not the class rule — the sub-item was INHERITED,
and re-deriving it is what this change owes to rule 1) and naming a gate that no longer existed. The re-probe
found the site carried more than the note claimed: the key WINDOW was `ImageWidth`-sized over an image whose
basis PB327 had made BYTES, so a `PIC N(1)` key covered only its high byte — U+0000 for every Latin national
character — and three distinct keys compared EQUAL, returning the release order in the NATIVE case too, not just
under a declared alphabet. And the same dispatch was wrong one class over: a BOOLEAN key took the alphanumeric
weights, where GR5 names a sequence for neither class boolean nor class numeric and §8.8.4.2.8 compares boolean
operands "by their boolean value, regardless of their usage" — an `ALPHABET … IS "10"` inverted every bit
position. The fix is one classification, `CollatingSelection.Of` (`Binding/CollatingModel.cs`), and one resolved
PAIR, `SortCollation`: the binder resolves both halves by GR5a then GR5b independently and CARRIES the national
one (it used to validate alphabet-name-2 and throw it away, correctly, while no reachable program could observe
it); `BoundSortMergeKey.Class` replaces the `bool Numeric` bit and keys read `OperandPic` (D20) and `ByteWidth`;
`CobolSort.KeyColumns` holds a sequence PER KEY and decodes a national key's UTF-16BE byte pairs back to
national positions through `CobolBits.NatReadWindow` before comparing, because §8.8.4.2.9 compares positions;
`TableCompare` dispatches on the class through `AsNat()`, `AsBits()` and `AsImage()`, so a national or bit GROUP
key is the elementary operand §13.18.29.4 GR2b/GR1b makes it, and the `SortBinder.cs:176` loud stage is DELETED.
Two inline copies of the same class→sequence ternary in `ConditionRenderer` were folded onto
`EmitContext.CollateArgFor` rather than joined by two more. Two things fell out of the work: `CobolSort.Sort` and
`Merge` no longer take collating arguments at all — `Init` is the ONE §14.6.6 r5 statement-start snapshot, and
the second mechanism made "the store holds null" mean both "native was snapshotted" and "nothing was" — and
`BoundaryImageChannelTests`, the drift test that holds the one-reader law for group images, gained the
`AsNat`/`FromNat` pair it had never covered since PB327 created it. Goldens:
`2023/pb678_sort_national_key_collating` (eight legs including MERGE's own; three deliberately different orders
so no leg can pass for another's reason), `2002/pb678_table_sort_key_class` (GR5b, the statement-overrides-program
case, a boolean key with patterns chosen only after the obvious ones proved non-discriminating, and a national
GROUP key agreeing with the elementary one) and `negative/pb678-sort-collating-for-forms-at-85` (COBOLNET0872, a
gate that guarded nothing observable until the phrase started being carried); both positives proven RED by
corruption. `GR-14.9.40.4-5` and `GR-14.9.24.4-5` CONFORMS; no diagnostic code claimed from the 1842–1843 range.
The self-review's sweep left three findings note-ready for the register (ids are centrally allocated, so the
lander does not mint them): SORT's own `WITH DUPLICATES IN ORDER` is
REJECTED at every edition ≥ 2002 because the grammar spells ORDER as a `cobolWord` and §8.9 reserved it in 2002
(it rejects legal source, and the workaround is a program whose result the standard leaves undefined); a level-88
over a national or bit GROUP reads `Pic` where D20's reader is `OperandPic` and so takes the alphanumeric
sequence; and the indexed-file half of national-key collating (§12.4.5.7) stays the COBOLNET1584 staged loud it
was — plan §8's SORT national-key carry is DISCHARGED and its residue re-scoped to that §12.4.5.7 row.

**PB350 — RETURN's mandatory AT END, screened at bind time (COBOLNET1850).** §14.9.34.2 prints
`AT END imperative-statement-1` on its own line with NO brackets, between a bracketed `[ NOT AT END … ]` and
`[ END-RETURN ]` — rendered from the printed page, folio 708, because the diagram is load-bearing — and §5.2.6.2
makes brackets the only convention that lets a portion of a general format be omitted. The phrase is therefore
mandatory, and the grammar's `returnAtEndPhrase?` under-rejected: `RETURN SF RECORD END-RETURN.` compiled clean
at all four editions and, at end of data, control fell THROUGH the statement onto a record area §14.9.34.4 GR3
leaves undefined — the probe printed `R=ONE / R=TWO / R=TWO`, re-displaying the previous record, so a loop
written on RETURN could never terminate from the statement. **The fix is a bind screen, not a grammar change,
and that is a recorded decision**: `StatementValidation.ScreenOmittedRequiredPhrase` →
`DiagnosticCatalog.FormatRequiredPhraseOmitted` (COBOLNET1850), the mirror of COBOLNET1720's forbidden-phrase
screen, called from `SortBinder.BindReturn` on `atEnd is null` **after** `PhraseBlocks.Split` has normalized
phrase position away. A bind-time diagnostic names the phrase, the statement and the clause where a parse error
can only report an unexpected token — and the post-Split call covers BOTH grammar arms with one check, where
screening the phrase NODE would have left §14.9.34.3 SR4's reversed NOT-only spelling compiling (the two-arm
dispatch with one arm fixed, this repo's most reproducible shape; it now has its own negative golden). It is a
hard error even under `--permissive`: nothing in either corpus depends on the omission, measured. `readAtEnd` is
deliberately untouched — §14.9.30.2 prints READ's pair inside brackets WITH choice indicators, §5.2.6.4's "zero
or more" — and the asymmetry is now enforced mechanically. Paired with a drift test as rule 5 requires:
`RequiredFormatPhraseDriftTests` scrapes every unbracketed conditional-phrase line out of the transcription
(measured: §14.9.34.2's is the ONLY one in any §14.9 statement format; SEARCH's WHEN is already `+` in the
grammar), asserts each is adjudicated WITH its enforcing mechanism, re-derives each mechanism from source, and
carries READ's bracketed line as a negative control — its failure branch proven by mis-keying the RETURN row.
`DESIGN-frontend-grammar.md` gains hard **invariant 8**, the mirror of invariant 7's repetition-arity rule: the
optionality is the general format's too, and a required phrase left permissive in the grammar owes a named bind
screen — with the reviewer's check, "point at the brackets that license that `?`". Goldens: 2023 and 85
positives (the 2023 one carrying the SR4 reversed pair as the legal complement, outputs derived from
§14.9.40.4 GR8 a) + §14.9.34.4 GR3/GR4) and two negatives at `reject-at: 85 2002 2014 2023`. `FMT-14.9.34.2` was
re-verdicted and stays **DIVERGES** — PB351 owns the residue (`statementBlock : statement+`, §14.5.1, one
mechanism under ~15 formats) and its note still claims the row — so this cluster moves the GAP by zero, which is
the honest number.

**PB692 — §12.4.5.5.2 SR2 gets a home, and the runtime's keyed-connector premise stops being an assumption.**
"The DYNAMIC and RANDOM phrases shall not be specified for a sequential file" had no enforcement anywhere:
`SELECT F ASSIGN TO "f" ORGANIZATION IS SEQUENTIAL ACCESS MODE IS RANDOM.` compiled *and ran*, at every edition.
The rule is about the FILE CONTROL ENTRY, so it now lives in `DataBinder.BindFileControl` beside the §12.4.5.9
SR2 screen already there — `file.IsSequential && file.AccessMode is Random or Dynamic` → **COBOLNET1858**,
positioned at the ACCESS MODE clause — and never on a verb: the entry violates SR2 whether or not the file is
ever opened. The predicate NAMES the two phrases the rule names rather than negating SEQUENTIAL, so a mode the
standard adds later is not screened by a rule that does not mention it. All three writable sequential spellings
are under it, the omitted ORGANIZATION clause included, because §12.4.5.10.3 GR6 makes a bare
`SELECT … ACCESS MODE IS DYNAMIC.` a sequential file and that is the shape a screen keyed on a written clause
would miss; the §12.4.5.9 SR2 sibling now shares `FileModel.IsSequential` instead of a second open-coded enum
pair. The most telling evidence was already in the tree: `KeyedConnector`'s class comment justified its own
existence with *"a sequential-organization connector has no access mode to carry (§12.4.5.5.2 SR2)"* — an
architectural invariant resting on a rule nothing checked. It now points at the check that makes it true. The
FILES design gains **D19** (numbered after train 13's D18, which the note and the implementer's draft still
called D18), deliberately written as the converse of D13: a syntax rule listed under a file control entry clause
belongs to the entry binder, a syntax rule listed under a statement belongs to that statement's binder, and
neither is a place to put the other. Four negatives (both phrases × explicit SEQUENTIAL / omitted / LINE
SEQUENTIAL) plus one positive golden covering the single legal cell the corpus lacked: a static scan of all 1483
SELECT entries found every rejected cell EMPTY and LINE SEQUENTIAL × explicit `ACCESS MODE IS SEQUENTIAL` with
zero witnesses. One instruction the implementer left for the lander was carried out here: PB334 was still open
on PB692's base, so the reachability sentence its §14.9.30.3 SR6 screen needed could only be written on the
PB692 side. PB334 landed with train 13, and its screen's own comment said in so many words that §12.4.5.5.2 SR2
“has no enforcement at the file control entry yet” — true when it was written, false the moment this cluster
landed beside it. It now says the opposite: on the sequential-organization arm that screen is DEFENCE IN DEPTH,
kept because it is the STATEMENT's own rule and costs a field test, never because the entry rule might be
missing. `SR-12.4.5.5.2-2` GAP → CONFORMS.

**PB346 — the SECONDS/FOREVER arm was split; eight descriptions of it were not.** The reported defect —
`RETRY FOR ZERO-SECS SECONDS` answering the deadlock '52' where §14.7.9.3 GR4 a) requires the record operation
conflict '51' — **did not reproduce**: `0b1c39bc` (PB142 cluster E) had already given `Seconds` its own
no-further-attempt arm on the GR2 clamp and scoped `ExhaustionStatus`'s deadlock to `Forever` on a RECORD
conflict. What had not travelled with that fix was every description of it. Eight sites still said
"SECONDS/FOREVER deadlock-bail to 52 (GR4a)" — `StatementBinder.BindRetry`, `FileLockBinder.BindVerbRetry`,
`BoundTree.RetryKind`, `BoundKeyedIo.Retry`, `FileRegistry.ConflictOnLockedRecord`, `constructs.json`'s
`retry-phrase-2002` row (and therefore the generated `ConstructRegistry.g.cs`), the PHASE-4 ledger in five
places, and a wave-C scout doc quoting the binder — and the claim was wrong twice over, because GR4 a) does not
govern FOREVER at all: FOREVER carries no arithmetic expression. The repair is not better wording. Each of the
five code sites now POINTS at `FileRegistry.RetryLoop` instead of restating its outcome (D8's no-restatement
rule), so the next change to the retry rules cannot leave a description behind it. The second half of the row was
worse: every RETRY test in the tree reached `RetryLoop` through DELETE FILE, REWRITE or DELETE RECORD, so **no
test had ever passed a RETRY phrase to a READ**, on either format — which is why `GR-14.9.30.4-9` sat at PARTIAL
with an empty `test-ref` long after the code was right, and why a read path could have dropped its retry
arguments and stayed green. `ReadUnderEveryRetryForm_BindsGR9ToTheRetryRules_OnBothFormats` is that missing drift
test (eight forms × both read formats, proved red against a deliberately reintroduced PB346), and
`2023/pb346_read_retry_record_conflict` with its 2014 twin is the corpus witness: four connectors, two physical
files, the amounts in DATA ITEMS so GR4 a)'s "result of the evaluation" is a real evaluation, and the
`RETRY FOR 0 SECONDS` / `RETRY FOR 30 SECONDS` pair agreeing at '51' as the observable form of the A.1 item 166
determination on the RECORD class — the same pair `pb142_retry_conflict_class_ec` pins on the FILE SHARING
class. D8 also gained the count nobody had updated since PB340: `RetryLoop` has **five** call sites, not six,
because the two READ formats both reach the one `ConflictOnLockedRecord`, and `WriteShared`'s attribution was
corrected to GR16's actual scope. No runtime behaviour changed — the `src/` diff is comment text plus the
regenerated registry. `GR-14.9.30.4-9` PARTIAL → CONFORMS; no diagnostic code needed.

**PB676 — §14.6.13.1.1's raise rule stops being twenty-six transcriptions.** `ExceptionState.cs` applied one
sentence of the standard — "if checking for an exception that occurs is not enabled, no exception condition is
raised" — by hand, once per condition: 22 copies of the identical five-line
`if (<X>Checking) { Set("<EC>", fatal: true); throw new CobolFatalException("<EC>", detail); }`, two nonfatal
one-liners, and two with an extra term. (The note said eighteen; its filing grep could not see `ArgumentError`,
which returns `long`, or `PerformVaryingIndexError`, which takes two parameters — the re-probe corrected the
count before the refactor was designed.) `FatalIfEnabled` / `NonfatalIfEnabled` now state the rule once and every
helper is a single expression naming its (flag, exception-name) pair — 1439 → 1323 lines, with the generated C#,
stdout, stderr and exit code of two EC-bearing programs byte-identical before and after; the static shim
forwarders needed no change, being one-line delegations already. The drift test needed no new declaration: the
pair is already declared in `EcEmitter`'s `FatalAmbientGates` / `NonfatalAmbientGates`, so it had been written
down TWICE — there and in each helper body — with nothing comparing them, and a helper carrying a neighbour's
flag would have been silent in both directions (`TURN <name> CHECKING ON` arming nothing, an unrelated condition
arming it instead). Both tables are now `internal` and `ExceptionRaiseHelperDriftTests` asserts the pairing
BEHAVIOURALLY — every helper raises nothing when every flag but its own is on — plus the fatality against
`ExceptionCatalog`'s Table 13 row and the existence of every emitter-named flag on the engine and the emitted
shim, which until now was only a generated-C# compile error in whichever program enabled that one condition. It
fired red twice before being trusted (`ReportInactiveError` mis-paired to `ReportActiveChecking`;
`DataConversionError` given a fatal raise). Three pieces of prose drift went with it: `CheckingFlags` named
EC-PERFORM-VARYING for EC-RANGE-PERFORM-VARYING and EC-SIZE-OVERFLOW for EC-DATA-OVERFLOW, and
`FloatOverflowError`'s inherited "§14.6.8.3 GR1" was re-derived to rule 2 (rule 1 is about
FLOAT-SHORT/-LONG/-EXTENDED receivers; what excludes an arithmetic ±Inf is that §14.9.25.4 GR6 d)4.a is
MOVE-specific). A sweep candidate — `CobolString.ThruMember` setting EC-RANGE-INVALID unconditionally — was
probed and DISPROVEN: `ConditionRenderer` emits `ThruMember` only under checking, so the emission is the gate.
The parameter-gated arm (four sites) and the emitted-text arm are different mechanisms and are now named as
such. No inventory rows move and no diagnostic codes were used, by design.

**PB688 — a COBOL-2023 file organization was being served to the 1985, 2002 and 2014 compilers, and the reason
nobody could date it was that everyone checked the wrong two places.** `ORGANIZATION IS LINE SEQUENTIAL` compiled
clean at `--std 85`: no `constructs.json` row, no `VersionConformancePass` arm. The note, and
`docs/COBOLNET_FILES_DESIGN.md` before it, said "a 2002 introduction — the exact edition is not derivable from
the 2023 spec; derive it from the 2002 standard". It is derivable, and it is not 2002: the 2023 **Foreword's**
list of the main changes over ISO/IEC 1989:2014 names "Line Sequential file organization" outright. Annex E
carries no item for it and `VERSION_CHANGE_REFERENCE.md` no row — the two places a reader checks — which is
exactly why the claim survived being written into a design doc, an inventory row's `editions` field
(`2002,2014,2023`) and a queue note without ever being re-derived. The fix is one construct row
(`file-organization-line-sequential-2023`, COBOLNET0900 — no code minted) and one parse-arm
`VisitOrganizationClause` beside its 14g.4 file-control siblings; `organizationClause` has TWO grammar parents
(the file control entry and the FD clause list) and the one arm covers both, with a negative golden on each.
**The edition-gate sweep was the larger half and it turned up a fact the design doc now records: a COBOL.NET
report file cannot be read back below 2023 at all.** The report writer frames a report file as CRLF-delimited
text whatever its ORGANIZATION, while the record-sequential connector reads fixed-size records — so the ten
report-writer/OO goldens that observed their report through a line-sequential read-back are 2023 programs, not
2002 ones; nine moved, the two duplicate `pb329` copies at 2002/2014 were deleted, and
`ReportWriterConformanceTests` moved whole to `--std 2023` for the same reason. The one program that had to stay
at 85 — `pb326_flow_report_unconditional`, which exists to pin §14.9.49.4 GR10 *at the oldest edition* — lost
nothing: §14.9.49.4 GR8/GR9d run the USE BEFORE REPORTING declarative once per presentation, so its `WS-N`
already **was** the detail-line count the read-back was re-observing. `GR-12.4.5.10.3-2` GAP → CONFORMS, plus
fourteen rows re-pointed by the second batch and three organization-scoped rows corrected from `2002,2014,2023`
to `2023`. **The sweep had one more instance than it could see**, and the train's gate found it: train 13's
`2002/pb352_start_sequential_first_last` landed on main AFTER PB688's base, and its fourth connector is a LINE
SEQUENTIAL file — the gate red was `error COBOLNET0900 … (targeting COBOL-2002)` on that one program, attributed
to this cluster by bisecting to the checkpoint that introduces the arm. Moving it whole to 2023 was the wrong
remedy for the same reason `pb326_flow_report_unconditional` had to stay at 85: START FIRST/LAST is a 2002
introduction and that program exists to pin §14.9.41.4 GR20/GR21 at the OLDEST edition that has it (its own
negative `pb352-start-sequential-first-below-2002` pins the 1985 refusal). So the program was SPLIT along the
edition seam rather than moved: the 2002 program keeps the record-sequential, empty-file and absent-OPTIONAL
legs, and the LINE SEQUENTIAL legs became `2023/pb352_start_sequential_first_last_line`, which displays the same
six values — `LL-OK / LSL=00 / LR1=L-3 / LF-OK / LSF=00 / LR2=L-1`, §14.9.41.4 GR21's LAST and GR20's FIRST read
back through §14.9.30.4 GR21 b)'s INCLUSIVE positioning — derived from the rules and then confirmed byte-for-byte
against a run. `SR-14.9.41.3-2`'s evidence was re-pointed to name both halves, because §9.1.7.2 gives that rule's
antecedent two framings and one program can no longer carry both.

**The train.** Six clusters, one landing, no cluster dropped. Three collisions had to be decided from evidence
rather than by taking a side. (1) PB692's FILES-design decision was numbered **D18**, which train 13's PB345 had
already taken on main; it lands as **D19**, and its note, the design section and this entry were renumbered
together — its `KeyedConnector` comment never named a number, so nothing else moved. While renumbering it, the
design section and the note were found to describe the predicate as `AccessMode is not FileAccessMode.Sequential`
where the landed code writes `AccessMode is Random or Dynamic`; the prose was corrected to the code, which is the
form that names the two phrases SR2 names. (2) PB692 and PB688 disagree about what edition
`ORGANIZATION IS LINE SEQUENTIAL` exists at: PB692's LINE SEQUENTIAL positive golden sat in
`tests/conformance/2002/` and its LINE SEQUENTIAL negative said `reject-at: 2002 2014 2023`, both written on the
pre-PB688 reading that only 1985 lacked the organization. PB688 is the newer authority and is derived from the
Foreword, so the positive golden moved to `tests/conformance/2023/` — with the manifests, the note, the design
doc and PB692's verdict-batch `test-ref` repointed with it — and the negative's band narrowed to
`reject-at: 2023`, because below 2023 that source is rejected by COBOLNET0900 and §12.4.5.5.2 SR2 never gets to
speak. The same reasoning settled the `COBOLNET_FILES_DESIGN.md` conflict in the edition-gate sweep section,
where each of two bullets had been rewritten on one side only: train 13's PB334 text for READ PREVIOUS (the
after-OPEN change is the INDEXED leg only) and PB688's text for LINE SEQUENTIAL. (3) The gate's ONE red was the
third: PB688's corpus sweep could not see train 13's `2002/pb352_start_sequential_first_last`, which landed after
its base and carries a LINE SEQUENTIAL connector — bisected to the row-6 checkpoint, split along the edition seam
rather than moved (above), and re-gated green. Every other conflict was additive adjacency in a manifest, in
`DiagnosticCatalog.cs`, in `StatementValidation.cs` or in `DIAGNOSTICS.md`, resolved by keeping both sides —
proven correct for the generated doc, whose regeneration from the merged catalog produced a zero diff.

Diagnostic codes claimed: **COBOLNET1850** (PB350) and **COBOLNET1858** (PB692); the other four clusters used
none of their reserved ranges and no two clusters collided — next free is COBOLNET1859. `constructs.json` was
written by PB346 and PB688 and the registry regenerated ONCE after both; `docs/DIAGNOSTICS.md` regenerated once.
The six verdict batches were re-applied in manifest order on the merged tree (five reported `rows changed: 0`,
confirming the patched inventory; PB692's changed one row, the repointed `test-ref`), a seventh batch re-pointed
`SR-14.9.41.3-2`'s evidence at both halves of the split PB352 golden, and `build_inventory.py` ran once:
**GAP 2820 → 2815 of 4371** (PB678 −2, PB692 −1, PB346 −1, PB688 −1; PB350 and PB676 zero by design).

Gate, on the merged tree, with the **FULL Unit assembly** rather than the union filter — train 13's lesson,
applied: Conformance filtered on the union of the six clusters' terms
(`~Sort|~Merge|~Collation|~Locale|~Corpus|~SpecTraceabilityInventory|~National|~BoundaryImageChannel|~DiagnosticRegistryDrift|~VersionMatrix|~RequiredFormatPhrase|~File|~Sequential|~Open|~Read|~CobolFileLock|~ConstructRegistry|~ExceptionCondition|~UseDeclarative|~Ec|~Exception|~Turn|~Raise|~ReportWriter|~Vcr|~Organization|~FixedFileAttribute`,
every term spelled `FullyQualifiedName~`). **Conformance 4642/4642 · Unit 22526/22528 · Characterization
33/33.** The Unit assembly's two reds are the known worktree shape by name —
`ExternalCorpusPopulationDriftTests.TheCensus_NamesEveryPopulationAndItsProgramCount` and
`.SweepExternalPopulation_EqualsTheDifferentialsCommittedCaseCount`, both of which need the GPL external corpus
that a worktree does not carry — and they are the ONLY two `[FAIL]` lines in the run. The first gate pass had a
third, the PB352 golden above; it is green here. Both citation audits are at their zero baselines (0 findings
over 3451 files scanned for phantoms and 1655 for construct agreement; 0 MISFILED of 346 verbatim-shaped
citations),
and fifteen citations sampled across the six implementer reports were re-checked with `cite.py --check` and all
fifteen resolved. `scripts/semgrep/verify.py` PASS with every count unchanged against
`scripts/semgrep/baseline.json` (raw-diagnostic 423 → 423). The new `2023/pb352_start_sequential_first_last_line`
golden was proven load-bearing by poisoning its `.out` — swapping the two records READ delivers, which is exactly
the §14.9.30.4 GR21 b) INCLUSIVE-positioning claim — and confirming that the corpus leg went red on that one case
and nothing else.

