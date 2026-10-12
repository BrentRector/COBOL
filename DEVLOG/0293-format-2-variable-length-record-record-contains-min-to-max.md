## Entry 293 — Format-2 variable-length record (RECORD CONTAINS min TO max) + XXXXX063 collating X-card → ST137A; ST147A made real (NIST 298→299)

ST137A crashed at runtime (rc=127, `ArgumentOutOfRangeException` in `FileStream.Write`) while BUILDing its
SORT input file. Two independent defects, one a real compiler bug:

**(1) Compiler bug — Format-2 variable-length records not recognized.** SQ-FS1's FD is
`RECORD CONTAINS 148 TO 1435 CHARACTERS` with a single 01 containing `STUFF-1 OCCURS 1 TO 100 DEPENDING ON
LENGTH-1`. That `min TO max` RECORD clause is ISO §13.18.43 **Format 2** — a variable-length record — but
`VisitRecordClause` only set `IsRecordVarying` for the explicit `RECORD IS VARYING` keyword (Format 3). So
`IsVariableLengthSequential` returned false (no VARYING keyword, single 01 ⇒ `HasMultipleRecordSizes` also
false), the WRITE took the FIXED path (`IrWriteRecordFromStorage`), and `SequentialFileHandler.Write` ran
`_stream.Write(recordData, 0, _recordLength)` with `_recordLength`=max(1435) against a record sliced shorter
→ overflow. Fix: `VisitRecordClause` now flags Format-2 `RECORD CONTAINS m TO n` (m≠n) as `IsRecordVarying`.
Because both Binder (`SetSequentialVarying`) and FileIoLowerer derive from the same
`IsVariableLengthSequential`, flagging at the source routes BOTH consistently through the length-framed
varying path. The varying WRITE needs no explicit DEPENDING-ON object: the record's `IrOdoGroupLocation`
already computes the runtime length (fixed-prefix + LENGTH-1×elementSize), and `EmitWriteRecordVariable`
with a null length keeps exactly that. Crash gone; build + record-count tests (SRT-TEST-001/002) pass.

**(2) Unsubstituted XXXXX063 collating-sequence X-card.** With the crash gone, SRT-TEST-003/004/005 still
FAIL*: COMPUTED was the correct ASCII-sorted key sequence but CORRECT was blank. `ASCIIS` (the expected
post-sort order) redefines `WRK-XN-2 VALUE XXXXX063`, an unsubstituted placeholder — the implementor's
native collating sequence as a 51-char literal. CobolSharp's native sequence is ASCII, so it is the 51 key
characters in ascending ASCII order: a leading space, `$$`, `()*+,-./`, `0-9`, `;<=>`, `A-Z` (derived from
ST137A's own scrambled key source `WRK-XN-0001` and cross-checked against the actual COMPUTED output). Added
a token-boundary-anchored substitution to `NistPreprocessor` (a `MatchEvaluator` so the literal `$`s pass
through verbatim; the lookbehind/lookahead skips the embedded `…XXXXXXXX063A…` inside IX106A's baselined
literal). ST137A → 6/6.

**Side effect — ST147A's collating checks made real.** ST147A (MERGE) also reads XXXXX063, for BOTH its key
source and its expected order. It had passed 26/26 *with the placeholder blank* — its NATIVE COLL.SEQUENCE
checks were comparing blank-against-blank (degenerate). With XXXXX063 substituted it passes 26/26 against
the REAL native collating sequence (proving the MERGE is genuinely correct, not vacuous), so ST147A was
re-baselined (its printed report now carries real key data). Both deterministic across runs; IX106A still
matches (boundary protected). ST137A added to the guard (self-contained). Guard ALL GREEN:
1000 unit / 347 integration / **299 NIST** (… + 29 ST), 0 regressions. ST near-misses complete; remaining ST
non-baselined are flagging modules (ST301M/ST302M) and intentional NO_OUTPUT producers.

