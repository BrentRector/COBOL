## Entry 483 — COBOL.NET G2-2(b): the §2.2 backend decomposition (no god class)

The mechanical half of the pivot (COBOLNET_DESIGN §17 §2.2) — a pure code-move now that the bound tree is the
contract, applied pre-emptively before G3/G4 grow the backend (the design's "decompose before past 1000 lines").
The 791-line parse-walk emitter → a 239-line orchestrator + focused collaborators over a shared `EmissionContext`:

- `CodeGen/Emit/EmitCore.cs` — `EmissionContext` (Writer + DataBinder + the mutable division `TargetScale`), the
  `NumX` carrier, and `EmitText` (loud-failure guards, literal escaping, numeric-literal scaling).
- `CodeGen/Emit/NumericRenderer.cs` — bound expression/operand → scale-tracked `NumX` (the `CobolNum.*` engine).
- `CodeGen/Emit/OperandText.cs` — bound operand → DISPLAY-image string (+ the string-vs-numeric classifier).
- `CodeGen/Emit/ConditionRenderer.cs` — bound condition → C# boolean (relational / logical / 88 / sign).
- `CodeGen/Emit/FieldEmitter.cs` — the DATA DIVISION (record structs / arrays / profiles / index fields / VALUE).
- `CodeGen/CSharpEmitter.cs` — the slim orchestrator: bind → wire the collaborators → emit the class shell +
  paragraph loop + the statement `switch` (each arm a thin call into a collaborator). 239 lines; every collaborator
  ≤ ~110.

Behavior-preserving (a pure move): full-solution build clean Debug (0/0, warnings-as-errors); Conformance 93 + Unit
3 green; legacy oracle untouched. **G2-2 (the bound-tree pivot) is complete.** RESUME AT → ref-mod (G2-1c, needs
`CobolString.RefMod`/`SpliceInto`) / class conditions, then G3 (the `CobolInt`/`TryStore` engine + ROUNDED + ON SIZE
ERROR + INSPECT/STRING/UNSTRING) and G4 (the PC dispatcher) — both now land against the bound tree, written once.

DEVLOG 483.

