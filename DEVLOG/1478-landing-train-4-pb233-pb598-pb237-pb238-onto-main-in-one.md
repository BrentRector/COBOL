## Entry 1478 — 2026-09-05 02:44 PDT — Landing train 4: PB233 + PB598 + PB237 + PB238 onto main in one landing — the CALL statement's exception partition, its argument classification and its program-prototype registry, plus the VALUE size sentences that stopped binding a condition-name (GAP 2906 → 2893)

**PB233 — the CALL statement's GR3h/GR3i partition, and the activation boundary that makes it decidable.**
`kb/Work/PB233` said a CALL written with only `NOT ON EXCEPTION` swallowed a failed activation, and that the same
unfiltered catch routed EC-FUNCTION-NOT-FOUND into the ON EXCEPTION phrase that §14.9.4.4 GR3h item 1 excludes.
Both reproduced on a fresh build. Probing them turned up a THIRD instance of the same mechanism that neither the
note nor the inventory rows had seen, and it is the one that named the root cause: a CALL whose callee is
*successfully activated* and whose callee's own inner CALL then fails ran the ACTIVATOR's `imperative-statement-1`
and let the run unit finish with exit 0 — which GR3i forbids in one sentence ("after control is returned from the
called program the ON EXCEPTION phrase, if specified, is ignored"). The emitted `try` spans the callee's entire
execution, so the CALL site had no way to tell a GR3b–f activation failure from an exception escaping the callee;
a family filter on the arm would have fixed the symptom the note named and left that one standing. So the boundary
became explicit. `ProgramTable.CallProgram` now marks `CobolCallException.ControlTransferred` on anything escaping
`inst.Call`, and every emitted arm filters on `!ControlTransferred`; the mark is monotone, so a failure deep in a
call chain is an activation failure for exactly one CALL site and post-transfer for every site above it. For
"escaped from `Call`" to *be* GR3g's test, GR3e's external-conformance check had to leave the callee body — GR3e
makes "the program call … not successful" and precedes GR3g's transfer of control — so it is now
`ICobolProgram.DescribeExternals()`, a default-no-op ABI member the boundary calls before `Call`. On that footing
the CALL emits three arms instead of two conflated ones, one per fact GR3h routes on: the enabled
EC-PROGRAM/EC-EXTERNAL arm (status set, then the phrase or the declaratives), the enabled non-family arm (status
set, then the declaratives ALWAYS — item 2's second disjunct, which had no implementation at all), and the
un-enabled family arm (the phrase only, and no status set, because item 1 carries no checking-enabled qualifier
while §14.6.13.1.1 does). The phrase arms key on `c.OnException`, never on the NOT phrase: §14.6.13.1.3 #1 admits
only "a conditional phrase without the NOT phrase". Item 2's second disjunct needed a binder half too, and that
is the part the row's own notes had mis-scoped: `EcBinder.QueryFor(BoundCallProgram)` queried only ProgramNames +
ExternalNames, so `>>TURN EC-FUNCTION-NOT-FOUND CHECKING ON` produced no `BoundEcChecked` wrapper and the emitter
had nothing to work from; a user-FUNCTION activation now also queries `FunctionActivationNames` (§8.4.3.2.4 GR6b →
GR6f). ⚠ The note's stated repro for that defect — a UDF miss while evaluating a CALL argument — does NOT
reproduce on today's tree: `UdfBinder.UdfWrapCalls` hoists the activation out of the statement, so it never runs
inside the CALL's `try`; its live carrier is a UDF miss inside the callee, i.e. the GR3i case again. Two facts
that were prose are now structure: GR3h item 1's family partition is `CobolCallException.IsProgramOrExternal`,
asked by both the compile-time split and the emitted runtime filter, and the carriable name set is
`CobolCallException.CarriedNames`, re-derived from the raise sites by the new `CallExceptionCarrierDriftTests`
(its failure branch fired once before the list was restored). A stale line fell out of the same sweep: the
carrier's class doc still claimed EC-PROGRAM-ARG-OMITTED travels on it, which stopped being true at PB133.
`GR-14.9.4.4-L2.1`/`-L2.2`/`-L2.3` PARTIAL → CONFORMS; goldens `2023/pb233_call_exception_partition` plus five new
`ExceptionConditionConformanceTests` (three of them `AssertFatal` arms a corpus golden cannot express). No
diagnostic code was needed — every arm is a runtime disposition rule — so COBOLNET1752/1753 return unused.

**PB598 — a size rule written for an item, enforced against a condition-name, and the two-parameter shape that
let it happen.** `01 XV PIC X. 88 XC VALUE "cd".` was COBOLNET1740 where ISO/IEC 1989:2023 makes it a legal,
permanently-false condition; the national and boolean twins (`88 NC VALUE N"ABC".` on `PIC N(2)`,
`88 BC VALUE B"101".` on `PIC 1(2) USAGE BIT`) were COBOLNET0898 and had been wrong since those arms were written
— only the alphanumeric one ever surfaced, as GnuCOBOL differential flip `syn_misc:388`, because the corpus
carries no oversize national or boolean level-88 literal (0 of 1,611 members). ⛔ The rules name their subjects
and the code did not. §13.18.63.3 SR4/SR5/SR10 are each a CLASS sentence plus a SIZE sentence, and the size
sentence bounds exactly two subjects — "an elementary item" ("the size indicated by an **explicit** PICTURE
clause") and a group item ("the size of the group item"). A Format-3 entry is `88 condition-name-1 value-clause .`
(§13.16.2), which admits no PICTURE clause; SR33 makes that level-88 entry the subject; §8.5.1.3.2 item 3 gives a
condition-name entry "no true concept of level", so it is neither of §8.5.1.3.1's subdivisions; and §13.18.63.4
GR19 gives it its conditional variable's characteristics only *implicitly* — precisely what "explicit" excludes.
The standard even says what an oversize one MEANS, which a size rule would make dead text: §8.8.4.5.3 item 2 sends
the comparison to the relation-condition rules (§8.8.4.2.7 / §8.8.4.2.8 / §8.8.4.2.10 item 2 extend the shorter
operand — with spaces, and for boolean with ZEROS), so it never compares equal, and §14.9.39.4 GR6 → §13.18.63.4
GR7 → §14.6.8.5 stores it "with space fill or truncation to the right". ⚠ ONE STRUCTURAL CORRECTION TO THE NOTE,
which strengthens the argument rather than weakening it: §13.18.63.3's syntax rules are printed in labelled blocks
(ALL FORMATS SR1–SR9 · FORMAT 1 SR10–SR15 · FORMAT 2 SR16–SR23 · FORMAT 3 SR24–SR33 · FORMAT 4 · FORMAT 5), so SR4
and SR5 govern a Format-3 entry DIRECTLY and only SR10 arrives through SR24 — and SR24 reads "Syntax rules 10 and
17 above apply", importing the rule WHOLE. The import is not what stops the size sentence; the sentence's own
antecedent is, and that antecedent is identical in all three rules. One argument for three arms instead of two.
⭐ The fix is a TYPE, not three nulls. The screen took an `int? sizePositions` *and* a `bool groupSubject` — one
fact spelled twice, so a caller could compose a pair the standard does not have, and the three `BindCondition`
sites did exactly that. They are now one `ValueSubject` descriptor (`Binding/ValueSubject.cs`) built only by
`ForElementary` / `ForGroup` / `ForConditionName`, each stating its size rule with its citation;
`ValueSizePositions` is deleted into `ForElementary`, and COBOLNET1570's edited-width check was brought into the
same discipline by the self-review. A fourth subject kind must add a factory; it cannot inherit a size by accident.
Goldens `85/pb598_condition_name_value_size` (the oversize singleton, an oversize THROUGH range, a mixed-size
value set, both truncating `SET … TO TRUE`s and the exact-fit boundary — every value derived before it was
measured) and `2002/pb598_condition_name_value_size_national_bit`; `ConditionNameValueSizeTests` carries the
edition axis, the COBOLNET0900 gating diagnostic below 2002 asserted to be the GATE and *not* a size code, the
CLASS half still binding a condition-name, and the drift proof that the two subjects the sentences DO name are
still measured — that last one measured RED, seven failures, against a deliberately over-withholding build before
being trusted, as was the corpus lane (a corrupted `.out` fails the runner by name; `FullyQualifiedName~pb598`
matches nothing, so a by-name green would have been silent). Rows `SR-13.18.63.3-4`/`-5`/`-10` re-recorded
CONFORMS — GAP delta ZERO, and that zero is the finding: they were closed CONFORMS on 2026-09-04 while sitting on
top of a rejection of legal source, so the rows are now true for what the rules ADMIT as well as for what they
forbid. COBOLNET1758/1759 return unused.

**PB237 — the program-prototype registry lands whole; the CALL Format-2 bracket was mis-transcribed in a code
comment, not in the spec.** `REPOSITORY. PROGRAM program-prototype-name-1 [AS literal-3].` (§12.3.8.2's
program-specifier) is now a `repositoryEntry` alternative, gated 2002+ by the new `repository-program-2002`
constructs row, and with it the three constructs that take that name as their subject came alive: CALL Format 2's
`AS` arm (§14.9.4.3 SR16), CALL Format 2's bare operand (§14.9.4.4 GR3 b) third bullet), and CANCEL's third brace
alternative (§14.9.5.3 SR3 / §14.9.5.4 GR1 c)). The pipeline mirrors the FUNCTION twin exactly because §12.3.8.4
GR10 a)/b)/c) and GR11 a)/b)/c) have identical shape: `DataBinder.BindProgramSpecifier` collects the written
specifiers and enforces §12.3.8.3 SR1/SR2; `BinderDriver.BuildProgramDefinitionTable` + `ProgramPrototypesOf`
resolve them against the compilation group and register §8.4.6.8's specifier-free containing-program spelling
(which is also how SR15's "this specifier is ignored" is realized); `CallBinder` consumes the table at both
reference sites through ONE `ResolvePrototype` lookup and ONE bare-word shape test. ⛔ RENDERING PDF PAGE 619
CHANGED THE FIX. The printed Format 2 is `CALL ⎡{identifier-1 | literal-1} AS⎤ {NESTED |
program-prototype-name-1}` — the optional bracket encloses the target brace AND the word `AS`, and the trailing
brace is not optional — so `CALL <prototype-name>` with no target and no `AS` is legal Format 2, already parses,
and needs a SEMANTIC discrimination rather than a grammar change. The g4 comment above `callStatement` had
paraphrased that format with the bracket dropped, which is why the rule carried a required target;
`specs/ISO_COBOL.md` had it right all along, and the comment now carries the rendered format and both
consequences. The structural move is renaming `NestedCalleeSignature` to `CalleeSignature`: §14.9.4.3 SR25 makes
§14.8.2/§14.8.3 apply to *a Format-2 CALL*, not to AS NESTED, so wiring the prototype producer delivered seven
conformance checks at once instead of copying them — the §14.8.2.3.2 restricted-data-pointer check is the one
deliberate exception, kept AS-NESTED-only because `StrongTypeModel` defers the cross-program axis a prototype's
definition sits on. A prototype whose program is not in the compilation group resolves WITHOUT a signature rather
than being rejected: that is GR10 c), and this implementation's external repository is the run unit's program
registry. `COBOLNET1760` (`program-prototype-undeclared`) and `COBOLNET1761` (`repository-program-specifier`) are
the two new permanent rejections; the 0899-staged `CallAsPrototypeName` descriptor is DELETED — its text asserted
that §12.3.8.2's specifier "has no repositoryEntry alternative, so no source can declare one", which this change
makes false — and `pb46-call-as-prototype-name` is rewritten from pinning the non-support to pinning the SR16
rule. Six goldens; five rows CONFORMS. ⚠ Two of the note's own claims did not survive the re-probe: `BindCancel`'s
target-discarding loop had already been fixed by PB130/PB154 (it reports and continues), and a prototype CANCEL
target was drawing a compile-time COBOLNET1639, not a run-time loud — so the note's `silent: true` flag is
retired. At landing the note flips to `landed`: the implementer had kept it OPEN so `SR-14.9.4.3-14` would not be
orphaned, but that row's other operand is a RESTRICTED program-pointer (§13.18.60.3 SR19 + §13.18.60.4 GR25), a
DATA DIVISION mechanism and the unlanded sibling arm of PB153's restricted data-pointer, and `PB609` on main
already owns the row.

