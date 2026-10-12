## Entry 1606 — 2026-09-13 14:39 PDT — Landing train 34 — FIVE clusters in one landing: PB562+PB565 (a report group entry may REPEAT, and repetition is a subtree replay) + PB504+PB831+PB828 (§13.16.3 SR9's implied PICTURE is synthesized, in one pass, for both formats that state it) + PB489 (a file clause's data-name operand is a qualified-data-name, resolved once) + PB502 (the discharged VALUE-range alphabet gets the evidence its discharge lacked) + PB522 (GROUP-USAGE and four siblings travel with the description), GAP 2481 → 2468

**PB565 + PB562 — a report group entry may REPEAT, and repetition is a subtree replay (ISO §13.18.38 format 3,
horizontal axis).** `OCCURS` anywhere in a report group description entry was rejected by name — `COBOLNET0899:
OCCURS in a report group description … is not yet implemented` — which is conforming source refused, and it kept
two general rules at an EMPTY POPULATION rather than merely untested: §13.18.63.4 GR21's import of GR9 ("a VALUE
clause specified in a data description entry that contains an OCCURS clause **or in an entry that is subordinate
to an OCCURS clause** causes every occurrence of the associated data item to be assigned the specified value")
and GR22's "except that a GROUP INDICATE, PRESENT WHEN, or OCCURS clause with the DEPENDING phrase may suppress
the appearance of the item". Underneath that refusal sat a SECOND rejection nobody had seen: the `STEP integer-3`
phrase of §13.18.38.2 format 3 was not in the grammar at all, so the clause never even parsed. And the defect
note's own repro was not conforming source — `03 COLUMN 1 PIC X(3) OCCURS 3 VALUE "AAA".` violates §13.18.38.3
SR25c ("The STEP phrase shall be specified if the entry … c) contains an absolute COLUMN clause"), so it became a
NEGATIVE golden and five legal shapes were derived to build the positive one from. The shape of the fix is GR10's
own words, "it causes the entry to define integer-2 distinct report items", read together with GR11's "the same
effect on each repetition as they would on a single data item without the OCCURS clause": REPLAY THE ENTRY'S
SUBTREE — bind the entry and every entry subordinate to it once per repetition — rather than teach each clause
about repetition, which is the only mechanism that reaches GR9's *subordinate to* leg at all. `BindReportGroups`'s
flat loop became a re-entrant `BindReportEntries` over the level-number hierarchy with the repetition frames on a
build state; every existing clause binder is untouched. Placement became `ReportColumnKindModel` {Absolute,
Relative, AnchorSeed, AnchorStep}: GR12's displacement Σ ordinal × integer-3 is ADDITIVE over nested repeating
entries, so an absolute operand simply moves by it, while a relative operand has no compile-time column and its
first repetition SEEDS a compose-local anchor `__raN` that the later ones place against — the anchor and not the
horizontal counter, because GR12 measures from the preceding occurrence's LEFTMOST while §13.18.14.4 GR9's counter
holds its RIGHTMOST. BOTH readers (the emitter's compose switch and the binder's line-width walk) handle all four
kinds and `ReportRepeatingEntryDriftTests` fails if a fifth reaches only one. DEPENDING (GR13) is emitted
literally and composed into the SAME guard as the PRESENT WHEN chain so GR22's suppressors cannot drift apart,
and every repetition is BOUND either way, which is GR23's "VALUE operands are nevertheless assigned to them, even
though they are not printed". VARYING became the closed form `FROM + n × BY` over a per-entry placement ordinal,
because a replayed entry is one FIELD per repetition and the old accumulator was local to a field. The grammar
gains `occursStepPhrase` behind an `occursStepAhead()` TEXT predicate — STEP is a §8.10 context-sensitive word,
never reserved, so it gets no lexer token (the LOCALE / ATTRIBUTE precedent). SR1a, SR10, SR16, SR17, SR24,
SR25c/d, SR26, SR27 and the formats-1/2/4-only phrases ride ONE bundled code, **COBOLNET2021** (the COBOLNET1559
precedent), with SR26's overlap test measuring the repeated item through the ONE width rule
`DataItem.DisplayTextWidthOf`, extracted from `DisplayTextWidth`. Edition: `report-occurs-2002`. RESIDUE, NAMED:
vertical repetition (an OCCURS over a LINE clause, GR10c/GR10d) still stages loud, and COBOLNET0899
`report-occurs-in-group` was NARROWED to say exactly that instead of refusing the clause wholesale; PB565 stays
open for that axis. **PB562 closed here, on a re-measurement the lander made rather than inherited.** Its own
closing condition was "this note closes when PB506 lands"; PB506 landed in train 33, and the lander re-ran the
three legs on the MERGED tree — after PB506's operand list had been merged into PB565's replay, which rewrites
the same method — getting `E=[AB CD] J=[AB   ] BZ=[0000] NE=[ 12]` from
`conformance:2023/pb506_report_value_operand_list`, i.e. §13.18.63.4 GR7's JUSTIFIED exception, GR7's no-editing
exception and GR8 all holding, 4/4 green with its 2002/2014 siblings and PB565's golden. `GR-13.18.63.4-21` is
therefore re-verdicted **DIVERGES → CONFORMS** with every leg of GR21's "General rules 1, 7, 8, and 9 above apply"
discharged: GR1 vacuous (§13.15.2 admits only DISPLAY/NATIONAL usage, confirmed by RENDERING the printed page),
GR7/GR8 by PB506, GR9 by PB565's replay. `GR-13.18.63.4-7` and `GR-13.18.63.4-8` did NOT close with it — both row
records say in their own words that the format-4 divergence "is recorded on GR-13.18.63.4-21's row, not here",
and what keeps them PARTIAL is that each rule's NATIONAL leg has no vehicle (a national-edited PICTURE stages loud
on COBOLNET0899). They are rehomed to a new note, **PB876**, so the residue stays claimed by a live note instead
of becoming an orphaned defective row; `DefectiveRowCoverageDriftTests` was watched going RED with PB876 removed
and green with it restored, so the rehoming is load-bearing and the gate demonstrably looks.

