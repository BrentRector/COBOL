## Entry 733 — 2026-07-09 22:04 PDT — PHASE-03 Step 14g.2: the data-description-clause gates (TYPEDEF → bound-arm; BASED/TYPE/PROPERTY → parse-arm)

The second 14g commit: the four data-description-clause introduction gates move from the bind-time
`DataBinder.BindEntry` clause loop (four `ConstructRegistry.Check` lines) into `VersionConformancePass`, split by
whether each carries a RESOLVED bound fact that survives declaration errors — the load-bearing finding of the recon
(`wf_0d98d218-087`).

- **TYPEDEF → BOUND-arm** (`GateData`). `DataItem.IsTypedef` is an `init`-only flag, never cleared: a level-01
  template lands in `TypeDecls`, a misplaced subordinate typedef keeps the flag in its `Roots` subtree (and also earns
  COBOLNET1529). `ConformanceForest()` yields both, so `if (item.IsTypedef) Check(TypedefDef2002, "the TYPEDEF clause")`
  fires exactly once per source TYPEDEF — the constant where-string, matching the former BindEntry Check byte-for-byte.
- **BASED / TYPE / PROPERTY → PARSE-arm** (recognition). Their bound carriers are cleared or nulled DURING bind, so a
  bound-arm gate would silently drop the 0900 (the DEVLOG-724 flaw): `DataItem.IsBased` is reset to `false` for a
  LINKAGE item (`DataBinder.Linkage.cs`), `DataItem.TypeRefName` is nulled by `ExpandTypes` the instant the clone is
  materialized, and the PROPERTY identity is consumed entirely by `OoBindPropertyClauses` (no persisted flag). Three new
  `ParseArm` overrides (`VisitBasedClause`/`VisitTypeClause`/`VisitPropertyClause`) fire on the single
  `dataDescriptionClause` node — the parse node IS the identity — each with the former binder site's exact
  constructId + where-string. The PROPERTY storage-loop branch (which only gated; the OO semantics were always in
  `OoBindPropertyClauses`) is DELETED.

**Byte-neutral, PROVEN.** The characterization diagnostic surface is content-SORTED
(`CharacterizationCorpus.FormatDiagnostics` → `OrderBy(Ordinal)`), so moving a gate between arms cannot reorder it —
`char_neg_typedef85` (which fires both a TYPE 0900 and a TYPEDEF 0900 at 85) stayed byte-identical, NO re-baseline (the
14g plan's predicted re-order was conservative — the sort makes firing order moot). The four gates now reference from
EXACTLY the pass + the `GateId` registry mapping; zero binder references.

New `DataClauseEditionTests` (Conformance, 6 witnesses) pins the FIRING COUNT the contains-based suite can't: one 0900
per BASED/PROPERTY/TYPEDEF at 85 (zero at 2002); a misplaced subordinate TYPEDEF still gated once; and the load-bearing
dedup case — a TYPEDEF referenced twice gates TYPEDEF ONCE (the template; the two clone subtrees excluded via
`TypeAnchor`) and TYPE TWICE (one parse node per written reference; clones are DataItem objects, not parse nodes).

Battery: greenfield conformance **3128** (3122 + 6) · unit **223** · characterization **32** (byte-exact, unchanged) ·
INV-1-strong @ 2023 permissive **349/349** · FULL legacy guard **NIST 353 MATCH / 0 regressions** (legacy unit 1196 ·
integration 608) — the `.g4` edits are comment-only, so the regenerated parser is byte-identical. RESUME AT Step 14g.3
(OO class/interface + OCCURS-DYNAMIC → parse-arm).

