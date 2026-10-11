## Entry 1651 — 2026-09-22 21:28 PDT — Landing train 50: wave 49 (AA–AG), seven clusters, fifteen notes, GAP −31 to 2130

Landing train 50 carried wave 49's SEVEN clusters and FIFTEEN notes to `landed` in one landing. It is the first train
landed under the owner's 2026-09-22 lander-throughput decision (DEVLOG 1645): it was merged and gated WHILE train 49 was
still in CI, then rebased onto train 49's head. One commit per cluster, a PB957 follow-up, and this entry's commit with
the verdict batches.

**AA — PB943 + PB944, group senders.** PB943's NUL/space image was the §13.18.38.4 GR8 extent computed with a
character stride against a byte total (`OdoModel.WrapGroup`); it now uses the extracted
`RecordLayout.PhysicalOccurrenceWidth`. Its "proof" filter was false for edited and boolean receivers and for bit-group
senders, so GR1's route now asks GR2's own receiver set (`MoveClassifier.SubstitutesForZeroLength`, one predicate for
literal, item and freeze). PB944's headline was CONFORMING — an alphanumeric group under §13.18.29.4 GR3 — but its sweep
found a national group decoded as UTF-16 bytes into a numeric receiver, a CS1061 on a zero-length national group, and
GR4 group moves re-encoding into national and bit receivers. All are fixed through one group value reader
(`PlaceRenderer.SendingGroupValue`) and a storage-unit decode in `EmitGroupToElementaryMove`. Merge finding: main's
PB871 had made `ReceivingStore.Characters` THE elementary character store; AA's new fit spelled
`RuntimeApi.StrStoreAligned` directly, which `ReceivingStoreDriftTests` forbids, so the resolution routes it through
`ReceivingStore.Characters`. Golden `2002/pb943_group_sender_conversions`, negative `pb944-bit-group-to-numeric`. Four
witness-only rows (GAP +0).

**AB — PB951 + PB957, report writer.** The relative operand is ONE fragment, `reportRelativeSign : PLUSWORD | PLUS`,
so `LINE + 1`, `COLUMN + 2` and `NEXT GROUP + 2` parse (§13.18.35.3 SR1, §13.18.14.3 SR2, §13.18.37.3 SR2), pinned by
`GrammarRelativeSignDriftTests`. The NEXT GROUP clause, previously COBOLNET0899 at every edition, is live in all three
forms with WITH RESET; `BindNextGroupClauses` screens §13.15.3 SR6 and §13.18.37.3 SR1/SR3–SR7 as the new COBOLNET2284,
and the engine's single `ApplyNextGroup` applies §13.18.37.4 GR1–GR6. Three goldens and ten negatives. The implementer's
two self-attributed Unit reds did not recur on the merged tree: `ClosesRowsBackLinkDriftTests` (train 47 applied the L
batch) and the `GrammarDiagramGeneratorDriftTests` shared-temp-dir race. Lander findings: (1) main's PB388 code-citation
audit refused the golden comment `GR5 b)` beside a `§14.9.46.4` citation — it meant §13.18.37.4 GR5 b) and now says so;
(2) the report's three DETERMINATIONS (PAGE-COUNTER reset at the next page advance; no FOOTING clamp in an unpaged
report; TERMINATE discards the save location) were applied in the Report Writer design doc but recorded nowhere as
determinations — they are now a ⚖ block in `docs/CONFORMANCE.md`, every citation `cite.py --check`ed. 17 rows CONFORMS.

**AC — PB961 + PB952, float edges.** A FLOAT-BINARY-32 signaling NaN was quieted twice — RyuJIT folding the inline
`Int32BitsToSingle` into a double-carried constant, and the binary32 image lane widening through
`FormatImageFloat(double)`. SET Format 15 now emits `CobolFloat.FromBinary32Bits/64Bits` (NoInlining) over
`IeeeSpecials.Bits` (`Text` deleted); `NumFormatImageFloat` / `NumParseImageFloat` take a MANDATORY width flag from the
item; a same-usage float MOVE copies on the carrier (§14.9.25.4 GR6 c)). PB952: §15.3 rule 14's argument-value rules
are screened on the EXACT operand before binary64 conversion for all seven rows that state one, from a new
`IntrinsicSig.Domain` column held to the spec by `IntrinsicArgumentDomainDriftTests`. The mandatory flag did exactly
what its author said it would: main's PB887 float device-ACCEPT arm (landed after AC's base) failed to compile against
it and now passes `fpic.IsSingle`. The implementer's one Unit red was the same diagram-generator temp-dir race; it did
not recur. Six rows CONFORMS (GAP +0), seven `IeeeSpecials.Text` witnesses retired.

