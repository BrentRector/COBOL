## Entry 482 — COBOL.NET G2-2(a): the bound semantic tree (the parse-walk emitter is retired)

The architectural pivot the advisor called for: build the real bound semantic tree (COBOLNET_DESIGN §2) BEFORE G3
(the CobolInt engine, INSPECT/STRING…) and G4 (the PC dispatcher) route through it — so the backend is written once,
not migrated twice. Behavior-preserving: the 93 differential + 3 unit tests pin the output and all stay green.

- **New `Binding/Bound/`:** `BoundTree.cs` — the node model (`BoundProgram`/`BoundParagraph`; statements
  DISPLAY/MOVE/the arithmetic verbs/COMPUTE/IF/inline+out-of-line PERFORM/SET/STOP; `BoundExpr` numeric tree;
  `BoundOperand`; `BoundCondition` relational/logical/NOT/88/sign; and `Bound*Error`/`BoundUnsupported` for
  loud-failure). `StatementBinder.cs` — walks the parse tree ONCE, resolving every reference to a `Place`, decoding
  every literal, and binding every expression/condition/statement; the 88-condition and SET nodes carry the
  already-resolved parent `Place`.
- **`CSharpEmitter` rewritten to RENDER the bound tree.** It no longer walks the ANTLR parse tree for the PROCEDURE
  DIVISION — the only two `Core.*` references left are the `Emit` entry point that locates the program and hands it
  to the binder. The numeric scale-tracking (`NumX`/`Combine`/`Align`) + the DATA DIVISION emission are unchanged;
  everything else became a `switch` over bound node types. This is the advisor's litmus test for a *real* bound
  tree (not a parse-context wrapper the emitter re-walks): a future desugar pass or the G4 dispatcher can walk
  `BoundProgram` without re-parsing.
- The control-flow EMISSION strategy stays the backend's concern (sequential paragraph calls now; the dispatcher at
  G4 swaps the strategy without touching the bound tree).

Verified: full-solution build clean Debug (0/0, warnings-as-errors); Conformance 93 + Unit 3 green; legacy oracle
untouched. NEXT → G2-2(b): the mechanical §17 §2.2 emitter decomposition (`EmissionContext` + per-family emitters,
a pure code-move) now that the bound tree is the contract. Then ref-mod / class conditions / G3.

DEVLOG 482.

