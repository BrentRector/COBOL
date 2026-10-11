## Entry 1843 — 2026-10-02 21:19 PDT — Train 1003: wave 1003 groups B, D, C, A (PB1140, PB749, PB1705, PB1660, PB1040, PB1094)

**Group B — PB1140 + PB749 (SORT/MERGE transfers).** PB1140: the short-record fill of a SORT/MERGE transfer took the
wrong space. A SORT USING one `N(2)` record into a 6-byte SD gave U+2020 characters, a GIVING file whose one record is
`PIC 9(3) USAGE NATIONAL` got alphanumeric spaces, and a two-record FD got national bytes. §14.9.40.4 GR7 / GR16 and
§14.9.24.4 GR2 / GR13 (all cite-checked) choose the national space only when the file has ONE record description that is
a national item or an elementary usage-national numeric, numeric-edited or boolean item. That test now lives once, in
`FileModel.ShortRecordFillNational`. `RecordFill.Fit` is the one right-fill: `FileConnector.FitRecord` calls it and the
connector's static `Fit(string,int)` is deleted. `CobolSort.FillTo` is emitted on the fixed-SD USING arm and on a
fixed-length, non-line-sequential GIVING file. SORT GR16 a) / b) print "file-name-2" where the lead-in names the GIVING
file; that is read as a slip (CONFORMANCE.md D-SMF). Three inherited "14.9.40 GR7c" cites on RELEASE were re-derived
and corrected. PB749: the note's table had been overtaken by PB837 / PB993; the defect that remained was that a
SUCCESSFUL as-if READ with status '06' stored no status and ran no EC-I-O-WARNING hook, although §9.1.13.1 makes a
nonzero successful status an exception condition. `SortEmitter.EmitSuccessfulTransfer` and `EmitSuccessfulTransferHook`
now store the status per record (FILE STATUS files only) and run the hook behind `IoStatusClass.Warning` under an EC-I-O
mask; the GIVING write's store moved out of its unsuccessful branch. Goldens `2002/pb1140_sort_merge_short_record_fill`
and `2023/pb749_sort_implicit_statement_status`. Rows GR-14.9.24.4-2, GR-14.9.24.4-13, GR-14.9.40.4-7 and GR-14.9.40.4-16
went DIVERGES → CONFORMS, and GR-12.4.5.8.4-1 went PARTIAL → CONFORMS. Self-review gaps: the GIVING success hook compiles
but no test executes it (the keyed '02' case is behind PB994's refusal; recorded in PB994), and no MERGE leg runs the
USING national fill into a fixed SD (PB1917).

**Group D — PB1705 + PB1660 (reference format, lexer).** PB1705: a `D` line was always a comment, because the
reference-format pass decided before any SOURCE-COMPUTER clause was bound, so `WITH DEBUGGING MODE` plus
`D    DISPLAY "X".` printed nothing at `--std 85`. Following owner decision R56 (a token rewriter), the lexer reads the
carrier as a hidden `DEBUG_LINE` marker followed by ordinary tokens. `Parsing/DebuggingLineRewriter.cs` runs first in
`Frontend.LexAndParse` and, per source unit (§12.3.5.4 GR1: contained units inherit, siblings do not), either keeps the
tokens or moves them to the new `ABSENT_DEBUG_LINE` channel. Following R61, the clause row became
`debugging-mode-removed-2014` (obsolete 2002 with COBOLNET0903), and `negative/debugging-mode` rejects at 2014 and 2023.
The NIST `S` / `Y` letters became CCVS-excluded comments. `CobolParserCoreBase.IsAtLineStart` skips the marker so that
DB101A's paragraph on a debugging line still begins its paragraph. PB1660: the text stages split and trimmed on .NET's
Unicode White_Space, so `>>DEFINE FLAG AS<NBSP>1` was accepted as `AS 1` and a line holding only an NBSP was dropped as
blank. `Cobol.Net.Editions/CobolSpace` is now the one separator set (space and line end; §8.3.5 1), §6.3.6). The two
copies left by PB1543 were deleted, every caller was changed, and `CobolSpaceDriftTests` pins it. Goldens:
`85/pb1705_debugging_lines_{compiled,per_unit,copy_replacing}`, `85/pb1705_debugging_line_paragraph`,
`2002/pb1705_debugging_mode_obsolete_2002` and `2023/pb1660_directive_nbsp_in_literal`, plus two negatives. No
inventory row was claimed (`closes_rows: []` with reasons); PB1660, PB1705 and PB1802 are landed. Three leads were
filed: a debugging line inside an open PICTURE or subscript stays a comment (PB1913); a `D` line between the parts of a
continued literal cuts the literal (PB1914, pre-existing); a header word retyped by >>COBOL-WORDS is invisible to the
rewriter (PB1915).

