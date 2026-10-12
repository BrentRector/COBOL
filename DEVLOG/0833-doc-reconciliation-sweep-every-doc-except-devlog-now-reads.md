## Entry 833 — 2026-07-12 00:45 PDT — Doc reconciliation sweep: every doc (except DEVLOG) now reads as CURRENT state

**Why.** Owner directive: other than this DEVLOG (which keeps the historical stages), ALL docs must reflect the
actual current state of the compiler — architecture/design docs do NOT need "how we got here" history. After the
Step 9/10 god-class dissolutions the doc corpus had accumulated heavy change-narrative (commit hashes, DEVLOG
pointers, "was X → now Y" reconciliations, "as-built"/"landed"/phase-tag provenance) plus stale present-tense claims
(dissolved god classes described as current, moved file paths, old battery counts).

**What changed.** 48 docs edited (+1404/−1576 — net history removed). The three most history-dense LIVE control docs
were rewritten by hand to lean current-state: `resume-prompt.md` (the forensic 10a–10t / hash log → a lean "Steps
1–10 done, RESUME AT Step 11" banner), `CLAUDE.md` (two big status snapshots trimmed to current), and `PHASE-07`
(its ~11k-char forensic STATUS block → a current-state STATUS; §1 preconditions, stale counts, the `Binding/Model/
Place.cs` path, and Appendix A's caption corrected — the completed Step 1–10 bodies left as the phase's execution
record, consistent with the owner leaving completed phases historical). The remaining ~45 design/architecture/ledger
docs were current-state-ified by a rewrite→verify workflow (surgical edits: strip provenance, fix stale claims,
preserve every design decision + ISO § citation; each verified against the tree). CONSTRAINTS.md's IR-layer doctrine
(contradicting the locked no-lowered-IR design) and its deleted-"Migration Ledger" references were repointed to the
bound-tree model + DEVLOG/resume-prompt. README/SSOT/all deep-dives now name the decomposed emitter/binder and the
`Binding/Model/Place.cs` / `EmitContext` / `RuntimeApi` / `GroupBindContext` current types.

**Left as historical (owner's call):** DEVLOG, the completed `PHASE-00..06` guides, the `SURVEY-*`/`CRITIQUE-*`
analyses, `PLAN-bindtime-gating`, `EVAL-antlr`; generated docs (`DIAGNOSTICS.md`, `VERSION_CHANGE_REFERENCE.md`'s
drift-guarded block) untouched. No code changed; no test touched — the drift-guarded generated docs were excluded, so
`VcrDriftTests`/`DiagnosticRegistryDriftTests` are unaffected. Process rule 4 (CLAUDE.md/resume-prompt) updated: docs
state the current design; the historical narrative lives ONLY here.

