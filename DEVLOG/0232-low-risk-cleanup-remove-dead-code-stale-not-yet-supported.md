## Entry 232 — Low-risk cleanup: remove dead code + stale "not yet supported" diagnostics

The spec-gaps.md CLEANUP / dead-code follow-ups. Each candidate from the audit was **re-verified
empirically** before removal (the audit mislabeled several items this session), so the actual work
diverged from the audit's list in two places (noted below).

**Dead code removed:**
- `CobolProgram.cs` (Runtime): the five arithmetic helpers `AddTo`/`SubtractFrom`/`MultiplyBy`/
  `DivideInto`/`DivideGiving`. Proven dead — zero references in the Compiler, and these
  `protected static` base-class methods can only be reached via emitted IL that names them, which the
  emitter never does (live arithmetic lowers to IR + `PicRuntime`). The `Arithmetic_AddTo` /
  `…SubtractFrom` unit tests exercise `CobolField` directly, not these helpers.
- `CilEmitter.EmitRuntimeCall`: the legacy `CobolRuntime.Display` branch (the only occurrence of
  `"CobolRuntime.Display"` in the tree was this consumer — no IR producer; real DISPLAY lowers via its
  own emitter) and the dead `"CobolRuntime.OpenOutput"` alternative of the OpenOutput branch (every
  producer uses `"FileRuntime.OpenOutput"`). The misleading comment ("DISPLAY emits
  Console.WriteLine(\"statement executed\")") is gone. `CobolRuntime.WriteText` was checked and **kept**
  — it has a live producer (`FileIoLowerer.cs:64`), contrary to a casual reading of the audit.

**Stale parse-error hints removed** (`CobolErrorStrategy.cs` + matching `DiagnosticDescriptors`):
heuristic hints that fire on a parse error to claim a feature is "not yet supported." Each named
feature was compiled AND run to confirm correct output before deleting its hint:
- COBOL0104 OCCURS DEPENDING ON, 0105 INSPECT CONVERTING (`"abcde"`→`"ABCde"`), 0106 INITIALIZE
  REPLACING (→`007`), 0108 multi-target SET (`SET C1 C2 TO TRUE`→`YY`), 0109 PERFORM VARYING … AFTER
  (2×3→`06`), 0311 NOT-= abbreviated (`IF X NOT = 4`→`NE`).
- Removing the COBOL0311 block left `next` and `prevUpper` locals unused in `GuessCobolIntent`; both
  declarations were removed too (no other readers).
- **Kept** (deliberately): 0100/0101/0102/0103 (degradation / generic SET / partial guidance) and
  0107 EVALUATE ALSO (hedged "may not be fully supported" — multi-subject EVALUATE only spot-checked).

**Audit corrections:** the audit's `DiagnosticDescriptors` COBOL0467 and the diagnostic codes
COBOL0393/0395/0433 **do not exist** anywhere in the codebase — nothing to remove. spec-gaps.md §2/§4
corrected to match.

Diagnostic-code numbering keeps gaps (0104–0106, 0108/0109, 0311 retired); existing stable codes were
not renumbered. No test depended on any removed descriptor.

Guard ALL GREEN: 1000 unit / 345 integration (+1 unrelated skip) / 148 NIST baselines 0 FAIL*.

