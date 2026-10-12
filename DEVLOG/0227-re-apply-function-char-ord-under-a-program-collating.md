## Entry 227 — Re-apply FUNCTION CHAR/ORD under a program collating sequence + remove dead pic-registry

With the figurative-SPACE/PCS defect fixed (Entry 226), the previously-reverted Gap 2 work re-applies
cleanly. Restored verbatim from the reverted commit (reflog fcaab53, whose parent is the current
baseline), exact per-file:

- CHAR(n)/ORD(c) honor the alphanumeric program collating sequence (ISO §15.15/§15.36, 1-based).
  Lowering-time baking matching comparisons & SORT/MERGE: `IrIntrinsicCall`/`IrFunctionCall` carry a
  `byte[]? CollatingSequence`, set in `ExpressionLowerer`/`DataMovementLowerer` from
  `_ctx.Semantic.ProgramCollatingSequence`; `CilExpressionEmitter` pushes it as a 3rd arg to
  `IntrinsicFunctions.Call`; `Call`/`Char`/`Ord` gain an optional `byte[]? collating`. Semantics:
  ORD(c)=seq[code]+1; CHAR(n)=first code whose weight==n-1; null=native. CHAR-NATIONAL left native.
- Removed the dead `SemanticModel` pic-descriptor registry (`RegisterPicDescriptor`/
  `GetPicDescriptor`/`_picDescriptors`) — zero callers since the Entry-225 numeric-key fix.

Note on interaction with Entry 226: STANDARD-1/2/NATIVE are now identity → normalized to null, so a
program whose PCS is STANDARD-2 runs CHAR/ORD natively (correct). The collating CHAR/ORD path is
exercised by a genuinely reordered alphabet — `IntrinsicCollatingTests` uses `ALPHABET REV IS
"B","A"` (ORD A=2/B=1, CHAR 1=B/2=A) plus a native control; both pass.

Guard ALL GREEN: 1000 unit, 341 integration (+1 unrelated skip), 148 NIST 0 FAIL*. Collating
subsystem complete: comparisons, SORT/MERGE/table-sort keys, and FUNCTION CHAR/ORD all honor the
alphanumeric program collating sequence.

