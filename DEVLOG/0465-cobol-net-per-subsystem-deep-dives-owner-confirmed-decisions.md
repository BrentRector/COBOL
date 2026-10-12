## Entry 465 — COBOL.NET: per-subsystem deep dives + owner-confirmed decisions + selectable Roslyn/CIL backend

Owner: "every subsystem should have a deep dive." Rendered all 11 subsystem designs + the project-org design from
the deep-design workflow into one canonical deep-dive doc each (`COBOLNET_{PIPELINE,DATA_MODEL,NUMERIC,CONTROL_FLOW,
REDEFINES,STRING_OPS,FILES,INTERPROGRAM,OO,CONDITIONS_EXCEPTIONS,INTRINSICS,PROJECT_ORG}_DESIGN.md` — 15–36 KB each:
decisions + rationale + rejected alternatives + C# mapping w/ examples + hard problems + edge cases + ISO citations).
`COBOLNET_DESIGN.md` §0.5 indexes them; it stays the SSOT for invariants/cross-cutting/settled-decisions/build-order.

Owner reviewed the §18 settled decisions; the four consequential forks were CONFIRMED on the recommended options:
mixed-USAGE REDEFINES → a tightly-confined byte[] pun (in-memory program data stays 100% typed); control flow →
PC-dispatcher for v1, idiomatic "pretty pass" later; arithmetic → native only (STANDARD-DECIMAL out of scope);
conformance → the differential harness (legacy vs COBOL.NET identical stdout = 364 free regression tests). The
boundary-bytes clarification (file serialization + the REDEFINES pun) is recorded in memory to prevent re-litigation.

NEW owner requirement: **keep a SELECTABLE code generator — Roslyn or CIL.** Codegen is now behind an
`ICodeGenBackend` over the **backend-neutral bound tree**, chosen by `--backend roslyn|cil` (default roslyn):
RoslynBackend (idiomatic C# source — v1) and CilBackend (typed-native CIL via Mono.Cecil — future-additive, no
C#-compile step / no Roslyn dependency, for AOT/direct-IL). The CIL backend's structure→branch lowering is private to
it — there is still NO shared lowered IR (the bound tree replaces it; each backend lowers only as far as its target
needs). The harness can cross-check the two backends. Design SSOT §1.1/§18 + the architecture overview updated.

