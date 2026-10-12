## Entry 824 — 2026-07-11 20:49 PDT — P7 Step 10n: `Verbs/SetAlterBinder` — the last plain verb

**What (the AS-BUILT PLAN's 10n batch).** The ALTER + SPECIAL-NAMES-switch subsystem became ONE
`SetAlterBinder` over `(ctx, host)` — exactly ONE instance per unit (the lazy host accessor), because the
whole-program ALTER prepass latches `_alterSwFields` against the COMPLETE procedure table. The prepass reads
the table through the NARROW internal host surface added for it — `Paragraphs` (existing) +
`ParaSections`/`CurrentSection` (new; `SectionInfo` made internal) — with the §8.4.2.2 in-section-first
save/mutate/restore around each `host.ResolveProcedure` preserved verbatim; the table + these edges hoist to
ProcedureTableBuilder at 10t. Host forwarders added for the collaborator callers (`SwitchBindSet` ←
SetBinder; `AlterGoTo`/`AlterBindBareGoTo` ← ControlFlowBinder — flip at 10t). `SwitchCondOf` keeps the
level-88-first caller contract (the NC211A guard); the `'_alter_' + Method[2..]` name derivation
byte-identical. The five bound types → records-only `BoundAlterSwitches.cs` (BoundAlter/BoundGoToAlterable
are pass gate anchors).

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

