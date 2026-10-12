## Entry 1514 — 2026-09-05 09:58 PDT — Landing train 10: PB323 + PB324 + PB325 + PB326 — the OPEN presence question, dynamic file assignment, Table 20's blank cells, and four report-writer exception conditions

**PB323 — the OPEN presence question gets three answers, not two.** Every `OpenCore` decided whether the physical
file was present with `File.Exists`, which swallows every access error and answers false, so a file the running
process was not permitted to reach classified as ABSENT: a non-OPTIONAL `OPEN INPUT` answered `'35'`, and a
`SELECT OPTIONAL` over the same path answered `'05'` — a SUCCESSFUL open — with the first READ reporting AT END
`'10'`, inventing an empty file where §14.9.27.4 GR3 requires `'37'`. The re-probe was wider than the note in
three directions: the note had measured sequential only and the defect was on ALL THREE organizations; it named
three `OpenCore` sites and there were TEN, including `FileConnector.Open`'s own second `File.Exists` — the same
question asked twice inside one OPEN statement; and it said nothing about OUTPUT, which is where a literal
reading of GR3 would have caused a rejects-legal-source regression. The fix is one probe with three answers at
one site: `HostFile.Probe` (`IO/FileSupport.cs`) returns `FilePresence { Absent, Present, Unauthorized }` from
`File.GetAttributes`, whose FAILURES carry the distinction `File.Exists` collapses (`FileNotFoundException` /
`DirectoryNotFoundException` → §9.1.13.6 5)'s `'35'`, `UnauthorizedAccessException` → GR3's `'37'`, every other
`IOException` propagating so `'30'` stays mapped in one place), and `FileConnector.Open` takes it ONCE per OPEN,
answers GR3 there, and hands `OpenCore(mode, presence)` down — the note's shape would have kept GR3 written
three times and two probes per statement, so rule 5 applied and `DESIGN-runtime-library.md` §2.2 records the
corrected shape as a named design point. OUTPUT is deliberately outside the short-circuit and that is a spec
consequence rather than a convenience: GR18 makes OUTPUT a creation, §9.1.13.6 5) is defined only "for an OPEN
statement with the INPUT, I-O, or EXTEND phrase", and a directory the process may write but not list
legitimately accepts a NEW file (measured). GR13 and GR17 both open "If the file is not present", so a
present-and-refused OPTIONAL file takes NEITHER optional arm — the silent half of the defect. Ten `.Exists`
sites were swept, including the `FixedFileAttributes` sidecar pre-check (deleted) and `FileRegistry.DeleteFile`,
which had held the only correct copy of the rule since PB140 — this row was the OPEN arm of that two-arm
dispatch, unfixed for five months. `unit:FileAuthorityPresenceTests` establishes the GR3 precondition for real
(a DACL deny ACE on Windows, mode 000 on Unix) and verifies the denial with a raw `File.OpenRead` rather than
with the subject under test; its eight GR3 assertions were proven RED with the short-circuit disabled.
`conformance:2023/pb323_open_presence_arms` pins the probe's other two answers across all three organizations
and both OPTIONAL states, because a refactor that got `Absent` wrong would be louder than the defect it fixes;
`unit:FileExistenceProbeDriftTests` bans the bare `.Exists` member everywhere under `Runtime/IO` but
`FileSupport.cs` and flags all ten pre-fix sites — its own first regex missed `var info = new FileInfo(p); if
(info.Exists)` and was fixed. `GR-14.9.27.4-3` DIVERGES → CONFORMS, with its `editions` field corrected from
`85,2002,2014` to `85,2002,2014,2023` to match its own adjudication sentence.

