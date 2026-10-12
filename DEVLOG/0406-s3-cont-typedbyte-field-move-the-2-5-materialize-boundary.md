## Entry 406 — S3 cont.: typed↔byte field MOVE — the §2.5 materialize boundary for MOVE (byte-identical)

A typed field can now MOVE to/from a **byte-backed** field (the common mixed case), via the IDataSlot materialize
boundary (ADR §2.5): byte source → typed dest reads the source window as a Latin-1 string (`CobolString.FromWindow
(byte[],int,int)`, new overload) then `CobolString.Store` at the dest width; typed source → byte dest lays the
source string into the destination window (`StorageHelpers.MoveStringToField` — the same call the byte path uses).
Latin-1 round-trips byte↔char losslessly, so both are byte-identical. Implemented as two branches in
`CilDataEmitter.EmitMoveFieldToField` (after the both-typed branch), reusing `EmitLocationArgs` for the byte side
and the typed helpers for the typed side — so MOVE now works in all four src/dst representation pairs
(typed↔typed, typed↔byte, byte↔typed, byte↔byte), removing the `EmitLocationArgs` throw for field MOVE. Test:
`WS-TYPED PIC X(5)` (flipped) ↔ `WS-BYTE` (byte via REDEFINES), both directions, byte-identical. Guard **ALL GREEN:
1187 unit / 486 integration / 364 NIST**. NEXT: typed COMPARE (`IF`), then the sender-materialize for the
remaining ops (INSPECT/STRING/refmod), then numeric (hard-gated on the `CobolNum` oracle), OCCURS/nested groups.

