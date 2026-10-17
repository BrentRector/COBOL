## PB3167: an ALIGNED member of a bit-group table is on a byte in every occurrence (whole-byte stride); a bit group begins where its first item does

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

**Self-review.** One mechanism, at its one place: the stride question lives beside the walk and uses its predicate.
Performance: `HoldsByteStart` returns at the first byte-starting member, and it is reached only by bit tables.
Duplication: one fewer copy of the placement dispatch.
