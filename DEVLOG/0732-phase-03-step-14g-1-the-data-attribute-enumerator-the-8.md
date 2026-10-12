## Entry 732 — 2026-07-09 21:44 PDT — PHASE-03 Step 14g.1: the data-attribute enumerator + the 8 PicInfo USAGE gates → bound-arm

The first 14g commit: the eight PICTURE/USAGE-category introduction gates (national / boolean / pointer /
object-reference / binary-char-family / float-trio, ISO §13.18.60) move from the bind-time `PicInfo.ParseUsage`
(11 Check lines) + `PicInfo.Analyze` (the PIC N / PIC 1 legs) to a NEW bound-arm enumerator in
`VersionConformancePass`, keyed on the RESOLVED `DataItem` — genuinely semantic (a resolved USAGE/PICTURE category,
not parse presence), so the bound-arm is the correct home.

- **`DataBinder.ConformanceForest()`** (new, public) — the SOURCE-declared forest: `Roots` (WS + FILE + OO-method)
  ∪ `LinkageRoots` ∪ `TypeDecls` templates (the latter two are OFF `Roots` — the pre-existing private `AllItems()`
  missed them), pre-order DFS, EXCLUDING the post-bind expansion products the binder never re-analyzed: a `TYPE IS`
  clone subtree (`DataItem.TypeAnchor != null`) and the OO/UDF compiler temps (`CompilerTempClones`).
- **`VersionConformancePass.GateData`** walks that forest + the RD printable items (`Reports → Groups → Lines →
  Fields → PrintItem`, off the forest), gating each via `UsageGateId` on `(OwnUsage, Pic.Category, Pic.Usage)` —
  `OwnUsage` mandatory (a group-header USAGE sheds `Pic` to null), keyed on the `Usage` MEMBER (never
  `IsFloat`/`ClrType` — `FloatLong`/`FloatExtended` share a `double` ClrType with COMP-2). Scope-aware where-strings
  (`"data item '…'"` main / `"RD '…' printable item '…'"` report) reproduced verbatim.

**Dedup is structural, not `PicInfo`-identity** — `PicInfo.PointerItem` is a static singleton shared by every
pointer item, so an identity dedup would collapse them (under-count). The `TypeAnchor`/`CompilerTempClones`
exclusion fires exactly once per source declaration.

`LoudGuardTests` (Unit) rewritten off its direct `PicInfo.ParseUsage`/`Analyze` gate assertions (the gate no longer
fires at the parse layer) — it now asserts the pure MAPPING/CLASSIFICATION; the E-symbol external-float PICTURE
stays a parse-layer `NotImplementedSkeleton` gate (Phase 6 / 14g.5). New `UsageDataEditionTests` (Conformance) pins
the FIRING COUNT the contains-based suite can't: one 0900 per source item, two distinct pointers → two 0900s (the
singleton proof), a TYPEDEF national member referenced twice → gated ONCE (the clone-exclusion proof). 8 witnesses.

Battery: greenfield conformance 3114 · unit 223 · characterization 32 (byte-exact snapshots UNCHANGED — the
enumerator reproduces the bind-time firing exactly) · 8 exact-count witnesses. Recon `wf_0d98d218-087`.

