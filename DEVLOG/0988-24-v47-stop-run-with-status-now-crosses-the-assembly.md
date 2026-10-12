## Entry 988 — 2026-07-22 16:33 PDT — §24 V47: STOP RUN … WITH STATUS now crosses the assembly boundary (runtime-side flush; a net deletion)

Started the §24 misc-semantics majors batch (spec-first, one at a time) with **V47** — a `STOP RUN … WITH STATUS`
executed in a **separately-compiled CALLed module** was silently discarded (process exit 0).

**Root cause (traced end-to-end before touching code).** `RunUnit.Current` is a process-ambient `AsyncLocal`, so the
sub's `SetExitStatus(16)` DID write the shared `RunUnit.ExitStatus`, and `StopRun` DID unwind through the CALL back to
the main assembly's `Main` wrapper. The bug was purely the EMISSION GATE: the `Environment.ExitCode` sink was emitted
only when `usesTerminationStatus = HasStatusPhrase(mainGroupTree)` — a compile-time parse-tree scan of the MAIN group.
A status phrase living solely in a sibling assembly is invisible to that scan ⇒ no sink ⇒ exit 0. In a SINGLE
compilation group the bug never manifests (the scan sees the sub's phrase), which is exactly why the existing inline
tests all passed — the hole is only reachable across an assembly boundary.

**Fix = the finding's second option (runtime-side always-on flush), NOT the unconditional-emitter option.** The
emitter can never know at compile time that a *different* assembly will set a status, so the only shape that fixes
cross-assembly WITHOUT per-`Main` scaffolding is to flush in the runtime. `RunUnit.ExitStatus`'s SETTER now does
`_exitStatus = value; Environment.ExitCode = (int)value;` — one field, one flush point, at the write site (§14.9.42.4
GR5/GR6). The compile-time `HasStatusPhrase` scan, the `usesTerminationStatus` flag threaded through
`BoundCompilation`, and the per-`Main` `setExit` string are all DELETED. Net effect: the entry wrapper is now
UNCONDITIONALLY scaffolding-free (upholds §18.16 more purely than the gate did), and the change is code-negative.

**Why the alternatives lose.** Unconditional-emitter (Shape A) would add `Environment.ExitCode = …` to every program's
`Main` — re-baselining every characterization snapshot and *adding* scaffolding (violates §18.16). Verified the
setter-flush has zero in-process pollution surface (no test/code writes `ExitStatus` in-process; generated `Main`
runs only in child processes) and zero `.g.cs` blast radius (grep: no committed generated-code golden carries the
`setExit` line — they all end in plain `STOP RUN`). Traced the interacting paths: GOBACK-in-main → `ProgramReturn`
caught at the dispatcher → normal `RunMain` return (flush already happened at the GOBACK `SetExitStatus` site);
fatal-EC → separate `Environment.ExitCode = 1` (abnormal termination, §14.6.12 — distinct from the STOP status, kept
separate); no-status → `ExitStatus` never written → exit 0 unchanged.

**Tests (`StopGobackExitCodeTests`).** New two-assembly fix-prover: `V47MAIN` (no status) CALLs sibling `V47SUB` →
`STOP RUN WITH ERROR STATUS 16` ⇒ **exit 16** (was 0). New in-group companion ⇒ exit 24. The 10 inline STOP/GOBACK
cases + the GOBACK-inert case stay green (**22/22**). Characterization **33/33 byte-identical** (status-free `Main`
unchanged). Full greenfield **Conformance 3855/3855, 0 regressions** (3853 baseline + the 2 new exit-code tests).

**Adversarial review (`wf_ee139367-102`, 4 refuters + a completeness critic).** All four refutation lenses
(cross-edition · alternate-writer/regression · host-pollution/concurrency · spec-fidelity vs §14.9.42/§14.9.18)
returned SOUND — the fix holds. The completeness critic earned its keep: it found **two SAME-CLASS siblings** of
V47 — a run-unit-termination observable emitted PER-MAIN and gated on a compile-time scan of the main group, so it
too is blind to a separately-compiled sibling. **V52** = the fatal-EC abnormal-termination surface (the
`abnormal run-unit termination …` diagnostic + `Environment.ExitCode = 1`), gated on `_ecState.Active`; a
`CobolFatalException` from a class's `__CobolInvoke`, a `CobolPtr` null deref, or a checking-on sibling escapes an
EC-free main as a raw CLR crash (§14.6.11 CloseAll bypassed) instead of the documented surface. **V53** = the
§14.6.11 implicit CLOSE + `FileInit`, gated on `anyFiles`; a file-less main that CALLs a file-writing sibling never
closes the sibling's connectors at termination (silent data loss). Both filed as §24 items and set as the
immediate next work — and NOT folded here: their fix must be runtime-side (mirror V47), because an unconditional
per-`Main` catch/CloseAll would re-scaffold every program's `Main` and violate §18.16 (unlike V47, which was
byte-neutral). Also reconciled the raw-capture ledger item **R13 → V47 LANDED** and documented the host int32/8-bit
exit-code clamp in `CONFORMANCE.md` §4.2.16. Docs swept: `RunUnit.ExitStatus` XML-doc, `COBOLNET_DESIGN.md` §§
(decision 20 + the two prose spots), §24 ledger (V47 LANDED, R13 reconciled, V52/V53 filed), plan §0.

