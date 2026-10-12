## Entry 1909 — 2026-10-07 03:09 PDT — Train 1026: the R0 instruments (census, oracle, performance baseline) and campaign planning land; PB2115, PB2116, PB2117, PB2120

Wave 1026's four R0 and process items land as one train (lander worktree `wf_71adb89f-039-5`, clusters in order F, D, C,
E, each its own commit, rebased onto d5fc9c998, after train 1027). None changes compiler source, and the proof is direct:
the R0 oracle recorded on the train's last product commit `f620ccf6d43d` is byte-identical, case for case, to the one
cluster D recorded on its own base `a02b165da` (7,357 cases; only the `commit` field differs), so trains 1025 and 1027
(the legacy engine's deletion) and these four clusters changed no emitted C# and no diagnostic.

**F — PB2120: a named kb/Work cluster is a plannable unit.** The fix lane ranks by harm to a user's program, so the two
campaigns (PB2108 the legacy retirement, PB1754 the architecture review) were dispatched from hand-written `groups.json`,
and nothing validated the `cluster:`/`blocked_by:` fields (the note's claim that `work.py check` did was false on the
base). `work.py#cluster_order` is now the one reader of both (`work.py next --cluster <lead>`), and `check` holds
`blocked:` to "a `blocked_by` note is still open", rejects unknown ids and cycles; it found PB1940 and PB1956 hidden by
stale flags and PB2077's prose blocker. `plan_wave.py --cluster` clusters ready notes by site through fix_clusters.py's own
functions, merges a dependent's blocker groups, plans a dependent as a successor at the tail of its blocker's chain, and
holds Fable/Mythos notes; `orchestrate.ps1 -Cluster` alternates campaign and fix-lane waves until the cluster lands. The
fix lane is proven unchanged by a golden of its whole plan written by the pre-change planner. On the merged tree the new
check flagged the notes this train unblocks (PB2118 and PB2119, both waiting on PB2115); the lander set
`blocked: false` on them.

**D — PB2116: the behavior-neutrality oracle.** `ArchOracle.Sources` enumerates the suites' own partitioned row sources
(corpus positives, negatives at each reject-at edition, the NIST golden run and its continuity cells, the version
matrix): 7,357 compiles through the option builders the theories now share (`ConformanceCorpus.PositiveOptions` /
`NegativeCase`, `CorpusManifest.CompileOptions`, `EditionHarness.CompileStaged` / `CompileNistObserved`; six inline
duplicates collapsed). Each case's C# and diagnostic stream are SHA-256'd into one committed manifest (the design's
§4.1 correction: blobs stay in gitignored TestResults); `compare_oracle.py` compares case by case. Found and fixed on
the way: a compiled-program cache hit dropped the compilation's DISPLAY-directive output (§7.3.12.4 1)); the cache
entry now stores it. The lander re-recorded the manifest at the train's product commit `f620ccf6d43d`, as the report
asked. Its COPY-spelling lead is PB1680's second witness (19 cells differ Windows vs Linux), not a new note; its
token-differential "next step" is already PB2113/PB2114's §4 item 3.

**C — PB2115: the R0 census.** `scripts/arch/census.py` (policy: scope rule, population check against each built
assembly's metadata, god-class threshold, dead-artifact caller queries, a 19-arm self-test) drives the Roslyn host
`tools/ArchCensus` (facts: a semantic reachability walk confirmed by SymbolFinder, every exclusion a stated rule; clone
families agreeing with the roslyn-analysis detector). Record at `a02b165da`: 2,087 types, 52 god-class candidates
(DataBinder 20,375 lines across partials), 9 clone families, 219 unreferenced and 266 test-only members, 15 dead
artifacts, 234 findings for a clerk to file. The design doc's hand-kept god-class table is now a pointer to the record.
Leads filed: PB2130 (three interface members kept alive only by the implementation cascade; Delete input, cluster
PB1754) and PB2131 (no gate builds `tools/ArchCensus`); its gate flake is PB1953 again.

**E — PB2117: the R0 performance baseline.** `tests/Cobol.Net.Benchmarks` gains compile-throughput and generated-program
benchmarks, each with a witness checked in GlobalSetup; `scripts/arch/perf_baseline.py` adds cold processes, the
GnuCOBOL comparison under WSL with a scaling curve, and gate time, and flags a row outside its noise band as a
REGRESSION (`PerfBaselineSelfTestDriftTests`; no test asserts a time). Found and fixed: `tests/Directory.Build.props`
linked xunit helpers into BenchmarkDotNet's generated host, so every benchmark reported NA. The record
(`perf-baseline/354c79ef3312.md`) answers A6's questions: PERFHOT 0.7x GnuCOBOL, MOVEHOT 3.0x (926 B per pass), SEQHOT
2.8x, IDXHOT 60x at 2,000 records and 330x at 8,000. A6 stays open for the storage-model decision; the indexed result
is filed as PB2129 (IndexedConnector keeps no index). The branch's own DEVLOG draft (numbered 1904) was dropped for
this entry.

**The train.** Conflicts: plan §0 (F's dispatch line beside main's newer quota and NEXT lines), DOC_INDEX and the
design doc's R0 bullets (C's census pointer beside D's oracle line; train 1027's LEGACY-ARCHIVE row kept), PB1953
(train 1027's recurrence paragraph beside F's), and `docs/DRIFT_RULES.md` (regenerated after each
cluster: 278 drift tests). Marker checks clean per cluster. Semgrep verify before and after: PASS, no count changed.
Citations re-run (§9.3.6, §14.8.2.3.2, §15.99.3 2), §7.2.3.4 3), §7.3.12.4 1), §12.4.5.12.3 2)): all OK. Notes landed:
PB2120, PB2116, PB2115, PB2117 (each `closes_rows: []` with a reason). Filed: PB2129, PB2130, PB2131; extended PB1680,
PB1953. GAP 173 → 173.

Gate: `build-local.ps1 -Mode lander` run `20261007T095545Z-45e660` on the train head, `=== BUILD-LOCAL GATE: GREEN — Conformance
10,720/10,720 · Unit 31,646/31,646 · Characterization 35/35 cases ran (skipped 0) in 1 of 1 leg(s) · lander mode ===`;
`=== LINUX GATE: GREEN (legs hooks unit characterization conformance guard) ===`. The legacy Integration assembly is
gone with train 1027, so there was no legacy leg to run. CI audits run locally (code and doc citations, evidence
supersession, witness loss, drift-rule index, `work.py check`): all clean. Review (full-code pass over the train's diff,
drift rules of every changed file, wall-clock assertions, citations): 0 findings, no cluster dropped.

