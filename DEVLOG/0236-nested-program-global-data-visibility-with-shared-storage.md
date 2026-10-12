## Entry 236 — Nested-program GLOBAL data visibility with shared storage (IC228A → CLEAN)

IC228A (was COMPILE_FAIL, then a graceful COBOL0415 after DEVLOG 235) tests the IS GLOBAL phrase: a
contained program `IC228A-1` references its container `IC228A`'s `01 GLOBAL-DATA IS GLOBAL` items
(GLO-DATA-1..4) without redeclaring them, and the storage is shared — `IC228A` sets GLO-DATA-4=1,
CALLs `IC228A-1` which does `ADD 10 TO GLO-DATA-4`, then `IC228A` checks GLO-DATA-4 = 11.

The compilation pipeline flattens nested programs and compiles each with an isolated symbol table and
its own static `State` (ProgramState/WORKING-STORAGE byte[]), so the inherited globals didn't resolve
and the storage wasn't shared. Implemented per ISO §8.4.5 (a global name is available to the declaring
program and every program contained within it, unless the contained program redeclares it).

Mechanism — **cross-program State access** (no symbol cloning, no shared-array bookkeeping). All
program types are emitted into one module and a containing program is emitted in full — including its
`public static State` field — before its contained programs, so a contained program can read the
container's storage directly:

- `StorageLocation` gains an optional `OwnerProgramId` (default null). When set, the bytes live in
  *that* program's ProgramState rather than the current one.
- `Compilation`: `CollectProgramContexts` now records each program's containing program
  (`programParents`); programs are processed outermost-first. After a program is built + laid out,
  `InheritGlobalItems` walks its containment chain (nearest first) and, for every `IS GLOBAL` 01/77
  item in an ancestor (plus all subordinates, skipping FILLER), calls the new
  `SemanticModel.TryInheritGlobal`, which declares the ancestor's `DataSymbol` into this program's
  data-division scope (so `ResolveData` finds it) and registers the ancestor's `StorageLocation`
  tagged with the ancestor's program id. `TryDeclare` failing on a locally-declared name gives correct
  shadowing.
- Emission: `CilLocationEmitter.EmitLocationArgs` routes an `IrStaticLocation` whose
  `OwnerProgramId` is set to `EmitForeignGlobalLocationArgs`, which loads the owner program type's
  static `State` field (found by name in the shared module) and its backing array, then the
  owner-relative offset. `ResolveLocation` needed no change — the tagged location rides through.

Verified: minimal repro (`/e/tmp/probe/GLOB.cob` → `GCOUNT=0011`, local var untouched) and IC228A
13→0 / "004 OF 004 TESTS WERE EXECUTED SUCCESSFULLY". Guarding test added
(`CallTests.NestedProgram_ReferencesContainingGlobalItem_SharesStorage`). Baselined + added to guard
(NIST baselines now 152 = 94 NC + 42 IF + 12 SM + 4 IC; IC 20/47). Full guard ALL GREEN — the pipeline
+ StorageLocation + emitter changes regress nothing (1000 unit / 348 integration / 152 NIST).

Scope (handled): whole-item and elementary inherited-global references (the common case; covers
IC228A's GLO-DATA-1..4). Deferred (not exercised by IC228A, would extend the same `OwnerProgramId`
plumbing through the element/ref-mod address paths and the condition-name table): subscripted or
reference-modified inherited globals, level-88 condition names declared under a global group, and
GLOBAL items in the FILE SECTION. Noted for follow-up.

