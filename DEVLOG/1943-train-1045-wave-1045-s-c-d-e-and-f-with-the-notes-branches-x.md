## Entry 1943 — 2026-10-08 19:51 PDT — Train 1045: wave 1045's C, D, E and F with the notes branches X, Z, P and L — ODO tables of variable-length elements in records, store format 4, leap-second offsets, USAGE BIT atoms, sum counter cells (PB2497, PB2689, PB2693, PB701, PB2074, PB2629, PB2631, PB2553, PB2691, PB2814 landed; PB2813, PB2815, PB2818, PB2819 filed; PB2816 retired into PB2814)

**C — PB2497.** A file or sort record holding an OCCURS DEPENDING table whose elements are variable-length groups
was refused at run time at the WRITE (w1042c repro). It is now written and read back on every record path: sequential,
line sequential, the fixed form, the sort store, indexed (the key position), EXTERNAL, READ INTO, WRITE FROM and SORT
USING/GIVING. `CellOdoTail` became `OdoTail` (Values/): one geometry for a cell window and a record layout, with
`CountFrom` as the inverse of `CutComponents`. `CobolContiguousLayout` takes the `OdoTail`. `Comps == 0` is the old
trailing-component arm, and `Comps > 0` lists the components at the maximum, with `AtCount(n)` and `Stated` serving
every public method. A READ or RETURN takes the count from the record's extent table, else the maximum (the fixed
form), else from data-name-1 read after a first decomposition at the maximum (§13.18.38.4 GR8 b; SR20 places
data-name-1 before the table) and a second at its clamped value. The determination is D-FRA (viii) in
`docs/CONFORMANCE.md` §3 (the duplicate "(vi)" renumbered, PB1195 updated). Deleted: `DataItem.RecordImageCapable`,
`CanCompose`, `CurrentExtentUse`, `TierCIsland`'s record arm, `VarGroupCurrentImage`'s `transfer:` parameter and
`GroupImageCodec.OdoTailWidth` (replaced by `RecordOdo`). Goldens `2014/pb2497_vlg_record_odo_element_tables` (seven
paths) and `negative/pb2497-record-odo-element-tables-below-2014` (COBOLNET0900), plus nine
`CobolVarGroupContiguousTests` cases; the stale `TierCRejectionTests` case is deleted. The train review found a
MEDIUM: a dynamic-length member before data-name-1 left the first decomposition, made at the maximum, holding too
little of the record, so data-name-1 was misplaced (LINE SEQUENTIAL "7a0" read P=[] K=7). When data-name-1's own value
does not read back the count it was decomposed at, the smallest count that does is taken, else the first count
(`PlaceRenderer.WriteVarGroupContiguous`). D-FRA (viii) states that and cites GR8 a/b and GR9 for the cases they cover;
golden `2023/pb2497_vlg_record_odo_count_after_dynamic`. PB2497 landed with `closes_rows: []` and a reason.

