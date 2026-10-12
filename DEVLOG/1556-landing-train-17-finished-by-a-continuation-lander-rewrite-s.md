## Entry 1556 — 2026-09-06 09:17 PDT — LANDING TRAIN 17, finished by a continuation lander: REWRITE's six alternate-key obligations and the declined annex as descriptor DATA; an as-if OPEN that renders the whole OPEN; three rows PB339 made true; every filter TERM put back to vstest; a shared EXTEND release placed and numbered at the WRITE; and the last ten optional words, with the audit at zero

**This train was landed by TWO landers.** The first was killed by an API session limit after committing rows 1
and 2 and part-way through row 3; the orchestrator preserved its uncommitted work as a WIP commit and dispatched
a fresh lander onto its branch. The continuation read the WIP against the row's expected share — 3 files
`+214/−4` where PB737's own work is 4 files `+238/−28`, the missing file being the traceability inventory the
standing rule discards anyway — and, rather than promote a partial checkpoint into a landing commit, reset to
row 2's boundary and redid row 3 cleanly. No `WIP` subject reached main. That is the checkpoint protocol working
as designed: one step lost, nothing repeated.

**Row 1 — PB702 + PB709 (`d578b720`).** PB702 verdicted two claimable rows from evidence rather than from the
note's summary of them. `GR-14.9.35.4-24` — REWRITE's alternate-key repositioning — turned out on a re-read of
the PRINTED rule to carry SIX obligations, not the two the note recorded; the existing `altkey_suppress_when.cob`
reaches only WRITE-time suppression and the READ/START path, so a new golden `2023/l1_rewrite_alt_key_suppress`
(`L1RWSUP`) makes a key ENTER suppression, LEAVE it back into its duplicate set (returning LAST, per GR24 b), and
puts two records at the suppression value of a key declared without DUPLICATES; its twenty expected lines were
computed from the spec before the first run and matched byte for byte. `SR-12.4.5.2-7` had both halves
re-measured live — COBOLNET1810 at 85, COBOLNET1811 at 2023 — plus the two ACCEPTING-arm goldens the note never
named. PB709 was WIDER than filed: §4.2.6 (Annex A.3, processor-dependent) was cited where §4.2.7 (Annex A.4,
optional) governs, and the difference is NORMATIVE — §4.2.6 alone carries the mandatory warning mechanism and the
"not required to diagnose syntax errors within" licence, which three VALIDATE sites were leaning on and which A.3
does not grant. Commit/rollback appears in BOTH annexes, so the annex is a SET, not a scalar. The fix is
structural: `[Flags] DeclinedAnnex` + `DiagnosticDescriptor.Annex` + a derived `PostureClause`, eleven declined
descriptors tagged, and `DeclinedFacilityAnnexDriftTests` (five facts, one of them a cross-check against
`docs/CONFORMANCE.md`'s §2/§5 registers) proved red by flipping COBOLNET1580. Thirteen sites corrected — eight in
code, five in docs — plus a correction banner on the Wave-H scout section, because `docs/CONFORMANCE.md` was the
SOURCE of the error: its §4 bullet, its §1 and its header all filed VALIDATE under §4.2.6.

**Row 2 — PB714 (`e5e25c55`).** `SortEmitter`'s implicit OPENs for SORT/MERGE USING and GIVING files rendered no
SHARING override, so §14.9.40.4 GR12 a) / GR15 a) and MERGE's §14.9.24.4 GR7 a) / GR12 a) never took effect —
`cite.py --find "as if an OPEN statement"` returns exactly those four, and their four CLOSE twins were already
correct. Measured: `IN-ST=00` and three records read where `'61'`, `'42'` and zero records were required. The fix
is ONE renderer, `SortEmitter.EmitImplicitOpen(file, mode, sharing, ruleComment)`; a non-null sharing renders
`CobolFile.OpenShared`, the same entry an explicit `OPEN … SHARING` uses, and the OTHERWISE arm renders the PLAIN
entry because GR12 a) continues *"the absence of the SHARING phrase means that the sharing mode is completely
determined by the SHARING clause"* — PB316's two arms, where a phrase-bearing entry would register a
record-locking participant the standard never made one. `RuntimeApi.FileOpenInput` and `FileOpenOutput` are
DELETED, so the drift is compile-enforced. A sibling fell out of the same read: the implicit OPEN also skipped
`EmitStoreFileStatus`, so the USE procedure read a stale status item (§9.1.13.1 + §12.4.5.8.4 GR1) — that half is
what makes the `'61'` visible at all. Six arms were enumerated and covered. Goldens
`2023/pb714_sort_using_sharing_read_only` (three legs), `pb714_merge_using_sharing_read_only` and
`pb714_sort_giving_open_status`, all measured pre-fix; `NO OTHER`'s exclusive half is pinned at the registry by
two `CobolFileLockTests` with control arms. `COBOLNET_FILES_DESIGN.md` D14 is corrected (its sentence had
recorded the true half as the whole) and PB714's own decision is **D22** — D20 was taken by PB683 in train 15 and
D21 by PB339 in train 16, and the collision was caught before it reached main.

