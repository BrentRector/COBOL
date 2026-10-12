## Entry 987 — 2026-07-22 15:49 PDT — V45 comprehensive gate ALL GREEN (a corpus-bookkeeping miss caught + a durable memory)

Closed V45 with the full comprehensive gate: greenfield **Conformance 3853/3853 0-reg** · **NIST 353 MATCH / 0 reg**
(ALL GREEN, no parallel-compile flake) · legacy Unit 1203/1203 · legacy Integration clean · characterization 33/33.

**The gate caught a real miss (feedback_transparency):** the first comprehensive Conformance run failed 1/3850 —
`CorpusRunnerTests.Manifest_CoversEveryProgram(negative)`. The 3 V45 externality negatives (`external-{file-status,
relative-key,linage}-externality-at-2023`) were added to `tests/conformance/negative/` WITHOUT listing them in
`negative/manifest.json`, so they were both flagged as undiscovered (the integrity fact) AND never actually run by the
negative-corpus runner. This is the SECOND instance in one session of the same class — a new corpus golden needs its
manifest/exclusion entry in the same commit (EC-seam positives lacked their `GreenfieldOnly` entries → the guard's
`int_rc`; V45 negatives lacked the negative-manifest entry → the integrity fact). BOTH surfaced only at the
comprehensive/guard gate, NOT the wave-local filtered run. Fixed (the 3 now run + assert COBOLNET1624), and recorded a
durable memory `feedback_corpus_golden_manifest_registration` so it does not recur. V45 is COMPLETE + MERGE-READY.

