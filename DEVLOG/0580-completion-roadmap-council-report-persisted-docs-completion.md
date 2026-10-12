## Entry 580 — 2026-07-03 13:46 PDT — Completion-roadmap COUNCIL report persisted (docs/COMPLETION_ROADMAP_COUNCIL.md) — advisory, docs-only

**Owner asked for a council to evaluate the best way to finish multi-edition support (the full G7/G8 runway).**
Ran an 8-agent workflow over the tree at f73595f (read-only): 5 lens advisors (edition-gating, feature-catalog,
architecture-readiness, verification, sequencing) → 2 adversarial critics (26 challenges, 14 gaps) → a chair
synthesis that re-verified every contested claim in-repo before adjudicating (~862k subagent tokens; zero
content-filter trips — the word-list prohibition was embedded in every brief per the DEVLOG-578 lesson).

**Verdict: the canonical spine is UPHELD** (validator → M2 with OO largest → M3 → M4 → closure → G8), with 7
proposed deltas (report §3), the sharpest being:
- **The critics' one FATAL finding:** nothing in the canonical plan ever *executes* behavior at 2002/2014/2023
  before G8 — fixed by attaching a 318-golden re-run at `--std 2023` permissive to the P2.7 flip commit, and
  making INV-1-strong-at-the-default-edition a G7 exit criterion.
- **Pre-P2.5 metadata scrub** so the drift tests never fossilize wrong data. Chair-verified defects: the
  `json/xml-generate-2014` constructs.json rows cite "ISO 2014 §14.9.x" but JSON/XML has ZERO hits in the whole
  2023 spec (→ vendor-dialect, removed from M3, owner decision); the `{is2023()}?` TYPE gate at CobolData.g4:246
  is provably wrong (TYPEDEF has ~33 spec hits yet zero Annex E 2014→2023 rows ⇒ predates 2023 → provisional
  2002 edge); XOR/EXCLUSIVE-OR adjudicated **2023** per the spec's own Annex E (VCR rows 32/41) over
  ISO2023_CONFORMANCE_PLAN M4-2a's unsourced "(2002-era)" parenthetical.
- New named workstreams absent from SSOT §16: W1.5 (~24 intro-gate diagnostic upgrades — the doc's 39 was
  stale), the silent-misbind loud-guard sweep (PicInfo.ParseUsage DISPLAY fallback, PIC N/E/1, dropped class
  defs — pattern-mates of the STOP-literal bug), discovery runners with pending manifests, the 43-row deferred-
  intrinsics backlog, the ~44 Table-1/5 behavior-row wave, the in-repo greenfield guard + a legacy-equivalence
  proof HARD-ORDERED before G8 Cut 1, CI wiring of the continuity sweep, and the ISO §4.2 conformance document.
- G8 split into three serial cuts (test-graph → deletion → rename); budget re-baselined ~22–32 sessions; OO
  re-rated XL (FACTORY isn't even in the grammar — CobolOO.g4:17; PROPERTY/INTERFACE-ID/universal reference are
  net-new mandatory surface).

**Report §5 = the 11-item owner decision packet** (standards acquisition, JSON/XML disposition, the Annex A.4
optional-module line, --permissive as supported surface, the OO grammar grant, Q13, etc.) with recommended
defaults — awaiting the owner.

**Durability note (verified first-hand this session): `/e/tmp/g7`, `/e/tmp/nc-sweep`, and `/e/tmp/phase2-briefs`
are GONE.** The in-repo docs are the sole surviving plan — exactly the DEVLOG-579 lesson; this report is
persisted in-repo for the same reason. The doc carries an ADVISORY banner: resume-prompt.md remains the live
plan until the owner ratifies; ratified deltas then fold into the canonical docs and this doc gets re-bannered.
Docs-only change set (report + DOC_INDEX row + resume-prompt STATE pointer); the tree is otherwise untouched at
the DEVLOG-577 code state, so no build/guard run is implicated.

