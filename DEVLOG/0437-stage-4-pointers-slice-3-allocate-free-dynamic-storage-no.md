## Entry 437 — Stage-4 pointers slice 3: ALLOCATE / FREE (dynamic storage, no native heap) — pointer subsystem COMPLETE

Pointer slice 3 (ISO §14.9.3 ALLOCATE / §14.9.15 FREE), the last pointer slice. Dynamic storage is a GC-managed
`byte[]` wrapped in a `ManagedPointer` — no native heap, no manual deallocation, no `unsafe`.

**Grammar** (corpus-checked first per plan §0: ALLOCATE/FREE have zero corpus occurrences, INITIALIZED appears only
in comments/string-literals; the statements are `{is2002()}?`-gated so NIST/Default parsing is unchanged): new lexer
tokens `ALLOCATE`/`FREE`/`INITIALIZED`; `allocateStatement` (`n CHARACTERS | based-item`, `[INITIALIZED]`,
`[RETURNING p]`) + `freeStatement` (`FREE p…`); parser regenerated.

- **Runtime:** `ManagedPointer.Allocate(int)` — `new byte[n]` (zero-filled ⇒ INITIALIZED satisfied), or NULL for
  n ≤ 0 (GR2).
- **ALLOCATE:** `BoundAllocateStatement` → `IrAllocate` → `ManagedPointer.Allocate((int)size)` stored into the BASED
  item's `_PTR_` field (form 2, size = the based item's byte size) and/or the RETURNING pointer field (form 1,
  size = `n CHARACTERS`). When both targets are present the same allocation is `dup`-stored into each.
- **FREE:** reuses the boundary-1 pointer-store-NULL path (`BoundSetPointerStatement` Null per operand) — the GC
  reclaims the released `byte[]`. No new IR ([[feedback_singular_pattern]]).

Conformance `tests/conformance/2002/pointer_alloc.cob`: `ALLOCATE B` (a BASED `PIC X(5)`) → MOVE/DISPLAY → `B=HELLO`;
`ALLOCATE 5 CHARACTERS RETURNING P` → `SET ADDRESS OF B TO P` → `B2=WORLD`; `FREE P` → `P = NULL` → `FREED=YES`.
EC-STORAGE-NOT-AVAIL / EC-STORAGE-NOT-ALLOC (GR5 / GR1c) belong to the deferred EC subsystem (M2-PROC-4).

**Stage-4 pointer subsystem COMPLETE** (slices 1a/1b/2/3): `USAGE POINTER` / `BASED` / `ADDRESS OF` /
`SET ADDRESS OF` / `NULL` / `=` `≠` / `SET … UP|DOWN BY` / `ALLOCATE` / `FREE` — all on ONE managed `ManagedPointer`
representation, always-typed, no 8-byte handle. guard-fast ALL GREEN (1196 / 510 / 364), NIST byte-identical to
serial. **Next: Stage-4 OO → .NET classes**, then Stage-5 Roslyn backend. Remaining pointer follow-ups
(PROGRAM-/FUNCTION-POINTER, a pointer inside a typed record/table, the EC exception checks) tracked in
`docs/RECORD_STRUCT_STORAGE_DESIGN.md` §10.4.