**Row 3 — PB737 (`af58ed80`).** A verdict-only adjudication that gives an owner to the three inventory rows PB339
implemented and pinned but deliberately did not claim (`grep -rln` over `kb/Work/` found no note owning them).
Each row was RE-DERIVED rather than inherited, and each turned out to carry a THIRD obligation no PB339 golden
reached. `GR-13.18.43.4-16`'s closing sentence: a current record of length zero — legal, because §13.18.43.3 SR7
asks only that integer-2 be greater than or equal to zero — sends no character positions and the receiving item
is space-filled (`SequentialFileIoSpecTests.VaryingRecord_ZeroLengthCurrentRecord_…`, a Theory at all four
editions). `GR-13.18.32.4-1` names BOOLEAN positions alongside the alphanumeric ones
(`NationalBooleanDataTests.JustifiedRight_SenderLarger_TruncatesLeftmostAcrossUsages`, with national, bit and
alphabetic JUSTIFIED legs). `GR-13.18.32.4-2`'s fill character is per USAGE, so a USAGE BIT leg was added — and
because §13.18.32.3 SR3 fixes the legal categories at exactly four, that measurement is CLOSED rather than a
sample. `GR-14.9.20.4-3` (PB419's row) rides the same batch notes-only: its recorded code location was a
GENERATED PARSER ACCESSOR, `ini.dataReferenceList()`, which train 16's rename to `initializeOperandList` would
have silently invalidated; it now names the grammar rule and the binder symbol, verdict untouched.

**Row 4 — PB708 (`53570d95`).** `build-local.{ps1,sh}`'s NO-VERDICT-LINE guard looked at the WHOLE filter, so a
dead `FullyQualifiedName~` term OR'd among live ones still produced a verdict line and was never named —
PB691's `~SpecTraceabilityInventory` aimed at Conformance, where that test does not exist, printed a clean
`Passed!` on every run. ONE mechanism now answers for it: `scripts/filter_population.py` (311 lines), called
with identical arguments by both twins right after the build, so neither script carries the logic. Every term is
probed THROUGH VSTEST ITSELF (`dotnet test --list-tests --filter <term>`, ~1.6 s each) because the cheaper
substring model was MEASURED WRONG — `--list-tests` prints DISPLAY names carrying theory arguments, so `~clock`
selects one test while dozens of printed names contain the string; negated terms are probed through their
positive form. Two outcomes, and they differ in what they do to the gate: DEAD — no test of that name in any
assembly the invocation runs — names the term and REFUSES to run (exit 2); INERT — live only in an assembly the
invocation runs UNFILTERED, which is PB691 exactly and every union filter naming a Unit drift test — names the
term, runs the gate, and stamps the verdict line `— WITH INERT FILTER TERM(S), SEE ABOVE`. It fails CLOSED on any
other return code and pins `DOTNET_CLI_UI_LANGUAGE=en`. `--self-test` is ALL GREEN across 8 arms whose names are
derived from discovery, and both twins were driven end to end with a dead term and with PB691's seven-term
filter. Plan §0 Gates and §9 now state the per-term rule where the whole-filter one stood.

**Row 5 — PB739 (`95ce8e8d`).** Two concurrent `OPEN EXTEND` connectors on one shared physical file both reported
`00` on WRITE and one record was LOST: .NET's `FileMode.Append` seeks to the end ONCE, at open, so both buffered
writers anchored on the same offset. Re-measured WIDER than the note: all three record-sequential framings (fixed,
line, varying) lose a record, and RELATIVE loses one AND mints a duplicate key — both connectors returned
`R-K=0002` — while indexed was already correct. The rule is one sentence in three vocabularies (§14.9.51.4 GR19,
GR29 a) both sentences, GR31): the position AND the identity of a released record are read from the SHARED MEDIUM
at the release, never from per-connector state captured at OPEN. `HostFile.OpenAppendStream` is the third
host-path role — a non-participant keeps the plain append handle byte for byte, a §9.1.15 participant gets
`SharedAppendStream`, repositioning at the physical end before every write over an unbuffered handle;
`SequentialConnector.ReleaseRecord` is now the ONE sequential release point, reached by all three write arms
including print control; the mint is `PhysicalFileTable.State.ReleasedOrdinal`, seeded by a shared `OPEN EXTEND`
and reset by a shared `OPEN OUTPUT` per GR17's successor relationship — a self-review caught that reset being
justified with GR29 a), a RELATIVE-FILES rule answering a different question. `RelativeConnector._seqNext` is
gone: `Math.Max(_st.Highest, _lastReleasedSlot) + 1` is all of GR29 a), and the clamp matters because a sibling
I-O connector may DELETE the top record. `RelativeStore`'s map is read-only behind `Put`/`Remove`/`Clear`
maintaining `Highest` in O(1), and `SharedStreams` is DERIVED from the new `SharedPhysical`, so no third writer of
the participation fact can exist. `SharedExtendWriteDriftTests` pins the rule across organization × framing × spelling, with the exclusive
control, the print-control third arm, the `RelativeStore` high-water invariant and a static ban on
`FileMode.Append` outside `HostFile`; the implementer measured 17 of its cases going red with the fix
behaviourally reverted. PB713's one-writer guard is re-pointed and strengthened to assert `SharedStreams`
has no setter. Goldens `2023/pb739_shared_extend_write` and `2002/pb739_shared_extend_write` pin COUNT and
PRESENCE — the ORDER GR19 explicitly leaves undefined is recorded as this compiler's determinate choice.