**PB324 — `ASSIGN … USING` is bound, and the association is made at every OPEN, SORT and MERGE.** `SELECT F
ASSIGN USING WS-NAME` compiled clean, reported `'00'` and wrote to `f.txt` — the FILE-NAME. Two independent
holes: `DataBinder.BindFileControl` read only `assignClause().assignTarget()`, so the `USING dataReference`
child was read NOWHERE, and `ResolveHostPath` ran once at activation into a get-only `HostPath`, so §12.4.5.3
GR3's timing sentence — "The association occurs at the time of execution of an OPEN, SORT, or MERGE statement" —
had no implementation at all. ONE entry point, not a second mechanism (design decision **D15** in
`docs/COBOLNET_FILES_DESIGN.md`; the implementer wrote it as D13 and the lander renumbered it past train 9's
D14, fixing the cross-reference in `kb/Work/PB324.md` with it): `FileConnector.Associate()` is the only writer
of `HostPath`, a deliberate no-op for the GR3 a) TO form — that association is static — and for an already-open
connector, since GR2's `'41'` is unsuccessful and GR25 leaves the file unaffected, so re-pointing would strand
the streams and the physical-file-table registration on the old path. ⛔ On the merged tree the registry-level
call is a SINGLE `Associate(name)` inside PB321's unified `FileRegistry.OpenCore` — covering both the plain
`Open` and `OpenShared`, where the implementer's own tree had needed two calls — placed BEFORE the Table-19
arbitration, because §9.1.13.9 defines a conflict over the physical file THIS statement names, and OUTSIDE the
RETRY loop, because GR3 b reads the content once per statement and not once per attempt. The emitter installs a
SOURCE — `CobolFile.SetAssignUsing(key, () => data-name-1)` — unguarded on every activation and on BOTH
registration arms (program `__Activate` and the object-class constructor) for PB168's reason: the closure must
read THIS activation's instance. §12.4.5.2 SR7 is enforced at bind as `COBOLNET1810` (the category half) and
`COBOLNET1811` (the subordination half — the dangerous one, since an operand inside the file's own record area
is overwritten by every READ of that file), and GR3's unmakeable association reports §9.1.13.6 item 2's `'31'`.
The note's estimate was right about the shape and short by one structure: making the path mutable invalidates
`ConnectorShare`'s cached host path, which six physical-file-table lookups depended on — they now read the
connector's live `HostPath`, and the record no longer carries a `Host` field at all. `DELETE FILE` on a
still-unassociated connector takes §14.9.10.4 GR14's `'05'` rather than deleting the registration-default
`<file-name>.txt` the program never named; re-resolving data-name-1 there would have been an implementor
extension to GR3's closed list of associating statements, on the one verb where guessing wrong destroys data.
Goldens `2023/pb324_assign_using_dynamic` and `85/pb324_assign_using_85`, negatives
`pb324-assign-using-not-alphanumeric` and `pb324-assign-using-in-own-record`; `docs/CONFORMANCE.md` §7 gains
`DOC-A.1-73` and `DOC-A.1-10`. `GR-14.9.27.4-26` DIVERGES → CONFORMS and both DOC rows → CONFORMS.

**PB325 — the OPEN MODE never selects a keyed verb's branch, it is only screened.** `RelativeConnector.Write`
and `IndexedConnector.Write` each opened with `_access == Sequential || Mode == Extend`, so a RANDOM- or
DYNAMIC-access connector opened EXTEND took the sequential-release branch: the WRITE appended and answered
`'00'`. §14.9.27.4 GR8's Table 20 leaves those cells BLANK — "'X' at an intersection indicates that the
specified statement, used in the access mode given for that row, may be used with the open mode given at the top
of the column" — and §9.1.13.7 item 8 b) is the answer: `'48'`. The re-probe again beat the note, and for a
structural reason: the predicate is written against the negation of `Sequential`, so the DYNAMIC arm of both
connectors is equally wrong — FOUR (organization × access) combinations, not one. Table 20 was read from the
RENDERED PDF page 709 rather than from the Markdown, because the load-bearing signal is a BLANK CELL and an
OCR'd blank is not self-evident. One rule written twice, so the extraction is the fix: a new abstract
`KeyedConnector : FileConnector` (`IO/KeyedConnector.cs`) carries the one fact both keyed connectors branch on —
`Access`, and the `KeyedAccess` enum moved out of `RelativeConnector.cs` — and states the invariant where the
next keyed organization will read it; six sibling `_access ==` sites moved onto `Access` with their own
citations, and one inherited wrong GR was corrected in passing (§14.9.35.4 GR5, not GR19). The row's own claim
that "most of Table 20 CONFORMS" was true with ZERO test evidence, which is why this ships a whole-table walker
rather than a WRITE-row one: `2023/l1_table20_seq_relative` and `2023/l1_table20_indexed` carry 139
observations, every expected value computed from Table 20 plus §9.1.13.7 items 7/8/9 rather than from the
compiler; negative `pb325-start-random-access` pins Random×START, blank in all four columns, at every edition;
`unit:Table20WriteOpenModeTests` holds 12 facts, four proven RED with the disjunct restored.
`GR-14.9.27.4-8` PARTIAL → CONFORMS.

