## Entry 633 — 2026-07-06 18:30 PDT — Phase 5: FUNCTION MODULE-NAME (§15.65) — LANDED COMPLETELY (with a real runtime module call-name stack)

MODULE-NAME returns the name of a runtime element of the running COBOL hierarchy — CURRENT / ACTIVATING /
NESTED / STACK / TOP-LEVEL (§15.65.2). The substrate gap was real: the runtime had a program *registry* (name
resolution + state) but **no ordered call-name stack** of currently running elements (`Node.Active` is a count,
not a history). Per the owner's *no residue* directive, that stack is BUILT completely — not stubbed. (Design
from the recon workflow wf_840f8070-fdf; runtime anchors verified before implementing.)

- **`CobolModule`** (new `src/Cobol.Net.Runtime/Control/CobolModule.cs`) — a thread-static frame stack; each frame
  carries the element's name, its compilation unit's outermost program-id (§15.65.4 r7 CURRENT), whether it is a
  contained/nested program (r8 NESTED), and whether it is the run-unit main (r5/r10). `Name(kind)` resolves all
  five keywords: CURRENT = top's outermost (r7); ACTIVATING = a single space in a main program else the frame
  below (r5/r6); NESTED = the most-recently-nested frame (r8); STACK = `CURRENT;<activators…>;TOP-LEVEL; `
  (r9 — semicolon-separated, ending in the operating-environment space); TOP-LEVEL = the bottom frame (r10). A
  dynamic-length result (r1) → trimmed; EC-BOUND-FUNC-RET-VALUE (r2) is vacuous for a dynamic-length item.
- **The push/pop seam is the ONE activation choke point** in `ProgramRegistry`: `RunMain` pushes the main frame,
  `CallProgram` pushes a frame for every resolved node (same-assembly / dynamic / cross-assembly CALL, UDF
  references, AND contained-program CALLs all funnel here), both popped in a `finally`; `Reset` clears the stack
  at run-unit start. `OutermostName(node)` walks the containment chain for CURRENT.
- **`BindModuleName`** extracts the lone keyword segment (TOP-LEVEL is one hyphenated SUB_IDENTIFIER token) onto
  `BoundIntrinsicCall.ModuleNameKind`; the renderer emits `CobolModule.Name(kind)`. NESTED outside a contained
  program is rejected at compile time — **COBOLNET1515**, gated by a new `StatementBinder.InNestedProgram`
  (`CallUnit.Parent is not null`, §15.65.3 argument rule 1). OO-INVOKE frame pushes are the documented single-
  line extension point (conformant per r3/r4 — method-id form is implementor-defined — not residue).

Golden `tests/conformance/2023/module_name.cob` (MAINMOD → a contained NESTPROG + a separate SUBMOD, proving
CURRENT/ACTIVATING/TOP-LEVEL/NESTED/STACK across the CALL chain), **GreenfieldOnly**. +6
`IntrinsicFunctionDifferentialTests` (the three main-program keywords, ACTIVATING+STACK across a CALL, the 2014
gate → 1502, the NESTED arg-rule-1 → 1515). Battery: **1916 conformance (+6) · 216 unit · 114 corpus goldens
GREEN**; greenfield-only. Phase-5 intrinsics live: CONCAT/BASECONVERT (628), TRIM (629), FIND-STRING (630),
SUBSTITUTE (631), CONVERT (632), MODULE-NAME (633); residue = SMALLEST/HIGHEST/LOWEST-ALGEBRAIC + the 2014 date
family, each to be done in full when reached.

