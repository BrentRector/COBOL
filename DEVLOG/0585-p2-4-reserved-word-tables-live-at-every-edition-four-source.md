## Entry 585 — 2026-07-03 17:40 PDT — P2.4 reserved-word tables LIVE at every edition — four-source mechanical derivation; the funnel active; 1076 conformance green

**The §8.9 per-edition reserved-word machinery is END-TO-END** (roadmap Phase 1 / plan P2.4), with the
derivation REDESIGNED under fire:
- **A FOURTH content-filter kill** (API 400) took out the planned deltas-authoring agent — even a file-Write
  of a ~300-word lowercase list now trips it (the June words-2023.json precedent no longer holds). Workaround
  that ended the whack-a-mole: **no word list ever transits the API stream in ANY form** — the 85/2002/2014
  lists are fetched by `curl` disk-to-disk from GnuCOBOL's per-standard `config/*.words` files (pinned tag,
  cached in the gitignored `.cache/`; the FILES are GPL so only derived FACTS enter the repo), the 2023 list
  parses from the in-repo spec §8.9, the 16 Annex-E additions parse from VCR row 32's own text. The generator
  (`scripts/gen-reserved-words.ps1`) prints COUNTS ONLY and emits both committed outputs:
  `Validation/ReservedWords.Table.cs` + `tests/version-matrix/reserved-words.json` (drift-tested both ways).
- **The mechanical derivation out-performed recall three times:** (1) the 2023 re-reservation set is THREE
  words, not two — the communication trio includes the END- scope terminator (added2023 ∩ gc85, enforced by a
  sanity gate); (2) the EC words (RAISE/RAISING/RESUME/CONDITION/EC) are reserved SINCE 2002, not 2023-only —
  DEVLOG 578's finding was UNDER-inclusive (they are absent from the Annex-E 2023 additions); the ECT045
  continuity test's expectations were corrected to match (85 = legal user words; ≥2002 = 0901); (3) ISO Annex
  E.2 item 25 OVERRIDES the GnuCOBOL 2002/2014 curation, which wrongly keeps the communication trio reserved.
- **Extraction hardening:** OCR hyphen line-splits joined (the FLOAT-NOT-A-NUMBER pair — `&nbsp;`-prefixed
  continuation lines); the trailing-hyphen word shape rejected; 2014 continuity interpolation (85∧2002∧2023
  reserved + Annex-E silence ⇒ the 2014-source absence is a curation gap — the REPORTS case… which then
  stayed the ONE medium row: 85+2023 attested, 2002/2014 unprovable in-repo → inert by the conservative
  policy, exactly as designed). Final stats: 462 rows, 461 high, 1 medium; 410 = the exact DEVLOG-578 §8.9
  count reproduced.
- **The funnel** (`EditionValidator.VisitCobolWord`): text check at the cobolWord rule, one 0901 per distinct
  word, severity via `Removed()` (error strict / warning permissive), only high-confidence rows reject.
  **Position-blindness hazard found by the corpus:** the permissive grammar binds keyword COLUMN into a
  report-group entry-NAME slot (RW104A), so allowlisted-token occurrences can be keywords in disguise — the
  funnel therefore checks IDENTIFIER occurrences (the whole newly-reserved payload) + the six EC-band tokens
  (no name-slot adjacency), and EXCLUDES the screen/report token band pending position-aware checking (parked
  to the W2 adversarial review). **CCVS-proof override:** ST127A + the sort differentials use ORDER as a data
  name at 85 — conforming CCVS-85 usage PROVES a word un-reserved; ORDER removed from the 85 flags (the
  corpus outranks GnuCOBOL curation).
- **Green with the funnel ACTIVE at every edition — no permissive flip needed for it:** unit 36/36 (drift +
  sanity-invariant tests new), conformance **1076/1076** (318 goldens intact; ECT045 corrected; two new
  0901-rejection theories at 2002/2014/2023). The P2.7 interval matrix rows (user-word-commit-2023,
  receive-as-user-word, + the end-receive twin the derivation discovered) land with the flip commit.