**Row 6 — PB695 family 3, the LAST family (`9132854f`).** The optional-word audit now reports **0 candidates**
(42 → 12 → 8 → 0), and every step of that descent was a grammar change or a structural fix to the tool — no
exclusions — so `kb/Work/PB695.md` goes LANDED. Ten tokens across seven rules, every underline re-measured off
the raw rectangles by a second reader: `SUPPRESS WHEN? literal` (§12.4.5.6.2), `LOCK MODE? IS?` (§12.4.5.9.2),
DELETE's `NOT ON? EXCEPTION` on the NEGATIVE-FIRST arm (§14.9.10.2 format 2 — PB134 had fixed the positive-first
arm only), `ERASE (END? OF? (LINE|SCREEN) | EOL | EOS)` (§13.18.21.2), `APPLY COMMIT ON?` (§12.4.6.3.2, with a
false grammar comment corrected), `… WHEN? NO? ERROR` (§13.18.62.2), INVOKE's `BY? REFERENCE` and `BY? CONTENT`
(§14.9.23.2 — PB130 had relaxed BY on the VALUE arm only), and the audit-invisible sibling `INHERITS FROM?` at
both `CLASS-ID` and `INTERFACE-ID` (§11.3.2 / §11.6.2), which had made `CLASS-ID. C INHERITS B.` a syntax error.
54 probes through a rebuilt PRE-FIX compiler went from 29 unexpected results to 0, and the three DECLINED
constructs family 3 relaxed now reach COBOLNET1709 / COBOLNET1708 / COBOLNET1560 BY NAME instead of a parse
error. Goldens `2023/pb695_alternate_suppress_no_when`, `2023/pb695_delete_file_not_exception_bare`,
`2002/pb695_lock_mode_optional_words`, `2002/pb695_invoke_by_omitted` and negatives `pb695-apply-commit-no-on`,
`pb695-validate-status-no-when`, `pb695-erase-no-end-of`, plus five `OptionalWordSubsetDriftTests` rows; the
three declined constructs cannot be subset rows — a subset row must COMPILE AND RUN — so the negative corpus
pins them. The row also carries the two PB688 repairs for family 2.

