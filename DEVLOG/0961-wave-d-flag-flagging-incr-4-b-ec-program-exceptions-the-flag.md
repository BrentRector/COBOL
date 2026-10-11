## Entry 961 — 2026-07-21 19:55 PDT — Wave D: FLAG flagging Incr 4 — b EC-PROGRAM-EXCEPTIONS; **THE FLAG SUBSYSTEM IS COMPLETE (all option-detectors)**

**Detector 17 of 19 — the LAST FLAG option.** FLAG-02 **b EC-PROGRAM-EXCEPTIONS** (§7.3.14.4 GR4 b): a `>>TURN`
directive for **EC-ALL / EC-PROGRAM / EC-PROGRAM-ARG-OMITTED / EC-PROGRAM-NOT-FOUND** is flagged when its source
element **(1) calls any function** or **(2) invokes any method** (per spec — CALL of a program is deliberately NOT
included; the two triggers are function activation and method invocation).

**Structural note — this flags a DIRECTIVE, not a statement.** The `>>TURN` occurrences are frontend events (not
parse nodes), so the detector runs POST-walk: a new `TurnState.DirectiveLinesNaming(ecNames)` exposes the lines of
`>>TURN` directives naming any EC-PROGRAM-family name (ON or OFF alike; the raw events store the canonical name
as-written, so a level-2 `EC-PROGRAM` matches without hierarchy expansion). During the walk, `VisitFunctionCall` /
`VisitInvokeStatement` / `VisitInlineMethodInvocationStatement` record the CURRENT unit's `ProgramUnitContext` in
`_unitsWithCall` (the per-unit `_currentUnitCtx` is save/restored in `VisitProgramUnit`, so a call in a NESTED
program is attributed to the nested element, not its container). `FlagEcProgramDirectives` then maps each directive
line to its INNERMOST containing unit (max Start.Line among the units whose span covers the line) and flags it if
that unit is in `_unitsWithCall`.

**CLI-probed all cases**: ✅ `>>TURN EC-PROGRAM` + `FUNCTION MAX` → flagged · ✅ `>>TURN EC-ALL` + FUNCTION → flagged ·
✅ `>>TURN EC-PROGRAM-NOT-FOUND` (level-3) + FUNCTION → flagged · ✅ `>>TURN EC-PROGRAM` with no function/method → NOT ·
✅ FUNCTION but `>>TURN EC-SIZE` (non-family) → NOT · ✅ directive OFF / no directive → NOT. 5 unit tests added
(`FlagDirectiveTests` 68→73). Wave-local gate green: fresh build + FLAG 73/73 + characterization **33/33 byte-exact**;
full greenfield unit suite re-run (touched the shared `TurnState` — a pure read-only accessor addition).

**⭐ THE FLAG-02 / FLAG-14 MIGRATION-FLAGGING SUBSYSTEM IS COMPLETE** — all 17 option-detectors landed (FLAG-14 b–m,
FLAG-02 b–f) plus the 2 directive-word edition gates (Incr 0b) = the full 19-item census. `docs/rearchitecture/
DESIGN-flag-directives.md` is now IMPLEMENTED. Remaining Wave-D residue (a separate track): >>COBOL-WORDS ·
CC-directives-inside-COPY · the PERFORM Format-3 runtime interceptor · the §24 fix-queue → Wave I merge.