**PB238 — a Format-2 CALL argument is classified ONCE, and then crosses on its own carrier.** §14.9.4.4 GR8 —
"an argument that consists merely of a single identifier or literal is regarded as an identifier or literal
rather than an arithmetic or boolean expression" — was written down inside `CallBinder`'s BY CONTENT arm and
nowhere else, so it held for `BY CONTENT N` and for none of the other four spellings. `Gr8Classify` is now the
ONE reduction and all five arms read it. ⚠ The consequence at the far end had MOVED since the note was written:
PB264 had already widened the BY VALUE emission from `ManagedPointer<long>`/18 digits to `Int128`/38, so the
recorded "19+-digit truncation" no longer reproduced — but the same unconditional narrowing truncates the FLOAT
lane, where widening is not available. `01 F FLOAT-LONG VALUE 1.5` passed BY VALUE to a `PIC S9(3)V99` formal
arrived as 001.00; §14.2.3 GR10 makes the crossing "a COMPUTE statement without the ROUNDED phrase" *into the
formal's description*, so the quantization belongs at the receiver at the formal's own scale and the caller was
doing it at scale 0. Both spellings were wrong (`BY VALUE F` and `BY VALUE F + 0`) and both are fixed: the float
lane joined the CALL ABI's carrier vocabulary — `ManagedPointer<double>`/`<float>`,
`CobolArgAdapt.ReadRealCell`/`WriteRealCell` — and `CallPlaceIsString` lost its float leg, which had been routing
a binary64 leaf onto the character image to be decoded by `CobolNum.ParseDisplay` through the receiver's ZONED
profile. Four more legs closed with it: boolean-expression-1 now crosses on `BoundCallArg.ContentBool`, the exact
counterpart of the INVOKE slot PB46 landed (it had been a bind-time refusal, i.e. one third of SR17's operand set
rejected on legal source); GR9 a)2 stopped resolve-RECEIVING a keyword-less argument, so `USING K` with
`01 K CONSTANT AS 42` no longer draws COBOLNET1548; GR9 b) derives a bare literal's mode from the corresponding
formal instead of leaving the VALUE outcome to the copy-out skip, which made §14.9.4.3 SR23's second subject
reachable for the first time (COBOLNET1762 — `callByValue` gained the `literal` alternative purely so the
standard's verdict is the compiler's, with its citation, instead of a raw ANTLR "no viable alternative"); and
SR20's carve-out is modelled — a bare object-property argument takes BY CONTENT, where binding it BY REFERENCE
had made `BoundStores` classify a SENDING occurrence ReadWrite and fire the §8.4.3.9.4 SET accessor (measured:
the golden printed `AFTER=00777` with the arm disabled). The sweep found a sixth defect in the reduction itself:
`valueOperand` is `arithmeticExpression | nonNumericLiteral` and only the first leg was recovered, so
`USING "AB" B1 B-AND B2` — where the literal reaches the boolean node on a LATER argument's B-operator — staged a
run-time `NotImplemented` on conforming source. ⚠ A CITATION THE SWEEP CORRECTED, of the shape the feedback file
names "a real clause can answer a different question": the pre-existing code attributed constant-name
SUBSTITUTION to §13.10.3 SR2, which `--check` passes but which is the ADMISSION ("constant-name-1 may be used
anywhere that a format specifies a literal"); the substitution is §13.10.4 GR1 and the class is GR2, corrected at
three new sites plus the user-visible COBOLNET1548 message. ⚠ And one adjudication claim did NOT hold, recorded
as a refutation in its row: `SR-14.9.4.3-17`'s "a bare Format-2 argument is classified as receiving" rests on a
wrong premise — in §14.9.4.2's Format-2 figure identifier-4 is the BY CONTENT / BY VALUE operand while the BY
REFERENCE arm takes identifier-2, so a keyword-less argument that meets SR3 IS identifier-2 (GR9 a)1) and SR5
makes it receiving. Three goldens, two negatives, and `CallAbiNumericCarrierDriftTests` pinning the boundary's
six-carrier vocabulary (failure branch fired). `SR-14.9.4.3-17`/`-20`/`-23` and `GR-14.9.4.4-8` → CONFORMS.

