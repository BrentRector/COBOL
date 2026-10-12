## Entry 501 — COBOL.NET G5 corpus drive: 7 NC programs green; the corpus map

With the differential harness live, mapped the whole NC series through COBOL.NET (compile-only sweep + a run-vs-golden
sweep, both with per-program timeouts since a few NIST programs hang). Snapshot: **68 / 95 NC programs compile** to
valid C#; **7 match the NIST golden** end-to-end — `NC101A NC110M NC111A NC112A NC113M NC127A NC136A` — all now
permanent `[InlineData]` regressions in `NistDifferentialTests`. The remaining 61 compile-but-mismatch (mostly an
unimplemented verb reaching its loud guard — INSPECT/STRING/SEARCH/EVALUATE/ACCEPT) or 24 compile-error / 3 compiler-
hang programs; these are the prioritized gap list for the next slices (the hangs — NC126A/NC207A/NC246A — are
compiler-side infinite loops to fix first, since a hang is worse than a loud failure). Also folded in the `Pow10D`
negative-scale fix (the exponentiation `Real()` path now scales a PICTURE-P operand correctly: 10^−1 → 0.1d).
Conformance 226 → 232 (the 6 added NC programs; +13 unit), green. Greenfield-only.