**AD — PB881 + PB856 + PB923, receiving-operand roles.** Every verb binder now states each operand's role
(`ResolveReceiving` or the new `ResolveSending`), and `ReceivingResolutionDriftTests` forbids `ctx.Refs.Resolve` under
`Verbs/`. The 50-site sweep found 16 receiving operands that bypassed §13.18.15.3 SR2. PB856: INSPECT SR1/SR2 read one
display-or-national predicate. PB923: a sole function-identifier INVOKE argument takes the MOVE lane for a non-numeric
formal. Merge findings: PB388's re-cited PtrBinder strings met AD's `receiving: true` (both kept), and train 49's
PB909 `BoundRejected` in `CorrespondingBinder` met AD's `ResolveSending` (both kept). Three goldens, seven negatives,
3 rows CONFORMS.

**AE — PB837 + PB186, SORT transfer.** The implicit READ, WRITE and CLOSE of a SORT/MERGE transfer each offer their OWN
I-O status to their own USE hook (§14.9.40.4 GR12/GR15, §14.9.24.4 GR7/GR12); three pb714 goldens were re-derived.
PB186's named table-sort arm is unreachable for binary keys, but its runtime twin `CobolSort.KeyColumns` decoded an
unsigned 16-byte COMP-5 key signed; it now has a UInt128 column. The table comparer decodes through the extracted
`NumericRenderer.WindowedNum`. Merge finding: `WindowedNum` absorbed `FieldNumCore`'s three windowed arms while AC gave
the float one a new argument and AA rewrote the group arm beside it — the resolution keeps AA's `SendingGroupValue` arm,
AE's single `WindowedNum` arm, and passes AC's `binary32Carrier: false` inside `WindowedNum` (an arithmetic read widens
to binary64, D16). No rows (both notes `closes_rows: []` with a reason).

**AF — PB941, >>PUSH / >>POP.** Both directives now save and restore directive state (§7.3.22.4, §7.3.20.4) through one
`DirectiveStateStack` with a stack per pushable row, each state-holding stage registering a carrier; an unsuccessful
named POP is warning COBOLNET2297 (DOC-A.1-140). Merge finding: main's PB829 had replaced the two private first-unit
detectors AF hung `state.AdvanceTo` on with `CompilationUnitStart.IsAt`; the resolution keeps AF's block on main's one
test. The implementer's red was the diagram-generator race again. 7 rows CONFORMS.

