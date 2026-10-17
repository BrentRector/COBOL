## Entry 2135 — 2026-10-16 20:20 PDT — PB3167: an ALIGNED member of a bit-group table is on a byte in every occurrence (whole-byte stride); a bit group begins where its first item does

**Re-probe.** On a fresh build, `05 BA PIC 1(2) USAGE BIT. 05 SUB GROUP-USAGE BIT OCCURS 3. 10 SA PIC 1 USAGE BIT.
10 TA PIC 1(5) USAGE BIT ALIGNED.` laid every occurrence 11 bits apart: FUNCTION LENGTH of the record 35, TA(2) at
bit 19 and TA(3) at bit 30, inside bytes. The same shape with the element's first item ALIGNED misplaced TA(2) at
bit 6 even though the table began on a byte. PB3158 had recorded this as a determination ("the standard is silent").

**Spec reading.** The standard is not silent. TA is subordinate to an OCCURS entry, so it has an occurrence for each
occurrence of SUB (§13.18.63.4 9) speaks of "every occurrence of the associated data item" for such an entry).
§13.18.1.4 GR2, "An ALIGNED clause specified for a multiple-occurrence data item applies to each occurrence of that
item", together with GR1, "aligned on the first bit of the first available byte boundary", puts TA on a byte of the
record in every occurrence. How the occurrences then sit is a DETERMINATION, recorded in docs/CONFORMANCE.md (USAGE
BIT) for the owner to overturn if he wants. A table whose element lays out differently at different bit phases
strides a whole number of bytes (its width rounded up to a byte). Every occurrence then begins at the first one's
phase and is laid out as the first, with implicit filler between occurrences and none after the last (§8.5.1.6.3
makes filler only "for the next item"). This keeps the one length §13.18.38.4 7) gives "the subject of the entry". It
matches the per-occurrence FILLER that §13.18.55.4 8) b) states for SYNCHRONIZED tables, and IBM's slack bytes between
occurrences. GnuCOBOL 3.2 rejects GROUP-USAGE BIT ("reserved word, but isn't supported"). The rejected reading puts
each occurrence at the next bit position after the previous one: the first occurrence's members would then sit at
other offsets than the rest, and its length would differ from theirs.

**Fix shape (BitLayout.cs).** `StrideBitsAt` asks `HoldsByteStart(item)`, true for an ALIGNED or group-typed TYPE
subject or for a member at any depth that the walk rounds up to a byte, where it used to ask `IsAligned` alone. The
walk at the first occurrence's phase is therefore exact for every occurrence. Every subscript (`ReferenceResolver`,
`PlaceCursor`, `ParameterConformance`), image composer (`GroupImageCodec`), VALUE seed and ODO extent reads the one
stride, so none of them changed. Siblings found and fixed in the same files (I9):
- `KeepsPosition` asked only the subject's own clauses. §8.5.1.6.3 says "the alignment of the start of a group item
  and the alignment of the first item within that group, when the first item is a bit data item, are at the same bit
  position", and the code applied it in one direction only. A bit group whose first item is ALIGNED began six filler
  bits before that item (FUNCTION LENGTH(SUB) was 11, now 5). It now recurses into the group's first item. Because it
  is the "requires a byte boundary" that §13.18.44.3 SR15 asks of a REDEFINES subject, `DataBinder.RedefinesEntry`
  now asks it too: such a subject over a target that begins mid-byte draws COBOLNET2739. The subject's own clauses
  used to pass it.
- The placement dispatch `prev is null ? KeepsPosition : SharesByteWith` had three spellings (the walk, `PhaseOf`, the
  new stride test). It is now one method, `Continues`.
