## Entry 154 — 2026-03-27: P0 Bug Sweep — 8 Critical Fixes from Spec Compliance Audit

**Context:** The 8-agent spec compliance audit (Entry 153) identified 8 P0 bugs that corrupt data
or crash the compiler. Fixed all 8 in a single pass using 4 parallel agents, each touching
different source files.

**Fixes:**
1. **OPEN multi-clause** (`BoundTreeBuilder.cs`): `OPEN INPUT A OUTPUT B` now wraps multiple
   open operations in `BoundCompoundStatement` instead of dropping all but the first.
2. **READ INVALID KEY** (`BoundTreeBuilder.cs`, `BoundNodes.cs`): INVALID KEY / NOT INVALID KEY
   now stored in separate fields on `BoundReadStatement`, not merged with AT END.
3. **WRITE/REWRITE INVALID KEY** (`BoundTreeBuilder.cs`, `BoundNodes.cs`): Now bound from
   grammar context; added `InvalidKey`/`NotInvalidKey` to `BoundWriteStatement`/`BoundRewriteStatement`.
4. **User-defined CLASS crash** (`BoundTreeBuilder.cs`, `DiagnosticDescriptors.cs`): Replaced
   `InvalidOperationException` with `COBOL0413` diagnostic + fallback `false` literal.
5. **NumericEdited→NumericEdited MOVE** (`CilEmitter.cs`, `PicRuntime.cs`): CIL dispatch now
   calls `MoveNumericEditedToNumericEdited` which de-edits (strips commas/currency/CR/DB),
   parses to decimal, then re-edits via `FormatNumericEdited`.
6. **LOCAL-STORAGE routing** (`CilEmitter.cs`): `EmitLoadBackingArray` now has explicit switch
   for all `StorageAreaKind` values. LOCAL-STORAGE routes to WorkingStorage (TODO: per-invocation
   re-init) instead of silently falling through to FileSection.
7. **File status codes** (`FileStatus.cs`): Corrected 43/44/47 to match ISO definitions. Added
   missing codes 46/48/49.
8. **Class condition on ref-mod** (`Binder.cs`): `LowerClassCondition` now uses
   `ResolveExpressionLocation` (handles ref-mod and subscripts) instead of requiring
   `BoundIdentifierExpression`. Emits diagnostic instead of throwing.

**Results:** 216 unit + 183 integration pass. Guard pending.

---

