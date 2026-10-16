## PB2754: one frame decoder for every framed file — a torn tail, a gap word and a huge table count now end the data for every reader

**The mechanism.** The varying-frame grammar (length word, gap tag, bit-31 extent flag, extent table, payload) was
encoded once but DECODED four times — `RecordFraming.DecodeStore`, `FrameStarts`, `StreamFramingParses` and
`SequentialConnector.NextFrame`, the last through a parallel Latin-1 character decoder set (`CharWord`,
`PrefixLength`, `ExtentCount`, `ExtentTableFromChars`). The copies disagreed on a damaged file.

**Re-probe (this worktree's build, before the change).** A RECORD VARYING sequential READ loop over crafted files:
a torn last frame was delivered as a third record (`REC 03 L=04 [PART] 00`) although `FrameStarts` — which feeds the
OPEN EXTEND ordinal and the READ PREVIOUS index — ended the data before it; a gap word, or an extent-table count of
0x7FFFFFF0, after two good frames ended the run unit with `Out of memory.`

**The fix.** `RecordFraming.ReadFrame<TMedium>` is the one decoder, over an `IFrameMedium` (`Fill`, `TakePayload`,
`Holds`): it alone says what a gap is and that a frame the data ends inside — or a head this writer cannot produce —
ENDS the data, and it asks `Holds` before allocating anything sized by a count read from the medium.
`ReadStreamFrame` maps a gap in a stream-level file (no slots) to the end. Three media: `ImageMedium` (the keyed
store image), `StreamMedium` (the frame walk and the OPEN's framing check, payloads seeked over) and the
connector's `ChannelMedium` (its Latin-1 channel, a head's characters converted to bytes once; a length query only
for counts above 64 KiB, so the common READ pays none). One encoder: `EncodeFrameHead` / `EncodeGap` /
`EncodeExtentTable`, used by `ComposeStore`, the sequential WRITE (`EmitFrameHead`) and the in-place REWRITE's
table (`OverwriteInPlace` now takes the framing as bytes). The char decoders, `WriteWord`, `ExtentTableChars`,
`FrameWord`, `DecodeExtentTable` and `WriteFrameHead(TextWriter)` are deleted with every caller changed.

**Behavior.** Every well-formed file reads exactly as before (the compiler's output is untouched: runtime only).
A damaged file now reads the same through every reader: after the change the probe shows `HELLO`, `WORLD!`, then
`10` for the torn, gap and huge-count files alike. The determination is recorded in `docs/CONFORMANCE.md`
DOC-A.1-151 (§9.1.7.2's implementor framing) and `docs/COBOLNET_FILES_DESIGN.md`.

**Witnesses.** New `FrameDecoderDriftTests`: the frame word's constants and the 32-bit word decode appear only in
the decoder and the encoder (Roslyn, over `src/Cobol.Net.Runtime/IO`); each of the four frame readers calls the
decoder; on seven damaged shapes the forward READ, `FrameStarts` and `StreamFramingParses` stop at the same frame,
a damaged first frame is not a framed file ('39'), and the store shape keeps a gap as a slot.
`RecordExtentsTests` reads its stream frames through the decoder; `SharedReadCoherenceDriftTests` admits
`ChannelMedium` as part of the one walk and checks that only `NextFrame` constructs it.

**Rows.** None: PB2754 is an R2 restructuring finding (`closes_rows: []` with its reason).

**Self-review.** Sibling arms: all four decoders and both encoders (store, stream WRITE, REWRITE table) now meet in
`RecordFraming`; no other little-endian word decode exists under `Runtime/IO`. Fixed on the way: a stale
`<see cref="WritePrefix"/>` in the class summary, a wrong `OverwriteInPlace` cref, a stale `WriteStore`/`ReadStore`
name in `COBOLNET_FILES_DESIGN.md`, and an unused `using System.Buffers`. Lead left open: OPEN EXTEND on a file whose
data ends at a damaged frame appends PAST it, where no reader reaches.

**Branch review, round 1 (R1-1, efficiency).** The decoder asked `Holds(payload)` and `StreamMedium.TakePayload`
(and `ImageMedium.TakePayload`) asked it again, and a table frame asked a third time through `ReadExtentTable`. On
a stream each ask was `fs.Length`, an uncached operating-environment query on any handle that admits other writers
(every `HostFile.OpenAuxiliary` handle shares `ReadWrite`), so the record count, REVERSED and the first READ
PREVIOUS paid two or three length queries per frame where the old walk paid one. Fixed at the contract:
`IFrameMedium.TakePayload` is called only after `Holds(length)` answered true, and the media no longer re-ask; and
`StreamMedium` snapshots the stream's length once, when the walk begins, bounding `Fill` and `Holds` by it, so a
walk now asks the length once in all. The snapshot makes the walk a point-in-time read: bytes another run unit
appends mid-walk end the data there, as an append in progress does, and the frame index and the count were
point-in-time answers already. Witnesses: `FrameDecoderDriftTests.TheFrameWalk_AsksTheStreamLengthOnce_NotPerFrame`
(50 frames, half with tables, one length query) and `TheFrameWalk_ReadsTheFileAsItStoodWhenTheWalkBegan`.
`ChannelMedium` was the sibling arm checked: its `TakePayload` never re-asked and its `Holds` asks the length only
above 64 KiB.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261016T155805Z-f7e334; Conformance 6,014 · Unit 33,193 · Characterization 6 ran in leg 1, the new FrameDecoderDriftTests cases among them); after the review fix, `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` again (run 20261016T161829Z-130770; Conformance 5,978 · Unit 33,156 · Characterization 6 ran in leg 1, both new walk witnesses Passed). The architecture oracle (`compare_oracle.py`) compares emitted C# and diagnostics, which no file of this change reaches (runtime only).
