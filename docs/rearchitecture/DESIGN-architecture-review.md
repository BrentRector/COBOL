# DESIGN — The comprehensive architecture review and restructuring (kb/Work PB1754)

> **Status: DESIGN; R0 and R1 STARTED by owner decision R69 (2026-10-06), the rest sequenced in §2.** The owner asked (2026-09-29) for "a full, 100%, end-to-end, architectural review
> of the entire COBOL compiler codebase, grammar and C# lexer/parser/code generator. Everything", using the latest
> review techniques, to eliminate duplicate code, refactor god classes, and refactor the file-system layout, file
> names and classes as necessary, following proven architectural designs. All C# is to use the latest .NET and C#
> features, upgrading the SDK if necessary. This is the **comprehensive pass** that `PROMPT.md` §4 requires "once
> the design has settled". The owner decides WHEN it starts (PB1754). This document is the plan it follows; the skill
> that runs it is `.claude/skills/architecture-review`, over the public `brent-tools:architecture-audit` base.

## 1. Scope

**In scope: every source artifact of the greenfield compiler.** Measured at `edda57d7f` (2026-09-29):

| Area | Where | Size |
|---|---|---|
| Compiler (binding, validation, lowering, code generation, OO) | `src/Cobol.Net.Compiler` | ~103,600 lines, 253 files |
| Runtime | `src/Cobol.Net.Runtime` | ~39,200 lines, 141 files |
| Front end (preprocessor, lexer/parser drivers) | `src/Cobol.Net.Frontend` | ~12,600 lines, 71 files |
| Editions and the diagnostic registry | `src/Cobol.Net.Editions` | ~7,900 lines, 21 files |
| Source generator, CLI | `src/Cobol.Net.Compiler.SourceGen`, `src/Cobol.Net.Cli` | ~700 lines |
| ANTLR grammars | `src/**/*.g4` | ~8,600 lines |
| Tests, scripts, hooks, CI | `tests/`, `scripts/`, `.github/` | layout and duplication only (§5.6) |

**Out of scope:**
- **The legacy `CobolSharp.*` projects** (~46,000 lines). They are deleted by kb/Work PB2110 (P15's Cuts 1–2,
  decoupled from v1.0 by R69) and are never refactored; until then the census (PB2115) and every wave exclude
  `src/CobolSharp.*`.
- **Behavior.** No change to any compiled program's output, any diagnostic, or any conformance verdict. A defect the
  review finds becomes a `kb/Work` note and is fixed in the normal fix lane, never inside a refactor (§4).

## 2. Preconditions (the trigger)

