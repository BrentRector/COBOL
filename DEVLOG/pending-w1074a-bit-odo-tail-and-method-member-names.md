## PB3030's bit arm: one OCCURS DEPENDING tail counted in bits; PB2921 review round 2 (data named like a method)

Wave 1074 group A, finishing the w1071 B branch (merged at 1a473341f) and taking PB3030's second half.

**PB2921, review round 2.** R2-1: the per-method §8.3.2.2 census (round 2's fix) let an object data-name spelled
like a METHOD-ID through to the emitter, which gave the private data field and the method one C# member name
(CS0102 on legal source; probe `E:\COBOL-coord\scratch\w1074a\r2\p1.cob`, and `p5.cob` for the factory pair). The
object data and the instance methods share one emitted C# class, so `OoDriver.BindClassData` now reserves the methods'
C# names in the data binder's member scope before the data binds (`DataBinder.ReserveMethodMemberNames`) and the field
takes `PQ_2`. A derived class's data spelled like an inherited method (`p6.cob`) and a method's LOCAL-STORAGE item
spelled like a sibling method (`p8.cob`) were already right. Golden `2002/pb2921_data_named_like_method`. R2-2: the
census paragraph of `docs/COBOLNET_INTRINSICS_DESIGN.md` and two `DataBinder` comments still described one census per
binder and a method declared by the OO driver; both now name the per-method census and `OoBindMethodData`.

**PB3030.** Re-probed: `FUNCTION LENGTH` of a group holding a dynamic-capacity table of dynamic-length elements and an
OCCURS DEPENDING table of USAGE BIT items still warned COBOLNET1756 and aborted the run unit. `DataItem.
CurrentExtentComposes` refused the bit-bearing member; admitting it needed the variable-length group codec to count the
table in bits (§8.5.1.6.3 places each bit occurrence "at the next bit position in storage", so occurrences share
bytes). The fix is one tail: `OdoModel.FixedImageTail` states `OdoModel.GeometryOf` as the runtime `OdoTail`, which
gained a lead and a unit (`PositionsPerCharacter`, 8 for bits). `CutAt` takes the ceiling of the bits (§15.50.4 rule
9), `KeepUnused` is §13.18.38.4 GR8 a)'s receiving splice (a `CobolBits` window when the first unused occurrence starts
inside a byte), and `RecordUnit` / `RecordMaxUnits` state the table as the record's last component, one CHARACTER per
unit (new determination D-FRA (ix): a record of characters cannot say a bit count). The declared group's codec and a
CELL-BACKED group's window both read it. The cell arm was the sibling: it measured a bit table one character per
occurrence, and `DISPLAY` of an ADDRESS-OF-taken group of this shape threw ArgumentOutOfRange (`q8.cob`).

Golden `2014/pb3030_bit_odo_variable_group` covers LENGTH / BYTE-LENGTH at three counts, DISPLAY, a GR8 a) MOVE into a
declared and a cell-backed group, a nested CALL BY REFERENCE and a WRITE / READ round trip, each value derived by hand.
Unit tests `CobolVarGroupContiguousTests.BitOdoTail_*` cover the tail arithmetic. No edition-gated construct was added.
No inventory verdict changed (`closes_rows: []` with its reason). Found on the way, not fixed: a MOVE into a
CELL-BACKED group whose dynamic-capacity table has variable-length elements loses the table (capacity 0; `q9.cob`).
That is a lead in the report. PB3101 was not started.

**The branch review, answered (wave 1074 A2 finisher, review R1-1).** The bit tail above measured a table from the member
that holds it, and a member that CONTINUES a same-level §8.5.1.6.3 run has no characters of its own: `05 B PIC 1 USAGE
BIT. 05 TB PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON N.` directly in a variable-length group packed B and TB into the
leader's field at the maximum, while the codec cut TB's EMPTY field to the count. Re-probed: `DISPLAY G` threw
ArgumentOutOfRange, at OCCURS 1 TO 8 it showed a NUL character LENGTH did not count, a cell-backed group of the shape
showed the same extra character, and a FILE record of it threw at WRITE. Fix: `GroupImageCodec.VarParts` classifies a
run's leader field by the member the run holds, and `OdoModel.FixedImageTail` counts the run's earlier bits as lead
(`BitLayout.BitsBeforeInRun`, read off the one run law `RunsOf`), so the declared codec, the record layout and the cell
window measure the table from the run's first bit. Golden `2014/pb3030_bit_odo_run_continuation` covers the three arms,
a nine-bit leader, a GROUP-USAGE BIT holder continuing a run, a GR8 a) MOVE and a CALL at the maximum, each value derived
by hand; it fails on the reviewed head. The PB3030 golden's comment cited GR8 b) for a sending group whose data-name-1
lies outside it, which GR8 a) governs; corrected. Gate run `20261014T064005Z-0faf96`: `=== BUILD-LOCAL GATE: LEG 1 ONLY
(batched gating, PB2515): GREEN`, both PB3030 goldens Passed in leg 1; semgrep verify PASS.

Finisher (landing attempt 20261014T065404523254Z, `ENQUEUE: GATE RED`): the landing's gate ran
`SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree` in leg 1 and it was red. Review round 2's
per-method census deleted `OoDriver.DeclareMethodWords`, but inventory row SR-12.3.8.3-12 still named it as a
code-location; the branch's own gate had left that case in leg 2 (its `not-run-Unit.txt` lists it), so nothing local saw
it (kb/Work PB3131). The row is re-sited by a `record_verdicts.py` batch to `DataBinder.Oo.cs#OoBindMethodData`, which
now declares the METHOD-ID (or the property-name it names) through `DeclareUserWord` inside the method's own census,
and retires the deleted member with its reason; a sweep of every inventory code-location against the tree finds no
other dangling member, and the branch deleted no other method declaration.
Finisher gate run `20261014T072258Z-315405`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`,
`EveryCodeLocation_ResolvesInTheTree` Passed in leg 1 (the previous red, tier 0a).