**PB504 + PB831 + PB828 — §13.16.3 SR9's implied PICTURE is SYNTHESIZED, in one pass, for both of the general
formats that state it, and the SR8 guard's carve-out is deleted rather than widened.** The rule had no
implementation site at all: `git grep '13.16.3 SR9' src/` returned nothing, and `BindEntry`'s `pic` derivation is
a chain keyed on USAGE with no arm keyed on the VALUE literal, so `01 A VALUE "HELLO".` — legal source whose
PICTURE is `X(5)` by SR9 a) — ended bind with `Pic == null` and reached the user as a named rejection of a program
the standard permits. `SynthesizeImpliedPictures` (`DataBinder.ImpliedPicture.cs`, registered in `BindPipeline`
between `ExpandTypes` and `UsageInheritancePass`) builds the character-string SR9 spells out — the class arm from
`CobolLiteral.ClassOf`, the length from `CobolLiteral.Decode(...).Length`, which is the standard's own per-format
count — and hands it to THE SAME `PictureAnalyzer.Analyze` a written clause takes, argument for argument, so from
that moment the item is indistinguishable from one whose source wrote the clause and §13.18.60.4 GR1 inheritance,
the SR3/SR5/SR12/SR20 screens, SIGN inheritance, the VALUE initializer and the emitter all reach it through their
existing single sites. The placement is the design in both directions: after `ExpandTypes` so a TYPE clone that
inherits its template's VALUE gets the implied clause too, before `UsageInheritancePass` because one pass later
that walk would already have rejected `01 A USAGE NATIONAL VALUE N"AB".` for having no PICTURE. PB831: the SR8
guard's figurative carve-out is GONE, not widened — PB487 had exempted any figurative VALUE on the premise that a
figurative constant "has no length of its own", which is §8.3.3.6.4 GR2 (the SIZED-receiver rule) applied to the
case GR3 governs; GR3 c) gives `ALL literal-1` the length of literal-1, so `01 G VALUE ALL "AB".` implies `X(2)`
and holds `AB` where the carve-out silently printed `A`. `IsFigurativeValueText` is deleted. The sibling sweep
found the second arm at its purest: `grep -n "PICTURE clause may be omitted" specs/ISO_COBOL.md` returns exactly
three hits because the standard states the rule once per entry kind, and §13.15.3 SR14 — the REPORT GROUP entry's
copy, word for word — was REJECTED by the report binder (`02 COLUMN 1 VALUE "HELLO".` drew "no PICTURE clause",
under a misfiled §13.16 citation, while the byte-identical data-division entry compiled). Both live arms now share
ONE classifier and ONE edition gate; the third hit, §13.17.3 SR10, is the SCREEN description entry and needs no
site (A.4.2 is a declined module, accounted for rather than skipped). PB828, the adjudication, is ratified as a
derivation rather than the front-end accident it was: §8.3.3.1 makes a figurative constant a literal, §8.3.3.6 is
a subclause OF §8.3.3 so SR9's length reference resolves for it, and §7.3.15.4 GR4 k's `>>FLAG-14
VALUE-FIG-CON-LENGTH` presupposes the construct compiles. The two `FlagDirectiveTests` facts PB828 named as
holding the gap open compiled `CheckOnly` over programs that never referenced the item, so neither the missing
field nor the truncated value was observable to them; all 40 `ImpliedPictureTests` facts REFERENCE the item, and
one of them re-reads SR9 a/b/c out of `specs/ISO_COBOL.md` each run and was proven to fail by swapping `N` for
`X`. LANDER MERGE NOTE: PB504 was written against a scalar `rawValue`, which PB506 (train 33) had already turned
into a format-4 operand LIST. SR14 says "where length is the length of THE LITERAL", singular, so the merged site
derives an implied PICTURE only from a clause supplying exactly ONE operand — a multi-operand format-4 VALUE
implies none and falls to the SR12 diagnostic, because the standard names no rule for which of several literals
fixes the one description every repetition shares and taking the first would silently truncate the longer ones.
That DETERMINATION is written at the site.