**AG — PB706 + PB778 + PB924, general formats.** `RECORD? SEQUENTIAL` (RECORD gated at 2023,
`file-organization-record-sequential-2023`); EDITING literals as `editingLiteral` through the one literal-position
chokepoint, which also closes a silent wrong answer (`IS SPACE` inserted as its source text); the lexer tracks the
REPORT SECTION so a bare SUM's '(' is arithmetic (§13.18.54.3 SR9). The generated construct files were regenerated on
the merged tree and matched. 5 rows CONFORMS (GAP −4).

**The train.** Gate on the merged tree before train 49 (`FullyQualifiedName~CobolNet.Tests`, i.e. the WHOLE
Conformance assembly, which includes CorpusRunner, VersionMatrix and NIST): Conformance 7,984/7,984, Unit
28,791/28,791, Characterization 33/33, legacy Integration 503 + 1 skipped; semgrep unchanged. The rebase onto train 49
conflicted in CODE (`CorrespondingBinder`), so the whole gate ran again on the rebased tree: Conformance 8,073/8,073, Unit 28,839/28,839, Characterization 33/33, legacy Integration 503 + 1 skipped. GAP
2175 → 2144 (−31). No cluster was dropped; the train claimed COBOLNET2284 and COBOLNET2297. The lander waited
44 (18:13 → 18:57 PDT) minutes for train 49. Registrar #12 (DEVLOG 1649), which landed while this train was in CI, filed the implementers' leads as notes among PB986–PB1032 (the diagram generator's temp directory is PB376). The leads were: the diagram generator's fixed temp
directory (reported independently by AB, AC and AF); a group RECEIVER still fitted by characters (PB546's side) and a
dynamic-length national receiver taking bytes as characters (AA); LINE NEXT PAGE still staged, LINE SR3/SR5 unscreened
and the placement arithmetic duplicated between binder and runtime (AB); COBOLNET0900 always naming FARTHEST-FROM-ZERO,
LOG of a sub-binary64 SDIDI, and a table(ALL) PRESENT-VALUE lead screened on a double (AC); CALL BY CONTENT FUNCTION's
diagnostic, INVOKE BY CONTENT FUNCTION INTEGER into alphanumeric, and a duplicated EXCEPTION-OBJECT screen (AD);
SORT/MERGE termination rules, relative/indexed USING/GIVING, SR11 and two inherited citations (AE); §14.9.28.4 GR14's
implicit POP ALL restoring TURN only, PUSH/POP SR3 unenforced, and `>>SOURCE` acted on inside an omitted `>>IF` (AF).

**CI caught what every local gate could not.** The first push-main run (35810600940) was RED on one job, the Conformance shard-population guard: "shards executed 8073 of 8074 discovered tests" on both platforms, every test green. The union resolution of the 2023 `manifest.json` had left `pb854_eop_perform_when` in the `enabled` list THREE times (train 49's line, then a copy riding on AF's `pb941` line and another after AG's entries). `--list-tests` counts each occurrence, execution does not, so the local whole gate reported 8073 green and could not see it. The fix is at the root, the manifest loader: `ConformanceCorpus.Load` now THROWS when a name appears twice in `enabled`, twice in `pending`, or in both, and names the entry. Proved to fail once (a planted `pb249` duplicate made `CorpusRunnerTests_P2` fail with "lists pb249_trim_argument2_repeats more than once"). A sweep of all five corpus manifests found no other duplicate. After the fix, `--list-tests` counts 8,072 and the corpus runner listed and executed the same 2,619.

**Train 51 landed first, and train 50 was rebased onto it.** The second push-main run (on registrar #12's head) had a green `ci-gate`, but its fast-forward was refused because train 51 had reached main while the run was in flight. That rebase conflicted in CODE:
- **AA `MoveEmitter`:** train 51's PB979 made `ReceivingStore.AnyLengthWidth` the one spelling of an ANY LENGTH receiver's width. AA's storage-unit fit now takes its any-length positions, and its national and bit area widths, from that helper instead of spelling `.Length` itself.
- **AD `CallBinder`, `InspectBinder`, `SetBinder` and `SortBinder`:** train 51's `DataBinder.WrittenText` met AD's `ResolveSending` and `BoundNop`. Both were kept.
- **AD's drift test:** train 51 had added a new `ctx.Refs.Resolve(undefined)` in `ConditionBinder`. `ReceivingResolutionDriftTests` forbids that call, so it now reads `host.Expr.ResolveSending(undefined)`, which is the same resolver under its stated role.
- **AB `DataBinder.Reports`:** the `using` lines from both sides were kept.
- **Manifests and `DIAGNOSTICS.md`:** unions, then checked. No manifest has a duplicate, and `gen-diagnostics-doc.ps1` and `gen-constructs.ps1` reproduced the committed files exactly.

The inventory merged textually. A per-row three-way check against train 49 found no row changed on both sides, and every row matches the side that changed it. GAP is 2161 → 2130, the same −31.

The whole gate was re-run on the rebased tree: Conformance 8,135/8,135, Unit 28,877/28,877, legacy Integration 503 + 1 skipped, build 0/0, citation audit 0 findings, witness loss GREEN. Characterization went 32/33. `char_string_ops` changed because train 51 now routes UNSTRING receivers through the MOVE chain, and there AA's §14.9.25.4 GR1 zero-length-sender arm meets the dynamic-length concept item. The emitted code gained an `if (Length == 0)` space-fill arm ahead of each `CobolString.Store`. For these alphanumeric receivers it yields the same value. The snapshot was re-baselined, and Characterization is 33/33.

