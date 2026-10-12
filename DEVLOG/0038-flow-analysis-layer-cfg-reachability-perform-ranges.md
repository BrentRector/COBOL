## Entry 038 — 2026-03-14: Flow Analysis Layer — CFG, Reachability, PERFORM Ranges

User provided layered flow analysis design: CFG first, then definite assignment, then
PERFORM/unreachable.

Implemented:
- BasicBlock + ControlFlowGraph: entry/exit blocks, successor/predecessor edges
- ParagraphReachabilityAnalyzer: depth-first reachability from entry, warns on
  unreachable paragraphs
- PerformRangeChecker: validates PERFORM A THRU B (start before end in declaration order)

These are ready to wire into the binder when BoundStatement types are implemented.
The definite assignment analyzer (dataflow over CFG with bitsets) is designed but
deferred until the binder produces bound trees.

CIL emission will use Mono.Cecil 0.11.6 (already in .csproj from the original project).

---

