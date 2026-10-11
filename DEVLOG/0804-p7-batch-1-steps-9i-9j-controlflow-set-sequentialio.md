## Entry 804 — 2026-07-11 17:26 PDT — P7 BATCH-1 (Steps 9i+9j): ControlFlow · Set · SequentialIo collaborators — the orchestrator core reaches ZERO bare runtime fragments

**What (the first batched cycle per the 803 directive).** Three collaborators extracted in one battery cycle:
- `Verbs/ControlFlowEmitter.cs` (9i) — IF · inline/out-of-line PERFORM (TIMES/UNTIL/VARYING) · serial SEARCH ·
  GO TO DEPENDING, over (ctx, num, cond, DispatchState, SetEmitter, host). The VARYING/SEARCH index advances
  ride SetEmitter's ONE store/augment pair — the census's cross-verb edge, now an explicit ctor dependency.
- `Verbs/SetEmitter.cs` (9i) — SET TO / UP-DOWN BY / pointer F4 / capacity / condition-names TO TRUE +
  `StoreSetTarget`/`AugmentSetTarget`, over (ctx, num, ArithmeticEmitter, PtrEmitter) — NO host edge at all.
- `Verbs/SequentialIoEmitter.cs` (9j) — registration (LINAGE/SHARING) · OPEN/CLOSE/UNLOCK/WRITE/READ/REWRITE +
  the file-I/O common services (EmitUseHook/EmitStoreFileStatus/EmitImageInto/EmitReadLengthStore/
  VaryingLengthArg/RenderRetry/RuntimeRecordLock) the 9e shims now forward to; KeyedIo injected for
  registration, `CallUnitState` carries the inherited-GLOBAL status routing.
**Ratchet:** `RuntimeApi` grew the sequential-file surface (Register/SetLinage/RegisterSharing/OpenShared +
the mode/kind-ANCHORED `FileOpen`/`FileClose` selections — the stringly-typed "OpenOutput"/"CloseWithLock"
switches are GONE — Unlock/WriteAdvancing/EndOfPage/Status/OpenModeOf/Rewrite; FileWrite gained the varying-
length arg). **`CSharpEmitter.cs` = 0 bare fragments** (98 → 0 across 9g–9j); its baseline entry DELETED.
Residues: SetEmitter 2 · SequentialIo 3 (typed `CobolRounding`). 34 = 0+0+2+3 + 29 routed — exact.

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions. (Transparency: the 803 DEVLOG-write flipped this file to LF in-repo — a
one-time cosmetic rewrite; autocrlf reconciles the worktree, entries unchanged.)

