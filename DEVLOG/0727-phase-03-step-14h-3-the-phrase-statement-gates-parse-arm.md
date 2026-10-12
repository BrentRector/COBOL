## Entry 727 — 2026-07-09 19:41 PDT — PHASE-03 Step 14h.3: the phrase statement gates → parse-arm

Six phrase gates — whose presence within a statement rule is purely syntactic — move from the bound-arm to the
`ParseArm`:

| Gate | from bound-arm | parse-arm | detect |
| --- | --- | --- | --- |
| `FileSharingClause2002` | `BoundOpen{SharingOverride}` | `VisitOpenStatement` | `openClause().Any(sharingPhrase != null)` |
| `GobackReturning2002` | `BoundGoback{ReturningSource}` | `VisitGobackStatement` | `dataReference != null` AND not in a method |
| `CallByValue2002` | `BoundCallProgram` (args) | `VisitCallStatement` | `callUsingPhrase.callArgument.Any(callByValue != null)` |
| `CallOnOverflowRemoved2023` | `BoundCallProgram` (flag) | `VisitCallStatement` | `callOn/NotOnExceptionPhrase.OVERFLOW() != null` |
| `StopRunStatus2002` | `BoundStop{HasStatusPhrase}` | `VisitStopStatement` (extended) | `stopStatusPhrase() != null` |
| `EndAccept2002` | `BoundAccept{HasEndTerminator}` | `VisitAcceptStatement` | END_ACCEPT token scan |

Each fires ONCE per statement (an `Any` over the clause/argument list where a statement can carry the phrase several
times — matching the binder, which collapsed them to one bound attribute), in the binder's order (CALL BY VALUE
before CALL ON OVERFLOW). The empty `case BoundCallProgram` is deleted from `GateStatement` (its ON-EXCEPTION walk
stays in `Recurse`).

Three subtleties verified against the binder:
- **GOBACK RETURNING excludes METHOD context** — `CallBindGoback` short-circuits to `OoBindMethodGoback`
  (→ `BoundMethodReturn`, ungated) when `InMethod`, so the bound-arm never saw it; a UDF GOBACK is NOT excluded (it
  binds through `CallBindGoback` like a program). `InMethodDefinition(ctx)` walks the parse-tree ancestors.
- **END-ACCEPT is a grammar gap** — `acceptStatement` has no `END_ACCEPT` alternative, so the binder's
  `AcceptHasTerminator` token scan (mirrored here) finds nothing and the gate fires ZERO times, before and after. It
  is kept in the correct home for when the grammar gains the terminator.
- **GOBACK RETURNING is another DEVLOG-724 fix** — an undeclared RETURNING operand returns a `BoundUnsupported`
  before `BoundGoback`, so the bound-arm dropped the 0900; the parse-arm names the edition on presence.

Removed the now-unused `using CobolNet.Runtime;` (its only consumer, the CALL `CobolPassMode` check, moved).
Battery: greenfield conformance 3114 · unit 227 · characterization 32 (fresh build); INV-1-strong + FULL legacy
guard unaffected (greenfield-only, no grammar change). Recon wf_be806171-b25.