**D — PB2689, PB2693, PB701.** PB2689: the §8.5.1.12 group walk compared `GroupAtom.ElementBytes` for a group element,
so an element holding OCCURS DYNAMIC did not match one holding OCCURS 2 (COBOLNET1931 on legal MOVEs), and a dynamic
table starting inside the shorter group was accepted. `GroupCompatibility.Walk` now returns pair-relative byte lengths;
a table pair recurses (`Elements`) and compares the MEASURED element lengths (§8.5.1.12.3), and `Tail` takes the
shorter group's length, so a component inside it is unpaired and the §8.5.1.12.2 latitude applies only past its end.
`GroupMismatch` carries the lengths into COBOLNET1931's wording. Golden `2014/pb2689_vlg_element_measured_length` and
two negatives. PB2693 and PB701: an indexed store kept per-key duplicate orders only in memory and persisted one
topological order (`PersistOrder`), so a cyclic set of per-key orders did not survive CLOSE (PB701), and a duplicate
walk lost its place across another run unit's DELETE (PB2693). Store format 4 (`RecordFraming`): the header carries
the release mint and each indexed frame carries its key-count × 8 bytes of ordinals; `IndexedConnector.Fill` and
`PersistFrames` keep them, and `PersistOrder` is deleted. A format-3 store opens as Foreign (file status '39'). The
RESERVE areas of a keyed file are now the buffer every store byte passes through (`KeyedConnector._areas`). Golden
`85/pb2693_dup_orders_survive_close` plus unit cases. PB701 had been blocked on an owner question (per-key ordinals on
disk, or an acyclic-only guarantee). The spec settles it: §14.9.30.4 GR26 requires duplicates of the key of reference
to be made available "in the same order … in which they are released by execution of WRITE statements, or by
execution of REWRITE statements that create such duplicate values", with no exception for a CLOSE and OPEN, so the
acyclic-only option would be a wrong answer. CLAUDE.md rule 4 sets no backward-compatibility requirement, so there is
no migration. PB701 states that basis and quotes GR26 exactly (checked again by the resuming lander with
`cite.py --check 14.9.30.4`, OK 26). Train review fix: `TakeFileLock` allocated the RESERVE areas after taking the
store handle, so a failed allocation leaked a handle holding its share mode; the areas now come first, and the
sequential sibling (`SequentialConnector.OpenReader`/`OpenWriter`) disposes its stream when the reader or writer
cannot be built. That fix made gate 2 red: `SharedReadCoherenceDriftTests.TheConnectorKeepsOneBuffer_ItsInputOutputAreas_SizedInOnePlace`
still asserted the old order of the two calls (Expected 1, Actual 0). The test now asserts the new order, in D's
commit. Four verdict batches re-applied on the merged tree, GAP 104 → 104.

**E — PB2074, PB2629, PB2631.** PB2629: under `>>LEAP-SECOND ON`, FORMATTED-TIME and FORMATTED-DATETIME gave an omitted
offset and a written 0 different answers (S = 86400.5: "2359605Z" against "0000005Z"), because a has-offset flag chose
the path. §15.41.3 6) evaluates an omitted argument-3 "as though 0 were specified". The flag is gone end to end
(`IntrinsicRenderer.OffsetMinutesArg` passes 0), and `CobolDate.EmitFormatted` sets the leap second aside as
23:59:59 plus the fraction before the UTC shift, then shows the shifted minute's second as 60 (§7.3.17.4 4),
§15.3.3.3). Golden `2014/pb2629_leap_second_utc_offset` and a negative below 2014 (COBOLNET1502). PB2631: the §15.4
returned-value maximum was a bare 8191 in two places. DOC-A.1-93 now states the carrier ceiling, 1,073,741,791 (A.1
item 62), by the rule-1 precedence: ISO is silent, and GnuCOBOL 3.2 returns 10,000-position values whole. One constant
(`CobolIntrinsics.ReturnedValueMaximum`) and one guard (`ReturnedValueOverMaximum`) serve CONCAT, SUBSTITUTE,
CONVERT, BASECONVERT and BOOLEAN-OF-INTEGER; BASECONVERT's quadratic front-insert is replaced. The two `2023/l1_returned_value_*`
goldens are rewritten, and `ReturnedValueMaximumTests` is added. PB2074 did not reproduce (closed by PB2079,
`IntrinsicBinder.ReportClass`); it is discharged and pinned by `negative/pb2074-upper-case-address-of` and a
`--permissive` theory row. Gate 1 was red on `AnnexA1RegisterDriftTests.EveryDocRow_IsFiledUnderTheItemAnnexA1Names`:
DOC-A.1-93's "Pinned by" named the test class where the inventory names its methods, and the row now names the three
methods. Train review fixes: CONVERT checks the maximum on a national source's serialized length before serializing it
(a dynamic national item past half the carrier ceiling threw OutOfMemoryException); `OffsetOutOfRange` tests the range
directly (`Math.Abs(long.MinValue)` threw); two comments that still stated the 8,191 maximum are corrected.