**PB326 — four report-writer exception conditions stop being names.** A `USE BEFORE REPORTING` declarative that
PERFORMs a GENERATE in a SECOND declarative section is legal — §14.9.49.3 SR10 forbids a GENERATE only "in a
paragraph within" the procedure — and produced TWO detail lines where §14.9.49.4 GR10 requires the nested
statement unsuccessful and the report unchanged; an INITIATE on a never-opened report file connector went active
and wrote into it, where §14.9.21.4 GR3 reading §14.9.27.4 GR7 ("The OPEN statement for a report file connector
shall be executed before the execution of an INITIATE statement") requires EC-REPORT-FILE-MODE and no action.
The note framed it as two rules and it is four: EC-REPORT-ACTIVE (§14.9.21.4 GR2) and EC-REPORT-INACTIVE
(§14.9.16.4 GR7 / §14.9.46.4 GR1) are the identical shape in the same three methods, were unadjudicated, and
were owned by no note — so they land here rather than becoming a fifth ticket. Two pieces of missing state and
ONE raise channel: `ReportFlowState`, a depth counter on `RunUnit.ReportFlow` entered and left by
`RunBeforeReporting` — the one funnel — in a `finally`; and `FileConnector.OpenModeIfOpen` → `FileRegistry` →
`CobolFile`, a NEW accessor beside `OpenModeOf` at each level and deliberately not a reuse of it, because
`OpenModeView` also answers with the ATTEMPTED mode of a FAILED open and would have let an INITIATE after a
failed `OPEN OUTPUT` proceed. ⛔ The range of GR10 is the RUN UNIT'S, not the report's — GR10 carries no element
qualifier where §14.9.18.4 GR6 explicitly does — and that was proven rather than assumed: a temporary per-report
flag turned `2002/pb326_flow_report_cross_report` red. The raise rides the existing
`CheckingFlags`/`ExceptionEngine`/`EcBinder`-precise-arms/`EcEmitter.FatalAmbientGates` channel end to end, so
nothing here is a second mechanism; RETURN is unconditional and the RAISE is gated by `>>TURN` (§14.6.13.1.1).
One misfiled citation of the implementer's own was caught before its own gate (§13.18.54.4 GR3, not §14.9.21.4
GR1a). Five goldens — `85/pb326_flow_report_unconditional` plus, at 2002, `pb326_ec_flow_report`,
`pb326_flow_report_cross_report`, `pb326_ec_report_file_mode` (its three arms) and
`pb326_ec_report_active_inactive` (with `S=008`) — all skipping all-space records so PB484's placement question
is not fossilized into them; `COBOLNET_REPORT_WRITER_DESIGN.md` §2/§5/§8 are current. `GR-14.9.27.4-7` and
`GR-14.9.49.4-10` → CONFORMS, and `GR-14.9.21.4-2`/`-3`, `GR-14.9.16.4-7`, `GR-14.9.46.4-1` GAP → CONFORMS.

**The train.** Four clusters, four cluster commits, one landing; **GAP 2821 → 2810** (11 rows, no row claimed by
two clusters, and each cluster's base→branch inventory delta mechanically checked to equal exactly the rule-ids
its own batch names before the four batches were re-applied in manifest order and `build_inventory.py` run
once). ⛔ **No cluster was dropped, but three of the four needed a merge decision rather than a merge.** PB324 ×
train 9's PB321/PB317 was the real one: PB324 had written `if (!Associate(name)) return;` into `Open` AND into
`OpenShared`, and main had since unified both into `FileRegistry.OpenCore` — resolved by the RULE and not by
keeping both spellings, so there is exactly one call site, sited by §9.1.13.9 (before Table-19 arbitration) and
by GR3 b (outside the retry loop); `ConnectorShare` lost PB324's `Host` field while keeping main's nullable
`Sharing` from PB322's undetermined implementor default; `SharedClose`/`DeregisterFromPhysical` and
`NoRewindPhraseEffect` stayed as main has them. PB324 × PB323 in `FileConnector.cs` was the ordering CONTRACT
rather than a conflict — PB324's two new `catch` arms sit beside PB323's `UnauthorizedAccessException` handling,
and PB323's presence probe reads the `HostPath` that `Associate` has already set — and the merged
`FileConnector.Open` carries both. PB325 × PB323 in the two keyed connectors was disjoint members of the same
files: the classes' base became `KeyedConnector` while PB323's `OpenCore(mode, presence)` overrides stayed as
PB323 wrote them. PB326's three `OpenModeIfOpen` additions are pure and stayed paired with `OpenModeOf` at each
level. Four manifest conflicts (the 2023, 85 and negative corpus manifests) were additive on both sides and
resolved keeping both.

The gate ran ONCE for the whole train, over the union filter
`~Corpus|~File|~Io|~Open|~Close|~Delete|~Sharing|~Sort|~Merge|~Assign|~VersionMatrix|~Inventory|~AnnexA1|~Table20|~Table19|~Table14|~ReportWriter|~ExceptionCondition|~UseDeclarative|~ProcessObservation`:
Conformance **4889 passed, 0 failed**; Characterization **33 passed**; the NIST leg run separately asserted its
population at **352 passed, 0 failed** — the same 352 train 9 measured. The solution builds with **0 warnings**.
`semgrep/verify.py` PASSes with every count identical to the registered baseline (`no-biginteger` 46,
`raw-diagnostic-code-literal` 440, `no-decimal` 2, `bound-node-carries-rendered-text` 3). The full Unit leg
first read **4 failed of 22454**, and both non-known reds were the deliberate deferral rather than a cluster
defect: `DefectiveRowCoverageDriftTests` and `DiagnosticRegistryDriftTests` fail by construction while the
notes read `landed` but the inventory and `docs/DIAGNOSTICS.md` have not yet been regenerated — the train
excludes both from every cluster patch precisely so they are produced once. After the four batches,
`build_inventory.py` and `gen-diagnostics-doc.ps1`, the Unit leg reads **2 failed of 22454**, and those two are
the known `ExternalCorpusPopulationDriftTests` pair that cannot pass in a worktree without the GPL corpus (which
is never `git add`ed). `work.py check` passes and both citation audits are clean at zero findings.
`COBOLNET1810`–`COBOLNET1811` are the only codes this train claimed; the lowest unclaimed code is still
`COBOLNET1779`.

