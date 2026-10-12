## Entry 1869 — 2026-10-04 16:45 PDT — Group Y landed alone: PB1674, PB1263, PB1261, PB1265 (GAP 289 → 281)

Train 1015's lander dropped group Y at its review (entry 1866) over one confirmed defect, and the group was landed afterwards
as a single cluster with the fix. The four notes: **PB1674** a contained program now sees a GLOBAL record's CAPACITY register,
and `GlobalNameClassDriftTests` checks by reflection that every name class is inherited; **PB1263** OCCURS KEY §13.18.38.3
SR4, SR6, SR8 and SR9 (COBOLNET2787); **PB1261** the DEPENDING ON object is checked for GLOBAL (SR18) and EXTERNAL (SR21),
report Format 3 included (COBOLNET2788), and SR20 spans one storage area; **PB1265** OCCURS is narrowed to the data-division
Formats 1, 2 and 4 (COBOLNET2789; three test sources with out-of-order Format 4 phrases were rewritten). A locale temp-dir
test race that turned a gate red was fixed by joining three locale-reading test classes. PB1042 stays open as its own
redesign (a dynamic-capacity table in an EXTERNAL record: a second managed-slot family in `StorageCell` plus emitter and
composer work).

**The review finding, fixed in place.** `DataBinder.Odo.cs#OccursKeyItemFault`'s SR4 walk started at `key.Parent`, so a key that
is the table itself (admitted by SR6) was charged with any OCCURS group that merely enclosed the table: a legal table inside
`OUTER OCCURS 2` was refused with COBOLNET2787 and then COBOLNET1965. The walk is skipped when the key is the table.
Golden `85/pb1263_occurs_key_is_table_in_outer_table` (FOUND 5) reproduces it. The fix took about ten tool calls because the
finding, the code site and the repro were written down; the owner's direction (2026-10-04) is to fix such findings while the
context is in hand, now recorded as L9 (entry 1868). Gates on the branch before the rebase onto train 1015: Windows
implementer gate GREEN (Conformance 10,402, Unit 30,561, Characterization 35) and the Linux gate GREEN; the rebased tree was
re-gated whole before the push (see the landing line below).

**Files:** `src/Cobol.Net.Compiler/Binding/DataBinder.Odo.cs` and the group's other sources, `tests/conformance/85/`,
`docs/DIAGNOSTICS.md`, `kb/Work/PB1263.md`. Codes used: COBOLNET2787-2789.

