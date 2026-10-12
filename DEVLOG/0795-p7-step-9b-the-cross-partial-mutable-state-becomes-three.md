## Entry 795 — 2026-07-11 16:09 PDT — P7 Step 9b: the cross-partial mutable state becomes THREE typed per-scope objects (`DispatchState` / `EcState` / `CallUnitState`)

**What (the phase doc's 9b; the audit's explicit-threading risk answered).** The 14 mutable fields the partials
shared blind — the decomposition's central hazard: a collaborator class can't reach another class's privates, and
IMPLICIT sharing was the god-class disease — become three cohesive state objects in NEW `CodeGen/EmitterState.cs`,
each field carrying its original doc contract (the ISO citations preserved):
- `DispatchState` — `CurrentPc`, `SentenceEndLabel`, `DispatchName` (the `__Dispatch`/`__MDispatch` OO swap),
  `UseDecls`, `OuterGlobalUse` (was 5 fields over 4 partials);
- `EcState` — `Active`, `UnitHasF3/F4`, `Info`, `SizeErrVar`, `SizeErrEcVar` (the EC↔arithmetic statement-scratch
  interlock now travels as ONE object; was 6 fields over 2 partials, read by 5);
- `CallUnitState` — `SelfPath`, `ReturningPlace`, `InheritedStatusPlace` (the §12.4.5.8.4 GR1 NOTE 1 routing).
Pure mechanical re-pointing (~70 sites) — object lifetime = the emitter instance, EXACTLY the replaced fields'
lifetime (no per-unit re-newing: e.g. `UnitHasF4` is deliberately NOT reset by the OO class-unit path — class
units emit before program units, so the initial false serves; re-newing would have been a silent semantics
change). The collaborators of 9c+ receive these objects by ctor — never a host-private field.

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

