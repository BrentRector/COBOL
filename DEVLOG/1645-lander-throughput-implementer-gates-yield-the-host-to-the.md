## Entry 1645 — 2026-09-22 17:19 PDT — Lander throughput: implementer gates yield the host to the lander (BelowNormal, never the whole assembly), and landing is pipelined

**Measured.** The lander is the campaign's serial bottleneck — train 47's took 84 min — and most of that is waiting.
Its whole-Conformance leg (~7,900 cases) measured **9.6 min on a quiet host, 18.5 min at battery #84 and 30.6 min in
train 48**: up to seventeen implementers were running the SAME whole assembly on the same 32 cores, because the
dispatch-spec tail had drifted from the brief ("shared seam → run the WHOLE assembly") while the brief and the
2026-09-13 owner decision only ever asked for `~CorpusRunner` + `~Nist`. Add ~25–30 min of `push-main.sh` watching CI
and a re-gate after almost every train's first red, and a train takes ~1.5 h to land while waves arrive every ~1.5–2 h.

Owner decision 2026-09-22 ("do all that we can"):
- `scripts/build-local.ps1 -Priority BelowNormal|Idle` sets the gate's own priority class; Windows passes those two
  classes (and only those) down to child processes — probed: a BelowNormal pwsh starts a BelowNormal child — so the
  build, every test host and every compiled COBOL program yield to the lander's Normal-priority gate.
- Implementer briefs (`fix-lane-implementer-brief.md`, `implementer-brief.md` step 7) and the dispatch tail: ALWAYS
  `-Priority BelowNormal`; NEVER the whole Conformance assembly (a shared seam adds `~CorpusRunner|~Nist` only).
- `workstream` SKILL.md: pipelined landing (the next lander merges and gates while the previous train is in CI, and
  blocks before push-main until that train is on origin/main), trains at 4–6 clusters, and the compiled-program cache
  for the Conformance runner, filed as kb/Work PB985 and dispatched now.

