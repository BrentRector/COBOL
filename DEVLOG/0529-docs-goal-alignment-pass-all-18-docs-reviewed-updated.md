## Entry 529 — 2026-06-10 01:30 PDT — Docs goal-alignment pass: all 18 docs reviewed + updated against the owner's restated goals

**Owner directive (2026-06-10): "review all the architecture docs for prior thinking; update them as necessary to
achieve the goals I've stated."** Executed as two multi-agent workflows: an 18-reviewer parallel REVIEW (one per
doc) producing ~120 anchored findings, merged by a synthesis agent into a cross-consistent edit plan (12
consistency rules R1–R12); then an 18-applier APPLY (one per doc, own input slice each) + a verification gate.
All edits applied, zero skipped. **The four goal axes now stated consistently across the corpus:**
- **G1 — four compilers in one executable** (ISO COBOL 1985/2002/2014/2023 via `--std`, default 2023, `--nist`⇒85):
  every COBOLNET_*_DESIGN deep-dive now carries a "Per-edition gating (G1)" section with the canonical
  two-co-equal-obligations sentence (complete per-edition behavior + correct diagnostic where the edition lacks
  the feature; tests VERIFY, never SCOPE). Wrong §-cites in VERSION_CHANGE_REFERENCE Tables 4/89-90 fixed
  (EXIT→§14.9.14, IF→§14.9.19, SEARCH→§14.9.37); INSPECT BACKWARD corrected 2002→2023 (VCR #77).
- **G2 — commercial/decades/no-back-compat + toolchain:** ".NET 10 / C# 14 — or later; a .NET 11(preview) upgrade
  is pre-authorized when its features advance the goals" now in the toolchain sections; TFM = pinned-and-
  deliberately-bumped, never "fixed forever".
- **G3 — greenfield; legacy = reference+oracle ONLY** (never authority — the spec is; never a fallback substrate;
  deleted at G8): stale "front-end reused from CobolSharp.Compiler"/"Cecil-primary" framings corrected, incl. the
  ISO2023_CONFORMANCE_PLAN's dangerous pre-PIVOT residuals (now LEDGER-titled, §0.5 tombstoned, the G4
  Cecil-primary inversion fixed, stale NEXT-UP blocks marked [STALE — pre-PIVOT]).
- **G4 — dual backend:** the ICodeGenBackend/`--backend roslyn|cil` stance (backend-NEUTRAL bound tree, NO shared
  lowered IR, semantics live in the binder, emitters only render, Cecil/CIL future-additive with private lowering)
  now stated once per deep-dive; `Place.Read()/Write()` documented as the RoslynBackend RENDERING of the
  structural contract. Pre-PIVOT "C#-oriented model"/"parse-tree-direct emitter (no bound nodes)" texts corrected.
- **Hygiene:** settled §18 decisions marked SETTLED inline (R10); M2/M3/M4 labels confined to the conformance
  ledger (R11); the ALTER-deleted-in-2002 vs §18 #10 tension recorded as an owner-visible TODO in the SSOT G7
  block (R12); transcript artifacts deleted from the SSOT; DOC_INDEX refreshed last. The verify agent found+fixed
  a RAW NUL BYTE in COBOLNET_INTRINSICS_DESIGN.md (ripgrep had been silently classifying the doc as BINARY and
  excluding it from every grep — a latent doc-tooling hazard). Status carriers updated to actuals (33 NC / 348
  conformance / 15 unit). Signed-off gate residuals: historical `src/CobolNet` rows inside the executed-G0 move
  record (they ARE the record); `StoreChecked` mentions inside retire/settle notes.
(Some doc edits landed mid-flight inside commit 794c88f's `git add -A` — content identical, split imperfect.)

