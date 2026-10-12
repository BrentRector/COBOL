## Entry 1619 — 2026-09-19 16:27 PDT — LANDING TRAIN 41: six clusters, twelve notes, GAP 2396 → 2375 — every conflict was a registry tail, and the one red was the sibling the migration missed

**Cluster 1 — PB461, the ALL-literal / figurative-constant classifier.** §14.9.39.4 GR6 puts a level-88 VALUE
literal into its conditional variable "according to the rules for the VALUE clause", and §8.8.4.5.3 3) makes the
round trip an invariant: `SET cond TO TRUE` then `IF cond` shall be true. It was not. Five private readings of the
`ALL` prefix had grown up in codegen and binding, each with its own idea of what `ALL "x"` denotes, so
`SET C TO TRUE` on `88 C VALUE ALL "*"` stored the literal text `ALL"` and the test that followed was FALSE. The
re-probe found worse than the note claimed: `88 N-ZERO VALUE ZERO` over `PIC 9(4)` emitted the bare identifier
`ZEROL`, so legal COBOL failed to compile at all with `error CS0103` — the note's `rejects_legal_source: false`
did not hold and is now `true`. The fix is one classifier, `FigurativeConstants.Classify`, that every reading now
routes through (`ValueInitializer`, `GroupValueSlicer`, `ConditionRenderer`, `EmitCore`, `SetEmitter`,
`CobolLiteral`, `DataBinder`), with `FigurativeOperandClassifierDriftTests` forbidding a sixth private
`StartsWith("ALL"` outside the three named files — INITIALIZE's copy is exempted by name and with a reason, and
is the registrar lead that goes with this cluster. Goldens: `pb461_all_literal_value_categories` (2002),
`pb461_all_literal_value_roundtrip_85` (85) and the negative `pb461-level88-all-literal-zero-length`. One row
re-verdicted, GR-14.9.39.4-6 DIVERGES → CONFORMS; COBOLNET2135–2137 were returned unused because §8.3.3.6.3
SR2/SR3 are already carried by COBOLNET1648 and COBOLNET1657, both re-measured.

