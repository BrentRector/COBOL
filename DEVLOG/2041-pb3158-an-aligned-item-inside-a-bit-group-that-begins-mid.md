## Entry 2041 — 2026-10-14 17:23 PDT — PB3158: an ALIGNED item inside a bit group that begins mid-byte is aligned to a byte of the record, not of the group

**Re-probe.** Both repros of `kb/Work/PB3158` reproduced on a fresh build. `01 BG GROUP-USAGE BIT. 05 BA PIC 1(2). 05 SUB
GROUP-USAGE BIT. 10 SA PIC 1. 10 TA PIC 1(5) ALIGNED OCCURS 1 TO 3 DEPENDING ON N.` answered `FUNCTION LENGTH` 31 (29 is
right: TA(k) sit at record bits 8, 16, 24) and staged `FUNCTION BYTE-LENGTH` ("fixed extent is negative"); the same shape
under an alphanumeric group threw `ArgumentOutOfRangeException` at a `MOVE`.

**Mechanism.** `BitLayout.Walk` kept its cursor relative to the group it walked, so every `RoundUpToByte` was a byte
boundary of the GROUP. §8.5.1.6.3 starts a bit group that follows a same-level bit item at "the next bit position in
storage", so the group begins mid-byte, and §13.18.1.4 GR1 aligns an ALIGNED subject "on the first bit of the first
available byte boundary" - of the record. The walk also rounded a group's FIRST child up, which the old origin (0) hid.

**Fix shape.** `BitLayout.PhaseOf(item)` is where in a byte a group begins (0 for everything but a GROUP-USAGE BIT group that
shares its predecessor's byte; a walk is paid only by a group that really continues one). The walk's cursor starts at that
phase, so every round-up is the record's; the extents and starts it reports stay group-relative, which keeps every caller
that adds an origin (`StartBitOf`, `DataBinder.AssignClassBitOffsets`, `GroupImageCodec`, `ParameterConformance`) unchanged.
A first bit child begins where its group begins (`KeepsPosition`, the subject conjuncts of `SharesByteWith`). The public
`WidthBits` / `StrideBits` / `RunBits` / `TableBits` / `ExtentBits` read new explicit-phase `...At` forms at `PhaseOf(item)`;
the walk hands its phase down and never asks `PhaseOf` per child, which would be exponential in a run of same-level groups.
`IntrinsicBinder.VariableLengthGroupSum` needed no change (its prefix is `StartBitOf`). Determination recorded in
`docs/CONFORMANCE.md` (USAGE BIT) and `docs/COBOLNET_DATA_MODEL_DESIGN.md` (layout rule 6): a table of mid-byte bit groups
lays every occurrence out as the first.

**Goldens.** `2002/pb3158_aligned_in_mid_byte_bit_group` (values derived from the standard: DISPLAY, MOVE to a bit item and
to an alphanumeric item, LENGTH, BYTE-LENGTH, an OCCURS DEPENDING table, the fixed twin, a three-level nest);
`negative/pb3158-call-bit-unaligned-after-aligned` (BY REFERENCE of the ALIGNED item is accepted, the item after it at bit
13 is the one COBOLNET1683); `BitRunImageDriftTests` BRD09 and BRD10 (the Tier-B alias lane; BRD10 fails on the old layout);
unit `BitGroupPhaseDriftTests`. Rows re-verdicted: none (the note claimed none).

**Found, left open.** `05 R2 REDEFINES R1 GROUP-USAGE BIT` over a GROUP-USAGE BIT group does not compile (CS0103: the
redefined group's name does not exist in the generated C#), with or without ALIGNED, on an older build too.

