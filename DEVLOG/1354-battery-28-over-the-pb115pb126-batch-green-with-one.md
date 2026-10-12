## Entry 1354 — 2026-08-29 00:49 PDT — Battery #28 over the PB115–PB126 batch: green with ONE attributed-and-fixed unit flake — and the flake's mechanism widened PB126's fix (the sweep's complement had been measured too narrowly)

`bash scripts/battery.sh` over `6c177947`: FULL Conformance 4934/4934 · characterization 33/33 · guard-fast
ALL GREEN (NIST 353 MATCH / 0 REGRESSION, audit CLEAN) · GnuCOBOL differential 0 PER-CASE FLIP(S) (1323
cases). Unit 4588/4589 — the one red is the PB126 family caught from its OTHER side:
CollationTailoringTests' `Assert.Same` on CollationEngine's per-locale collator cache raced
CobolLocaleTests/LocaleManagerTests' `LocaleManager.ClearCache()` (which flushes the engine + CLDR caches) —
identical collators, different instances. My PB126 sweep an hour earlier had measured only ENV-stability
asserters and declared the pair closed; the battery falsified that complement claim immediately
(feedback_measure_the_selectors_complement, again). The re-measured complement — grep `Assert.Same(`
suite-wide plus every ClearCache caller — yields exactly five process-global mutator/asserter classes, now
serialized under ONE `[Collection("process-globals")]` (retiring both `process-environment` and
`site-tailoring-directory`); every other Assert.Same is over a local instance or an immutable root singleton.
FULL Unit re-ran 4589/4589 GREEN on the fixed tree. Plan §0's battery reference now names #28 with the flake
attribution on its face; #27 demoted to PRIOR.

