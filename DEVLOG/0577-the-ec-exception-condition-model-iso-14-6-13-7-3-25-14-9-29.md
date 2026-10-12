## Entry 577 — 2026-06-11 12:09 PDT — The EC exception-condition model (ISO §14.6.13 / §7.3.25 / §14.9.29/33/49-F3/18) — END TO END (1074 conformance + 29 unit; FULL legacy guard re-run on the grammar change)

**SSOT §16 next-step ① delivered: the COBOL-2002+ EC exception model, complete per the conditions-exceptions
deep-dive D9–D12** (the deep-dive's new "As built" section records the file map + every refinement). A prior
session left a large uncommitted in-flight slice (the runtime `Exceptions/` package, TurnState/TurnDirectiveProcessor,
the binder/emitter partials, grammar+regen) with NO tests, NO DEVLOG, and several unfinished seams — this session
verified it against the spec, FINISHED it, and netted it:

- **What the in-flight slice already had** (verified, kept): the `>>TURN` frontend stage (line-count-preserving,
  post-COPY, `TurnEvent`s anchored to final-text lines); the compile-time `TurnState` fold (§7.3.25.4 GR1–GR8 —
  EC-ALL/level-2 expansion, EC-I-O-WARNING explicit-only, file scoping, last-event-wins, strict succeeding-lines);
  the bind-time `EcWrap` → `BoundEcChecked` (zero-scaffolding: checking-off binds bare nodes); RAISE/RESUME/
  SET LAST EXCEPTION/RAISING binds with the full 0710–0719 + 0875–0879 diagnostics band; USE F3 (SR13/SR14, the
  GR3c–g `__EcDispatch` tier selector); `__IoCheckEc` (the §9.1.13.1 status→EC bridge, F3 behind F1, fatal classes);
  the EC-SIZE name latch through `CobolSizeError.EcName`; `ExceptionCatalog` (Table 13, NAME-keyed — EC-USER-*/
  EC-IMP-* open families), `ExceptionState` (last-exception register + propagation slot + the EC-ARGUMENT-FUNCTION
  ambient gate), `EcFunctions` (§15.28/30/32/33), `ResumeSignal`/`CobolFatalException`, the Main fatal catch.
- **What was BROKEN/MISSING mid-edit** (the interruption point — the tree didn't even compile: `CallEmitExitProgram`
  referenced but never written; MSBuild's up-to-date check masked it): ① `CompilerDriver` never passed
  `frontend.TurnEvents` to the emitter — every TURN was silently dead (the smoke test caught it immediately);
  ② `CallEmitExitProgram` + the GOBACK/EXIT RAISING staging (`CallEmitRaisingStage`: ¶27403 SR2 enforced at bind,
  GR2's no-raise-in-a-main gate, the #8 fatal-with-checking-off loud choice) — written; ③ the ACTIVATOR-side
  propagation pickup — written two-tier: the EC-active CALL site consumes (`TakePropagated` → `__EcDispatch` →
  RESUME honored → fatal default; `siteHandlesPropagation: true` tells the registry to stand down), and
  `ProgramRegistry` applies the boundary default for EC-free callers + `RunMain` (a main's GOBACK RAISING);
  ④ the EC-PROGRAM bridge — `CobolCallException.EcName` existed but NOTHING consumed it: CALL/CANCEL under enabled
  checking now emit a name-FILTERED catch (status + phrase-wins / F3 selection + fatal default); ⑤ `EcEmitOverflow`
  had NO call sites — wired into the shared STRING/UNSTRING overflow dispatch (GR8b/GR16b); ⑥ EC-ARGUMENT-FUNCTION
  had machinery but NO raise points — every §15.3 default-result site in the intrinsic runtime now routes through
  `ExceptionState.ArgumentError` (FromDouble NaN/∞, FACTORIAL, MOD/REM, NUMVAL/-C, the CobolDate range checks,
  CHAR/ORD; same defaults when checking is off — NIST-invariant); ⑦ the EXCEPTION-STATUS/-LOCATION/-STATEMENT/-FILE
  render arms (`IntrinsicRenderer.RenderString` + the EXCEPTION-FILE no-arg catalog row) — the functions were
  catalogued `Runtime` but unrenderable; ⑧ `displayStatement` gained the `functionCall` operand (§8.4.4.1 — an
  identifier includes a function-identifier; `DISPLAY FUNCTION EXCEPTION-STATUS` is the canonical shape) +
  `BindDisplay`'s `IntrinsicOperand` arm; regenerated `Generated/*` committed alongside (the DEVLOG-554 CI rule).
- **Tests (the missing half of the in-flight slice): `ExceptionConditionConformanceTests` — 48 spec-pinned facts**
  (TURN scoping/expansion/diagnostics, RAISE fatal/nonfatal × enabled/off, RESUME NEXT/procedure + SR1–SR3 rejects,
  F3 tier selection incl. the USE AFTER EC spelling, EC-SIZE zero-divide/truncation with receiver-unchanged +
  phrase-wins + fatal default, EC-OVERFLOW-STRING, EC-I-O at-end/EXCEPTION-FILE/phrase-suppression/fatal-35,
  EC-PROGRAM not-found ×3 forms, EC-ARGUMENT-FUNCTION ×3, GOBACK/EXIT RAISING propagation ×5 incl. LAST EXCEPTION
  and the GR2 main-program no-raise, WITH LOCATION on/off, SET LAST EXCEPTION, the 85-edition gates, EC words as
  user-defined words at 85+2023, and the ZERO-SCAFFOLDING invariant asserted on generated source) **+
  `TurnStateTests`/`ExceptionCatalogTests` — 13 unit facts** (the GRs the corpus can't reach: EC-I-O-WARNING's
  explicit-only toggle, file-scoped events, WITH-LOCATION-tracks-the-enabling-event, Table 13 fatalities, the
  §9.1.13.1 map, mask-bit roundtrip). One test-side correction along the way: the two RAISING-propagation programs
  initially omitted the PD-header RAISING phrase — the BINDER was right to reject them (¶27403); fixed the tests
  and added the 0717 diagnostic fact. The legacy oracle has NO EC model: every expectation derives from the cited §.
- **Suites: 1074 conformance (1026 + 48) + 29 unit (16 + 13) ALL GREEN; the FULL legacy guard re-run ALL GREEN
  (353 MATCH + 11 LEGACY_DIVERGENT, 0 regressions) on the shared-frontend grammar change** (RAISE/RESUME/STATEMENT/
  CONDITION/EC/RAISING tokens + cobolWord continuity, USE F3, SET LAST EXCEPTION F13, raisingPhrase/raisingClause,
  the DISPLAY functionCall operand — all pre-authorized NIST-band frontend work, logged here per
  `feedback_autonomous_grammar_nist`). Deep-dive updated in the same change set (rule 4). NEXT (SSOT §16): G7
  per-edition correctness — Track-1 Phase-2 EditionValidator + the M2 2002 catalog (OO largest) → M3 2014 → M4 2023;
  the reserved-word-table scout re-run; then G8.

