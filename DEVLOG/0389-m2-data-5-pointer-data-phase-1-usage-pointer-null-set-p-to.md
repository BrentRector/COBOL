## Entry 389 — M2-DATA-5: POINTER data Phase-1 — `USAGE POINTER`, `NULL`, `SET p TO NULL/q`, `= NULL`/`= q` (ISO §13.18.60.4, §8.4.3.10, §14.9.39, COBOL-2002)

Implemented COBOL-2002 pointer data Phase-1 from the recipe in plan §3.2 (DEVLOG 388). A `USAGE POINTER` item is
an **8-byte opaque machine-address handle** (no PICTURE); `NULL` is the predefined null address (the all-zero
handle). Scope: declaration, `SET p TO NULL`, `SET p TO q` (handle copy), and `= NULL` / `NOT = NULL` / `= q`
equality. **Confirmed Phase-1 does NOT need the deferred handle→address `PointerRegistry` design decision** —
that is only for Phase-2 dereference (ADDRESS OF / BASED / ALLOCATE, the .NET managed-memory problem), still
owner-gated. NULL = 8 zero bytes, so the lean reuse approach worked: `SET p TO …` lowers to a MOVE
(NULL → `MoveFigurativeToField` 0x00×8; pointer←pointer → 8-byte byte copy); `= NULL` reuses the
figurative-comparison path (added `FigurativeKind.Null` → `'\x00'` in `MakeFigurativeString`); `= q` reuses
`IrStringCompare` — **no new IR/runtime compare nodes.**

Touch: `CobolCategory.Pointer` + `IsPointerLike()`; 8-byte sizing (FieldSizeCalculator / ComputeStorageLength /
StorageLocation synth / MapToIrType); `PicUsageResolver` no-PIC POINTER→category; `usageKeyword += POINTER`;
`NULL_` added to `figurativeConstant` + bound to `FigurativeKind.Null`; `BindSetToValue` pointer target → MOVE,
new `BindSetObjectReference` for `SET p TO NULL` (NULL_/data-ref in the objectReference form, which had no binder);
`CategoryCompatibility` `(Pointer,Pointer)` + `IsPointerFamily`; `EmitMoveFieldToField` pointer 8-byte copy;
`DataItemClassifier` rejects `VALUE` on a pointer (§13.18.26 SR9 → CBL1002, before the group check);
`DataMovementLowerer` skips a pointer in default INITIALIZE (§14.9.20.3 — pointers left unchanged).

**Three real bugs found and fixed during implementation (self-review + the adversarial review confirmed clean):**
- **`SET p TO NULL` was a silent no-op.** `DataTypeSymbol.Category` derived only from PIC/numeric/alpha flags, so a
  no-PIC pointer was `Unknown`, not `Pointer` → the binder-level pointer checks never fired (P kept its initial
  spaces). Fixed by deriving `Pointer` from `Usage` when there is no PIC.
- **A no-PIC pointer was classified `IsGroup`** (the root of two symptoms: the group-VALUE warning preempting the
  pointer-VALUE reject, and `MOVE X TO P` / `MOVE 5 TO P` skipping the MOVE-legality check). Fixed by adding
  POINTER to the no-child elementary exclusions in `DataSymbol.IsGroup`/`IsElementary` (mirrors INDEX/COMP-1/2/
  BINARY-*). Now those illegal MOVEs are cleanly rejected (CBL0901).
- VALUE-on-pointer check ordered before the group check.
Plus removed one unreachable line (the pointer arm in `RecordLayoutBuilder.MapToIrType` — a no-PIC pointer is
already `ByteArray` via the `pic == null` path).

Verified end-to-end + by the review's 13 probes: 8-byte layout in records/REDEFINES/OCCURS, SET/copy, equality vs
NULL and vs another pointer, group INITIALIZE leaving the pointer untouched, pointer↔non-pointer MOVE rejected,
VALUE-on-pointer rejected, DISPLAY non-crashing, and no mis-categorization of normal programs. Conformance
`tests/conformance/2002/pointer_data`. **Deferred (Phase-2/3, the owner-gated design decision):** ADDRESS OF /
SET … UP/DOWN BY / BASED / ALLOCATE / FREE; also ordering-operator rejection on pointers (`< >` compile to a
byte-compare today — invalid input, no crash), PROGRAM-/FUNCTION-POINTER distinctions, and a stale orphaned
`LoweringTable` (no Pointer cases; it is called from nowhere). **Phase-1 is thin standalone** — until ADDRESS OF/
ALLOCATE exist every pointer can only be NULL; its value is the foundation.

Guard ALL GREEN (1047 unit / 478 integration / 364 NIST), 0 regressions.