The review runs when all of these hold. The owner confirms the start (PB1754).
1. **The design has settled.** No open redesign of a subsystem (such as PB1708's ordered gate) is in flight.
2. **P15 is done.** The legacy engine is deleted, so the review never touches code that is about to vanish.
3. **The behavior-neutrality oracle exists** (§4, built in phase R0).
4. **A quiet fix lane.** The review's refactors touch every file, so fix waves pause or run only in subsystems the
   current review wave does not touch. Merge conflicts across a whole-codebase rename are the main schedule risk.

**The split start (owner decision R69, 2026-10-06).** Preconditions 1 and 2 are not yet true (the external-repository
slices PB2097–PB2104 are in flight; the legacy delete is PB2110), and the owner chose not to wait for them for the
parts that cannot conflict with the fix lane: **R0** (PB2115 census · PB2116 oracle capture · PB2117 performance
baseline) and **R1** (PB2118) run now, R1 taking the approved `DESIGN-external-repository.md` as given; **R3 leaf
waves and Delete waves** (PB2119) run between fix-lane trains in subsystems the current train does not touch; **R2
and R3 over binding and code generation** keep R64's condition (GAP near zero). Why now: the remaining GAP rows
land in the god classes (48 in §14.9, 22 in §13.18, 14 in §12.3 at 173 GAP), so every fix landed before the split
enlarges the extraction, and R1 lets those fixes be written into the target layout. The partition is enforced by
the file set a wave's brief declares, checked against the in-flight trains before dispatch, not by hope.

## 3. Phases

Every phase lands through the normal lander and `push-main.sh`. Every phase is behavior-neutral by the §4 contract.
Every finding is a `kb/Work` note (CLAUDE.md rule 8), never a list in this document.

### R0 — Baseline and oracle (measured, before any opinion)
- **Census** (`scripts/arch/census.py`, built in R0; a Roslyn workspace pass, not grep):
  - per type, including every partial file: lines, members, fields, fan-in/fan-out, and the namespace-to-folder
    agreement;
  - per project, the dependency graph;
  - clone families (`roslyn-analysis`'s type-2 detector);
  - unreachable members, measured, never deduced (`engineering-standards`);
  - dead artifacts beyond code: scripts, configuration, docs, test scaffolds and drift-test literals with no caller
    or reader, measured by a caller query (the Delete program PB2119's input).
  R0's instruments are kb/Work PB2115 (census), PB2116 (oracle capture) and PB2117 (performance baseline, which is
  also kb/Work A6's instrument: one mechanism, one place).
- **God-class candidates.** Any type over ~800 lines across its partials, or with more than one reason to change.
  The MEASURED list, with each candidate's responsibilities named, is the newest census record's `godClasses`
  table and its findings file (`docs/rearchitecture/evidence/arch-census/<sha>.json` and `<sha>.findings.json`);
  this document keeps no copy, because a hand-kept list is stale the day after it is written.
- **The census instrument** is `scripts/arch/census.py` driving the Roslyn host `tools/ArchCensus` (MSBuildWorkspace,
  never grep). It measures a DETACHED worktree at one commit; its scope rule makes every `src/Cobol.Net.*` project a
  census project and every `tests/Cobol.Net.*` project a reader, and refuses a solution project it does not classify.
  Its POPULATION CHECK compares the types it saw with the type definitions of each built assembly's metadata and
  refuses to write a record on any difference. Reachability is a semantic walk confirmed by `SymbolFinder`, with
  every exclusion a stated rule the record carries. `ArchCensusDriftTests` keeps the newest record whole and the
  script's policy arms firing. Its findings are filed as kb/Work notes by a clerk, never kept as a list.
- **The oracle** (§4.1) is recorded at the baseline commit: `python scripts/arch/capture_oracle.py --record`.
- **Performance baseline** (`brent-tools:performance-diagnosis`): the compile-throughput benchmark and the
  whole-population gate time, so no refactor regresses speed unseen. The instrument is
  `scripts/arch/perf_baseline.py` (kb/Work PB2117, which is also kb/Work A6's): warm compile throughput and the
  generated programs' hot paths through `tests/Cobol.Net.Benchmarks` (BenchmarkDotNet, each with a witness), cold
  whole-process compiles, the same hot paths against GnuCOBOL 3.2 under WSL with a 1x/2x/4x scaling curve, and the
  gate time read from `build-local.ps1`'s `verdict.json`. It writes the record, conditions and noise band included, to
  `docs/rearchitecture/evidence/perf-baseline/<product commit>.md`. The legacy byte engine is not a baseline
  (kb/Work R69 §1).

### R1 — The target architecture (designed, adversarially reviewed, owner-approved)
A design section, not code. It is produced by an architect agent, broken by an adversarial reviewer, and revised
until approved (the PB1708 workflow). kb/Work PB2118: the author is Mythos 5.1 under the owner's explicit approval
for that dispatch (R69 §4), the refuter is Opus, and the section lands as §8 of this document. It states:
- **The layers and their allowed dependencies.** Editions → Frontend (preprocessor, lexer, parser) → syntax tree →
  Binding (data, procedure) → bound tree → Validation → Lowering → CodeGen (the Roslyn backend behind
  `ICodeGenBackend`, keeping the CIL backend's seam) → Runtime.
  - Each allowed edge is written down, and an **architecture drift test** fails on any other edge: a Roslyn- or
    reflection-over-metadata test, one rule in one place.
- **Layout rules.** The namespace equals the folder path; one public type per file; a file name equals its type; and
  a partial type is split only along a named responsibility (one partial per concern, each named for it).
- **Decomposition targets** for each god-class candidate:
  - the responsibilities it holds;
  - the types they become;
  - the seam each new type exposes;
  - the order of extraction.
  Data-shaped code becomes data: `DiagnosticCatalog` is the obvious candidate, a generated or data-driven table
  keeping the PB1708-era lessons on static-state coupling.
- **Grammar structure.** Modular grammars per division or facility, where ANTLR's import rules allow. The rules
  already enforced stay: no left-edge semantic predicate in a lexer mode (`LexerDfaCacheDriftTests`); version
  predicates follow `{isXXXX()}?` factoring; generated parsers are never committed.
- **The .NET and C# target** (§5.5).

### R2 — The review fleet (findings, not fixes)
One review agent per (subsystem × dimension), in chunks of four, each finding adversarially verified. It follows
`.claude/skills/review` over `brent-tools:review`, extended by `brent-tools:architecture-audit` for whole-codebase
mode.
- **Subsystems:** preprocessor · lexer and grammar · parser drivers and the syntax tree · data binding · procedure
  binding · reference resolution · validation and edition gating · lowering and the bound tree · Roslyn code
  generation · OO · runtime values and numerics · runtime I/O · runtime control and exceptions · editions and
  diagnostics · CLI · tests, scripts and CI.
- **Dimensions:** the four of `PROMPT.md` §4 (architecture · full code · performance · duplication and efficiency),
  plus modern-C# conformance (§5.5).
- **Every finding** carries its site, the rule it breaks, a concrete scenario, the proposed target, and its R3 wave.
  Findings are filed as `kb/Work` notes of `kind: analysis` in an `arch-review` cluster, grouped by the file they
  touch (`fix_clusters.py`), so R3's waves are computed rather than hand-picked.

### R3 — Restructuring waves (behavior-neutral, one mechanism each)
Rolling waves, one mechanism per implementer. Each wave is one of:
- **Extract** (a responsibility out of a god class);
- **Unify** (a duplicate family into one rule in one place);
- **Move and rename** (layout, namespaces, file and type names, with EVERY caller changed in the same change, and no
  alias, forwarder or shim: CLAUDE.md rule 4);
- **Data-ize** (code that is a table becomes data plus a generator or loader);
- **Delete** (a census-measured dead member, type, file, script, doc or scaffold, removed with every caller and
  the drift test that pinned it; the wave records HOW the item was measured dead; a deletion that changes behavior
  is a `kind: defect` note for the fix lane, never a deletion). Owner mandate R69 §3; the program is PB2119 and the
  legacy engine (PB2110) is its first item.

**Order.** Leaves first: runtime values, then editions, then the front end, then binding, then code generation.
Each move then has a stable base, and renames land before extractions in the same area, so extraction diffs stay
readable.

**Every wave:**
- proves the §4 contract;
- adds or extends the drift test that keeps its new boundary true;
- updates the design docs it changes (CLAUDE.md rule 6).

### Who does what (R69 §4)
- **Mythos 5.1** (`claude-mythos-5-1`, Fable-tier price): R1's author (PB2118) and a second adversarial round only if
  the Opus refuter cannot break the design; explicit owner approval per dispatch, never a default
  (MANDATORY-PRACTICES P1).
- **Opus**: R0 tooling, the R1 refuter, R2 reviewers, extract and unify waves, landers and refuters;
  `model_rules.json` routes `^architecture` and `^build/(ci|legacy)` to it.
- **Sonnet**: census re-runs, the prose and register sweeps, move-and-rename and analyzer waves driven by a Roslyn
  rewriter or a code fix, Delete waves whose items the census measured dead. The §4 oracle proves a mechanical
  wave; the model does not. A Sonnet agent that meets a judgment returns `NEEDS-OPUS`.
- Review waves are planned from the `PB1754` cluster like any campaign (kb/Work PB2120,
  `DESIGN-orchestrator-loop.md` section 9.1): `plan_wave.py --cluster PB1754`, or `orchestrate.ps1 -Cluster PB1754`.

### R4 — Modernization (mechanical, analyzer-driven)
Per §5.5: language-version and SDK upgrade first, then one analyzer rule per wave applied with its code fix or a
`roslyn-analysis` rewriter across the whole tree, each wave behavior-neutral by §4.

### R5 — Close
- The full battery, the GnuCOBOL differential and the NIST run.
- The performance re-measure against the R0 baseline (a regression is a finding).
- Docs: `COBOLNET_DESIGN.md`, `DOC_INDEX.md`, `DRIFT_RULES.md` and this document's status.
- The architecture drift tests stay in the suite, so the structure stays true after the review.

## 4. The behavior-neutrality contract (every R3 and R4 wave)

A refactor is proven neutral by ALL of these, compared case by case, never by totals:
1. **Emitted C#.** Every program the suites compile (the corpus, NIST and the version-matrix samples) emits C#
   byte-identical to the baseline's, or each difference is a listed, explained, output-irrelevant change (a
   renamed runtime helper). The capture is recorded in R0.
2. **Diagnostics.** Every negative fixture and every diagnostic golden produces the same codes, severities,
   positions and messages.
3. **Tokens and parse trees**, for any grammar or lexer change: the token-stream differential that M6 used (type,
   channel, offsets, line, column, text and final mode) plus a parse-tree shape differential.
4. **Program output.** The whole-population gate (Conformance, Unit, Characterization), the Linux gate, and CI.
5. **Performance** not worse than the R0 baseline beyond noise, with conditions recorded:
   `python scripts/arch/perf_baseline.py --against docs/rearchitecture/evidence/perf-baseline/<R0 record>.md`. A row
   slower than its band (the script's docstring states the band) is a REGRESSION and a finding; a row the run no longer
   produces is a red too. No test asserts a time (kb/Work PB1590): the record is compared, never a stopwatch.

### 4.1 The oracle of items 1 and 2 (kb/Work PB2116)

**What is recorded is a manifest of hashes, not the emitted C#.** Thousands of programs at up to four editions would
put tens of megabytes of generated C# into the repository on every re-baseline, so the committed record is one
manifest, `docs/rearchitecture/evidence/arch-oracle/<commit12>.manifest.json`: one line per case with the SHA-256
of its emitted C# (null when the backend never ran) and of its diagnostic stream. The blobs themselves go to the
gitignored `TestResults/arch-oracle/<commit12>/` (`<id>.g.cs`, `<id>.diag.txt`), so a difference is diffed locally.
That directory holds exactly one manifest, the current baseline; re-recording replaces it and git history keeps the
old one.

**The population is the suites' own, by construction.** The cases and their compiles live in the Conformance
assembly (`tests/Cobol.Net.Tests.Conformance/ArchOracle.cs`), beside the theory families they mirror. Each case comes
from a `[PartitionedRowSource]` row member and is compiled through the same option builder or harness path its
theory calls (`ConformanceCorpus.PositiveOptions` and `NegativeCase`, `CorpusManifest.CompileOptions`,
`EditionHarness.CompileStaged` and `CompileNistObserved`), so a golden added to a manifest is captured with no edit.
The cases are:
- `corpus/<edition>/<name>`: each enabled positive golden at its edition directory's edition.
- `negative/<name>`: one per fixture, every edition its `*> reject-at:` header names, folded into one stream.
- `nist/<name>`: each green or divergent CCVS program at the golden run's edition (85), plus the chain predecessors
  its run compiles first.
- `nist-continuity/<name>@<edition>`: the INV-1 cells, check-only, permissive then strict.
- `matrix/<construct>@<edition>[+permissive]`: every compile the version-matrix theories perform. The obsolete
  theory's strict compiles are the strict matrix cells, so they count once.

The characterization corpus is not enrolled. It is already a byte-for-byte emitted-C# and diagnostic snapshot
oracle of its own (`EmittedCSharpSnapshotTests`, `DiagnosticSnapshotTests`). Programs written inline in test
classes are not enumerable from a row source and are covered by item 4 alone.

**A diagnostic stream** is the outcome, the compile-time DISPLAY output, every error and warning in the order the
driver produced them (code, severity, line, column and message, as the driver formats them), and whether the
compilation read the compile clock. **Portability:** the repository root and each scratch directory are written
`<repo>` and `<scratch>` with forward slashes, line endings are LF, and a compilation that read the compile clock has
its WHEN-COMPILED stamp masked. The capture also runs with every `COBOL*` variable scrubbed, the compiled-program
cache off (the oracle observes the compiler, never a stored result), and the repository root as its working
directory. Two captures of one commit are therefore identical. Windows and Linux captures of that commit differ
only in cases whose diagnostic names a COPY text found by a case-insensitive probe. The compiler names the text
as probed (`K1FDA.CPY` on Windows, the on-disk `K1FDA.cpy` on Linux), which affects 19 NIST continuity cells at the
first baseline. So the manifest records its `platform`, `compare_oracle.py` prints a NOTE when the two differ, and
a wave compares captures from one platform.

**Commands.** `python scripts/arch/capture_oracle.py --record` records the baseline (a clean tree only). It is run
once in R0 and again after each landed fix-lane train, because a fix legitimately changes emitted C#. The oracle is
re-based by that script, never by hand. A wave runs `python scripts/arch/compare_oracle.py`, which captures the
current tree and compares it with the recorded baseline case by case, printing `CSHARP`, `DIAGNOSTICS`, `ADDED`
or `REMOVED` per case and the verdict line `=== ARCH-ORACLE: IDENTICAL|DIFFERENT …`. Add `--diff` for unified
diffs. The wave's DEVLOG entry shows the IDENTICAL line or explains every listed case. The host process is
`tests/Cobol.Net.ArchOracle`, a member of the solution that holds no logic of its own.

**Drift.** `ArchOracleDriftTests` holds every `[PartitionedRowSource]` of the Conformance assembly enrolled, every
non-sentinel row a case, no two rows one case, an observation free of machine paths and the clock, and the recorded
baseline one well-formed manifest. It pins the enumerator, not the recorded manifest, which every golden-adding fix
outgrows until the next re-record.

**Item 3 is not yet built.** The token-stream and parse-tree differential for grammar changes is the next step, and
kb/Work PB2113 and PB2114 are its first consumers. It needs a capture of the lexer's token stream and the parse
tree's shape over the same population. That is a second observation per case on this same enumerator (a `tokens`
mode of the host), not a second population.

## 5. Standards applied

### 5.1 Architecture
Single responsibility per type; clean phase boundaries (no C# text in a bound node, no semantics in a renderer);
dependency direction enforced by test; one mechanism per job; explicit registries over reflection discovery;
composition over deep inheritance; closed hierarchies with exhaustive switches.

### 5.2 Duplication
Clone families by structure, not text: `roslyn-analysis` type-2 detection. Duplicate RULES matter as much as
duplicate code (one rule in one place); parallel mechanisms are the anti-pattern.

### 5.3 Layout and naming
Folder equals namespace; one type per file; names state the responsibility (no `Helper`, `Utils` or `Manager`
without a noun that says what); a partial type splits only along named concerns.

### 5.4 Review technique
- Parallel dimension agents with adversarial verification (`review`).
- Mechanical checks before judgment: semgrep invariants, analyzers at `latest-all`, and the drift-rule query.
- Calibrated severity.
- A sibling sweep for every confirmed finding (`variant-analysis`).
- A measured, not deduced, premise for every performance claim (`performance-diagnosis`).

### 5.5 .NET and C#
- **Today:** `net10.0`, C# 14 (`LangVersion` 14); `global.json` pins SDK 10.0.100 with `latestMinor` roll-forward.
  The .NET 11 SDK is installed as a preview.
- **Target: the latest STABLE .NET and its C# at the time R4 runs.** If .NET 11 is GA by then (its usual November
  release), that is `net11.0` with C# 15. Moving to a preview SDK is an owner decision (PB1754), never a default.
- **The one named exception:** `Cobol.Net.Compiler.SourceGen` targets `netstandard2.0` because Roslyn hosts
  generators on it. That is a real consumer, and it stays.
- **Features applied analyzer-first** (`brent-tools:dotnet-engineering`, "Modernizing an existing codebase"):
  - primary constructors; collection expressions; the `field` keyword;
  - extension members (C# 14); `System.Threading.Lock`;
  - `params` spans; frozen collections for static tables;
  - `[GeneratedRegex]`; span-based parsing on hot paths;
  - `required` members; pattern matching over type tests.
  Each is one R4 wave, and each is kept only if the §4 contract and the performance baseline hold.

**Alternative: a future .NET 11 / C# 15 update** (owner, 2026-09-29). If R4 runs after .NET 11 is GA, or the owner
chooses its preview (PB1754), two .NET 11 items are candidates for R1 to evaluate. Neither is adopted until then.
1. **C# 15 union types** (`union`: a value is exactly one of a fixed set of types, with compiler-enforced exhaustive
   pattern matching).
   - The bound tree and many dispatchers are closed hierarchies whose exhaustiveness is enforced today only by a
     `default: throw` arm and review. A union makes the compiler prove every arm is handled, which targets the
     forgotten-arm defect class (the "two-arm dispatch" pattern) at the type level.
   - R1 decides which hierarchies become unions and in what order. Each conversion is one R4 wave under the §4
     contract.
2. **.NET 11 JIT improvements**, for the compiler's own throughput and for the generated programs.
   - They are adopted only on a MEASURED gain: re-run the R0 performance baseline (the whole-population gate time
     and the compile-throughput benchmark) on net11.0 against net10.0, with conditions recorded
     (`brent-tools:performance-diagnosis`).
   - A regression blocks the upgrade, however the release notes describe it.

**Considered and set aside** (measured 2026-09-29):
- **Runtime-native async.** Only 2 files of the current compiler use `async`/`await`, so there is little to gain.
- **NativeAOT for the CLI.** The compiler runs Roslyn in-process and loads the assemblies it emits, and NativeAOT
  supports neither. AOT-publishing a compiled COBOL program would be a separate feature, not part of this review.
- **SIMD for packed and zoned decimal.** Those bytes exist only at the file boundary, so it is only a candidate if
  profiling shows that codec is hot.

### 5.6 Tests, scripts and CI
Layout and duplication only. Test behavior is the oracle and is not rewritten, except to follow a renamed
production type. Scripts follow the same no-wrapper rule.

## 6. Risks
- **Merge conflicts with the fix lane.** A whole-codebase rename touches everything. Mitigation: §2's quiet lane,
  renames per subsystem in one wave, then an immediate landing.
- **A neutral-looking refactor that changes behavior** through static-state or registration order (the PB1708
  lesson). Mitigation: the §4 emitted-C# and diagnostic differentials, not only test results.
- **Scope creep into fixes.** Mitigation: a defect found is a note, fixed in the fix lane (§1).
- **The split start** (R69): an R3 or Delete wave in a subsystem a fix-lane train is also editing. Mitigation: the
  wave's brief declares its file set, checked against the in-flight trains' file sets before dispatch; a collision
  defers the wave, never the train.
- **Cost.** About 16 subsystems × 5 dimensions of review agents, then tens of R3 waves. R2's findings set R3's real
  size, and the owner sees the estimate before R3 starts.

## 7. Owner decisions (tracked in PB1754, never here)
When to start; whether a preview .NET SDK is acceptable; whether project and assembly names may change (the NuGet id
`WiseOwl.COBOL` is public); and whether the tests/ layout is in R3's scope. **Decided:** R64 (2026-09-30: after zero
GAP; the SDK question trails; names may change; tests/ in scope) and R69 (2026-10-06: the start is SPLIT as §2
states; the legacy delete runs now; Delete is a wave kind; Mythos authors R1 under explicit approval).
