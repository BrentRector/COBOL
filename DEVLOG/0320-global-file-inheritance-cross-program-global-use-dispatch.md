## Entry 320 — GLOBAL FILE inheritance + cross-program GLOBAL USE dispatch → IC233A + IC234A baselined (NIST 357→359)

Fourth and hardest parallel-implementation fix — the GLOBAL-FILE feature DEVLOG 236 deferred. **IC233A/IC234A**
were COMPILE_FAILs: a contained program OPENs/READs a `FD … IS GLOBAL` file declared in its containing program
(legal per ISO §8.4.6.2.2) and the container's `USE GLOBAL AFTER ERROR PROCEDURE ON INPUT` must fire on the
contained program's I/O exception (ISO §14.9.49.4 GR4). Two parts, both implemented:
- **Part A (compile-unblock):** `Compilation.InheritGlobalItems` now also inherits each ancestor `FD … IS
  GLOBAL` FileSymbol into the contained program's GlobalScope (`SemanticModel.TryInheritGlobalFile`);
  `CollectInheritedGlobalNames` yields the GLOBAL file's NAME; `ReferenceResolver`'s OPEN/READ/CLOSE check
  accepts a name in `_knownNames`. Clears CBL3121 + CBL3128.
- **Part B (the real PASS):** cross-program GLOBAL USE-declarative dispatch. `BoundUseStatement.IsGlobal` is now
  tracked in the SemanticModel (`GlobalUseDeclaratives`); the Binder resolves each GLOBAL declarative section to
  a paragraph-index range (`IrModule.GlobalUseHandlers`); CilEmitter emits + registers each in a new
  `CobolSharp.Runtime.GlobalUseDeclarativeRegistry` (run-unit-shared); `FileIoLowerer.EmitUseDeclarative`, after
  the current program's candidates find no match, consults the registry to PERFORM a containing program's GLOBAL
  declarative via the shared Dispatch helper. The container's `ADD 1 TO DILFRAP` lands in the GLOBAL (shared)
  cell → DILFRAP=1 → PASS.

Both the single-CALL-level IC233A and the 4-level GLOBAL-attribute + open-mode-filtered IC234A → 001 OF 001, 0
FAIL*, baselined. **Integration:** this fix (11 files + the new runtime registry) was implemented in a worktree
off an older base; merging onto main (with this session's SQ212A/RL111A/IC235A/IC114A changes) auto-resolved 9
files via 3-way merge and conflicted on 2 (Binder + CilEmitter — both also touched by IC114A; each conflict was
two independent additions, kept both). Critically, the merge of my and the agent's `EmitUseDeclarative` edits is
coherent — SQ212A (017/017, declaratives-return), RL111A (022/022, re-entrancy guard) and IC228A
(nested-GLOBAL-data) all stay MATCH. Guard ALL GREEN: **1040 unit / 347 integration / 359 NIST** (IC 21→23), 0
regressions. **⇒ All four parallel-diagnosed+implemented backlog fixes (RL213A, IC114A, IC227A, IC233A/234A) are
integrated; the file-I/O/IC diagnosed backlog from DEVLOG 311 is COMPLETE except IX108A (separate scope-terminator
bug).**