- The character channel of an OCCURS DEPENDING extent (`CobolTable.OdoExtentChars`) rounded up BEFORE giving the last
  occurrence's trailing filler back. That was harmless while only an ALIGNED table, which begins on a byte, had any.
  A whole-byte-stride table that begins mid-byte made FUNCTION BYTE-LENGTH a staged COBOLNET1756 ("fixed extent is
  negative") and would have sent one character too many. The function now takes `trailingUnits` and rounds
  `OdoExtentTrimmed`. The renderers and `IntrinsicBinder` pass it in both units. That changes the shipped runtime
  surface, so the runtime major goes from 6 to 7: Directory.Build.props, `RuntimeAbiPinDriftTests` re-pinned, and the
  version string in the 14 characterization snapshots.

**Goldens.** `tests/conformance/2002/pb3167_aligned_member_in_bit_group_table` (2002 is the edition that introduced
USAGE BIT; the layout does not change with the edition). It covers the mid-byte table (DISPLAY, LENGTH 45,
BYTE-LENGTH 6, LENGTH of SUB(1) and SUB(2) both 11), the phase-0 table whose first item is ALIGNED (22), the group
whose first item is ALIGNED (5), an alphanumeric group image with VALUE in every occurrence, an OCCURS DEPENDING bit
group and alphanumeric group (29 / 4 / 2), and BY REFERENCE of TA(2), which §14.9.4.3 6) now accepts. Every value was
derived by hand. The negative `negative/pb3167-redefines-group-first-item-aligned` (COBOLNET2739 once) is the SR15
sibling. `BitRunImageDriftTests` BRD13 and BRD14 check that both image lanes agree, and
`BitGroupPhaseDriftTests.TheStride_AsksWhetherTheLayoutDependsOnThePhase_AndAGroupBeginsWithItsFirstItem` pins the
shape. No inventory row is re-verdicted (`closes_rows: []` with its reason).

**Review round 1 (R1-1).** The branch review found a third extent channel the change had missed. `OdoTail`, the
geometry the variable-length group codec (`GroupImageCodec`: the member's current image, the receiving splice, the
record layout) and a cell window (`Place.OdoOf`) read, measured the table as `lead + count x stride` with no trailing
term. For a whole-byte-stride table that begins mid-byte that is one character past the real end. The re-probe
reproduced both symptoms in a group with a dynamic-length item and the table `05 ABA PIC 1(2). 05 ASUB OCCURS 0 TO 3
DEPENDING ON N. 10 ASA PIC 1. 10 ATA PIC 1(5) ALIGNED.` (all USAGE BIT). At N = 0 the sent image lost AG's one
character, so a MOVE gave the receiver ABA = 00. With the receiver's count at 2, the splice that keeps occurrence 3
started at bit 26 instead of 34, so ATA(2) came back 01111, not 01010. The fix: the rule
`fixed + c x stride - (c > 0 ? trailing : 0)` is now one internal method, `CobolTable.TrimmedExtent`.
`OdoExtentTrimmed` and `OdoTail.CharactersAt` both read it. `OdoTail` gains `Trailing`, which
`OdoModel.FixedImageTail` fills from `OdoGeometry.TrailingUnits`, and `RuntimeApi.OdoTailOf` renders it when it is
not zero. The `OdoTail` constructor and `Deconstruct` change in the shipped surface; the branch already takes the
runtime to major 7, so this needs no second bump, only a re-pin (4D14503EE215A383, which also absorbs main's PB3293
re-pin from the merge). Golden: `tests/conformance/2023/pb3167_bit_odo_tail_in_variable_length_group` (2023, the
edition that introduced DYNAMIC LENGTH). Every value is hand-derived, and a mutant without the trailing term fails
both of its arms. `AlignedBitTableExtentDriftTests.OdoTail_AgreesWithTheCharacterExtent_AtEveryCount_WhenTheTableBeginsInsideACharacter`
pins `OdoTail` to `OdoExtentChars` at every count and checks the splice. Sibling sweep: `OdoModel.GeometryOf` has two
readers, `OdoGroupPlace` (it already carried `TrailingUnits`) and `FixedImageTail` (fixed here). Every runtime reader
of `OdoTail` (`StorageCell`, `CobolContiguousLayout`, the codec) goes through `Width`, `CutAt`, `KeepUnused` or
`RecordMaxUnits`, so all of them now see the trailing term. The other two `OdoTail` producers count characters, so
their tables carry no trailing filler.

**Self-review.** One mechanism, at its one place: the stride question lives beside the walk and uses its predicate.
Performance: `HoldsByteStart` returns at the first byte-starting member, and it is reached only by bit tables.
Duplication: one fewer copy of the placement dispatch.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261017T014548Z-ba6d09` at b2ac0ff92; Conformance 5,950 · Unit 33,015 · Characterization 5 cases ran). After the review fix and the merge of main: GREEN again (run `20261017T022004Z-882502` at fea15f9f9; Conformance 6,035 · Unit 33,154 · Characterization 5 cases ran; the new golden, the new `OdoTail` fact and the re-pinned `RuntimeAbiPinDriftTests` Passed; the 14 `EmittedCSharp_MatchesSnapshot` cases run directly, 14/14 Passed). The 14 characterization snapshots that carry the runtime version are not in leg 1 (the kb/Work PB3144 trap), so they were run directly: 14/14 Passed. LINUX GATE: the landing's (batched gating).

