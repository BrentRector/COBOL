## Entry 590 — 2026-07-03 21:10 PDT — ⛔🎉 ROADMAP PHASE 1 COMPLETE — CI sweep wired, the Validation deep-dive, the corpus runner shells; every exit criterion satisfied

**The Phase-1 closeout lands and the ratified roadmap's Phase 1 (EditionValidator Wave 1) is DONE in one
session — every exit criterion:**
- ✅ P2.1–P2.7 green (DEVLOG 583–589); ✅ the permissive continuity sweep 419 OK / 0 BREAKS / 40 SKIP85 at all
  four editions (every strict failure traces to the edition band); ✅ the 2023-permissive golden triage
  complete — **349/349 byte-exact at the default edition, zero behavioral diffs**; ✅ both drift disciplines
  green over the scrubbed metadata; ✅ the runners discovering with manifests.
- **CI:** `version-continuity-sweep.sh` wired into the Linux guard job (the ISO-validation "unowned gap") —
  fails on any BREAKS line; the CLI build step added.
- **The subsystem deep-dive:** `docs/COBOLNET_VALIDATION_DESIGN.md` (AS-BUILT — channels, pass, band, the
  four-source word tables + funnel restrictions, registry + drift, runners, the MEASURABLE G7 exit criteria
  incl. the §4.2.2 forward-cite, the perf note: ~1.35 s full-pipeline CLI compile of SQ207M, guard wall time
  unchanged across the wave) + the DOC_INDEX row.
- **Runner shells** (`CorpusRunnerTests`): per-edition `manifest.json` discovery — enabled compile-asserted
  strict, pending catalogued (the mass-red guard: 2002 = 0 enabled / 33 pending, 2023 = 0/1, 2014 SEEDED
  EMPTY so the directory gap stops being invisible), integrity facts forbid silent non-discovery; the
  `negative/` must-reject shell (`.cob` + `.err` + `*> reject-at:` headers) awaits its W2 seeds. One
  PowerShell single-element-array JSON collapse fixed en route.
- Suites at closeout: **conformance 1195/1195 + unit 38/38**; guard green at P2.6 (081c4ef, no compiler
  change since).
**RESUME = roadmap Phase 2:** W2 (MOVE rows §14.9.25 SR5, the loud-guard silent-misbind sweep + the
national/boolean skeleton, the negative-corpus seeds, position-aware checking for the screen/report token
band, VCR flips, adversarial review) → W1.5 (~24 intro-gate 0900 upgrades) → W3 (the serialized grammar
batch: XOR→2023 per Annex E, notInGrammar 85-acceptance, preprocessor DialectLevel threading, the 2002-corpus
edition audit; FULL legacy guard).

