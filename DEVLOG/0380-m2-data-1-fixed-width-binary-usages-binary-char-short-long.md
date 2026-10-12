## Entry 380 — M2-DATA-1: fixed-width binary usages `BINARY-CHAR/SHORT/LONG/DOUBLE` (ISO §13.18.60, COBOL-2002)

Closes the audit-flagged 🐛 "BINARY-CHAR/SHORT/LONG/DOUBLE silently mis-typed". These COBOL-2002 usages
declare a native two's-complement integer of a **fixed byte width** (1/2/4/8) with no PICTURE — `SIGNED`
(default) or `UNSIGNED`. Semantically they are COMP-5 (full binary capacity, not digit-limited) at a fixed
width, so the design maps them onto the existing COMP-5 runtime rather than a new numeric format.

The width must survive compiler-side (for layout) while the runtime sees only COMP-5. So four compiler-only
`UsageKind` markers (BinaryChar/Short/Long/Double) carry the identity, and `StorageLocation.FromDataSymbol`
synthesizes a descriptor with `usage: Comp5` + explicit `StorageLength` (1/2/4/8) + `TotalDigits`
(3/5/10/19-signed or 20-unsigned, for DISPLAY width) + `IsSigned`. The runtime never sees the marker kinds.

Touch points: `UsageKind` (+4); lexer `BINARY-CHAR/SHORT/LONG/DOUBLE` + `SIGNED`/`UNSIGNED` reserved words
(corpus-checked — only ever in string literals/comments); grammar `usageClause`/`usageKeyword` + a `binarySign`
sub-rule (`USAGE IS? usageKeyword binarySign?` and a bare `BINARY-xxx binarySign?` alternative); SemanticBuilder
reads the bare-binary token directly (`BareBinaryUsageText`, so a trailing SIGNED/UNSIGNED isn't glued onto the
keyword by GetText) + `DataSymbol.IsUnsignedBinary`; `DataSymbol.IsGroup/IsElementary` exclude the new usages
(like COMP-1/2); `PicUsageResolver` marks them numeric; `FieldSizeCalculator` 1/2/4/8 bytes; `RecordLayoutBuilder`
maps them to Int32/Int64 by elem size; `UsageMapper` keyword→kind.

One runtime gap surfaced in testing: the 2/4/8-byte BINARY types worked immediately but BINARY-CHAR read back
000. Root cause — no PIC'd COMP-5 is ever 1 byte (`ComputeBinarySize` floors at 2), so `DecodeComp5`/`EncodeComp5`/
the COMP-5 overflow check had no 1-byte arm (decode fell through to 0; overflow defaulted to "always overflow").
Added the 1-byte case (`(sbyte)`/`(byte)`, low-byte write, sbyte/byte range) to all three. Verified BC=127,
BC=-128 (two's-complement), BCU=255, SHORT/LONG/DOUBLE full-width, and `COMPUTE BL = BC * 10`. (A unique
PROGRAM-ID per throwaway test this time — lesson from Entry 379.) Conformance `tests/conformance/2002/binary_usage`.

Guard ALL GREEN: **1047 unit / 472 integration / 364 NIST**, 0 regressions.