**F — PB2553, PB2691.** PB2691: the §8.5.1.12 atom builder counted each USAGE BIT leaf as its own byte, so bit groups of
different shapes moved and compared ordinally (G2=[x  a] where [xa dd] is due), sharing-byte layouts were refused
(COBOLNET1931/2492), and a Format 2 CALL AS NESTED crashed the compiler (`ProgramEmitter.FormalAtoms`).
`VariableLengthCompatibility.Atoms` asks `BitLayout.RunsOf`: a run that is not byte-granular is ONE fixed atom of
`BitLayout.RunCharacters` bytes (§8.5.1.6.3), byte-granular runs keep per-item atoms, and `GroupAtoms` is non-null for
bit groups. MOVE, relation, the fixed-group operand and the CALL/INVOKE/activation atoms all read `GroupAtoms`. The
one-layout fallback is deleted (`CobolVarGroup.CompareInLayout`/`Positional`, `CobolContiguousLayout.ComponentOffsets`,
`RuntimeApi.VarGroupCompareInLayout`/`VarGroupComponentOffsets`, `PlaceRenderer.VarGroupComponentOffsets`,
`VarGroupWindow.RunFixedAt`). Determination: a dynamic table inside a run that is not byte-granular has no relative
byte position, so the group is compatible with no group. Golden `2014/pb2691_vlg_bit_member_shapes` and two
negatives. PB2553: a report sum counter was a native Int128, so a character stored through reference modification or
a figurative fill was lost (and MOVE SPACE aborted). `StorageFormPass` makes every sum register `CharImage(Numeric)`;
`PlaceRenderer` reads it through `CobolReport.SumImage`/`SetSumImage`; `SumEntry` keeps the image, and GENERATE's
addition reads it through `CobolNum.ParseImageSending`, raising §14.6.13.2 rule 2 on the reference, not on the store.
Goldens `85/pb2553_sum_counter_character_cell` and `2002/pb2553_sum_counter_incompatible`. Gate 1 was red on
`SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree`: GR-13.18.54.4-12 named the deleted
`ReportWriter.SetSumValue`, and a `record_verdicts` batch re-sited it to `SetSumImage`. Train review fix:
`SumEntry.Store` dropped a valid but non-canonical image (a plain digit over the sign position read back as the
overpunch); it keeps the characters unless they are the value's own image (§8.4.3.3.4 GR5), and the 85 golden gains
that leg. The train merge keeps C's `Odo` parameter documentation in `CobolContiguousLayout` without the deleted
`ComponentOffsets` reference. Lead 1, a dynamic-capacity table of USAGE BIT elements that does not compile (CS0029),
reproduced on the train build and is filed as PB2818.

**X — PB2813, PB2814 (tooling).** PB2813 filed: the budget verdict depends on who asks (the supervisor passes the
owner's `--borrow-days 3`, while a unit or the meter template runs `budget.py` bare). PB2814 filed and fixed: a
restarted operator session armed no mailbox watcher, and a Mythos question waited 1 h 40 min. `mailbox.py status`
prints this session's lane, its open messages and whether a watch process descends from THIS session's Claude Code
process, with a stop line naming the arm command when none does. `session-probe.ps1` prints it at every start, resume,
clear and compaction; `list` prints it, `done` prints it when unarmed, and a firing watch prints the re-arm command.
`coord.process_table()` is added, and `account._same` became the public `same_dir` with both callers changed. The
design is `DESIGN-orchestrator-loop.md` §15. The one item of PB2816 this fix lacked landed with it: `send` warns the
sender when the recipient's inbox already holds messages open over 30 minutes (`mailbox.stale`, with self-test
checks). Train review fixes: `coord.process_table` returns [] on a host with no /proc; the watcher pattern sees a
watch armed after global options or with `--inbox=<lane>` (self-test 43/43); `stale()` ignores a non-string
`created_at`. Two quieter arms of `status` are filed as PB2819.