**Group C — PB1040 (dynamic CALL, RETURNING); SPLIT.** `CALL "C41IN" RETURNING R`, with R `PIC X(5)` and the callee
returning `PIC X(3)`, stored the three-character image raw (`[ABC]`, FUNCTION LENGTH 5) and raised no
EC-PROGRAM-ARG-MISMATCH under `>>TURN EC-ALL CHECKING ON`. §14.8.3.3 and §14.9.4.4 GR3 d) (cite-checked) require the
check. The two sides of the CALL are compiled apart, so both now state a description: `CobolArg.Length` sits beside
`Num` / `Layout`, `BoundaryItem(Num, Length)` has ONE `Conforms` rule, the callee registers its RETURNING item, and
`ProgramTable.CallProgram` compares the two before activation under the enabled-in-both gate. When checking is off,
every `StoreReturn` text leg stores into the receiver's own width (§14.6.8.5; CONFORMANCE.md §3). The sweep fixed two
sibling arms. A formal-less unit used to register no count facts. The program-pointer and function-pointer arms of
`CallEmitter.InvocationText` used to drop the activating half of the gate. Fourteen emitted-C# snapshots changed one
Register line each. Goldens `2002/pb1040_returning_length_conformance` and `2002/pb1040_activation_mismatch_every_lane`;
`BoundaryItemConformanceTests` (12). No row was closed (`closes_rows: []` with a reason). PB165 now carries the shape
for the argument side. PB1144 leg 2 (`MOVE GS TO GR` prints `1234 5678` where §14.6.9.2 gives `3400 7800`) was
re-probed and left OPEN: the split landed only PB1040. Lead: the function-pointer arm has no golden of its own
(PB1916).

**Group A — PB1094 (DYNAMIC LENGTH STRUCTURE).** A dynamic-length item naming a DYNAMIC LENGTH STRUCTURE now sits in the
record a file holds as `[length field][data][delimiter]` (§12.3.7.4 GR18 / GR19, cite-checked), while the procedural
view remains the data alone (§8.5.1.11.2). Re-probe: the round trip already worked (PB1053's extent table postdates the
note) but the file held `41 42 43 44 45`, with no length field and no delimiter. Now DELIMITED writes `AB 00 CDE 00` and
SHORT PREFIXED writes `00 02 AB 00 03 CDE`. `CobolContiguousLayout.MediumImage / MediumExtents / Decompose` over the
per-member `CobolDynStructure` is the ONE place both forms are built. Every send arm (struct group, cell-backed group,
ODO wrapper, elementary record, key image, released record, SORT RELEASE / USING / GIVING) goes through
`OperandText.RecordSendImage / RecordSendExtents`, and record sizes count the structure. Determination D-DL3 was
rewritten: the length field is big-endian, 4 characters for PREFIXED and 2 for SHORT, and the delimiter is one position
of zeros. Four 2014 goldens (`pb1094_dynamic_length_structure_{record_image,elementary_record,organizations,record_size}`)
and `CobolDynStructureTests` (31). GR-12.3.7.4-19 went DIVERGES → CONFORMS.

**The train.** The four clusters were applied in order B, D, C, A onto `9b764bdc1` (unchanged origin/main) from their
branch diffs. DEVLOG and the inventory were excluded from those diffs; the inventory was instead re-applied from
`batch-w1003b.json` (GAP 675 → 670) and `batch-pb1094.json` (670 → 669). The only conflicts were two whole-element
manifest entries (`2002`, `2023`), resolved keep-both, with the entry counts checked (597 and 774, all distinct). The
Windows lander gate was GREEN: Conformance 9,757/9,757, Unit 30,273/30,273 and Characterization 35/35, one leg (run
`20261003T040911Z-64a980`). The legacy Integration suite passed 503 with 1 skip, the WSL Linux gate was GREEN
(unit / characterization / conformance at `2c4d39004`), semgrep was unchanged (BigInteger 46, decimal 2, raw code 294,
bound text 3) and `drift_rules --check` was current. Review of the train diff: 0 findings, 0 clusters dropped.
Diagnostic codes: none were claimed, and COBOLNET2692-2699 were returned. GAP 675 → 669. New notes PB1913-PB1917 were
filed and PB994 was extended.