**The train.** Four clusters, four cluster commits plus this prose, brought in on the manifest's order —
PB233 (base `4fac59ef`, pre-train-2) · PB598 (`f78a825a`) · PB237 (`408e360f`) · PB238 (`408e360f`) — onto main
at `bd7910fe`. Three real merges and no cluster dropped. ① `ProgramEmitter.EmitCallMethod` against PB204: PB233
DELETES the in-body `__DescribeExternals();` call because GR3e moved to the activation boundary, and a merge that
restored it would describe externals twice — verified by grepping the whole tree for the symbol (zero hits) and
reading the emitted `ICobolProgram.Activate()`, which calls `DescribeExternals()` exactly once. ②
`kb/Work/PB594.md`'s phantom `§8.8.4.1.2` correction, which landing train 3 had ALREADY made on main in identical
substance — main's line kept, and the file ends the train byte-identical to main. ③ `CallBinder.cs` between PB237
and PB238, a genuine two-sided merge: PB237's `NestedCalleeSignature` → `CalleeSignature` rename carries the local
`nestedFormals` to `calleeFormals`, and PB238's three `Gr9BareLiteralMode(nestedFormals, …)` call sites plus its
`byContentAssumed` declaration are re-seated on it, so `Gr8Classify`, `Gr9BareLiteralMode`,
`Sr23ConstantIsNumeric`, `IsObjectPropertyReference`, `ResolvePrototype` and `CalleeSignature` all survive whole.
⛔ THE CROSS-CLUSTER ROW IS CLOSED, NOT LEFT ON A LANDED NOTE. `GR-14.9.4.4-9` was PARTIAL on PB238's tree
because leg (c) — GR9 b)'s mode read from a PROTOTYPE's formal — had no prototype to read, and PB238's branch
moved the row onto PB237. Both land here, and the leg is witnessed on the merged tree by reading PB237's own
golden: `2002/pb237_program_prototype` declares `PROGRAM SCALE-IT AS "PB237MUL"`, `PB237MUL` is
`PROCEDURE DIVISION USING BY VALUE LK-V RETURNING LK-R`, and the keyword-less `CALL SCALE-IT USING WS-N RETURNING
WS-OUT` prints `SCALED =0004` with `UNCHG  =0002` — the unchanged caller operand is the witness, correct only if
GR9 b) derived BY VALUE from the prototype's formal (`ResolvePrototype` → `callee ??= proto2.Signature` →
`calleeFormals` → `Gr9BareLiteralMode`) and §14.2.3 GR10 then kept the callee's stores off the caller. Re-verdicted
CONFORMS with both goldens as its `test-ref`. The traceability inventory was never merged textually: every
cluster's hunk was discarded on arrival and the four batches re-applied in manifest order on the merged tree, plus
that fifth cross-cluster record — 18 rows, 16 CONFORMS, then `build_inventory.py` once, **GAP 2906 → 2893**.
`docs/DIAGNOSTICS.md` was regenerated once rather than merged (COBOLNET1760/1761/1762 in, the retired 0899
`call-as-prototype-name` row out); the 85, 2002, 2023 and negative corpus manifests were resolved as
de-duplicating unions and re-parsed as JSON. Diagnostic-code hygiene was checked across the whole train, not per
cluster: 1760–1762 are unique and collide with nothing on main (the two duplicate spellings the catalog carries,
COBOLNET1535 and COBOLNET1573, both predate the train). ONE build and ONE gate for the whole train, over the
union of the four filters (43 `FullyQualifiedName~` terms — Call · Cancel · Exception · External · InterProgram ·
Corpus · Program · Function · Udf · RuntimeApiGuard · Value · ConditionName · CorpusRunner · Negative ·
NationalBoolean · Group · GroupImage · DynamicLength · Boolean · Bit · Table · TableValue · Drift · Traceability ·
SpecTraceabilityInventory · DefectiveRowCoverage · CorpusManifest · Diagnostic · Redefines · Condition · Level88 ·
Edited · Picture · VersionMatrix · Repository · Invoke · Property · Constant · Float · Prototype · Linkage ·
Interprogram · CallAbiNumericCarrier): filtered Conformance **5083 / 5083** (8 m 56 s), full Unit
**5378 / 5380** (1 m 38 s) and Characterization **33 / 33**. The two Unit reds are
`ExternalCorpusPopulationDriftTests.TheCensus_NamesEveryPopulationAndItsProgramCount` and
`…SweepExternalPopulation_EqualsTheDifferentialsCommittedCaseCount` — the known fresh-worktree shape (the GPL
corpus is ignored by version control and absent here), attributed by name; PB588's load-sensitive
`GrammarDiagramGeneratorDriftTests.Generator_RunsClean` passed. No cluster was dropped and no red needed a bisect.
The **GnuCOBOL differential** was then run against `battery-43`'s read-only corpus and produced ONE flip,
attributed by reading its program: `syn_misc:3951` ("use of program-prototype-names") is literally
`REPOSITORY. PROGRAM test-prog.` with `CALL test-prog` and `CANCEL test-prog` — all three of PB237's surfaces —
and it moved WE_REJECT_THEY_ACCEPT → AGREE_ACCEPT. It is legal source under §12.3.8.2 / §14.9.4.3 SR16 /
§14.9.5.3 SR3, and the undefined `test-prog` resolving WITHOUT a signature rather than being rejected is
§12.3.8.4 GR10 c) exactly, so the row was rebaselined and the re-run reads
`=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`. ⭐ `syn_misc:388` — the one flip battery #45 recorded as owed to
PB598 — is silently back on its committed AGREE_ACCEPT row, which is the whole point of PB598's landing; that
debt is retired. `python scripts/spec/work.py check` →
`✓ 671 work items, all well-formed`; `python scripts/semgrep/verify.py` **PASS** with every count at baseline
(raw-diagnostic-literal 441 → 441, no-biginteger 36 → 36, no-decimal 2 → 2, bound-node-carries-rendered-text
3 → 3); both citation audits at zero after the last write.

