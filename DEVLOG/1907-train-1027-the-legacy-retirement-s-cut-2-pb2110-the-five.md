## Entry 1907 — 2026-10-07 02:31 PDT — Train 1027: the legacy retirement's Cut 2 (PB2110); the five CobolSharp trees are deleted from main

**Cluster B, PB2110 (legacy retirement Cut 2, owner decision R69).** The attended session deleted the five trees on a branch
(`36441a24c`: `src/CobolSharp.Compiler`, `src/CobolSharp.Runtime`, `src/CobolSharp.CLI`, `tests/CobolSharp.Tests.Unit`,
`tests/CobolSharp.Tests.Integration`, 231 files and 74,493 lines, with their five `CobolSharp.sln` entries) because the permission
classifier refused a subagent's `git rm -r` (PB2142); the wave 1027 finisher then removed every remaining reference. The Frontend's
`InternalsVisibleTo CobolSharp.Tests.Unit` is gone, and with it the only non-comment change under `src/Cobol.Net.*`, so emitted C#
and diagnostics are unchanged by construction (Characterization 35/35). The impact map stops naming the legacy assemblies
(`record_impact_map.py` PROBED, `ImpactRecording.targets`, `ImpactedTestsDriftTests`' `CobolSharp.CLI` exemption). Six drift tests
lose their legacy lists or exclusions: `ParenTokenTwinDriftTests` no longer scans `CobolSharp.Compiler`, and its missing-project
`continue` now throws, because a renamed project silently shrank that theory to nothing; `ReservationGateParseEntryDriftTests` asserts
one front end; `BooleanOperatorTokenDriftTests` and `ClockSeamDriftTests` scan all of `src/`; `SpecTraceabilityInventoryDriftTests`
keeps its "a site outside `src/Cobol.Net.*` does not implement" case with `tools/impact/ImpactProbe.cs#Hit` as the outside path. The
grammar's comments that justified the additive operand alternatives by the legacy consumer now point at PB2114 (no rule changed). The
CI guard comment, the semgrep header and three frozen-evidence claims (marked `superseded_by`) follow. CBL3617/CBL3618 needed nothing:
PB756 had deleted both descriptors. `docs/rearchitecture/LEGACY-ARCHIVE.md` and its DOC_INDEX row name the archive tag
`legacy-byte-engine-final` and its run recipe; the orchestrator creates that tag on `48a44548c` (this train's base, the last main commit
that holds the engine), since a lander neither tags nor pushes anything but the train. PB1861 retires as moot; PB2113 and PB2114 are
unblocked. The implementer's gate 1 was red on two drift tests that named legacy files by relative path (the project-name grep cannot
see them) and on the evidence-supersession audit; gate 2 was green, and its Linux gate green (NIST through `cobol`: 362 MATCH, 0
regressions).

**The lander's review** found one more reference the implementer's grep could not see, because it searched `src tests scripts .github`
and not the repository root: `Run-Cobol85Audit.ps1` ran only `CobolSharp.Tests.Unit`, `CobolSharp.Tests.Integration` and a
long-gone `CobolSharp.Tests.Nist`, and snapshotted the legacy source directories, so after the delete it could only fail. It is
deleted in cluster B's commit, `kb/Modernization/Audit Artifacts.md` stops listing it, and the plan's Cut 2 step 7 says no repo-root
script names the engine. Two skills still taught the engine: the lander brief told every train to gate `CobolSharp.Tests.Integration`
`--no-build` (the assembly no longer exists), and `land-a-fix` said not to add a `GreenfieldOnly` entry because the legacy
differential was opt-in (no such entry type exists in the tree). Both sentences are removed. The remaining comment residue in `src/`
and `tests/` (about 39 and 75 lines, `DataDisplayDifferentialTests.cs:11`, `RoundedDifferentialTests.cs:27`, `CobolIO.g4:568,889`)
is the same root as PB2111's doc sweep, so PB2111 gains it as scope item 7 and is unblocked; PB1953 records its third recurrence
(eight `UniversalCrossingShapeDriftTests` rows NEVER RAN in the implementer's gate 1).

**The train.** One cluster, applied blob-identical to the implementer's head `d6e2aaef1` on all 258 paths except
`.github/workflows/build-and-test.yml`, where entry 1906's hook self-test step and this cluster's guard comment both survive. The
lander gate is the whole population: `=== BUILD-LOCAL GATE: GREEN — Conformance 10,715/10,715 · Unit 31,643/31,643 · Characterization 35/35 cases ran (skipped 0) in 1 of 1 leg(s) · lander mode` (run `20261007T092334Z-f5d665`, after the review fixes; the first run, `20261007T091806Z-17d89c`, was green on the cluster alone). The Linux gate: `=== LINUX GATE: GREEN (legs hooks unit characterization conformance guard; HEAD a077dbad6) ===`, NIST through `cobol` 362 MATCH, 2 TERMINATES (declared), 0 regressions. CI audits (code citations, doc citations, evidence
supersession, witness loss, drift rules, `work.py check`) and `semgrep/verify.py` (no count changed) are green. GAP 173 → 173: the
cluster re-verdicts no inventory row (`closes_rows: []`, process work). REVIEW: 1 finding, 1 fixed in the train, 0 clusters dropped.
(Entry 1906's 03:20 stamp is ahead of the host clock; this stamp is the host's.)

