## Entry 705 — 2026-07-08 18:19 PDT — P3 Step 9 — corpus discovery runners already exist (CorpusRunnerTests); added the one missing 2014 seed

**PHASE 03 step 9.** Recon found — like Steps 1/4 — that the deliverable was already built: `CorpusRunnerTests` IS
the per-edition corpus DISCOVERY runner. It already provides everything Step 9's doc asked for as separate classes:
- `Manifest_CoversEveryProgram_NoOverlap` — the integrity fact (every on-disk `.cob` is manifest-listed;
  no silent non-discovery), enabled/pending split.
- `EnabledProgram_CompilesStrict_AndMatchesOutIfPresent` — the per-edition POSITIVE runner (compile strict at the
  edition + byte-match a sibling `.out`).
- `EnabledNegativeCase_RejectsWithItsDiagnostic` — the NEGATIVE runner (reject with the `.err` diagnostic at the
  editions named in the `*> reject-at:` header).

Per the owner-corrected singular-pattern principle I REUSED it rather than forking `NegativeCorpusDiscoveryTests`/
`PerEditionPositiveCorpusTests` (which would be parallel mechanisms). The corpus is fully enabled: **2002: 78 · 2014:
17 · 2023: 11 positives · 46 negatives, 0 pending** — the 2014 "≥3 beyond OCCURS-DYNAMIC/OPTIONS" seed requirement
was already met (ROUNDED MODE IS ×3 + FORMATTED datetime).

The ONE genuine gap: `ARITHMETIC IS STANDARD-DECIMAL` (the corpus had `ARITHMETIC IS STANDARD` via `options_paragraph`
but not the STANDARD-DECIMAL mode). Verify-by-running confirmed it works: @2014 `2 / 7 * 7 = 2.00000` (full-decimal
intermediate, §8.8.1.4), @2002 correctly rejected `COBOLNET0804` (the OPTIONS-paragraph 2014 gate). Added
`tests/conformance/2014/arithmetic_standard_decimal.{cob,out}` + the manifest entry; the runner auto-discovers it
(`CorpusRunnerTests` 157 → 158). (My first attempt failed with a generic `COBOL0001` — an OPTIONS *placement* error
in my fixture, NOT a compiler hole: OPTIONS goes right after PROGRAM-ID, not in CONFIGURATION SECTION.)

Corpus data + manifest only (no code change). **Battery:** conformance **3098** · unit 227 · characterization 32
GREEN; FULL legacy guard NIST **353 MATCH**. RESUME AT step 10.

