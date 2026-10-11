## Entry 815 — 2026-07-11 19:46 PDT — P7 Step 10e: the MOVE-family tier — `Verbs/{Move,Corresponding,Initialize}Binder` + the CheckStrongMove/CheckComposite/INITIALIZE lifts

**What (the AS-BUILT PLAN's 10e batch — the heaviest so far).**
- **`Verbs/MoveBinder`** — `BindMove` moved off the core AND the whole `.MoveFigurative` partial absorbed
  (the census: a pure leaf over `data`): the SR1 class-index errors, the SR5 pre-removal `MarkImageForced`
  marking, the Table-16 legality arms, and the W2 ref-mod image marking fire at the SAME per-statement
  points — the caller sequence Gates → Legality → MarkRefMod → StrongMove is byte-preserved (the
  `StorageFormPass` + `VersionConformancePass.GateMove` choreography the census flagged). Format 2 routes to
  `corr.Bind(CorrVerb.Move, …)` by ctor-injected collaborator ref.
- **`Verbs/CorrespondingBinder`** — the ONE `_corrCounter` owner (hoist numbering spans all three verbs —
  exactly one instance per unit, the snapshot hazard); `CorrAccess` moves wholesale; the `:219` ≥2002
  pair-selection window STAYS in-binder (BEHAVIORAL semantics — the sanctioned in-binder edition read, per
  the plan block); the 4 bound types → records-only `BoundCorresponding.cs`.
- **`Verbs/InitializeBinder`** — the ONE `_initializeLoopVar` owner; `ActiveScope` read through ctx PER
  CALL (never captured); the 0830–0833 2002-surface gate block VERBATIM; the nested Spec/Cursor types move
  wholesale; the 7 bound types → records-only `BoundInitialize.cs`.
- **StatementValidation grows four lifts:** `CheckStrongMove` (SR2/§8.5.3.3), `CheckComposite` (§14.7 r2 —
  ALL 12 call sites retargeted: the core arithmetic binders via `Ctx.Validation` + Corresponding's per-PAIR
  site, granularity preserved), `CheckInitializeReplacingUnique` (SR6 — the `continue` stays in the binder),
  `CheckInitializeTargetRenames` (SR5). Host spine bumps: `ResolveTargets`/`RoundingOf`/`BindSizeError`/
  `IntrinsicOperand` internal.

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact — the counter-lifetime and
hoist-order preservation proven) · 281 unit · 3166 conformance — verdicts read as separate actions.

