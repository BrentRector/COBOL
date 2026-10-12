## Entry 464 — COBOL.NET: the deep, decision-complete DESIGN (docs/COBOLNET_DESIGN.md) — owner-directed reset

Owner: "your planning, design and rearchitecture docs are not very well thought through." Fair — the repeated
corrections (byte State, decimal, BigInteger, paragraphs-as-methods) proved I was designing reactively per-slice.
So I stopped coding and commissioned a real design: two parallel design workflows — (1) 11 subsystem agents
(pipeline, data-model, numeric, control-flow, REDEFINES, strings, files, interprogram, OO, conditions/EC, intrinsics)
each producing a decision-complete design grounded in `specs/ISO_COBOL.md` + the semantics-proven legacy compiler,
then a synthesis with a cross-cutting consistency pass + a dependency-ordered G0–G8 build sequence; (2) a project-
organization design (rename to **Cobol.NET / cobol.exe**, the target solution/folder/namespace layout, extracting
the reusable front-end out of the legacy assembly with history-preserving steps, the no-god-class discipline, C# 14
usage). Combined into **`docs/COBOLNET_DESIGN.md`** (the SSOT, 1300+ lines, §0–§18). The stashed control-flow rework
(paragraphs-as-methods) is abandoned — it was the wrong model.

Key design decisions the deep pass produced (sharper than my reactive ones):
- **No lowered IR — a BOUND SEMANTIC TREE, emit C# directly from it.** C# has if/while/switch/try/GC/exceptions, so
  the legacy CIL-driven basic-block IR is pure waste here AND destroys readable output. The current parse-tree-walk
  emitter is empirically failing (only unqualified refs resolve, conditions fall back to `false`, scale re-derived
  per call-site) — it is superseded.
- **One unified `Place` typed-lvalue** (Read→rvalue, Write(rhs)→store) built once by ReferenceResolver and consumed
  identically by MOVE / arithmetic / INSPECT-STRING-UNSTRING / file READ INTO·WRITE FROM / CALL-by-reference — the
  spine of cross-subsystem consistency (no second lvalue type).
- **Single PC-dispatcher** for control flow (paragraphs = pc cases; GO TO sets pc; PERFORM range = recursive bounded
  dispatch; ALTER = mutable target var — the tie-breaker for switch-on-pc; STOP RUN/GOBACK = the only exception
  signals). Confirms the model the owner steered me to; supersedes paragraphs-as-methods.
- **Differential conformance harness:** run the legacy compiler + CobolNet on each NIST program and assert identical
  stdout — the 364 passing legacy tests become an instant free regression net.
- **Decomposed emitter over a shared EmitContext** (the legacy CilEmitter hit 2458 lines before its 9-way split —
  decompose CobolNet's emitter BEFORE it bloats).

§18 settles all the surfaced open questions per their recommendations (autonomy: sensible-default-and-proceed); the
one with real tension against "no byte substrate" — bytes at the file/REDEFINES-type-pun BOUNDARY only — is decided
per the owner-co-authored ADR (in-memory program data stays 100% typed) and flagged owner-vetoable. `COBOLNET_-`
`ARCHITECTURE.md` demoted to overview + its decimal rows corrected; DOC_INDEX + resume-prompt + CLAUDE.md repointed
at the design. NEXT: implement FROM the design — G0 reorg/rename + front-end extraction + emitter decomposition,
then G2 (bind→bound tree→Place; data model; numerics) per §16, driving NIST via the differential harness.