**PB489 — a file clause's `data-name-n` operand is a QUALIFIED-DATA-NAME, resolved once.** `LINAGE IS SZ OF
GRP-B LINES` built the page on the FIRST-declared `SZ`: the binder kept only the first WORD of the written
reference and threw the qualifiers away, so a qualified operand bound another item of that name — a silent wrong
answer — an ambiguous operand resolved by declaration order, and `LINAGE IS LINAGE-COUNTER OF LPF` recorded the
FILE NAME as the clause's data-name and killed the process at OPEN naming a word the programmer never wrote.
`DataBinder.QualifiedCandidates` is now THE ONE §8.4.2.2 matcher, hoisted out of
`ReferenceResolver.ResolveQualified` with the binder's weaker private `FindQualified`/`QualifiersMatch` DELETED
and `ReferenceResolver` calling it; `ClauseDataName` captures the whole written reference and refuses the three
shapes a data-name-n cannot be — a special register, a subscript (§8.4.2.3's qualified-data-name-WITH-subscripts
is an identifier form), and a reference-modifier (§8.4.3.3.3's NOTE: "where data-name-n is used in a general
format or syntax rule, then reference-modification is not permitted") — on one new code, **COBOLNET2024**;
`ResolveClauseOperand` reports zero/many survivors under the existing COBOLNET1639. §13.18.34.3 SR1
("Data-name-1 … shall not be subject to any OCCURS clauses") had no site anywhere and gets **COBOLNET2025**, and
`ResolveReceiving` gains its missing LINAGE-COUNTER arm for §8.4.3.14.3 SR2 ("The LINAGE-COUNTER identifier shall
not be referenced as a receiving operand") as **COBOLNET2026**. The sweep found FILE STATUS, RELATIVE KEY and
RECORD … DEPENDING ON carrying BOTH defects and fixed them the same way; ASSIGN USING and the two RECORD KEY
clauses take their candidates from the one matcher. No grammar change. The golden pins the point of the whole
change: a literal page and a qualified-data-name page must be branch- and byte-identical.
`ClauseOperandCaptureDriftTests`'s four assertions were each proven red on the base.

**PB502 — the discharge was real; what it lacked was the evidence.** All six repros are DISCHARGED on today's
tree, cured by PB398's one resolver `TryResolveRangeAlphabet` for both clauses with PB695 (bracket order) and
PB761 (national range un-staged) — so this landing is the witness, not a fix. PB398's positive golden names its
alphabet as the PROGRAM COLLATING SEQUENCE, which BLINDS its level-88 legs (re-dropping the bind leaves it
green), and every one of its negatives is an EVALUATE-arm case. The new golden names NO program collating
sequence and makes every leg the opposite of its no-phrase twin, measures EC-RANGE-INVALID on the NAMED sequence
in both directions, and prints the bracket order; three VALUE-arm negatives cover COBOLNET1997/1998 at all four
editions and COBOLNET1999 at 2002+ (85 is excluded because a second independent cause fires there). The design
doc records why one program cannot discriminate for both clauses. The sibling sweep confirms all four
`IN alphabet-name` grammar slots have readers and that the CLASS clause's range is correctly NATIVE per
§12.3.7.4 GR12.

**PB522 — GROUP-USAGE and four sibling clauses no longer fall off a TYPE / SAME AS copy.**
`DataBinder.CopyEntryDescription` is now THE ONE copy: `CloneItem` funnels through it and its duplicate field
list is DELETED, which recovers GROUP-USAGE, ALIGNED, ANY LENGTH, DYNAMIC LENGTH (+LIMIT) and SYNCHRONIZED on
every copy. Its `copySync` parameter became `copyAlignment` on a DETERMINATION: §13.18.57.4 GR1 excludes "the
level-number, name, **alignment**, and the GLOBAL, SELECT WHEN, and TYPEDEF clauses", and the word *alignment*
names SYNCHRONIZED AND ALIGNED, so a level-1/77 TYPE subject takes neither. The structural half is the point:
`Binding/Model/DescriptionCopy.cs` puts a `[DescriptionCopy(kind, reason)]` on all 53 stored `DataItem`
properties and `DescriptionCopyCompletenessDriftTests` asserts that no field is unclassified, that the copy
transfers exactly the Clause/Alignment set and that `CloneItem` re-spells no clause — so the NEXT property a
sibling adds is a compile-visible decision instead of a silent omission, and the test proves its own failure
branch. Measured against byte-identical inline controls: `8 8 → 4 4`, `1 1 → 3 3`, `8 8 1 1 → 4 4 5 5`, `6 → 3`,
ALIGNED `1 → 11`, DYNAMIC LENGTH `1 → 5`. A citation sweep in the same pass fixed JUSTIFIED cited as §13.18.34
(LINAGE) at six sites and `DataItem.Own88s` §13.18.4 → §13.16.3 SR24. LANDER MERGE NOTE: PB463 (train 33)
replaced `DynLengthLimit` with `DynMaxSize` under this note's feet; the merged tree carries PB463's field, PB522's
two `[DescriptionCopy]` annotations on it, and `CopyEntryDescription` copying `DynMaxSize`.

**The train.** Five clusters, five commits, one landing, one build, one gate. Every cluster was rebased onto train
33's head and `DEVLOG.md` and `tests/version-matrix/traceability-inventory.json` were EXCLUDED from all five
patches by construction — one entry for the train, and the six verdict batches re-applied in manifest order on the
merged tree, which is the only way a JSON list that five branches all append to cannot lose an element. Three
merges were real work rather than hunk arithmetic. `DataBinder.Reports.cs` had PB565 restructuring
`BindReportGroups` into `BindReportEntries` + `ReportGroupBuild` while PB506 (already on main) had added the
repetition count to the same scope stack and turned `ReportFieldModel.Source` into `Sources`; the merged file
takes PB565's structure with PB506's operand list re-applied into it, the scope-stack frame now carrying
`EntryRepetitions(columns, ownOccurs)` — which had to grow its OCCURS arm, because PB506's screen would otherwise
have REJECTED a legal multi-operand VALUE on an entry subordinate to the repeating entry PB565 had just made
legal. `ReportWriterEmitter.FieldImage` took ONE `rep` signature, with the operand cycle indexed by
`RepetitionOrdinal + rep` (the entry-wide ordinal §13.18.63.4 GR23 counts) while the VARYING counter keeps the
per-field placement index. `DataItem.cs` merged PB463's `DynMaxSize` with PB522's annotations. Generated outputs
were REGENERATED on the merged tree and compared, not hand-merged: `gen-constructs.ps1` reproduced the merged
`.g.cs` line for line (236 rows), `gen-diagnostics-doc.ps1` reproduced the hand-merged `DIAGNOSTICS.md` with zero
drift, and `gen-cobol-words.ps1` printed 124 — the count train 33 had to correct. Gate: one
`dotnet build CobolSharp.sln -c Debug`, then Conformance on the union filter
`~Drift|~EditionGate|~Report|~Nist|~ImpliedPictureTests|~CorpusManifest|~VersionMatrix|~ClauseOperandQualificationSpecTests|~Linage|~DataClause`
(every term measured with `filter_population.py` first; all fifteen live across the two assemblies, and
`~ImpliedPictureTests` is reported INERT against Conformance because its 40 tests live in the Unit assembly the
gate runs UNFILTERED — named rather than dropped), the FULL `Cobol.Net.Tests.Unit` assembly, the legacy
`CobolSharp.Tests.Integration` assembly, the Characterization suite, and a DisplayName leg over the eighteen new
corpus cases. Conformance **3048 / 3048**, the full Unit assembly **23993 / 23993**, Characterization **33 / 33**, legacy Integration **503 / 504** (1 skipped) and the DisplayName leg **18 / 18**. ONE red had to be attributed and it was the LANDER'S OWN, not a cluster's: `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree` rejected the re-measure batch's `src/Cobol.Net.Compiler/Binding/Model/ReportFieldModel.SourceAt` — no such file, `ReportFieldModel` lives in `DataBinder.Reports.cs` — which is the gate doing exactly the job its banner claims ("it is what checks that these references actually resolve"); the batch was corrected and re-recorded and the assembly is green. Semgrep unchanged on every rule
(`bound-node-carries-rendered-text` 3, `no-biginteger` 46, `no-decimal` 2, `raw-diagnostic-code-literal` 419).
GAP **2481 → 2468**: twelve rows from the five clusters' batches plus the lander's re-measure of
`GR-13.18.63.4-21`. Notes: PB562, PB504, PB831, PB828, PB489, PB502, PB522 → `landed`; PB565 stays open for the
vertical axis; PB876 filed for the national-edited residue. No cluster was dropped. Diagnostic codes claimed:
COBOLNET2021, 2024, 2025, 2026.