**Cluster 2 — PB465, SET pointer UP/DOWN BY as TWO rules.** §14.9.39.4 GR19 tests the AMOUNT ("If
arithmetic-expression-3 does not evaluate to an integer, the EC-SIZE-ADDRESS exception condition is set to
exist") and GR20 tests the RESULT ("If this new address is outside the range of values allowed by the implementor
for a data-pointer data item, the EC-RANGE-PTR exception condition is set to exist"). The compiler had one check
doing neither job properly: `CobolPtr.UpByScaled` raised EC-SIZE-ADDRESS on a `FLOAT-LONG 1.0E19` that IS an
integer, while the emitter's `long __ptrBy = (long)(…)` silently WRAPPED both a 2^64+2 displacement and an
integer written at scale 1. The note claimed only the FLOAT arm over-fires; probing the magnitude axis on the
other arm found two more, and a third fell out of the repair (`UP BY long.MaxValue` had been reading position 0
of a 4-character cell, now a fatal EC-BOUND-PTR). `SetAmount` is the new shared exact-value carrier;
`CobolIndex`'s two method bodies became delegations to it, so the index and pointer forms now answer one
question once. COBOLNET2132–2134 were returned UNUSED on the deliberate ground that GR19 and GR20 are RUNTIME
rules with spec-named runtime outcomes — a compile-time rejection would reject legal source. Determination
DOC-A.1-216 (§A.1 216, "USAGE POINTER clause … and range of values. This item is required.") is now documented
in `docs/CONFORMANCE.md` §7: the data-pointer range is the carrier's signed 64-bit displacement interval, which
is the MEASURED answer and not PB460's proposed "unbounded" — PB460 stays open with row 216 supplied and
cross-referenced. Goldens `pb465_set_pointer_range_ec` (2002) and the negative
`pb465-set-pointer-up-by-at-85`; two rows re-verdicted. Fixed in passing: a raw NUL byte in
`tests/Cobol.Net.Tests.Unit/CobolPtrTests.cs` that had made the file binary to grep — the lander had to
regenerate that cluster's patch with `--binary` to carry it, and the repo-wide NUL scan is a registrar lead.

**Cluster 3 — PB554 + PB564 + PB513 + PB536, the data-description screens.** Two of the four fixed, two closed on
does-not-reproduce evidence — and that split is the finding. PB513 reproduced exactly: §13.16.3 SR14 forbids TYPE
sharing an entry with any clause outside a named list, and with SR14 unenforced the subject's own PICTURE won
through `CopyEntryDescription`'s receiver-wins `??=`, so `01 T IS TYPEDEF PIC X(3). 01 A TYPE T PIC 9(5).` gave A
a 5-digit numeric description the type never declared. SR14 is the WARRANT for that one description copy, so its
absence was not merely accepting illegal source; it was silently discarding a declared one. COBOLNET2150 now
names it. PB536 reproduced as a false rejection — §13.18.52.3 SR3 requires signed numeric items under a CODE-SET
file to carry SIGN IS SEPARATE, and the compiler was raising COBOLNET1672 at the FD instead of screening the
records; the new `DataBinder.CodeSetRecords.cs` runs as a `BindPipeline` pass at `PassPhase.SignResolved`, where
the sign representation is actually known, and `ValidateDag` asserts that ordering. PB554 and PB564 did NOT
reproduce: PB554's premise ("`PicCategory.NationalEdited` is a distinct member") is false on today's tree since
`fafeadb8`/`609e1e71`, and PB564's two paths both draw COBOLNET1639 at bind since `1aa1310e` — both closed on
measurement, which is what a re-probe is for. Goldens `pb513_type_entry_composition` (2002),
`pb536_code_set_group_sign` (85) and two negatives; three rows re-verdicted; 2151–2154 returned.

**Cluster 4 — PB491 + PB531 + PB532 + PB568, PictureAnalyzer repetition factors and the EDITING phrase.** PB531
was a crash, not a diagnostic: `01 W1 PIC X(-3).` threw an unhandled `ArgumentOutOfRangeException` out of
`PictureAnalyzer.cs:580` with rc=1 and no message at all, `PIC 9(CONSTANT-N)` over a negative CONSTANT did the
same through the other arm, `PIC X(2000000000)` was an `OutOfMemoryException`, and `PIC X(+3)` was accepted
silently as `XXX`. §13.18.40.3 SR6 says the parenthesised repetition factor is "an unsigned nonzero integer", so
all four are one rule with one screen: COBOLNET2147 for the shape, COBOLNET2148 for the size, and a determination
the standard does not make — the maximum item size is 2^27 character positions (A.1 carries no item for it).
PB532 is SR4's flat limit ("The maximum number of characters allowed in character-string-1 is 63"), which 64
written X's had been passing at every edition; COBOLNET2146. PB491 and PB568 are the EDITING phrase, which was
drawing COBOLNET0899 on Annex D.24's own images: `CobolEdit` is re-architected around `Render` to a logical image
with `Materialize`/`DeEdit` at GR14 widths, `ExpandCurrency`/`CollapseCurrency` are DELETED, and
`RuntimeApi.MaskScale` now takes `PicInfo`. The grammar's `editingPhrase` takes `EDITING ( cobolWord | literal )`
and twelve corpus and test files moved to the bare `EDITING X` spelling; a quoted character-1 is now refused by
name at bind (COBOLNET2149). What did NOT hold: PB531's zero case no longer produces a zero-length item —
PB528's composition validator closed that in train 26 — but it was still being reported by the WRONG rule, GR11's
"at least one symbol '9'", a rule about a NUMERIC string, and it now names SR6. Ten rows re-verdicted, three of
them re-anchored; 2146–2149 all used; `COBOLNET_DATA_MODEL_DESIGN.md` §D24 carries the two new paragraphs.

**Cluster 5 — PB882 + PB883 + PB840 + PB852, the report-writer sum counter.** Three questions about one counter —
its identity, its name, and the form of its operand — that turned out to be one mechanism. §13.18.54.4 GR1: "Each
entry containing a SUM clause establishes an independent sum counter", and the compiler had been sharing one, so
two SUM entries over the same addend printed `0022  0022` where `0011  0022` was owed. GR5 makes the data-name
the counter's own name and GR12 permits procedure division statements to alter it, neither of which a
`COBOLNET1639: 'CF-SUM' is not defined` allowed. §13.18.54.3 SR1 makes each addend "data-name-1, identifier-1 or
arithmetic-expression-1", and SR3 attaches ROUNDED to COLUMN, where the grammar had a bespoke `sumOperand` that
admitted neither an expression nor a rounded phrase — `SUM A + B` was `COBOL0307: unexpected '+'` and
`SOURCE (X)` was `COBOL0001: unexpected '('`. The grammar now spells one `reportValueOperand :
arithmeticExpression` with the shared `roundedPhrase?` and `sumOperand` is deleted; `Place.cs` gains
`ReportSumCounterPlace`, `ReferenceResolver.SumCounterFor` resolves the name, and `MoveEmitter.ConvertSource` and
`NumericRenderer.Align` each took ONE optional parameter whose default preserves every existing caller.
Determination: §13.18.54.4 GR4 — SUM's ROUNDED governs the accumulation of each addend INTO the counter, not the
counter's later presentation. Every note held on today's tree; one note's predicted FIX SHAPE did not (PB852).
Seven rows re-verdicted across two batches; 2141–2145 all used. Registrar lead: a parenthesised-FIRST addend
`SUM (A * B)` is still refused by the LEXER, because `SUM` carries `subscriptTrigger: true` and puts the scanner
into SUBSCRIPT mode — one lexer-region mechanism note for the §8.9 ∩ §8.11 words (SUM, LENGTH, RANDOM, SIGN).

**Cluster 6 — PB428, §8.4.3.1.2 Format 4 inline method invocation.** `MOVE A1 :: "ADDTO" (AMT) TO R` produced four
raw parse errors naming a colon, because `::` had no lexer rule at all and the parser rule bearing the
construct's name matched `dataReference ( argumentList )` as a STATEMENT — the identifier format was implemented
in the one place it is not an identifier. The fix is the shape the next case gets for free: a `COLONCOLON` token,
one `inlineMethodInvocation` rule in `CobolOO.g4`, and Format 4 added to ALL TWENTY operand rules that already
admit a `functionCall`, with `inlineMethodInvocationStatement` deleted. `VersionConformancePass.VisitArgument`
gates the boolean operators, `OoBinder.InlineInvocation.cs` binds the receiver and the RETURNING obligation
(COBOLNET2138–2140), and the construct is registered at its introducing edition 2002. Two PB331 negatives moved
from COBOL0001 to COBOL0307 — the OLD code was the artefact of the missing token, and the reason is written into
the fixtures themselves. One row re-verdicted. Registrar lead: an INVOKE argument that is a FUNCTION-IDENTIFIER
is bound as an expression and draws COBOLNET0828 on an alphanumeric function into a PIC X formal — §14.8.2.3.3
rule 2d, not 2a — to be filed with the sole-inline-invocation recovery.

**The train.** Six clusters is the top of the 4–6 band and the largest train run so far; all six landed, none
dropped. Every cluster was cut from `01062f29` and rebased across trains 39 and 40 by the lander. EVERY conflict
in the train was a registry TAIL — `DiagnosticCatalog.cs`, `docs/DIAGNOSTICS.md` and four corpus `manifest.json`
files, in every case both sides appending to the same list — and the textual three-way merge produced a
syntactically BROKEN JSON tail twice before the lander stopped merging registries as text: the corpus manifests
were rebuilt structurally (revert to HEAD, parse, insert the entries the patch adds, re-parse to prove the file
still loads and the element count moved by exactly the number added), and `DIAGNOSTICS.md`,
`ConstructRegistry.g.cs` and `Constructs.g.cs` were REGENERATED from their sources afterwards — the two `.g.cs`
files came back byte-identical to the merge, which is the evidence that the merged `constructs.json` (238 rows,
unchanged) is what the generator would have written. The traceability inventory was excluded from all six patches
up front and rebuilt by re-applying the eight verdict batches in landing order: GAP 2396 → 2375, exactly the −21
the six reports predicted, with no row dropped. The union gate was fifteen terms —
`~CorpusRunner|~Nist|~Drift|~EditionGate|~VersionMatrixTests|~SpecTraceabilityInventory|~PictureEditing|
~PictureComposition|~CobolEdit|~ReportWriter|~OoSpine|~Pointer|~DataSkeleton|~Invoke|~FigurativeOperandClassifier`
(four of them INERT against Conformance and named as such by `filter_population.py`, their tests living in the
unfiltered Unit leg) — and IT WENT RED, on exactly the term the brief insists on and no implementer filter runs:
`VersionMatrixTests_P7.Construct_MatchesEditionExpectation(constructId: "picture-editing-2023", edition: 2023)`
drew cluster 4's own new COBOLNET2149, because the version-matrix construct SAMPLE in `constructs.json` was the
THIRTEENTH site of the `EDITING "T"` → `EDITING T` spelling migration and the only one outside the twelve corpus
and test files the implementer converted. That is the shape the cluster's own note predicted — "a sibling editing
any of those … MUST take this spelling or its case draws COBOLNET2149" — landing on a file the note did not think
to name, and it is the second train running in which the version-matrix shard caught a construct sample that a
newly enforced syntax rule refused. The sample was migrated (the fix cluster 4's change requires, not a new
decision: §13.18.40.3 SR8 types character-1 as a basic letter, so the quoted form was never conforming source),
the registry regenerated, and the affected legs re-run. Verdicts, in order: Conformance union 5511 tests, 1 failed
→ after the repair Conformance `~VersionMatrixTests|~PictureEditing|~DataSkeleton` 2383 passed, 0 failed; Unit
24,249 passed, 0 failed (re-run after the sample change); Characterization 33 passed; the legacy
`CobolSharp.Tests.Integration` 503 passed, 1 skipped. The external GnuCOBOL corpus fetched into this fresh
worktree on the first try, so `ExternalCorpusPopulationDriftTests` measured a real population. Semgrep: no count
moved against `scripts/semgrep/baseline.json`. `work.py check` — 939 work items, all well-formed; the code,
doc and evidence-supersession citation audits all at zero findings; the Annex A.1 audit reports no misfiled
determination. And the CI matrix then found a SECOND red that no local gate had: the Linux and Windows `rest` Conformance shards — the tests matching NEITHER `~VersionMatrixTests` NOR `~CorpusRunner` NOR `~Nist`, which is precisely the population a union filter built out of the clusters' own subjects cannot reach — failed on `IntrinsicFunctionDifferentialTests.Length_SuperIsStructurallyUnparseable`, attributed to cluster 6. §15.3 11) says "the predefined object reference SUPER shall not be specified" as a function argument, and the test pinned that refusal by asserting the diagnostic TEXT names SUPER. It still refuses — the `Assert.False` never fired — but PB428 made SUPER the LEFT EDGE of a Format-4 receiver in every operand position, so the parse now runs one token FURTHER and fails at the `)` where `:: method-name (arguments)` never arrived. The repair is NOT a weakened assertion: the test now measures BOTH surfaces. The bare form keeps its Assert.False and pins the loud frontend refusal (COBOL0001) instead of ANTLR's choice of offending token, and a SECOND case pins the surface Format 4 newly makes reachable — §8.4.3.8.3 SR3 says SUPER 'may be specified only as the object in an object-property identifier or as the object used to invoke a method with the INVOKE statement or an inline invocation of a method', so a COMPLETE `SUPER :: "M"` is the PERMITTED use and is an identifier, and it is refused by NAME (COBOLNET0827) outside a method definition. That assertion is one the old test could not make at all, because the form did not parse; without it the whole receiver surface PB428 opened would be pinned by nothing there. Measured, not assumed: both probes were run on this tree before the fixture was touched. Both of this train's reds were a GREEN TEST PINNING A STAGE that a landed feature legitimately moved, and in both cases the shard that caught it was one that no implementer filter, and no union of implementer filters, runs. One report claim found false: PB882's report mis-names its own branch
(`worktree-agent-a5626b9658b4327de`); the manifest carries the verified one, `worktree-agent-ad001d586dc96f0a2`.

