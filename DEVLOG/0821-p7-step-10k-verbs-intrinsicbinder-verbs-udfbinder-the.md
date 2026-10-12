## Entry 821 — 2026-07-11 20:34 PDT — P7 Step 10k: `Verbs/IntrinsicBinder` + `Verbs/UdfBinder` — the bidirectional pair lands together

**What (the AS-BUILT PLAN's 10k batch — the largest remaining verb pair, 905+237 lines).** Both partials
became real collaborators in ONE batch, exactly because of the §12.3.8.2 GR12 bidirectional coupling: the
IntrinsicBinder's UDF-first dispatch (BEFORE the catalog lookup — the order invariant preserved verbatim)
reaches `host.Udf.UdfBindCall`, and UdfBinder's argument parse reaches BACK into
`host.Intrinsic.ParseArgSegment` (both lazy host accessors — always wired). Load-bearing decisions:
- **`CompileClock` relocated onto IntrinsicBinder** with the ONLY external consumer (IntrinsicRenderer's
  WHEN-COMPILED literal, 2 sites) re-pointed same-commit — no test injectors existed.
- **The injection surface STAYS on the host:** `UserFunctions`/`UdfSelfName`/`InNestedProgram` carved OUT of
  the moving files into the core partial — BinderDriver's object-initializer contract is untouched (the
  null path is load-bearing, COBOLNET1505); re-homing is 10t's business.
- **`_udfPendingCalls` moved INTO UdfBinder** with the mark/drain suffix protocol exposed as
  `PendingCount` — the host's BindStatement / BindFlatSequence chokepoints mark and drain through it, so
  the §8.4.3.2.4 GR2 registration-order semantics and the property-op wrap nesting are byte-preserved.
- **The line-65 `ConstructRegistry.Check` moved VERBATIM** — THE documented bind-time gate exception (fires
  on RECOGNITION, pre-hoist); the D8 IntroducedIn/RemovedIn windows (1502/1503), the <2002
  keyword-omitted PARSE-ROUTING gate (a behavior switch, not a diagnostic — stays in-binder by design),
  and TRIM-arg2 (2023) all verbatim.
- **The recursive-descent arg parser stays ONE cohesive block** inside IntrinsicBinder for Step 12's
  deletion; `OperandOf` is public static (the RefExpr chokepoint calls it type-qualified);
  `EcNoteFunction` internal (the group EC gate still flips through the host's Exceptions partial).

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

