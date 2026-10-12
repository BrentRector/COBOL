## Entry 892 — 2026-07-18 13:12 PDT — PHASE-13 remaining-waves re-scout + STOP/GOBACK status→exit-code slice (VCR 75)

Resumed Phase-13 on `phase-13-m4-2023`. Two commits.

**(1) The remaining-waves re-scout (docs).** Ran a 9-agent parallel spec-first anchor re-scout of every remaining
P13 wave (Wave C residuals, D/E/F/G/H) → `docs/rearchitecture/PHASE-13-remaining-waves-scout.md`, the trusted
worklist ([[feedback_persist_anchor_rescout]]). Each wave was independently derived from `specs/ISO_COBOL.md` with
grep-verified line numbers, CLI-probed, and adversarially checked against `PHASE-13-audit.md`. It caught fresh audit
drifts before any code was written: PICTURE EDITING renders via **Table 9** not Table 8, its intro leg is Annex
**E.3.3 item 19** not item 1, and **EDITING is a NEW 2023 reserved word** (needs a COBOLNET0901 user-word gate like
XOR/COMMIT); the reference-mod GR is **§8.4.2.3** not the audit's §8.4.2.4 (which resolves to numeric comparison);
EC-BOUND-OVERFLOW is **§8.5.1.9.6 GR1** (first-crossing-only, with the "already-exceeded ⇒ no exception" edge) not
§8.5.1.9.1. Also refreshed the master roadmap (`COBOLNET_REARCHITECTURE_PLAN.md`) STATUS banner + §4 Phase-13 row,
which were stale ("Wave B done; resume at Wave C").

**(2) STOP/GOBACK status → process exit-code wiring (VCR 75).** The first greenfield wave off the re-scout: the
`STOP RUN` / `GOBACK … WITH {NORMAL|ERROR} STATUS [value]` phrase had bound presence-only; now its value reaches the
process exit code (ISO §14.9.42.4 GR5 / §14.9.18.4 GR3/GR10). Independent spec read confirmed the derivation (GR5
@spec:32250, GR3 @27745, GR10 @27765). On .NET the sole observable is `Environment.ExitCode`, so the STATUS value and
the ERROR/NORMAL indication collapse into ONE run-unit field `RunUnit.ExitStatus` (the documented implementor mapping,
Annex A required items 192/193 — recorded in `CONFORMANCE.md` §3): value-when-present, else ERROR⇒1 / NORMAL⇒0. A
main-program GOBACK is STOP-equivalent (GR3); a called-program GOBACK status is inert (GR2) — the emit guards it with
`!__asCalled`. Modeled `TerminationStatus(bool Error, BoundExpr? Value)` on `BoundStop`/`BoundGoback`; the Main
exit-code sink is gated on a bind-computed `BoundCompilation.UsesTerminationStatus` (a parse-tree `statusPhrase` scan)
so a status-free program's entry wrapper stays byte-identical (zero-scaffolding, SSOT §18.16). Corrected DESIGN
decision 20 (RETURNING/GIVING is the activation result §14.9.18.4 GR2, NOT the exit code; the status rides the
run-unit field, not a `ProgramReturn` payload — the design sketch was as-designed-not-as-built). Added
`StopGobackExitCodeTests` (11 facts asserting the NUMERIC exit code — the golden harness only checks `ExitCode==0` as
a bool; `CutRunner.RunExit`/`CompileAndRunExit` expose the value).

**Verification.** CLI-probed all 8 spec cases — every exit code matched the hand-derived value (ERROR STATUS 42→42,
bare ERROR→1, NORMAL STATUS 7→7, plain→0, data-item(13)→13, main GOBACK→5, called-GOBACK inert→0); below-edition
gates still reject (COBOLNET0900). Battery: greenfield conformance **3633** (+11) · unit **311** · characterization
**33** (no drift). Greenfield-only (no shared grammar / no legacy compiler touched); guard-fast's NIST leg = 350
genuine MATCH + the 3 pre-existing known states (ST122A/ST123A `pending`, ST146A `LEGACY_DIVERGENT` §14.9.30 GR18) —
zero regressions (the diff cannot touch the legacy path).

**Misstep (transparency).** First CLI probes used `cobol run file.cob` (the scout doc's shorthand); the actual CLI
is `cobol <source> --run`. Corrected after the first run dumped usage help — a reminder to verify tool invocation
against `--help`, not a scout doc's convenience phrasing.

