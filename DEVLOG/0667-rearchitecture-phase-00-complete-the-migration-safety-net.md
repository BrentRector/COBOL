## Entry 667 — 2026-07-07 19:05 PDT — Rearchitecture PHASE 00 COMPLETE — the migration safety net (steps 3–13)

Finished Phase 00 of the rearchitecture roadmap (`docs/COBOLNET_REARCHITECTURE_PLAN.md`) — the net that lets every later
phase *prove* it changed no observable behavior, and that survives the eventual G8 deletion of the legacy engine. Zero
compiler behavior change (the only `src/` edit remains step 2's behavior-neutral ref-cache). Full §5 battery GREEN.

**Characterization harness — gates 2 & 3 (steps 3–6).** New `tests/Cobol.Net.Tests.Characterization` project (references
`Cobol.Net.Compiler` + `Cobol.Net.Runtime` ONLY — the probe drives the greenfield `CompilerDriver`, never the legacy).
A curated corpus (`tests/characterization/` — 14 positive one-per-family + 3 negative) feeds two snapshot gates:
`DiagnosticSnapshotTests` (gate 2, `CheckOnly` diagnostics) and `EmittedCSharpSnapshotTests` (gate 3, the `.g.cs`),
seeded once from today's emitter; 32 tests, compare-run green. Snapshots are path-portable (forward-slash + repo-root
strip). **Finding for PHASE-02:** a MOVE to an undefined data-name and JUSTIFIED-on-numeric are NOT rejected at bind
time (both accepted under `CheckOnly`) — a real diagnostics gap the two original negatives exposed (they were swapped
for genuine-diagnostic negatives, `occurs_dyn85`→1522 / `typedef_sr`→1529).

**Corpus consolidation (steps 7–8).** Killed the 3-way "which NIST programs are green" triplication
(`NistDifferentialTests` `[InlineData]` × `chains.tsv` × `guard.sh` `LEGACY_DIVERGENT`). One SSOT `tests/nist/corpus.tsv`
(459 rows = 338 green + 11 divergent [each with its ISO § citation] + 110 pending), generated MECHANICALLY;
`CorpusManifest` loader + a 5-assertion drift guard proving the fold LOSSLESS (green∪divergent == a committed 349-name
baseline). `NistDifferentialTests` repointed at `[MemberData(CorpusManifest.GreenData)]` + `CorpusManifest.Chains` — the
487-line `[InlineData]` block + the private `Chains` lazy deleted (−487 net), **349/349 verdict-equivalent**. `chains.tsv`
KEPT (the bash guard + off-repo sweep still read it; retirement is a G8 concern).

**Differential oracle bake-out (steps 9–11) — the piece that survives G8.** `DifferentialGolden.Assert` funnels every
differential case through a committed golden (`tests/differential/<Class>/<hash>.out`, hash = edition+newline-normalized
source) with three modes: `golden` (default/CI — no legacy run), `bake` (assert cobolnet==legacy, then write — only
GREEN cases can bake), `verify` (assert cobolnet==legacy AND ==golden). Conversion of the 46 files that construct
`new LegacyCompiler()` was run as a **parallel Workflow — one agent per file, 0 errors** (owner's max-parallelism
directive; the per-file discriminator "convert only funnels asserting `Assert.Equal(lout,cout)`, leave spec-pinned
funnels + their fields" was too nuanced for a blind sed — e.g. SortMerge mixes both; MoveEdition is spec-pinned-only).
Result: 42 funnels converted + 360 goldens baked; 4 files correctly untouched (MoveEdition / SignedAlphanumericMove /
AllLiteral / AbbreviatedCondition — no `lout==cout` funnel); fields kept where a spec-pinned funnel still uses them.
Central validation was the exact backstop: build 0-warn (warnings-as-errors would flag any dead/dangling field), bake
644/0, golden-mode 644/0 (legacy untouched), and a re-bake left `git status tests/differential` CLEAN (complete +
byte-idempotent). `DifferentialGoldenDriftTests` guards folder-level orphans (3/3).

**CI (step 12).** Characterization compare-gate added to `greenfield-tests` + `windows-build-test` (CI never sets
`COBOLNET_UPDATE_SNAPSHOTS`). New **opt-in** `legacy-oracle` job runs the differential suite in `verify` mode
(cobolnet==legacy==golden) NIGHTLY + on manual dispatch only — deliberately NOT every PR, so the normal battery stays
severed from the legacy engine (the whole point of the bake). Legacy `guard`/`inv1-sweep`/windows jobs untouched;
deleted at G8 with the legacy projects.

**Battery (§5, close-out):** `dotnet build CobolSharp.sln` 0-err · greenfield unit 213 · conformance 2036 (golden mode;
+8 = 5 `CorpusManifestTests` + 3 drift) · characterization 32 · differential verify 644/0 · FULL legacy guard NIST 353
MATCH. **Ref-cache (step 2) measured ~40–55% test-execution speedup.**

**Transparency.** (1) The drift test's own doc comment contains the literal `DifferentialGolden.Assert`, so the first
`ConvertedClasses()` scan mis-detected the drift test as a converted class → false RED; fixed by excluding the
`DifferentialGolden*` infrastructure files. (2) Two original characterization negatives compiled clean under `CheckOnly`
(the diagnostics gap above) and were swapped. (3) `NistDifferentialTests` was miscounted from a line count (318) — the
real `[InlineData]` count is 349 (attribute count); the fold pins the correct 349. NEXT: **Phase 01** — mechanical
namespace rename (`CobolSharp.Compiler.* → CobolNet.*`) + dead-grammar / JSON-XML (non-ISO) removal.

