## Entry 909 — 2026-07-19 12:50 PDT — Legacy-suite latent-miss #3: `external_type_decl` GreenfieldOnly exclusion (Wave E pt1 omission)

Running the legacy conformance suite after the VCR-15 shared-corpus manifest change (the
[[feedback_legacy_suite_on_shared_corpus]] rule) caught a PRE-EXISTING miss: the Wave E pt1 golden
`2023/external_type_decl` (VCR 63, `9a33dfa7`) landed WITHOUT its GreenfieldOnly exclusion — the frozen legacy
grammar cannot parse the 2023 STRONG TYPEDEF + EXTERNAL TYPE surface, so the legacy runner failed on it. The
THIRD instance of this latent-miss class (after `ec_bound_ref_mod`/`ec_bound_overflow`, Entry 897). Exclusion
added; legacy suite 165/165 green. The pattern-level fix candidate (a drift test asserting every
2023-manifest-enabled golden either legacy-compiles or has a GreenfieldOnly row) goes to the review ledger's
Wave I batch — this class keeps recurring precisely because only a full legacy-suite run surfaces it.

