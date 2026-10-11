## Entry 454 — Phase B1: OO typed-native SLICE 1b — NUMERIC object data → per-instance `long`/`decimal` (+ typed-numeric→byte MOVE cell)

Extends DEVLOG 453 from character to **numeric** object data. The classifier already classifies object numerics, and
the numeric typed paths (sender-materialize, arithmetic receiver prologue/epilogue) **already funnel through the three
per-instance value-access primitives** instance-ized in 453 — so the only blocker was the `classCharOnly` collect
restriction. Repurposed it to **`classFlatOnly`**: a class now flips FLAT object data (character **and** numeric →
per-instance `string`/`long`/`decimal`), still deferring record-struct/OCCURS object data (their `EmitInstanceAddressChain`
`ldsflda` / `EmitTypedElementAddress` `ldsfld` address chains are still static-only — a later slice).

**One real infrastructure gap surfaced + filled: the typed-numeric→byte MOVE cell.** `oo_method_args` does
`MOVE BAL TO LK-RES` (a typed `long` object datum → a byte LINKAGE item); only the typed↔typed and string↔byte MOVE
cells existed, so a typed-numeric source hit the loud guard. Fixed generally and DRY-ly: `EmitMoveWithStandardSignature`
now pushes the **source** through `EmitLocationArgsWithPicMaterializingTyped` (the §2.5 materialize floor) instead of
`EmitLocationArgsWithPic`. A typed numeric/string source is materialized into a scratch byte window and the byte MOVE
runs identically; for a byte source the helper is **byte-identical** (it falls through to the same `EmitLocationArgs`),
so the whole legacy corpus is unaffected. The DESTINATION stays byte-only (a byte→typed-numeric MOVE still loud-guards —
no OO path needs it yet; it routes through a typed cell upstream when it lands).

**Verified (Mono.Cecil):** `oo_method_perform`'s `01 N PIC 9` → `instance Int64 _T_N` on TICKER; `oo_method_args`'s
`01 BAL PIC 9(4)` → `instance Int64 _T_BAL` on ACC (LINKAGE `LK-AMT`/`LK-RES` stay the static `ManagedPointer` byte ABI —
correct). Outputs unchanged: `N=1/2/3`, `A1=0010/A2=0100/A1=0015` (the two ACC objects accumulate **independently** —
per-instance numeric proof). All 5 `oo_*` conformance tests + `OoTests` green; the existing object data of every OO test
is now fully off the byte path. NEXT: multi-method classes + `INVOKE SELF`, then INHERITS-typed + subclass typed data;
record-struct/OCCURS object data when their address-chain primitives go per-instance.