**Z — PB2815, PB2816 (notes).** PB2815 (MAJOR, open): the fix-lane plan ignores in-flight dispatch-ledger groups.
PB2816 (the mailbox inbox watcher is not re-armed or reported after a CLI restart) states the mechanism PB2814 files
and fixes in this same train, so it is RETIRED as a duplicate folded into PB2814, cross-linked both ways.

**P — PB2614 (notes).** Two more recurrences of the dispatch-ledger over-hold. Wave 1046's campaign plan came out
empty: 26 of 152 deferred notes were held only by hand ledger entries (the landed train 1043b lander's 363 files, a
note-filing clerk's 182, and wave 1045's Workflow re-recorded at 511 files). The same 1043b lander entry bound wave
1045's in-flight A, B and H worktrees as R3 work and stopped this train's C and F at `landing_check.py`, until the land
unit released it by hand. The fix shape is extended: record the notes a dispatch is ASSIGNED, never every id it
mentions, and never re-record a wave already in the ledger.

**L — PB2426 (one ledger-trend point).** Train 1043b's ledger render appended one program-series point to
`docs/rearchitecture/evidence/ledger-trend.json` (208 unfinished, 42 landed of 250 at 29362158) in main's checkout,
the dirtying PB2426 records; this carries the point off the checkout on a branch, as w1043y and w1042z did.

**The train.** Eight clusters, one commit each, plus a lead-notes commit (PB2818 and PB2819 filed; PB2590 gains the
w1045e and w1045f load-sensitive self-test arms, PB2690 the w1045f by-reference capacity case) and this entry. No
diagnostic code was claimed (the manifest's six ranges COBOLNET3248–3259 went unused). The first lander (dispatched
18:09 PDT) brought all eight in without a conflict-marker finding, ran the review (five agents: one MEDIUM and seven
LOWs fixed in the train, two LOWs filed as PB2819) and took two whole-population gates. Gate 1 (run
20261009T012529Z-14ffa9) was RED on two Unit cases, attributed per cluster in about a minute: E and F each owned one
and each was fixed in its own commit. Gate 2 (run 20261009T014545Z-9652d8) was RED on one Unit case, owned by D's own
review fix and fixed in D's commit. That lander then stopped at its 220-turn cap, and a fresh lander resumed from its
checkpoint and took the lease again. Gate 3, on the resumed train (run 20261009T021249Z-fceee5), was GREEN: Conformance 11,284/11,284 · Unit 32,952/32,952 · Characterization 36/36 cases ran (skipped 0) in one leg, every population EXACT. So the train took three whole-population runs, and three reds were fixed in it. The Linux gate was GREEN on every leg (selftests, unit 32,985, characterization 36, conformance 11,284, guard with NIST 362 MATCH and 0 regressions, selftests-built), and `self_tests.py` on Windows was GREEN (67 ran, coord, mailbox and account among them). The oracle was DIFFERENT, 169 of 7,657 cases against c6339810570b, and every case is accounted for by class: 15 ADDED are the train's new goldens and negatives; 94 are C's (every variable-length group's emitted `FromContiguousImage` now returns bool and takes `__odo`, and `CobolContiguousLayout` is built from an `OdoTail`); 34 are F's PB2553 (a SUM reference reads `CobolNum.ParseImageSending(__RPT.SumImage(n, _P))` where it read `(long)SumValue(n)`); 24 are E's PB2629 (`CobolDate.FormattedTime`/`FormattedDatetime` lose the has-offset bool argument); 2 are E's PB2631 (the two rewritten `l1_returned_value_*` goldens). No existing negative's diagnostic changed, and D, X, Z, P and L change no emitted C#. The baseline is re-recorded as 974322c12634 in the last commit. semgrep verify
PASS before and after (no count moved). The inventory GAP stays 104 → 104. The landing check PASSED with no stop and no warning, after the land unit released the landed train 1043b lander's stale dispatch-ledger entry (PB2614).