**The train.** Six clusters, six commits, one gate, one push. Rows 1, 2 and 4 came in as plain patches; rows 3, 5
and 6 as three-way squash merges of branches built on other clusters' branches (PB339's, PB713's and family 2's,
all landed by train 16). Thirteen merge conflicts in all, and every one was resolved HUNK-WISE from evidence,
never with `git checkout --ours/--theirs` on a whole file — which would have discarded main's auto-merged PB338
content inside `FileRegistry.cs`. Two resolutions were decisions rather than mechanics. In
`CobolDeclined.g4`'s `validateStatusClause` comment, main's PB693 paragraph was KEPT (it is the current truth:
FORMAT gets its own arm because it is §8.9-reserved from 2002 AND has a lexer token, where the branch's older
text still said it had none) and family 3's two measured WHEN paragraphs were carried on top — with the branch's
§4.2.6 corrected to §4.2.7, because row 1 of this same train had just established that VALIDATE is an Annex A.4
OPTIONAL element. And the corpus manifests were resolved as UNIONS, which exposed a defect the squash had
introduced twice: rows 5 and 6 each re-added `pb713_shared_extend_open` to the 2002 and 2023 enabled lists that
main already carried. Row 5's commit was amended to fold the duplicate out rather than leave a broken
intermediate commit in the range, and every enabled list is now checked for repeats as part of the resolution.

The three verdict batches were re-applied on the post-denominator tree in the closing commit: **GAP 2788 → 2780
of 4,348** (PB702 −2, PB737 −3, PB739 −3), `build_inventory.py --check` clean at both ends. `docs/DIAGNOSTICS.md` was regenerated in row 1's own
commit, which is where the descriptor DATA changed; re-run here on the merged tree after the last write,
`gen-diagnostics-doc.ps1` reproduces it BYTE-IDENTICALLY, and `DiagnosticRegistryDriftTests` is green. `audit_grammar_optional_words.py` prints **0 candidates**
on the merged tree — family 3's central claim, re-verified here rather than inherited. Gate: the union
Conformance filter `Passed! — Failed: 0, Passed: 4558, Total: 4558` (14 m 15 s), the FULL Unit assembly `Failed! — Failed: 2, Passed: 22604, Total: 22606` (2 m 29 s), Characterization `Passed! — 33/33`, the legacy
`CobolSharp.Tests.Integration` assembly `Passed! — Failed: 0, Passed: 503, Skipped: 1, Total: 504` (2 m 50 s) and NIST separately (`~Nist` = 352 cases, all green — **NC215A included**, so train 14's collating regression stays fixed by PB741); semgrep held at its
baseline (bound-node 3, biginteger 46, decimal 2, raw-diagnostic 427); both citation audits and
`work.py check` clean. The only red is the KNOWN pair, `ExternalCorpusPopulationDriftTests.TheCensus_NamesEveryPopulationAndItsProgramCount` and `.SweepExternalPopulation_EqualsTheDifferentialsCommittedCaseCount` — the external corpus is not in the tree and no row here touches it. And the gate demonstrated PB708 on the same run that landed it: **7 of the 21 union terms came back INERT** (`CobolFileLock`, `OpenTable19`, `SharedExtend`, `HostFile`, `Table20`, `SpecTraceabilityInventory`, `DefectiveRowCoverage` — every one of them a Unit-only name aimed at the Conformance assembly), each named with the count it actually matches and where, and the verdict line stamped accordingly. Under the old whole-filter guard those seven would have been invisible, exactly as PB691's was.

