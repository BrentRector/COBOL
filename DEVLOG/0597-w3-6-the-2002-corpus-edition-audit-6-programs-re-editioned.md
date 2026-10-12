## Entry 597 — 2026-07-03 22:40 PDT — W3 ⑥: the 2002-corpus edition audit — 6 programs re-editioned, 11 ENABLED with a live run contract, a Pic-null emit crash fixed, 2 ISO re-baselines

**The audit of the 32 pending 2002-corpus programs (roadmap W3 item ⑥), plus the CI proof: the first
always-regenerate run on the untracked Generated/ passed BOTH jobs (Linux guard + Windows).**

- **Six programs were mis-editioned** (their construct's introduction is later than 2002):
  `inspect_backward` → **2023** (spec-confirmed: Annex E.3 item 34 — "INSPECT statement, BACKWARD context
  sensitive word added"; the program's own header claimed 2002 — corrected);
  `corresponding_rounded_mode`, `rounded_modes`, `rounded_mode_prohibited`, `options_default_rounded`,
  `options_paragraph` → **2014** (ROUNDED MODE IS / the OPTIONS paragraph are §11.9/§14.7.4 2014 features —
  the registry rows options-arithmetic-native-2014/rounded-mode-is-2014 already said so). The remaining 26
  claims verified correct (OO/UDF/pointers/BASED/national/boolean/float/binary/GOBACK RETURNING/INITIALIZE
  phrases/REPOSITORY/table-SORT/table-VALUE/>>DEFINE — all genuinely 2002).
- **ELEVEN programs ENABLED with a real RUN CONTRACT** (the runner shell asserted compilation only; the
  enabling wave owns the run contract — landed now in `CorpusRunnerTests`: compile strict at the edition, run,
  byte-compare vs the sibling `.out` on the same `CutRunner.Normalize` basis as the NIST harness):
  @2002 `conditional_compilation`, `goback_returning`, `repository_paragraph`, `sort_table_format2`,
  `initialize_phrases`, `table_value_occurs` · @2014 `corresponding_rounded_mode`, `rounded_modes`,
  `options_default_rounded` · @2023 `logical_xor`, `inspect_backward`. The 2002+ positive corpus is
  no longer empty — the G7 exit criterion's "runners discovering" now DISCOVERS AND RUNS.
- **A Pic-null emit CRASH found and fixed** (exposed by probing the corpus for enablement): the W2 skeleton
  usages (BINARY-CHAR/POINTER/OBJECT REFERENCE/FLOAT-x/NATIONAL/BIT) are legally PICTURE-LESS (§13.18.60), so
  the recovery-after-diagnosis discipline had NO recovery SHAPE — a MOVE receiver with `Pic == null` NRE'd
  the doomed emit (`ConvertSource`) and the crash MASKED the 0899/0900 diagnostics (binary_usage/float_usage
  presented as hard crashes, exit 139). Fix mirrors the `IndexItem` pattern exactly: `ParseUsage` gains an
  `out bool skeleton`; a picture-less skeleton entry gets the reference-comparable `PicInfo.RecoveryItem`
  singleton; group headers shed it in `ResolveIndexItems` (usage merely inherits, §13.18.60.4 GR1).
  Crash-swept the whole corpus clean.
- **Two `.out` files re-baselined to ISO-conforming output** (`initialize_phrases`, `table_value_occurs`):
  they encoded the legacy DISPLAY trailing-space TRIM (non-conforming per §14.9.11.4 GR6 — the long-
  established finding); the spaces sit INTERIOR to the expected lines, so no normalization bridges the two
  engines. The legacy `ConformanceTests` gained the same divergence protocol as guard.sh's LEGACY_DIVERGENT
  list (compile+run still asserted; output comparison skipped for the two entries, citations in-code).
- Two honest pendings with corrected rationales: `rounded_mode_prohibited` (a REAL behavioral gap — ROUNDED
  MODE IS PROHIBITED must raise SIZE ERROR on an inexact result; we store the truncated value silently —
  the Phase-5/7 rounding follow-up), `options_paragraph` (ARITHMETIC IS STANDARD → 0807
  documented-unsupported until Phase 4(e) lands the positive 2002/2014 behavior).

**Battery: greenfield unit 102/102 · conformance 1378/1378 (+11) · INV-1-STRONG 349/349 at 2023-permissive ·
legacy NIST 353 MATCH / 0 regressions · legacy unit 1204/1204 · integration 537 green.**

