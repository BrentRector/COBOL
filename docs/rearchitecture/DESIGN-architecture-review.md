# DESIGN — The comprehensive architecture review and restructuring (kb/Work PB1754)

> **Status: DESIGN; R0 LANDED (PB2115 census · PB2116 oracle · PB2117 baseline); R1 APPROVED: §8 is the target architecture
> (Draft 10; Draft 1 by Mythos 5.1, Drafts 2–10 by Opus revisers answering nine Opus refuters; approved by the owner
> 2026-10-07 17:42 PDT, PB2118; owner questions 1–5 answered the same day); R2 to R5 sequenced in §2 and §3.** The owner asked (2026-09-29) for "a full, 100%, end-to-end, architectural review
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
baseline) and **R1** (PB2118) run now, R1 taking the approved `DESIGN-external-repository.md` as given; after R1's
approval, **R2 and every R3 wave** (the Delete program PB2119 included) run between fix-lane trains, each dispatched
only when its file set collides with no in-flight train and no open external-repository slice. That **file-set
partition is the only gate** between restructuring waves and fix-lane trains: the owner dropped R69 §2's hold on binding and code-generation restructuring on 2026-10-07 (PB2118 question 5, "Drop it"), together with
R64's zero-GAP hold on the `tests/` layout (PB1754), because the reason recorded for the hold, "the fix lane lives
there", was R64's residue, and the partition now handles that reason mechanically (a wave declares its file set, the
dispatcher checks it against the in-flight trains, and a collision defers the wave, never the train). Kept: "leaves
first" as an ORDERING preference, never a gate; the v1.0 timing of Cut 3 (PB2349, a release decision on the public
runtime namespaces); the external-repository slice blockers (a note whose files collide with an open slice's names
that slice in `blocked_by`); PB2417 first (the drift test and the edges file before any Move/rename or Extract);
PB2350 before any grammar wave; PB2118's own approval before any R3 wave (given 2026-10-07 17:42 PDT). Why now: the remaining GAP rows land in the
god classes (48 in §14.9, 22 in §13.18, 14 in §12.3 at 173 GAP), so every fix landed before the split enlarges the
extraction, and R1 lets those fixes be written into the target layout.
R0 has landed (the census, oracle and baseline records §8 cites). R1 is §8, Draft 10, approved by the owner on 2026-10-07 17:42 PDT: the nine Opus
refuters' findings against Drafts 1–9 (13+4, 10+3, 6+3, 4+0, 3+3, 3+1, 1+2, 1+1 and 3+5, blocking + non-blocking) are answered in the text
and recorded in PB2118; the 52 god-class findings are Extract notes PB2296–PB2347, and the designed extraction steps,
the tolerated rows' removals, `BoundTree.cs`'s split, the grammar regroup, the NumX re-model, the four namespace-root
flips, the report models' parse-context extract, the drift test's landing and the four removers the fifth refuter's
what-if needed are notes PB2352–PB2421. It binds every R2–R5 wave (PB2118, approved; R69 §7); owner questions 1–5
were answered on 2026-10-07 (R69 §7).

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
    or reader, measured by a caller query (the Delete program PB2119's input). The frozen evidence tree
    (`docs/rearchitecture/evidence/`, kb/Work/PB785) is not judged: a record there is kept as evidence and a later
    result supersedes it with a `superseded_by` marker, never a deletion (kb/Work PB2231, PB2232).
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
**Status: DONE** — §8 Draft 10, approved by the owner 2026-10-07 17:42 PDT (PB2118, landed). A design section, not code. It is produced by an architect agent, broken by an adversarial reviewer, and revised
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
  predicates follow `{isXXXX()}?` factoring (the five live ones are enumerated in §8.4); generated parsers are never
  committed.
- **The .NET and C# target** (§5.5).

### R2 — The review fleet (findings, not fixes)
The fleet as built (kb/Work PB2558–PB2561, after the owner-approved adversarial review of its first brief, w1034:
seven blocking findings B1–B7 and seven non-blocking N1–N7, all answered here). It follows `.claude/skills/review`
over `brent-tools:review`, extended by `brent-tools:architecture-audit` Phase 2 for whole-codebase mode. Every part
is tracked tooling with a self-test, so the next batch is a command, never a brief written from memory.
- **Subsystems are computed file sets** (B1). `scripts/arch/r2_subsystems.py` holds the table below, the one place
  it is written; this block is rendered from it (`--write-design`), and `--check` fails when the block differs or
  when the sets do not PARTITION the reviewed code (a file in no subsystem is a hole, a file in two an overlap). The
  drift test `ArchReviewFleetDriftTests` runs the check on the committed tree, so a new folder no subsystem owns is
  red until the table owns it. At 8be230068: 1,511 files (the root scripts and the agent role files included), 0 holes, 0 overlaps. The runtime's collation, Unicode and
  globalization code is a seventeenth subsystem, and the source generator belongs to the bound tree it generates for.
<!-- r2-subsystems:begin -->
| Subsystem (`key`) | Files: include | except |
|---|---|---|
| preprocessor (`preprocessor`) | `src/Cobol.Net.Frontend/Preprocessor/**` | — |
| lexer and grammar (`lexer-grammar`) | `src/Cobol.Net.Frontend/Grammar/**` `src/Cobol.Net.Frontend/*.ps1` | — |
| parser drivers and the syntax tree (`parser-tree`) | `src/Cobol.Net.Frontend/{Parsing,Cst,Expressions,Pipeline,Common}/**` `src/Cobol.Net.Frontend/*.csproj` | — |
| data binding (`data-binding`) | `src/Cobol.Net.Compiler/Binding/*.cs` `src/Cobol.Net.Compiler/Binding/Model/**` `src/Cobol.Net.Compiler/Binding/Passes/**` | `src/Cobol.Net.Compiler/Binding/ReferenceResolver*.cs` `src/Cobol.Net.Compiler/Binding/RefResolution.cs` `src/Cobol.Net.Compiler/Binding/QualifiedNameClasses.cs` `src/Cobol.Net.Compiler/Binding/EcNameResolution.cs` `src/Cobol.Net.Compiler/Binding/ReportGroupResolution.cs` `src/Cobol.Net.Compiler/Binding/Oo*.cs` `src/Cobol.Net.Compiler/Binding/Prototype*.cs` |
| procedure binding (`procedure-binding`) | `src/Cobol.Net.Compiler/Binding/Procedure/**` | — |
| reference resolution (`reference-resolution`) | `src/Cobol.Net.Compiler/Binding/ReferenceResolver*.cs` `src/Cobol.Net.Compiler/Binding/RefResolution.cs` `src/Cobol.Net.Compiler/Binding/QualifiedNameClasses.cs` `src/Cobol.Net.Compiler/Binding/EcNameResolution.cs` `src/Cobol.Net.Compiler/Binding/ReportGroupResolution.cs` | — |
| validation and edition gating (`validation`) | `src/Cobol.Net.Compiler/Validation/**` `src/Cobol.Net.Compiler/Binding/Validation/**` | — |
| lowering and the bound tree (`bound-tree`) | `src/Cobol.Net.Compiler/Binding/Bound/**` `src/Cobol.Net.Compiler.SourceGen/**` | — |
| Roslyn code generation (`codegen`) | `src/Cobol.Net.Compiler/CodeGen/**` `src/Cobol.Net.Compiler/*.{cs,csproj}` | — |
| OO (`oo`) | `src/Cobol.Net.Compiler/Oo/**` `src/Cobol.Net.Compiler/Binding/Oo*.cs` `src/Cobol.Net.Compiler/Binding/Prototype*.cs` | — |
| runtime values and numerics (`runtime-values`) | `src/Cobol.Net.Runtime/{Values,Intrinsics,Verbs}/**` `src/Cobol.Net.Runtime/*.{cs,csproj}` | — |
| runtime collation, Unicode and globalization (`runtime-text`) | `src/Cobol.Net.Runtime/{Collation,Unicode,Globalization}/**` | — |
| runtime I/O (`runtime-io`) | `src/Cobol.Net.Runtime/IO/**` | — |
| runtime control and exceptions (`runtime-control`) | `src/Cobol.Net.Runtime/{Control,Exceptions}/**` | — |
| editions and diagnostics (`editions-diagnostics`) | `src/Cobol.Net.Editions/**` `src/Cobol.Net.Frontend/Diagnostics/**` | — |
| CLI (`cli`) | `src/Cobol.Net.Cli/**` | — |
| tests, scripts and CI (`tests-scripts-ci`) | `tests/**` `scripts/**` `tools/**` `.github/**` `.claude/**` `*.ps1` `Directory.*.props` `Cobol.Net.sln` | — |

Under review: `src/**/*.{cs,g4,ps1,csproj}`, `tests/**/*.{cs,csproj,props,py,ps1,sh}`, `scripts/**/*.{py,ps1,sh,js,mjs,cs,yml}`, `tools/**/*.{cs,csproj,targets,sh,py,ps1}`, `.github/**/*.yml`, `.claude/**/*.{py,js,ps1}`, `.claude/agents/*.md`, `*.ps1`, `Directory.*.props`, `Cobol.Net.sln`. Left out: `**/*.{g.cs,g.i.cs,Designer.cs}` (generated, by the census rule `generated`; reviewed through its generator); `**/Generated/**` (a build output, the ANTLR parser, never committed); `**/obj/**` (a build output); `tools/claude-skills/**` (the pinned public-skills submodule, BrentRector/claude-skills, reviewed in its own repository).
<!-- r2-subsystems:end -->
- **Shards a reviewer can read whole** (B2). Each subsystem is cut by path into balanced shards of at most 6,000
  physical lines at the pin (the census `lines` column is per type, and a partial type's files fall in different
  shards, so only a file-level size adds up); a single larger file is its own shard. At 8be230068 the binding family
  is 9 + 5 + 1 shards, code generation 5, the runtime 10, the CLI 1. A shard (or a resumed pair's unread remainder)
  over 4,000 lines gets two finders that start from opposite ends (base `review` Scale: audit) and meet in the middle:
  each skips a file any finder of its pair has marked read. Every finder records a `read` line for each file it read
  WHOLE; the workflow diffs the union against the shard's files and sends a finisher to exactly the remainder (twice
  at most), and a pair still missing a file is reported INCOMPLETE.
- **The census and member index of the pin** (B7). The fleet reads one pinned commit, and its census record and
  member index are recorded at that commit first (`census.py --commit`, `member_index.py --record`); every reviewer
  cites the record of the tree it reads, and the shards and inputs are computed from it.
- **Mechanical inputs, once** (B4). `scripts/arch/r2_inputs.py --pin <tree> --out <dir>` runs the semgrep
  invariants, the analyzers at `AnalysisLevel=latest-all` (one C# project at a time in a detached worktree of the pin,
  each cached; `--projects` measures some now, and a shard's input is written once the projects holding its files
  are measured, because csc on `Cobol.Net.Compiler` with every rule on ran past 2,000 CPU-seconds and 28 GB while
  the leaf projects took under a minute), the drift-rule query
  per file and the open-notes scan ONCE, caches each step under the KEY it was made for (the pin, the step's version,
  the rule table; a step cached under another key is recomputed, never reused), and writes one self-contained input
  per shard: its files
  and lines, those facts filtered to its files, the census rows for it (god classes, unreachable and test-only
  families, folder/namespace disagreements, clone families touching it) and the measured namespace edges of its
  namespaces. **Duplication** is ONE whole-codebase pass over the census clone report (every family, each copy
  mapped to its shard, a cross-shard family one finding), plus each shard's "two mechanisms, one rule in two places"
  lens. **Performance** gets the R0 baseline record and `perf_baseline.py --against`; a claim without a measurement
  on the pin (a row of the baseline, or a count at a stated input size read from the pinned code; a reviewer builds
  nothing) is a lead (N3). **Modern C#** is analyzer-first: a finding is one analyzer rule (`wave_kind: modernize`,
  `analyzer_rule`), and a point without a rule id is a lead (N2). `AnalysisLevel=latest-all` turns on the CA rules
  only, so the analyzer tree's `.globalconfig` raises the IDE and SYSLIB rules of §5.5's features to warning; the
  feature-to-rule table (`MODERN_RULES` in `r2_inputs.py`, carried in every shard input) is the one place that mapping
  is written, and a feature with no rule (extension members, `params` spans, frozen collections, `required`) is a lead.
  Without it batch 1's inputs held 284 CA warnings and no IDE or SYSLIB rule (the w1034 claim refuter's C4); with it
  the runtime alone reports IDE0305, IDE0032, IDE0028, IDE0300, IDE0290, IDE0078, IDE0306, IDE0330 and IDE0066, and
  the frontend SYSLIB1045 sixteen times. The rules cost nothing measurable (Cobol.Net.Editions built in 4 s with and
  without them); the analyzer steps of batch 1's projects took 7 s (CLI), 10 s (Editions), 24 s (Frontend) and 23 s
  (Runtime).
- **The workflow** is `.claude/skills/workstream/templates/wf_r2_review.js` (`check_practices.py` holds its required
  parts; `scripts/arch/test_wf_r2_review.mjs` dry-runs every arm with stubbed agents). Every agent is the read-only
  role `cobol-reviewer` (Opus, effort high, 120 turns, the read-only hook, the bar; N1). It records one JSON line
  per decision the moment it decides (B3), through ONE tool that owns the format: `r2_collect.py --append <file>
  '<json>'` validates the record as the collector will (and refuses it with the reason, so the agent fixes it in
  context), into its own checkpoint file `review-<shard>--<dimension>--f<k>.jsonl`, `null-<pair>.jsonl` or
  `refute-<pair>--c<j>--<lens>.jsonl`; `r2_collect.py --status <batch dir> <pair>` tells it what its PAIR has decided
  (the files any finder read, every finding on disk, the decisions per lens, the next finding number), and it skips
  that. **Resume is from disk, per pair** (the w1034 claim refuter's C2: a workflow that planned from what agents
  RETURNED re-read a finisher's files on a relaunch, never ran the finisher again, left its findings unverified and
  re-decided findings whose skeptic chunks had moved; a finder that wrote findings and returned nothing orphaned
  them). Every launch, the first included, is made from `launch-args.json`, which `r2_collect.py --out <batch dir>
  --launch` writes: the batch args plus each pair's on-disk state. The workflow refuses args without it and plans
  each pair from it: finders only for the unread remainder, each new agent under a new finder number, a finisher
  after any agent that returned nothing (only an agent can read the disk), skeptics only for findings their lens has
  not decided; a decided pair starts no agent. Width: eight agents at once across every stage; a stop file (the
  owner's `scratch\STOP` or the fleet's `scratch\STOP-r2`) ends the batch after the agent that saw it.
- **Verification** (B6). Every finding is attacked by three skeptics with distinct lenses — the site (real, current,
  not already a note), the rule (it applies, the target agrees with §8, the wave kind and severity), the scenario (it
  occurs; the measurement; a defect is a spec defect) — in chunks of four findings per transcript. A finding stands
  when two of the three fail to refute it, and takes the corrections of the skeptics that upheld it, each only for
  the fields its lens judged (site: files, sites, members, the existing note; rule: design reference, wave kind,
  severity; scenario: measurement, harm, clauses), validated again after. A pair that found NOTHING is examined by an agent that tries to find what
  the finders missed, and a null result is accepted only when the pair is complete and examined.
- **Every finding carries** (B5) its id, kind (finding · lead · defect), title, the rule it breaks, a concrete
  scenario, the target, the repository-relative `files` (each must exist in the pin), the exact `sites`, the
  `members` a wave would move or change, the `census_ids` it rests on, its `design_ref` (§8.x, or the PBnnnn that
  already plans it), its wave kind (extract · unify · move-and-rename · data-ize · delete · modernize · a
  defect for the fix lane, with its harm and its `spec_refs` — the clause and its words, each run through
  `cite.py`'s check at collection, so a defect note's `spec_refs:` holds only checked clauses), calibrated severity
  (Critical · Warning · Suggestion; long-lived, high consequence; Critical files as MAJOR, the register reserving
  BLOCKER for an adjudicated severe wrong answer), `existing_note` and `owner_question`.
- **Deciding and filing.** `scripts/arch/r2_collect.py --out <batch dir>` reads the JSON lines FROM DISK (never the
  workflow's return) and decides each finding (upheld · refuted · unverified · lead · invalid · already tracked;
  two records under one id are invalid, never last-wins), each pair's completeness and each null, and says COMPLETE
  or LAUNCH NEEDED. **One mechanism, one note** (the w1034 claim refuter's C3: on 15 of batch 1's 35 pairs two
  finders read the whole shard, and five dimensions read each shard, so one mechanism can be found several times):
  upheld findings of the same wave kind whose members overlap or whose sites overlap are one mechanism, whichever
  finder, pair or dimension found them, and the most specific record is its primary.
  `python scripts/arch/file_census_notes.py --r2 <batch dir>/collected.json` refuses a batch that is not COMPLETE
  (an unverified duplicate upheld later would become a second note) and files every upheld MECHANISM as one note in
  the **`PB1754` cluster** (N6; the planner's `--cluster PB1754` reads it), carrying every id and an "Also found as"
  line per merged provenance; a mechanism an earlier batch filed takes the new ids on that note instead:
  `kind: analysis` for a restructuring finding, `kind: defect` for a defect the fleet hands the fix lane, its sites as
  backticked paths and its members as the `**Moves or changes:**` line, so `fix_clusters.py`, the member index and
  `plan_wave.py` compute the wave's file set (§8.7) rather than anyone listing it. Ids come from `alloc.py`; the
  finding ids on the note's `r2_ids:` line (and its `r2_members:`) make a re-run file only what is new; a path the
  committed tree no longer has is refused as stale.
- **Running a batch** (MANDATORY-PRACTICES O8, N7): read `scripts/orchestrator/budget.py` before each batch; write
  its args with `r2_inputs.py --out <dir> --batch <label> --shards <ids>` (the SMALL shards first, so the shape is
  measured before binding's 76,000 lines); `r2_collect.py --out <batch dir> --launch`, then launch the workflow with
  the `launch-args.json` it wrote; start `tools/claude-skills/skills/agent-fleet/references/stall_watch.py <its
  transcript dir>` beside it; after it ends (or stops), `r2_collect.py --out <batch dir> --launch` again, and launch
  again while it says LAUNCH NEEDED; file once it says COMPLETE.
- **Dimensions:** the four of `PROMPT.md` §4 (architecture · full code · performance · duplication and efficiency),
  plus modern-C# conformance (§5.5). Their criteria are written once, in `.claude/skills/review/SKILL.md`, the base
  skill's `references/dimensions.md` and §5; the workflow adds only each batch's inputs (N4).
- **When:** after R1's approval (PB2118), between fix-lane trains under the file-set partition (§2; R69 §2 as the
  owner amended it on 2026-10-07): the fleet writes findings, never code, so it collides with no train. Authority:
  R1's approval (2026-10-07 17:42 PDT, "Yes, approve and land"), this section, and the standing Workflow opt-in
  (kb/Work R49) (N5).

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
readable. "Leaves first" is the ORDER the planner prefers, never a gate: every R3 wave runs between fix-lane trains
once R1 is approved, dispatched only when its file set collides with no in-flight train and no open external-repository
slice (the file-set partition, §8.7; the owner dropped R69 §2's hold on binding and code-generation restructuring on 2026-10-07 (PB2118 question 5, "Drop it")).

**Every wave:**
- proves the §4 contract;
- adds or extends the drift test that keeps its new boundary true;
- updates the design docs it changes (CLAUDE.md rule 6).

### Who does what (R69 §4)
- **Mythos** (the family's newest Mythos or Fable model id, looked up at dispatch and recorded in that brief, never an id carried over from an earlier brief or this document (MANDATORY-PRACTICES P1; owner 2026-10-07: every model family runs at its latest version); Fable-tier price): R1's author (PB2118; Draft 1 ran on Mythos 5.1) and a second
  adversarial round only if the Opus refuter cannot break the design; explicit owner approval per dispatch, never a
  default.
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
- `optword/<format>`: every subset spelling of a format's optional words, as `OptionalWordSubsetDriftTests` compiles
  them, folded into one stream per format (enrolled when the sweep became a partitioned family, kb/Work PB2527).

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
diffs: the recorded baseline is hashes only, so when this machine never captured the baseline commit, `--diff`
captures it first, running that commit's own `capture_oracle.py` in a detached worktree of it, and keeps the blobs
under `TestResults/arch-oracle/<commit12>/`; a capture that does not reproduce the recorded hashes diffs nothing
(kb/Work PB2152, where the first explanation of a difference needed that capture by hand). The wave's DEVLOG entry shows the IDENTICAL line or explains every listed case. The host process is
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
- **Cost.** 17 subsystems cut into about 75 shards (`r2_subsystems.py`) × 5 dimensions of review agents, each
  finding with three skeptics, then tens of R3 waves. R2's findings set R3's real
  size, and the owner sees the estimate before R3 starts.

## 7. Owner decisions (tracked in PB1754, never here)
When to start; whether a preview .NET SDK is acceptable; whether project and assembly names may change (the NuGet id
`WiseOwl.COBOL` is public); and whether the tests/ layout is in R3's scope. **Decided:** R64 (2026-09-30: after zero
GAP; the SDK question trails; names may change; tests/ in scope), R69 (2026-10-06: the start is SPLIT as §2
states; the legacy delete runs now; Delete is a wave kind; Mythos authors R1 under explicit approval), and PB2118's
owner questions 1–5 (2026-10-07, recorded in R69 §7): question 5 DROPPED R64's zero-GAP hold, both on the program's
binding and code-generation waves and on the tests/ layout (PB1754 amended), so the file-set partition of §8.7 is the
only gate between restructuring waves and fix-lane trains; zero GAP stays v1.0's conformance half.

## 8. Target architecture (R1, kb/Work PB2118) — Draft 10, approved

> **Status: Approved by the owner 2026-10-07 17:42 PDT (Draft 10)** — "Yes, approve and land" (PB2118 "Approved").
> Draft 1 was written by Mythos 5.1 under the owner's per-dispatch approval of that
> day; an Opus refuter broke it on 13 blocking and 4 non-blocking findings (C1–C20) and Draft 2 answered them; a second
> Opus refuter broke Draft 2 on 10 blocking and 3 non-blocking findings (D1–D13) and Draft 3 answered those; a third
> broke Draft 3 on 6 blocking and 3 non-blocking findings (E1–E10) and Draft 4 answered those; a fourth broke Draft 4
> on 4 blocking findings (F1, F2, F5, F6) with three wording notes on upheld claims, and Draft 5 answered those; a
> fifth broke Draft 5 on 3 blocking and 3 non-blocking findings (G1–G6), and Draft 6 answered those; a sixth broke
> Draft 6 on 3 blocking findings and 1 non-blocking (H1–H4, two leads; H5 undecided, H6 upheld), and Draft 7 answered
> those; a seventh broke Draft 7 on 1 blocking and 2 non-blocking findings (J1–J3; J4 upheld), and Draft 8 answered
> those; an eighth broke Draft 8 on 1 blocking and 1 non-blocking finding (K1, K2; K3 upheld), and Draft 9 answered
> those; a ninth broke Draft 9's landing check on 3 blocking and 5 non-blocking findings (renames, identity by commit
> wording, a landing stopped by its own implementer; N1–N5), and this draft answers those, finding by finding in
> PB2118 "Draft 10". Owner questions 1 (§8.2), 2 to 4 (§8.7) and 5 (§2, §8.7: the file-set partition is the
> only gate between restructuring waves and fix-lane trains) were answered on 2026-10-07. The owner
> approved it the same day (PB2118); it is the design every R2–R5 wave executes against. Every claim about the code cites the R0 census record
> `docs/rearchitecture/evidence/arch-census/a02b165dae86b674e16399468144e1386171236b.json` (commit `a02b165da`,
> 2026-10-07; its findings file carries the `R0-nnnn` ids) by a field or an id, a `file:line`, or a number that record
> computes; a count the census does not yet compute is marked *text scan, provisional* and §8.2 makes the census compute
> it. Every claim about COBOL cites the standard through `cite.py --check`. The oracle (`arch-oracle/f620ccf6d43d`) and
> the baseline (`perf-baseline/354c79ef3312`) bound every wave below. **This section holds shapes only**: the work
> (each wave's order, blockers and sites) is the `kb/Work` notes that cite it (CLAUDE.md rule 8).

What the census measured, in one line: 2,087 types (1,413 authored) in 6 product projects; 52 god-class candidates;
206 namespace-to-namespace edges across 34 authored namespaces; 9 clone families; 219 unreachable members in 71
families and 1 unreachable type; 266 test-only members in 64 families and 3 test-only types; 15 dead artifacts; 234
findings. Filed: the 103 leaf notes PB2155–PB2257 (75 Delete, 19 Move/rename, 9 Unify) and the 52 god-class findings
as Extract notes PB2296–PB2347 (one per finding, in finding order, by `scripts/arch/file_census_notes.py --targets`
from §8.3's shapes); and, from the same targets file, kept in the repository beside the record it refines
(`docs/rearchitecture/evidence/arch-census/targets/a02b165dae86b674e16399468144e1386171236b.json`), 66
more, PB2352–PB2417: the 39 later steps of the seven designed extraction orders of §8.3, and 27 designed waves no
census finding names (the removals of §8.1's tolerated rows and the report models' parse-context extract they need,
`BoundTree.cs`'s steps, the grammar regroup of §8.4, the NumX re-model of §8.5, the four namespace-root flips of
§8.2 and the drift test's landing, PB2417, §8.1). The notes are rendered from that file and never hand-edited (`--rerender` keeps them
equal to it); all are cluster PB2119. The 67 test-only findings are judged by the rule of §8.7.

### 8.1 Layers and allowed edges

**The layers, as the code is.** The plan's list (Editions → Frontend → syntax tree → Binding → bound tree →
Validation → Lowering → CodeGen → Runtime) is refined on three measured points. (1) **There is no lowering layer**:
`COBOLNET_PIPELINE_DESIGN.md` D1 decided "no separate IR layer; the bound tree IS the model", `DESIGN-codegen-backend.md`
§2.3 made `Place` backend-neutral and structural, and the census has no namespace between the bound tree and code
generation; the lowering-shaped computations (`MoveClassifier`, `MoveOverlap`, `AccessPath`, `TableValuePlan`) are
bound-tree and model types, and each backend lowers privately. (2) **The runtime is a foundation, not the last
stage**: `Cobol.Net.Runtime` references no other project (census `projects`), and every compiler layer uses it —
`CobolNet.Frontend.Expressions → CobolNet.Runtime` 13 uses, `CobolNet.Binding → CobolNet.Runtime` 50,
`CobolNet.CodeGen → CobolNet.Runtime` 81 — because the one numeric and text law is written there once
(`CobolDec`, `CobolNames`, `CobolSpace`, `ExceptionCatalog` are the runtime types the front end names) and the
compiler folds constants with the same code the generated program runs. (3) **The compiler's own layers are already
namespaces**: `CobolNet.Binding.Model` (147 types), `CobolNet.Binding.Bound` (263), the binders (`CobolNet.Binding`
229, `.Procedure` 101, `.Passes` 10, `.Validation` 4, `CobolNet.Compiler.Oo` 22), `CobolNet.Validation` (25),
`CobolNet.CodeGen` (77) and `.Emit` (17); the layering is enforced between them, and inside a layer the structure is
§8.3's concern. Within the front end, the ANTLR-generated parser and its hand-written superclass are one unit by
construction (`CobolParserCore.g4` names `superClass = CobolParserCoreBase`; the measured edges run both ways,
`Generated → Parsing` 2 and `Parsing → Generated` 40), so they are one layer with the compile-time expressions that
parse directive fragments (`Expressions → Parsing` 1, `Preprocessor → Expressions` 17).

**What the backend reads today, and what it may read.** `DESIGN-backend-abstraction.md` §1.1 (given, §8.6) lists what
a bound node MAY contain — resolved symbols ("References, not names"), a structural `Place`, categories and numeric
facts, literal values, neutral runtime-data strings and the control-flow shape — and neither a binder nor a parse
context is on that list. The input that crosses `ICodeGenBackend` carries both today: `BoundCompilation` holds
`Core.CompilationUnitContext Tree` and `IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> InterfaceData`
(`Binding/Model/BoundCompilation.cs:34,38`); each `BoundUnit` holds `Core.ProgramUnitContext Ctx`, `DataBinder Data`
and `ReferenceResolver Refs` (`Binding/Model/BoundUnit.cs:34,48-49`); `EmitContext` exposes `DataBinder Data`
(`CodeGen/Emit/EmitCore.cs:19,34`), `UnitEmitters` holds `ReferenceResolver Refs`, emitters call
`ReferenceResolver.ResolveItem` (the `Place` builder) 24 times at emit time, and `OoEmitter`, `ProgramEmitter` and
`CSharpEmitter` call `BinderDriver`. Those are the facts §8.1's refusals rest on (kb/Work PB2293 records the leak
against backend-abstraction §1.3, whose table does not list it), and the wave kind that removes them is an Extract
(§8.3 (1) and (4): the binder hands the backend an immutable `DataBindResult`, and `PlaceBuilder` moves to
`compiler.model` and runs at bind time), not a Move.

The layers, bottom up, with today's namespaces (the §8.2 rename maps each one to one; the file below is keyed on the
names as they are at the time the test runs):

| # | Layer id | Namespaces (census `types` ids) | ISO anchor |
|---|---|---|---|
| 0 | `runtime` | `CobolNet.Runtime`, `.Collation`, `.Collation.Cache`, `.Collation.Cldr`, `.Collation.Locale`, `.Exceptions`, `.Globalization`, `.IO`, `.Unicode`, `.Unicode.Segmentation` | none: the standard defines no run-time library; its general rules assign duties to "the runtime system" (§8.4.3.2.4 GR6 b), "the runtime system attempts to locate the function being activated"), which this layer is part of. It is not a runtime module: "A runtime module results from compiling a compilation unit" (§14.6.1), which is the emitted `Cobol.<S>` assembly |
| 1 | `editions` | `CobolNet.Editions`, `CobolNet.Editions.Diagnostics` | the editions' word lists (§8.9, Reserved words) and the conformance criteria (§4.2.1: "To conform to this Working Draft International Standard, an implementation of standard COBOL shall provide the required normative elements") |
| 2 | `frontend.common` | `CobolNet.Frontend.Common`, `CobolNet.Common` (absorbed, R0-0221) | source text, spans, literals, compilation inputs |
| 3 | `frontend.diagnostics` | `CobolNet.Frontend.Diagnostics` | the front end's diagnostic bag |
| 4 | `frontend.parse` | `CobolNet.Frontend.Generated`, `CobolNet.Frontend.Cst`, `CobolNet.Frontend.Parsing`, `CobolNet.Frontend.Expressions` | the compilation stage's syntax: lexer, parser, parse tree, compile-time expressions (§7.1) |
| 5 | `frontend.preprocessor` | `CobolNet.Frontend.Preprocessor` | the text manipulation stage (§7.1) |
| 6 | `frontend.pipeline` | `CobolNet.Frontend` | the front-end orchestrator |
| 7 | `compiler.model` | `CobolNet.Binding.Model` | the compile-time facts: items, places, files, options, the symbol table, the bound unit; after §8.3 (1) and (4), `DataBindResult` and `PlaceBuilder` |
| 8 | `compiler.bound` | `CobolNet.Binding.Bound` | the bound tree (the seven `[BoundNode]` roots and their leaves) |
| 9 | `compiler.binding` | `CobolNet.Binding`, `CobolNet.Binding.Procedure`, `CobolNet.Binding.Passes`, `CobolNet.Binding.Validation`, `CobolNet.Compiler.Oo` | data and procedure binding (§10.3's source units; §13, §14) |
| 10 | `compiler.validation` | `CobolNet.Validation` | the post-bind passes and the one edition gate |
| 11 | `compiler.codegen` | `CobolNet.CodeGen`, `CobolNet.CodeGen.Emit` | the backends behind `ICodeGenBackend` (§14.1's statements rendered) |
| 12 | `compiler.pipeline` | `CobolNet` (root: `CompilerDriver`; `BinderDriver` and `BackendFactory` after §8.3) | the driver, a composition root |
| 13 | `cli` | `CobolNet.Cli` | the command line, a composition root |
| — | (tool) | `Cobol.Net.Compiler.SourceGen` | a build-time generator with no product reference; outside the graph by rule, not by omission |

The ISO anchors are the seams the standard itself draws: §7.1 names the "two logical stages of compilation group
processing — the text manipulation stage and the compilation stage" (layers 5 and 4); §10.3's source units are what
layer 9 binds ("A source unit begins with an identification division and ends with an end marker or the end of the
compilation group"); §14.1's statements are what layer 11 renders ("The procedure division in a function, a method,
or a program contains statements that are to be executed").

**The allowed edges — the whole list, and the file that holds it.** The rule lives in ONE place,
`docs/rearchitecture/architecture-edges.json`, which the drift test reads and this section only explains; an edge
between two layers is allowed if and only if the pair is listed; an edge inside a layer is always allowed; a namespace
the assemblies contain that the file does not place fails the test. The file's content as proposed:

```json
{
  "schema": 1,
  "rule": "DESIGN-architecture-review.md §8.1: a cross-layer dependency is allowed iff its (from, to) pair is listed; intra-layer edges are free; every product namespace is placed; a tolerated row's uses never grow; its removedBy names the kb/Work notes that remove it",
  "layers": [
    { "id": "runtime", "namespaces": ["CobolNet.Runtime", "CobolNet.Runtime.*"] },
    { "id": "editions", "namespaces": ["CobolNet.Editions", "CobolNet.Editions.Diagnostics"] },
    { "id": "frontend.common", "namespaces": ["CobolNet.Frontend.Common", "CobolNet.Common"] },
    { "id": "frontend.diagnostics", "namespaces": ["CobolNet.Frontend.Diagnostics"] },
    { "id": "frontend.parse", "namespaces": ["CobolNet.Frontend.Generated", "CobolNet.Frontend.Cst", "CobolNet.Frontend.Parsing", "CobolNet.Frontend.Expressions"] },
    { "id": "frontend.preprocessor", "namespaces": ["CobolNet.Frontend.Preprocessor"] },
    { "id": "frontend.pipeline", "namespaces": ["CobolNet.Frontend"] },
    { "id": "compiler.model", "namespaces": ["CobolNet.Binding.Model"] },
    { "id": "compiler.bound", "namespaces": ["CobolNet.Binding.Bound"] },
    { "id": "compiler.binding", "namespaces": ["CobolNet.Binding", "CobolNet.Binding.Procedure", "CobolNet.Binding.Passes", "CobolNet.Binding.Validation", "CobolNet.Compiler.Oo"] },
    { "id": "compiler.validation", "namespaces": ["CobolNet.Validation"] },
    { "id": "compiler.codegen", "namespaces": ["CobolNet.CodeGen", "CobolNet.CodeGen.Emit"] },
    { "id": "compiler.pipeline", "namespaces": ["CobolNet"] },
    { "id": "cli", "namespaces": ["CobolNet.Cli"] }
  ],
  "compositionRoots": ["compiler.pipeline", "cli"],
  "excluded": [ { "assembly": "Cobol.Net.Compiler.SourceGen", "reason": "build-time generator; references no product assembly (census projects[].projectReferences = [])" } ],
  "allowed": [
    ["frontend.common", "editions"], ["frontend.common", "runtime"],
    ["frontend.diagnostics", "frontend.common"], ["frontend.diagnostics", "editions"],
    ["frontend.parse", "frontend.diagnostics"], ["frontend.parse", "frontend.common"], ["frontend.parse", "editions"], ["frontend.parse", "runtime"],
    ["frontend.preprocessor", "frontend.parse"], ["frontend.preprocessor", "frontend.diagnostics"], ["frontend.preprocessor", "frontend.common"], ["frontend.preprocessor", "editions"], ["frontend.preprocessor", "runtime"],
    ["frontend.pipeline", "frontend.preprocessor"], ["frontend.pipeline", "frontend.parse"], ["frontend.pipeline", "frontend.diagnostics"], ["frontend.pipeline", "frontend.common"], ["frontend.pipeline", "editions"], ["frontend.pipeline", "runtime"],
    ["compiler.model", "frontend.common"], ["compiler.model", "editions"], ["compiler.model", "runtime"],
    ["compiler.bound", "compiler.model"], ["compiler.bound", "frontend.common"], ["compiler.bound", "editions"], ["compiler.bound", "runtime"],
    ["compiler.binding", "compiler.bound"], ["compiler.binding", "compiler.model"], ["compiler.binding", "frontend.preprocessor"], ["compiler.binding", "frontend.parse"], ["compiler.binding", "frontend.common"], ["compiler.binding", "editions"], ["compiler.binding", "runtime"],
    ["compiler.validation", "compiler.binding"], ["compiler.validation", "compiler.bound"], ["compiler.validation", "compiler.model"], ["compiler.validation", "frontend.preprocessor"], ["compiler.validation", "frontend.parse"], ["compiler.validation", "frontend.common"], ["compiler.validation", "editions"], ["compiler.validation", "runtime"],
    ["compiler.codegen", "compiler.bound"], ["compiler.codegen", "compiler.model"], ["compiler.codegen", "frontend.common"], ["compiler.codegen", "editions"], ["compiler.codegen", "runtime"],
    ["compiler.pipeline", "compiler.codegen"], ["compiler.pipeline", "compiler.validation"], ["compiler.pipeline", "compiler.binding"], ["compiler.pipeline", "compiler.bound"], ["compiler.pipeline", "compiler.model"], ["compiler.pipeline", "frontend.pipeline"], ["compiler.pipeline", "frontend.preprocessor"], ["compiler.pipeline", "frontend.parse"], ["compiler.pipeline", "frontend.diagnostics"], ["compiler.pipeline", "frontend.common"], ["compiler.pipeline", "editions"], ["compiler.pipeline", "runtime"],
    ["cli", "compiler.pipeline"], ["cli", "frontend.pipeline"], ["cli", "frontend.preprocessor"], ["cli", "frontend.diagnostics"], ["cli", "frontend.common"], ["cli", "editions"], ["cli", "runtime"]
  ],
  "tolerated": [
    { "from": "CobolNet.Binding", "to": "CobolNet.CodeGen", "uses": 3, "removedBy": "PB2395 (extract: the figurative-constant classification and FillChar to compiler.model — DataBinder.cs:2435,2470,2850,4855; DataBinder.ImpliedPicture.cs:212); and the numeric-edited VALUE image compose, DataBinder.cs:4792 -> ValueInitializer.EditedImageOfNumericValue, which PB2395 also moves to compiler.model once the alphabet family is there (Draft 6, G1)" },
    { "from": "CobolNet.Binding", "to": "CobolNet.CodeGen.Emit", "uses": 1, "removedBy": "PB2396 (move: EmitText.RepeatToWidth to frontend.common — DataBinder.cs:2970)" },
    { "from": "CobolNet.Binding.Procedure", "to": "CobolNet.CodeGen", "uses": 2, "removedBy": "PB2395 (the same extract — InitializeBinder.cs:869)" },
    { "from": "CobolNet.Binding.Model", "to": "CobolNet.CodeGen", "uses": 1, "removedBy": "PB2397 (extract: the C# member chain leaves the model — BoundUnit.cs:67, RuntimeApi.OuterChain)" },
    { "from": "CobolNet.Binding.Model", "to": "CobolNet.Binding", "uses": 20, "removedBy": "PB2361 (DataBindResult: BoundUnit.Data and .Refs, BoundCompilation.InterfaceData, OoClassUnit's binder), PB2375 (PlaceBuilder at bind time: OdoModel.SearchDepending's resolver, OdoModel.cs:188), PB2402 (OoMethodDataScope and OptionsModel.cs's types move with the OO symbols) and PB2399 (the rest: SymbolTable's binder, the binder statics the model calls, BoundCompilation.Turn, LinkageFormal, Table16Operand, the alphabet family); PB2399 also ends DataItem and FileModel -> ReferenceResolver (HasVariableLengthSubordinate, DataItem.cs:956, FileModel.cs:566,627) and moves NationalAlphabetDef with the alphabet family (Draft 6, G1)" },
    { "from": "CobolNet.Binding.Model", "to": "CobolNet.Binding.Bound", "uses": 2, "removedBy": "PB2400 (move: BoundUnit, OoClassUnit and BoundCompilation to compiler.bound, once they hold no binder, C# chain, parse context or OO-layer answer)" },
    { "from": "CobolNet.Binding.Model", "to": "CobolNet.Compiler.Oo", "uses": 5, "removedBy": "PB2402 (move: the OO symbols with the closure they name, to compiler.model; 5 -> 2) and PB2403 (AdapterPair to compiler.model; the OoClassTable answers become BoundCompilation data; 2 -> 0)" },
    { "from": "CobolNet.Binding.Model", "to": "CobolNet.Frontend.Generated", "uses": 4, "removedBy": "PB2398 (extract: BoundCompilation.Tree and BoundUnit.Ctx, BoundCompilation.cs:34, BoundUnit.cs:34, carry parse contexts across ICodeGenBackend; each reader's fact becomes a bound-node fact, DESIGN-backend-abstraction §1.1 lists no parse context)" },
    { "from": "CobolNet.Binding.Bound", "to": "CobolNet.Binding", "uses": 33, "removedBy": "PB2347 (move: StatementBinder and its SubscriptSegments partial out of Bound/) and PB2405 (the rule families the bound nodes read move into compiler.bound); PB2420 (move: EditionContext, SortCollation, IeeeSpecial to compiler.model) and PB2419 (BoundAllLiteral.Of leaves for ConcatFolder) end the last nine pairs (Draft 6, G1)" },
    { "from": "CobolNet.Binding.Bound", "to": "CobolNet.Binding.Procedure", "uses": 39, "removedBy": "PB2347 (the same move; MoveClassifier and MoveOverlap stay in compiler.bound); the walk reads 39 at 669e32aa9, two pairs more than at a02b165da (StatementBinder -> PlacementRules, UntilExitPlacement, landed after the census; Draft 7, H1)" },
    { "from": "CobolNet.Binding.Bound", "to": "CobolNet.Binding.Validation", "uses": 1, "removedBy": "PB2347 (StatementBinder moves to Binding/Procedure, the same layer as StatementValidation; a row landed after a02b165da by the fix lane, measured by the walk at 669e32aa9, StatementBinder.cs:446; Draft 7, H1)" },
    { "from": "CobolNet.Binding.Bound", "to": "CobolNet.Compiler.Oo", "uses": 6, "removedBy": "PB2402 (the OO symbols to compiler.model; 6 -> 1) and PB2347 (StatementBinder -> OoClassTable leaves Bound/)" },
    { "from": "CobolNet.Binding.Bound", "to": "CobolNet.Frontend.Preprocessor", "uses": 1, "removedBy": "PB2347 (the StatementBinder move)" },
    { "from": "CobolNet.Binding.Bound", "to": "CobolNet.Frontend.Generated", "uses": 18, "removedBy": "PB2347 (StatementBinder, the parse-context reader, out of Bound/) and PB2398 (BoundOo.cs's parse-context use becomes a bound-node fact)" },
    { "from": "CobolNet.Binding.Bound", "to": "CobolNet.Frontend.Parsing", "uses": 1, "removedBy": "PB2347 and PB2398 (the StatementBinder move and the parse-context extract)" },
    { "from": "CobolNet.Binding", "to": "CobolNet.Validation", "uses": 15, "removedBy": "PB2320 (move: BinderDriver, whose ten *Pass.Run calls are BinderDriver.cs:61-217, to compiler.pipeline) and PB2404 (move: IntegerOperandRules with IntegerSlot and IntegerSlotKind, IntegerOperandPass.cs:14-89, called from DataBinder.Constants.cs:858, to compiler.binding); PB2421 ends DataBinder -> VersionConformancePass (PictureConstructId to PicInfo, DataBinder.Reports.cs:3777), the pair no move of BinderDriver removes (Draft 6, G1)" },
    { "from": "CobolNet.Binding.Passes", "to": "CobolNet.Validation", "uses": 1, "removedBy": "PB2320 (BindPipeline's VersionConformancePass row, BindPipeline.cs:163, goes with the driver's pass schedule)" },
    { "from": "CobolNet.Binding.Procedure", "to": "CobolNet.Validation", "uses": 2, "removedBy": "PB2320 and PB2404 (the same moves)" },
    { "from": "CobolNet.CodeGen", "to": "CobolNet.Binding", "uses": 93, "removedBy": "PB2361 and PB2375 (extract: the backend input stops carrying the binder — DataBindResult replaces EmitContext.Data, EmitCore.cs:19,34, and BoundUnit.Data/Refs; PlaceBuilder runs at bind time, so UnitEmitters.Refs and the 24 emit-time ReferenceResolver.ResolveItem calls go), PB2290 and PB2320 (the BinderDriver calls of CSharpEmitter, ProgramEmitter and OoEmitter leave code generation), PB2402 and PB2399 (the OPTIONS, alphabet and linkage types move to compiler.model) and PB2405 (the report-model, intrinsic-rule, class-rule and MOVE-rule families move to compiler.bound); PB2418 (OoEmitter -> BinderDriver: ExternalItemIdentity to DataItem) and PB2420 (the value types: EditionContext, CallExternalBacking, ProgramSpecifier, RangeCollationCarrier, CellLifetime, IeeeSpecials, the report field sources) end the pairs onto types other than DataBinder and ReferenceResolver (Draft 6, G1)" },
    { "from": "CobolNet.CodeGen.Emit", "to": "CobolNet.Binding", "uses": 33, "removedBy": "PB2361, PB2375, PB2402, PB2399 and PB2405 (the same extracts and moves); PB2420 ends EditionContext, RangeCollationCarrier and RelationPair (Draft 6, G1)" },
    { "from": "CobolNet.CodeGen", "to": "CobolNet.Binding.Procedure", "uses": 4, "removedBy": "PB2406 (the four cursor records of Binding/Procedure/PlaceCursor.cs and ReferenceResolver.BuildCellPath to compiler.model; EcBinder.ExternalNames to the model, IntrinsicBinder.CompileClock read at bind time)" },
    { "from": "CobolNet.CodeGen", "to": "CobolNet.Compiler.Oo", "uses": 19, "removedBy": "PB2402 (the OO symbols to compiler.model) and PB2403 (AdapterPair to compiler.model; the OoClassTable, ActivationDescriptions and OoStandardClasses answers reach the backend as BoundCompilation data); PB2403's sites include ProgramEmitter's OoClassTable read (Draft 6, G1)" },
    { "from": "CobolNet.CodeGen", "to": "CobolNet.Frontend", "uses": 3, "removedBy": "PB2407 (move: CompilationInputs with the InputProbe and EnvironmentRead it names, Pipeline/CompilationInputs.cs, to frontend.common)" },
    { "from": "CobolNet.CodeGen", "to": "CobolNet.Frontend.Generated", "uses": 2, "removedBy": "PB2398 (the two code-generation parse-context reads become bound-node facts)" },
    { "from": "CobolNet.CodeGen", "to": "CobolNet.Frontend.Preprocessor", "uses": 1, "removedBy": "PB2408 (extract: the DirectiveResults read, CSharpEmitter.cs:32, moves to bind time)" },
    { "from": "CobolNet.Frontend.Preprocessor", "to": "CobolNet.Frontend", "uses": 3, "removedBy": "PB2407 (the same move)" },
    { "from": "CobolNet.Frontend.Parsing", "to": "CobolNet.Frontend.Preprocessor", "uses": 1, "removedBy": "PB2409 (move: ReferenceFormatProcessor.DebugLineCarrier to frontend.common — DebuggingLineRewriter.cs:95)" }
  ]
}
```

Of the 207 measured edges, 48 are intra-layer, 132 are allowed and 27 cross a layer boundary the list does not admit
(the census's 26 plus the walk's `Binding.Bound → Binding.Validation`, H1); the 27 total 314 uses, and every row's `removedBy` names exactly the kb/Work notes that remove part of it (each
note's "Removes" line names the rows back, both rendered from the targets file); a note a removal must wait for is in
the remover's `blocked_by`, never in `removedBy`.

**The file is keyed to the tree PB2417 lands on, not to `a02b165da`.** The census is a snapshot and the fix lane keeps
landing: three landings after it (861110338, 8a22e2a37, cad6957d0) gave `StatementBinder` one pair onto
`Binding.Validation` and two more onto `Binding.Procedure` (the sixth refuter, H1), so a file frozen at the census fails
its own walk on the day it lands. The rows above are the census's values plus what the walk measured at `669e32aa9`
(the one new row, and the grown `Binding.Bound → Binding.Procedure` pairs inside its cap); they are the proposal, not the
landed file. PB2417 lands the file by measuring it, never by copying this block: on its landing tree it runs a new
census record and the walk's seed mode; the seed writes `uses` (census) and `walkUses` (walk) for every row, and lists
EVERY cross-layer type pair the file does not allow, the pairs inside an already-tolerated row included. The checker's
what-if then names the remover of each pair mechanically: it replays the planned Move/rename and Extract notes' move
maps in their `blocked_by` order (the maps are the targets file's) and names the first note after which the pair is
allowed or intra-layer; each tolerated row keeps its pairs with their removers (`pairs`), its `removedBy` is their
union, and a pair no note removes blocks PB2417 until a note is filed for it. Per row was not enough (the seventh
refuter, J1): a fix-lane pair added before PB2417 inside a row the file already tolerates raised only that row's count,
which the seed rewrites, so it entered with no remover and "no tolerated pair is left at the end" failed with no
ratchet noticing. From PB2417 on, a landing that adds a cross-layer pair fails the drift test in that landing's own
gate, because the file lists the pairs and a pair it does not list fails even when its row's count does not grow; no
hand refresh of this file is ever the mechanism that notices drift.

**Where a moved type lands: its closure, measured before the wave.** A Move changes no reference; it changes only the
namespace a type is in. So the rows after a move are today's type pairs re-keyed by the move map, and that what-if is
computable before the wave from the same pairs the walk and the census already count. The rule: a type moves to the
lowest layer whose allowed list admits every type it names, and every type it names that is not admitted moves with
it in the same wave (its closure); a reference no move can carry (a parse context, the binder or resolver instance, a
validation pass, a preprocessor type) is cut by an Extract note first, and that note is in the move's `blocked_by`. A
wave's brief runs the what-if on its move map, and a row that would grow, or a pair on a row the file neither allows
nor tolerates, is a blocker found before dispatch, not by the ratchet after. Drafts 1–3 moved type lists that fail it
(refute 3, E1): the OO symbols alone grow `Binding.Model → Compiler.Oo` 5 → 6 and `Binding.Model → Binding` 20 → 22,
because they name `NamingConvention`, `StandardMethod`, `OoMethodBinding`, `OptionsModel` and `OoMethodDataScope`. The
waves are therefore: PB2401 cuts what the symbols name that the model may not (the three `Ctx`, `OoMethodBinding`'s
`BoundDeclarative` list, `OptionsModel`'s two parse-context factories); PB2402 moves the symbols with their closure
(`NamingConvention`, `StandardMethod`, `OoMethodBinding`, `OoFormal`, `OoMethodDataScope` and `OptionsModel.cs`'s
types); PB2403 moves `AdapterPair` and turns the remaining OO answers into `BoundCompilation` data; PB2399 ends the
model's other binder reads; PB2416 cuts the report models' parse contexts, so PB2405 can move the four rule families
the emitters read into `compiler.bound` (they name bound nodes, so `compiler.model` cannot hold them); and PB2400
moves `BoundUnit`, `OoClassUnit` and `BoundCompilation` last. **The measure is the walk, not a text scan.** Draft 4's
scan could not see `Core.`-aliased parse contexts or qualified reads (refute 4, F1: `Binding.Bound → Frontend.Generated`
scan 0, census 18), so its "no row grows" was not evidence. The per-row before and after of every Move/rename and
Extract wave is now the drift test's own walk (below) over the built assemblies, re-keyed by the wave's move map and cut
list, and it is in kb/Work PB2118 (Draft 5, F1); kb/Work PB2417 lands that walk as the program's first wave, so a
wave's "neutral" means "the walk shows no row growing". The walk re-scoped six waves: PB2416 also cuts `ReportModel`'s
and `ReportCodeModel`'s parse contexts and moves the two PICs `AlgebraicRanges` reads from the procedure binder to the
model; PB2405 sends `CobolClass` (an enum the model's `RecordLayout` names) to `compiler.model`; PB2391 moves only
`BoundStores`, because `MoveClassifier` and `MoveOverlap` are the bound-tree computations of point (1) above, which a
bound node calls and an emitter reads; PB2404 moves `IntegerOperandRules` with `IntegerSlot` to `compiler.binding`, which
may read the parse contexts its table is keyed on; PB2406 moves all four cursor records with
`ReferenceResolver.BuildCellPath`; PB2407 moves `CompilationInputs` with `InputProbe` and `EnvironmentRead`. Draft 5's
sequence left out the Extract notes that move a member across a layer (the fifth refuter, G1), so Draft 6 runs the
what-if over EVERY note that does (26 steps, PB2118 Draft 6): PB2320 moves `BinderDriver` to `compiler.pipeline` only
after PB2418 takes out the three members other layers call (`BindProcedures`, `UserFunctionsOf` and
`ProgramPrototypesOf`, `ExternalItemIdentity`); PB2395 waits for PB2399, which moves the alphabet family
`FillChar` names (`NationalAlphabetDef` added to it); and every tolerated pair the named removers left now has one
(PB2395 also moves the numeric-edited VALUE compose, PB2399 also ends `HasVariableLengthSubordinate`'s model reads,
PB2419, PB2420 and PB2421 end the rest; the rows' `removedBy` say which). Over the whole sequence, in the order the
notes' `blocked_by` forces, no row grows at any step on the walk and no tolerated pair is left at the end (the union
observer adds one text-scan match, `ProgramSpecifier`'s property named `ExternalizedName`, which names no type). The
largest refusal is
`compiler.codegen → compiler.binding` (149 uses: `CodeGen → Binding` 93, `CodeGen.Emit → Binding` 33,
`CodeGen → Binding.Procedure` 4, `CodeGen → Compiler.Oo` 19), and it is what makes the backend-neutrality contract
(`DESIGN-backend-abstraction.md` §1) checkable: an emitter that can reach a binder reads binder state — today it does,
through the facts listed above — instead of the immutable `BoundCompilation`. Neither `compiler.model → frontend.parse`
nor `compiler.bound → frontend.parse` is allowed: a parse context is not on §1.1's list, so the 23 uses that cross those
pairs today are tolerated rows with an extract, and the walk fails the day a new one appears.

**Allowed pairs with no measured edge.** Seven allowed pairs measure zero today, all from the two composition roots:
`cli → editions`, `cli → frontend.common`, `cli → frontend.pipeline`, `compiler.pipeline → compiler.bound`,
`compiler.pipeline → compiler.validation`, `compiler.pipeline → editions` and `compiler.pipeline → frontend.common`. A
composition root may name every layer below it, because composing them is its one job (D7 threads the typed edition
carrier from the CLI to the driver; the driver hands `BoundCompilation` to the validation schedule of §8.3 once
`BinderDriver` moves to it); the file names them in `compositionRoots` so the reason is data, not prose. Every other
allowed pair is measured. Draft 1's three unmeasured non-root pairs (`compiler.binding → frontend.diagnostics`,
`compiler.binding → frontend.pipeline`, `compiler.validation → frontend.diagnostics`) had no such reason and are
dropped: a binder that needs one adds it to this file in a reviewed change.

Two measured cycles are dissolved by placement rather than by refusal: `CobolNet.Compiler.Oo ↔ CobolNet.Binding` (34
and 40 uses) is one layer, and `CobolNet.Frontend.Generated ↔ CobolNet.Frontend.Parsing` (2 and 40) is one layer, for
the reasons given above. The runtime is one layer here because its root namespace is a bag (census `types`: every
file under `Control/`, `Values/`, `Intrinsics/` and `Verbs/` declares `namespace CobolNet.Runtime`; findings R0-0225,
R0-0229–R0-0234) and the measured cycles `Runtime ↔ Runtime.IO` (10 and 49), `Runtime ↔ Runtime.Exceptions` (44 and
8), `Runtime ↔ Runtime.Globalization` (8 and 9) cannot be read until that bag has namespaces; the Move waves of §8.2
give it them, and the first census after those waves writes the runtime's own layer table into the same file (values
← exceptions ← control ← I-O, verbs and intrinsics, the order `DESIGN-runtime-library.md` §2 already states), with
its own tolerated rows. The front end's internal order is in the file now because its namespaces exist.

**The drift test.** `ArchitectureEdgesDriftTests` (tests/Cobol.Net.Tests.Unit) is a metadata walk over the built
product assemblies, not a source scan and not an MSBuild workspace, so it costs about a second per gate:
- **Population, one rule for both observers:** it opens each product assembly the test project's build output contains
  (`Cobol.Net.Editions`, `Cobol.Net.Frontend`, `Cobol.Net.Compiler`, `Cobol.Net.Runtime`, `cobol`) with
  `System.Reflection.Metadata` and walks the AUTHORED type definitions only, by the census's rule: a type is generated
  only when every document that declares it is a generated output (a `Generated/` folder, a `*.g.cs` file, a source
  generator's output; `tools/ArchCensus/Program.cs:166`), so a partial with one hand-written document is authored. An
  attribute test is not that rule: at `a02b165da` the two populations differ by 12 types — the source generator's
  `BoundNodeAttribute`, `BoundVisitor`, `BoundStatementTree` and seven `I{Root}Visitor<T>` interfaces and the generated
  `CobolNet.Editions.Constructs` carry no `[GeneratedCode]` (the census marks them generated), and the census counts
  `CobolNet.Frontend.Generated.CobolParserCore` as authored (its hand partial `Parsing/CompilationUnitTokenRetypes.cs`)
  while the merged metadata type carries `[GeneratedCode("ANTLR", …)]`. So the walk reads each type's documents from
  the portable PDB beside its assembly (the documents of its methods' sequence points, and the `TypeDefinitionDocuments`
  custom debug information the C# compiler writes for a type with no method body) and applies the census's rule, and
  its self-test asserts that its authored set equals the newest census record's. The 12 types' edges are intra-layer
  today, so the difference changes no count in this section. Nested types fold into their outermost type, as the
  census's `fan` rule does.
- **What a reference is:** every type named by the base type, the interface implementations, the generic parameter
  constraints, the field, property, event and method signatures, each body's local-variable signature, each body's IL
  operand tokens (the opcode table is read from `System.Reflection.Emit.OpCodes`, in-box on net10.0, so the decoder is
  forty lines and owns no opcode list), and each custom attribute's constructor and its `typeof` arguments (decoded from
  the blob's serialized type names); a generic instantiation contributes each of its arguments.
- **"uses" is defined once:** the number of distinct (outermost source type, outermost target type) pairs between two
  namespaces — the census's unit (`RawEdge.Pairs`, `tools/ArchCensus/RawCensus.cs:64`), counted over the walk's own
  population.
- **Two observers, two ratchets, one file.** The `uses` column of a tolerated row is the census's count (the values
  above are its measure at `a02b165da`), and `ArchCensusDriftTests` gains the assertion that every cross-layer entry of
  the newest record's `namespaceEdges` is allowed, or tolerated with a count no greater than the row's `uses`. The walk
  cannot see what the compiler erased — an inlined `const` read (`ConditionRenderer.cs:830` reads
  `ClassConditionModel.Numeric`; `OoEmitter.cs:398` reads a `NamingConvention` const) and a `nameof` — so its count can
  sit below the census's; it therefore keeps its own column, `walkUses`, which its first run writes (the test's seed
  mode, run once in kb/Work PB2417, the note that lands the test and the file as the program's first wave) and every
  later run ratchets; the seed runs on PB2417's landing tree and keys the file to it (above). Every Move/rename and Extract note of PB2119 waits for PB2417, and `work.py check` fails one that
  does not. Each observer fails when its count for
  a row grows, and a row is deleted only when BOTH measure zero (a pair whose last uses are consts or `nameof`s is
  still a dependency).
- **It fails when:** a product namespace is not placed by the file; a cross-layer edge is neither allowed nor
  tolerated; a tolerated row's `walkUses` grows; a namespace the file names exists in no assembly; an allowed pair that
  is not from a composition root measures zero in both observers (a dead allowance is deleted, never kept).
- **The what-if.** The checker takes an optional move map (type → namespace) and re-keys its pairs by it before it
  aggregates, so the same code that ratchets the rows answers "what would this move do to them"; a move wave's brief
  records that answer for its map. A rename wave (a folder-equals-namespace move or a root flip) re-keys the file in
  the same change: the new namespace joins its folder's layer, so no layer pair changes, and each tolerated row it
  splits is re-measured by the walk, the parts summing to the row (refute 4, lead 1).
- It is seen failing once, on a planted edge, before it lands (`brent-tools:architecture-audit` Phase 3), through a
  self-test that feeds the checker a synthetic edge set.

One rule, one file, two observers at two cadences — the per-gate metadata walk and the per-wave census — which is the
oracle's own shape (§4.1: a hash manifest per gate, the battery per batch).

### 8.2 Layout and naming rules

The rules (each a drift assertion, listed with the measure that shows today's distance from it):
1. **Folder equals namespace**, the project's root namespace plus its folder path (the census's `folderNamespace`
   rule). Measured: 19 folder–namespace disagreements (R0-0216–R0-0234) covering 187 authored files (*text scan,
   provisional*), of which 83 are the runtime's `Control/`, `Control/Signals/`, `Values/`, `Values/Numeric/`,
   `Values/Tables/`, `Values/Text/`, `Intrinsics/` and `Verbs/` folders declaring the root namespace, 74 are the
   compiler's `Binding/Procedure/Verbs/` (40), `CodeGen/Verbs/` (20) and `CodeGen/Roslyn/` and `CodeGen/DataDivision/`
   (7 each) declaring their parent's namespace, 13 are `Oo/` declaring `CobolNet.Compiler.Oo` under a root of
   `CobolNet`, and the rest are the front end's `Common/` (2, in `CobolNet.Common`), `Parsing/` (3, in
   `CobolNet.Frontend.Generated`) and `Pipeline/` (2, in `CobolNet.Frontend`) and the runtime's `Collation/CLDR/`
   (4, `Cldr`), `IO/Sharing/` (3) and `Control/Signals/` (2, in `Runtime.Exceptions`) and `IO/` (1). The 19 notes are the first
   Move/rename waves. A folder whose name is not a namespace segment (`CLDR` versus `Cldr`) renames to the segment.
2. **One public type per file.** Measured (*text scan, provisional*): 178 of 553 authored files declare more than one
   top-level type and 129 declare more than one public one. The exception, stated once: **a closed node family is one
   file named for its root** — a `[BoundNode]` root (or a §8.5 union) and its sealed leaves (`BoundCall.cs` holds
   `BoundCallProgram`, `BoundCancel`, `BoundGoback`…), and a small enum or record that exists only as a member of the
   type beside it (`PassPhase` with `IBindPass`); the family is the unit a §8.5 union declares, so the file and the type
   coincide again when the union lands.
3. **File name equals its type** (for a family file, its root). Measured: 22 files are named for no type they declare
   (`CallAbi.cs` holds `CobolPassMode`, `CobolArg`, `BoundaryItem`…; `EmitCore.cs` holds `EmitContext`, `NumX`,
   `NumXCarrier`; `ReportWriter.cs` holds `CobolReport`; `ExceptionState.cs` holds `ExceptionEngine`).
4. **A partial type splits only along a named concern, and the partial is named for it.** Measured: 11 of the 52
   candidates are partial, with 45 named concerns between them (`DataBinder.Reports.cs`, `CobolIntrinsics.Dec.cs`);
   `DESIGN-module-topology.md` §2.15 goes further ("partial files are allowed only for source-generated halves") and
   that is the end state for a hand-maintained type: a concern that deserves a partial deserves a type (§8.3), so a
   partial is a transitional seam, allowed while the concern it names is on its way out.
5. **Names say the responsibility** (§5.3): no `Helper`, `Utils`, `Manager`, `Core` or `State` without the noun that
   says what; `EmitCore.cs` and `ExceptionState.cs` are the measured offenders among the 22 of rule 3.
6. **The census computes rules 1–3** — three new per-type columns (`files`, `publicTypesInFile`, `fileNameMatches`)
   so these counts stop being text scans, and `file_census_notes.py` files their findings like the others (rules 2 and
   3 have no notes until it does); the drift assertions read the newest record, as `ArchCensusDriftTests` does.

**Names: what changes, what does not, and the one owner question.**
- *Project and assembly names do not change.* `Cobol.Net.{Editions,Frontend,Compiler,Runtime,Cli}` and the
  `cobol` executable (`COBOLNET_PROJECT_ORG_DESIGN.md` §1.1) stay: the host-entry and activation design
  (`DESIGN-external-repository.md` §4, §7.3) names `Cobol.Net.Runtime` as the assembly a host references, and the
  NuGet ids `WiseOwl.COBOL` and `WiseOwl.COBOL.Runtime` (`DESIGN-USER-DOCUMENTATION.md` §6, owner R50) are package
  ids, which NuGet keeps independent of assembly names. R64 #3 allows a change ("Project and assembly names may
  change in the review"); nothing is gained by one.
- *The namespace roots become the project names: **owner decision, PB2118 question 1, answered YES on 2026-10-07***
  ("Yes to: May the namespace roots CobolNet, CobolNet.Frontend, CobolNet.Editions and CobolNet.Cli become the project names (Cobol.Net.Compiler, Cobol.Net.Frontend, Cobol.Net.Editions, Cobol.Net.Cli), reversing COBOLNET_PROJECT_ORG_DESIGN.md §1.1, which keeps the single token CobolNet?"). It was the owner's question because R64 #3 speaks of project and assembly
  names only. The record that speaks of namespaces is `COBOLNET_PROJECT_ORG_DESIGN.md` §1.1 ("root namespaces stay
  the single token `CobolNet`", with the owner named as the one who may prefer `Cobol.Net` namespaces), and §8.2
  rule 1 does not force a flip: the census's rule is the project's `RootNamespace` plus its folders, and the
  compiler's `RootNamespace` is `CobolNet` (`Cobol.Net.Compiler.csproj:16`); Draft 2 had decided the flip from R64 #3,
  which was the owner's to decide. The owner chose shape A of the two Draft 3 put:
  - **Shape A — the roots become the project names (CHOSEN):** `CobolNet` → `Cobol.Net.Compiler`, `CobolNet.Frontend` →
    `Cobol.Net.Frontend`, `CobolNet.Editions` → `Cobol.Net.Editions`, `CobolNet.Cli` → `Cobol.Net.Cli`, the test roots
    likewise, the generated parser through the single-sourced `<AntlrNamespace>`; it reverses PROJECT_ORG §1.1 and
    resolves R0-0220 by itself (`Oo/` under `Cobol.Net.Compiler` is `Cobol.Net.Compiler.Oo`). Files (*text scan,
    provisional*): the editions flip edits the `using` lines of 117 binding, code-generation, validation and OO files
    and 48 test files, the front-end flip 113 and 152, the compiler flip every compiler file (288) and about 650 test
    files. So every flip is a wide wave: it runs between fix-lane trains only when its file set (those files)
    collides with no in-flight train (the file-set partition, §8.7), which in practice means a quiet fix lane. The oracle: IDENTICAL for these four (no emitted `.g.cs` and no diagnostic stream names a
    compiler namespace, checked over the 14 characterization snapshots and the oracle blobs). Cut 3: the runtime root's
    flip to `Cobol.Net.Runtime` (given, v1.0, PB2349) becomes the first of five flips to one spelling. The four waves:
    PB2412 (editions), PB2413 (front end, after it), PB2414 (CLI) and PB2415 (compiler, last; it resolves R0-0220,
    so PB2243 waits for it); `COBOLNET_PROJECT_ORG_DESIGN.md` §1.1 and `DESIGN-module-topology.md` §2.2 record the
    reversal.
  - **Shape B — PROJECT_ORG §1.1 stands (not chosen):** the roots stay `CobolNet`, `CobolNet.Frontend`, `CobolNet.Editions`,
    `CobolNet.Cli`; rule 1 holds against each project's `RootNamespace` as the census already measures it; R0-0220
    (`Oo/` declaring `CobolNet.Compiler.Oo` under the root `CobolNet`) is a 13-file Move like the other 18
    disagreements (the namespace follows the folder, `CobolNet.Oo`, or the folder follows the namespace); and
    `DESIGN-module-topology.md` §2.2's `CobolNet.Compiler` row is corrected to the csproj's `CobolNet`. Files: none
    beyond the 19 Move notes. The oracle: unaffected. Cut 3: still flips the runtime to `Cobol.Net.Runtime` (given), so
    after v1.0 the runtime is the one root spelled with dots, which PROJECT_ORG §1.1 then records as its exception.
- *Cut 3 is more than a `using` line.* The emitted bodies name runtime types fully qualified inline — the oracle blobs
  carry `CobolNet.Runtime.Exceptions.ExceptionCatalog.IsFatalIoStatus` 6,249 times (from
  `CodeGen/Verbs/IoStatusClass.cs`) and `CobolNet.Runtime.IO` 1,185 times — so Cut 3's oracle difference is every
  case that emits an I-O status check or a qualified runtime name, not one line per case; the plan's Cut 3 already
  routes every runtime reference through the `RuntimeApi` façade and takes ONE reviewed re-baseline, which §8.3 (9)
  keeps true by making that façade the `RuntimeMembers` catalogue first.
- *The emitted program's namespace is given*: `Cobol.<S>` per `DESIGN-external-repository.md` §4.5. The
  external-repository design's `CobolNet.Runtime.Repository` attribute family is spelled under the runtime root and
  follows Cut 3 mechanically; nothing in that design is reopened.
- *Two namespaces named `Validation` at two layers* (`CobolNet.Binding.Validation`, the statement rules a binder
  applies while binding, 4 types; `CobolNet.Validation`, the post-bind passes, 25 types) is a naming defect rule 5
  catches: the binding-layer one is renamed to say what it is (`StatementRules` is the shape; R0-0015's extract,
  PB2310, is where it happens).

**Tests.** R64 puts `tests/` in scope and kept today's test layout "until zero GAP"; the owner dropped R69 §2's hold on binding and code-generation restructuring on 2026-10-07 (PB2118 question 5, "Drop it") and that hold
with it (PB1754 amended), so the re-home is a wave like any other, between fix-lane trains under the file-set
partition (§8.7): a test file the in-flight train edits defers the wave that moves it. Measured: `Cobol.Net.Tests.Unit` holds 449 files, 435 of them in
the one flat namespace `CobolNet.Tests.Unit` (12 in `.Collation`, 2 in `.Unicode`); `Cobol.Net.Tests.Conformance` holds
200 files in one flat namespace; `tests/_shared` holds 11 (`CobolNet.Tests.Shared`, `CobolNet.Gate`). Rule 1 applies: a
test file lives in the folder of the subsystem it tests, mirroring `src/` (`tests/Cobol.Net.Tests.Unit/Binding/…` →
`Cobol.Net.Tests.Unit.Binding`), a drift test in the folder of the boundary it pins, and the 278 `*DriftTests.cs` are
the first population to re-home because their `<summary>` names the files they govern (`drift_rules.py` already reads
them). The gate is unaffected: `scripts/gate_plan.py` keys every case on its display name's class and method
(`name_key`, gate_plan.py:16), not its namespace, and a stale impact-map key only changes the order, never what runs
(PB1708). The oracle is unaffected: `ArchOracleDriftTests` enrolls row sources by attribute, not by name.

### 8.3 Decomposition of each god class

**Method.** For each of the 52 candidates (census `godClasses`; the finding's `responsibilities` gives the partial
concerns and the member-name subjects, its `measure` the sizes and fan), the responsibilities are read from the
members and partials, the target types are named, and the seam each exposes is named. That is all this section holds.
The order of extraction and what blocks each one are register content: each candidate is one Extract note
(PB2296–PB2347, `census_ids:` naming its finding), and the note carries its sites, its measures, its order code and
its `blocked_by`. Where this section designs an extraction ORDER (the nine entries below), the candidate's note is
step 1 and every later step is its own note, blocked by the step before it, because `plan_wave.py` groups NOTES
(`plan_wave.py:217-221`): one note is one wave, so a decomposition the planner must run as eleven waves is eleven
notes. The steps, their sites and their blockers are records in the targets file and the notes it renders
(`file_census_notes.py --targets … --rerender` keeps the two equal), and a note claims only its own step: a class
note's target, seam and sites are step 1's, and it names the later steps by note id only. This section names each
entry's step notes and holds no measure, count or blocker of its own: the census record and the notes hold those. The
order codes, defined once: **A** a wave between fix-lane trains under the file-set partition (§8.7), whatever its
layer; **C** after a named prerequisite (an external-repository slice of §8.6, PB2151, PB2129, PB2350, or another
Extract note), then the same. There is no B: the order that held binding and code generation for GAP near zero is
gone (the owner dropped R69 §2's hold on binding and code-generation restructuring on 2026-10-07 (PB2118 question 5, "Drop it"); Draft 5's B rows are A, its C+B rows C). "Leaves first" stays the planner's preferred order. "Which candidate is hot" is a query the dispatcher runs at wave time over the open notes that
name the type; this document keeps no count of it.

**(1) `CobolNet.Binding.DataBinder`** — R0-0001.
*Responsibilities, from the partials and the pass list.* The data-description pipeline whose pass BODIES are its
methods: `BindPipeline.Build` (`Binding/Passes/BindPipeline.cs:49-142`) registers its `BindPass` rows as lambdas over
the binder (`d => d.ExpandTypes()`), from `SynthesizeImpliedPictures` to `MarkFileRecordImageLeaves`, under the
`PassPhase` watermark (`Passes/IBindPass.cs:23`: None, TypesExpanded, UsageResolved, SignResolved,
RedefinesClassified, StrongTypeChecked, OccursResolved, FilesResolved, ProcedureBound, UsageCollected, StorageComputed,
EditionConformanceChecked); §12.3's configuration facts ("The configuration section specifies aspects of the data
processing system that are dependent on the specific system", §12.3.1) in `Switches` and `IoControl`; §13.18's clauses
one partial each (`Aligned`, `SignClause`, `UsageDeclaration`, `ValuePlacement`, `TableValue`, `GroupValue`, `Odo`,
`RedefinesEntry`, `RenamesEntry`, `ConditionName`, `ConstantRecord`/`Constants`, `ImpliedPicture`, `PictureRequired`,
`Ptr`, `CodeSetRecords`, `Linkage`, `ClausePlacement`, `EntryOrder`); the report section (`Reports`); OO data (`Oo`);
and its fields, the blackboard every pass reads and writes (`DESIGN-binder-bound-tree.md` §1.2).
*Why a phase key cannot be the seam.* The watermark describes what a pass DECLARES, not what it writes:
`BindPipeline.ValidateDag` reads only each row's `Requires` and `Produces`; rows whose `Requires` equals their
`Produces` still write (`CheckClauseSubjects` and `CheckSignClauses` clear refused clauses); the rows that run at the
ONE watermark FilesResolved → FilesResolved (`GateFileRecordByteSurface`, `ResolveReports`, `CallBindExternalAndGlobal`,
`PtrBindBasedAndAddressables`, `RouteStaticUnitStorage`, `MarkFileRecordImageLeaves`; BindPipeline.cs:135–142)
include storage-tier writers and image-fact recorders that run after them, so a reader keyed by FilesResolved cannot
know which of them has run. And four facts ACCUMULATE across phases, which no single-producer slice holds:
- `WholeGroupReferenced` has two producers: `MarkFileRecordImageLeaves` adds the FILE record areas at FilesResolved
  (`DataBinder.cs:718`), and `UsageCollectionPass.Collect` adds the boundary formals and the whole-group operands at
  UsageCollected (`Passes/UsageCollectionPass.cs:73-110`); `StorageFormPass` reads the union (`StorageFormPass.cs:65`).
- `ImageForcedItems` is appended at resolve time (Tier-B and CALL-cell REDEFINES leaves, FILE record leaves, report
  print faces: `DataBinder.cs:742,7567,7581`, `DataBinder.Linkage.cs:868`, `DataBinder.Reports.cs:3210`) and during
  procedure binding (`MoveBinder`'s figurative-fill and ref-mod-store receivers, `Procedure/Verbs/MoveBinder.cs:458,551`),
  and it is read MID-BIND in statement order (`IsImageBackedEarly`, `ReferenceResolver.cs:651,1057`: "a fact recorded
  by an earlier statement's bind is visible to a later statement's; one recorded later is not", `DataBinder.cs:278-284`),
  where it decides the `NumericImagePlace` wrap and so the emitted C#: a slice frozen at its phase's end would hide the
  procedure-bind facts from later statements and change the `Place`.
- `CharacterChannelItems` is appended by `UsageCollectionPass` after procedure binding (`UsageCollectionPass.cs:69`).
- `ActivationSites` (`DataBinder.cs:458`) is appended during procedure binding (`IntrinsicBinder.cs:419`,
  `ReferenceResolver.cs:2569`, `ReferenceResolver.ObjectProperty.cs:256`) and read whole after it by
  `FlagConformancePass` (`FlagConformancePass.cs:173`).
And procedure binding itself keeps state on the binder that is no fact at all, in three shapes the data pipeline has
no word for:
- a **scoped override**: `Options` is swapped, per METHOD, to the method's own OPTIONS model for that method's
  sentences and restored after (`Bound/StatementBinder.cs:350-374`; §11.9.4 GR1, kb/Work PB135), and read per
  statement (`ExpressionBinder.cs:703`, the default rounding; `IntrinsicBinder.cs:2598`, the arithmetic mode). Frozen
  as a value slice, a method's bare ROUNDED would take the class's mode: different emitted C#, and a wrong answer;
- an **ordered drain queue**: `PendingPreOps` and `OoPendingPropertyOps` (`DataBinder.Oo.cs:583,609`) are
  statement-scoped, mark-on-entry and drain-own-suffix: a statement marks the count when it starts binding and takes
  only the entries registered after its mark, removing them (`ReferenceResolver.ObjectProperty.cs:266`, `RemoveRange`),
  so a reference in an IF condition belongs to the IF, not to an arm that binds later; neither append-only nor frozen;
- a **scope cursor**: `OperandEvaluations` is saved, zeroed and restored around each statement
  (`StatementBinder.cs:432-498`), and `ActiveMethodScope` is set per procedure-table entry (`DataBinder.Oo.cs:44`,
  `BinderContext.cs:150`).
And around a unit's data phases `BinderDriver` writes three more kinds of state, none of them an `IBindPass` (R1
Draft 6 re-modelled them from the code after the fifth refuter, G2):
- a **configuration seed** (`BinderDriver.cs:634` → `DataBinder.InheritConfiguration`, `DataBinder.Switches.cs:449-484`):
  BEFORE a contained unit binds, the container's whole configuration-derived state (SPECIAL-NAMES: decimal point,
  currency signs, switches, classes, alphabets, order tables, structures, locales; the OBJECT-COMPUTER collating
  sequences and classification; DEBUGGING MODE; the REPOSITORY specifiers) is copied into the child's OWN
  configuration fields, and the container's OPTIONS become the child's baseline, which the child's own OPTIONS
  paragraph overrides clause by clause at `Bind`. The standard makes this one producer per unit, not a merge: a
  contained program has no configuration section of its own (§12.3.3 SR1: "The configuration section shall not be
  specified in a program that is contained within another program"), the container's entries "apply to each directly
  or indirectly contained source unit" (§12.3.4 GR1), and OPTIONS clauses apply to contained elements "unless
  overridden by a clause in an OPTIONS paragraph in a contained source element" (§11.9.4 GR1). That rule (§12.3.3 SR1)
  governs contained PROGRAMS only. A class definition's OBJECT and FACTORY halves are the second producer the seed must
  cover (the sixth refuter, H2): a half MAY carry its own configuration section, limited to SPECIAL-NAMES (§12.3.3
  SR3: "The SOURCE-COMPUTER, OBJECT-COMPUTER, and REPOSITORY paragraphs shall not be specified in a factory definition
  or an instance definition"), and its own OPTIONS paragraph. `OoDriver` builds a half's configuration by reparenting
  the half's own ENVIRONMENT DIVISION before the class definition's into one synthetic unit (`OoReparentClassData` and
  `OoReparentFactoryData`, `OoDriver.cs:176-196`), which `DataBinder.EnvDivisions` (`DataBinder.cs:1109-1117`) reads
  outermost first, so the half's entries override the class's clause by clause; it builds the half's OPTIONS by
  overlaying the half's paragraph on the class's (`OoDriver.cs:99-101` for the OBJECT half, `:119-120` for the
  FACTORY half; an INTERFACE binds its own paragraph over none, `:41`), and `CallInheritOptions`
  (`DataBinder.cs:294`) writes the same `_inheritedOptions` field `InheritConfiguration` writes
  (`DataBinder.Switches.cs:428,483`). A method declares no configuration section (§12.3.3 SR2: "The configuration
  section shall not be specified in a method definition") and binds through its half's binder, so it reads the half's
  slice; its OPTIONS are the procedure-bind scoped override below. And `MnemonicRegistry.Of`
  (`Binding/Procedure/MnemonicRegistry.cs:35-60`) builds the device map by its OWN walk up the parse ancestors
  (program unit, OBJECT or FACTORY paragraph, class, interface; `TryAdd` keeps the nearest), a third reader of the
  same configuration entries;
- a **checking fold**, for EVERY unit and every class half and method, not only contained programs: five unit flags folded
  from the group's `TurnState` at the unit's own lines (`ExternalCheckMask`, `ArgMismatchChecking`, `ExternalDescribe`,
  `OdoReferenceChecking`, `AutomaticPropagation`; `BindUnitData`, `BinderDriver.cs:667-683`, writes them after
  `data.Bind` for the outermost unit too; the class loop writes `ExternalDescribe` and `OdoReferenceChecking` on both
  halves and three flags on each method, `OoUniversalCheckingHere` (§14.9.23.4 GR7 c)'s method half, folded at the
  procedure division header), `ExternalCheckMaskHere` and `AutomaticPropagationHere`, `BinderDriver.cs:112-147`; code
  generation reads them, `ProgramEmitter.cs:255,396,747,753` and `OoEmitter.cs:504,539,1212,1322`);
- for a contained program, the **GLOBAL merge** (§13.18.27.4 GR1: "A constant-name, data-name, file-name, report-name,
  or screen-name described using a GLOBAL clause is a global name"; §8.4.6.2), which writes the child's state after its
  data phases have frozen (`BinderDriver.cs:685-751`): it adds the containers' GLOBAL files to `FilesByName` by
  `TryAdd` (a local declaration shadows) and their other files to `ContainerLocalFiles`; inherits their GLOBAL reports
  (`InheritGlobalReport`) and GLOBAL subtrees with their depth (`InheritGlobalSubtree`: `ByName`, `Conditions`,
  `IndexNames`, `DataBinder.Linkage.cs:285`) and bridges; marks the formals' omitted-argument guards (`MarkFormals`);
  and drops the inherited constants a nearer name hides (`DropShadowedConstants`), nearest container first. Its seed
  half runs before `data.Bind` (`InheritGlobalConstants`, `InheritGlobalTypeDecls`, `ReserveInheritedMemberNames`,
  `BinderDriver.cs:640-654`), because the child's first phase reads it.
*Target types.* Binder state takes the shapes the code already has. A **value slice** is a record of the facts ONE phase
produces (the type forest's derived tables, the usage facts, the sign facts, the REDEFINES classes, the OCCURS facts,
the file and storage-tier facts …): each field in exactly one slice, written through the slice's builder by the passes
of that phase, frozen when the phase ends, read only after it. A **fact set** is append-only with an ordered read:
`IAppends<TFacts>` admits only `Add`; `IReadsEarly<TFacts>` answers "recorded so far", the mid-bind read, whose
statement-order visibility is today's exactly because the binder appends and reads in the order it binds;
`IReads<TFacts>` answers the complete set and is legal only after the last phase that appends. The four accumulators
are the four fact sets, each with its declared appending phases (`WholeGroupReferenced`: FilesResolved and
UsageCollected; `ImageForcedItems`: FilesResolved and ProcedureBound, read early by the resolver;
`CharacterChannelItems`: UsageCollected; `ActivationSites`: ProcedureBound, read whole by the validation schedule).
The procedure-bind state is NOT in `DataBindState`: it is `BindSession`'s `ProcedureBindState`, which exists only while
procedure binding runs and is never in `DataBindResult`, in its three shapes: a **scoped override** (`Scoped<T>`:
`Push(value)` returns the scope whose disposal restores the previous value, the `using` shape `EnterMethodScope`
already has at `StatementBinder.cs:350`; it holds `Options`, whose pushed value is the method symbol's own
`MethodOptions`); an **ordered drain queue** (`DrainQueue<T>`: `Add`, `Mark()` and `DrainFrom(mark)`, which returns and
removes the entries after the mark in registration order; it holds `PendingPreOps` and `OoPendingPropertyOps`); and a
**scope cursor** (`Cursor<T>`: `Enter(value)` returns the restoring scope; it holds `OperandEvaluations`, entered at
zero per statement, and `ActiveMethodScope`). The three kinds `BinderDriver` writes around the data phases take
three shapes. The **configuration slice** is a value slice with ONE producer per bound unit, `ConfigurationStep`, at
the environment phase, before every data phase that reads it (today's literal and PICTURE reads), and its source is a
closed union with three arms, one per kind of unit the standard gives a configuration: **Own** — an outermost program,
function, class definition or interface binds its own CONFIGURATION SECTION; **Contained** — a contained program binds
none (§12.3.3 SR1) and takes the container's frozen `DataBindResult` (§12.3.4 GR1); **ClassHalf** — an OBJECT or
FACTORY half takes its class definition's configuration (§12.3.4 GR1: the class contains the half) overlaid clause by
clause by the half's own SPECIAL-NAMES, the one configuration paragraph §12.3.3 SR3 leaves it. The class definition
binds no data, so it has no `DataBindResult`: its arm's source is a `ClassConfiguration` value, bound once per class by
the same step from the class's own section and OPTIONS paragraph and handed to both halves, which replaces the synthetic
reparent units' environment children and `EnvDivisions`' reverse order (the override becomes the arm's overlay, written
once). A method is not a configuration unit (§12.3.3 SR2): it reads its half's slice. The OPTIONS slice is produced by
the same step and the same three arms from the baseline the arm names (none for Own; the container's for Contained;
the class definition's for ClassHalf) overlaid by the unit's own OPTIONS paragraph clause by clause (§11.9.4 GR1), so
`InheritConfiguration`'s copy and `CallInheritOptions`' overlay, today two writers of `_inheritedOptions`, become the
slice's single write; a method's own OPTIONS stay the procedure-bind scoped override below. `MnemonicRegistry.Of`'s
parent walk becomes a reader of the slice (the device map is a configuration entry like the switch map), and so does
`OoRepositoryScope.Build` (`Oo/OoNameResolution.cs:167-190`), a fourth reader with its own ancestor walk over the
REPOSITORY paragraphs (the seventh refuter, J2): the REPOSITORY specifiers are a slice entry the same three arms produce
(an outermost program, function, class or interface declares its own; a contained unit's come from its container,
§12.3.4 GR1; a half's are its class definition's, since §12.3.3 SR3 forbids the paragraph in a factory or instance
definition), so no reader re-derives the scope. The given external-repository design widens `OoRepositoryScope` into
the per-source-element `IdentityScope` (its §8.7, slice 8): that is not a second owner. The slice owns the specifiers
(the DATA: each word, its kind and its AS phrase); `IdentityScope` owns the word ↔ identity resolution (the
BEHAVIOR), built from the unit's slice plus the definition's own name (§12.3.8.3 SR5: a specifier naming the
containing class definition "is ignored"), and it replaces `OoRepositoryScope` in slice 8, which PB2296 waits for.
The given says what `IdentityScope` maps and when, not where its entries come from, so it is not reopened. The **checking fold** is two value slices with ONE producer, `CheckingFold`: `UnitChecking`
(the five unit flags) for every program unit and every class half, after the unit's data phases (where `BindUnitData`
writes them today) and before ProcedureBound; and `MethodChecking` (`OoUniversalChecking`, `ExternalCheckMask`,
`AutomaticPropagation`) for every method, folded from the group's `TurnState` at the method's own lines as
`BinderDriver.cs:112-135` does today, after `ResolveOverrides` and before any body binds, read through the method
symbol as today. `OoUniversalCheckingHere` is a binder-state kind of this shape, not a sixth one: a `TurnState` fold the
binder writes once and code generation reads (`OoEmitter.cs:539`). The
**inheritance merge** is `BindSession`'s `InheritStep`, run once per contained program, which receives the containers
as their `DataBindResult`s, nearest first (a parent binds before its children), and writes only one slice of its own,
never a data phase's: `InheritedGlobals` (the inherited files, container-local files, reports, subtree tiers with their
depth, bridges, and the shadowed-constant drop list; a lookup reads the child's own slice first and this one after,
which is `TryAdd`'s local-shadows rule made structural), carried into `DataBindResult`. `InheritStep.Seed` writes the
seed part at a phase before the first data phase, `GlobalsSeeded`; `InheritStep.Merge` writes the rest at
`GlobalsInherited`, after the last data phase and before ProcedureBound; `MarkFormals` stays a write to the formals'
items, ordered by the step as today.
A pass declares its views on its `IBindPass` class, and `ValidateDag` checks
the declarations against the phase order at startup: a value slice is written only in its phase and read only after
it; a fact set is appended only in a phase that declares it, read early only by a pass that says so, and read whole
only after its last appender. **What `ValidateDag` and the type system enforce for each shape, and what they do
not:** a value slice and a fact set are checked by `ValidateDag` as above, and a pass receives only the views it
declares (constructor parameters, never the state object), so a write to an undeclared slice or fact set does not
compile. For the configuration slice and the checking fold, `ValidateDag` checks them as value slices: one producer
each (`ConfigurationStep`, `CheckingFold`; the source union's three arms are a `switch` the compiler checks for
exhaustiveness, so a unit kind with no arm does not compile), the configuration phase before every data phase that declares a read of it,
the fold after the data phases and before ProcedureBound, and every reader declaring it; that the producer runs for
EVERY unit (an outermost program and each class half included) is the driver's unit loop, and the oracle proves it,
because an outermost unit missing its flags changes the emitted checks. For the inheritance merge, `ValidateDag` checks
that `GlobalsSeeded` precedes every data phase and `GlobalsInherited` follows them and precedes ProcedureBound, that only
`InheritStep` produces `InheritedGlobals`, and that every reader declares it and runs after the phase that writes it; the
type holds the containers: the step and `ConfigurationStep` receive them as `DataBindResult`, which is immutable, so
neither can write one. Neither checks the merge's order (nearest
container first, a local name shadowing), which stays the step's loop, as today, and the oracle proves it.
`ValidateDag` checks nothing about the three procedure-bind shapes, because they live inside ONE phase
(ProcedureBound), where order is the statement walk's, not the pass list's; what holds them is the type: no
`IBindPass` can receive `ProcedureBindState` (`BindSession` constructs it when procedure binding starts, after the
pipeline's last data pass), a scoped override and a cursor change only through a restoring scope, and a drain queue
admits no removal but `DrainFrom` its own mark. That a statement drains exactly its own suffix stays the statement
walk's duty, as today, and the oracle proves it. Most of a data pass's output is mutation of the `DataItem` forest
(`UsageInheritancePass` writes `item.Pic`, `DataBinder.cs:6752,6775,6816`), which no slice declaration covers: those
writes stay ordered by the explicit pass list, exactly as today, and the oracle proves the order; the type system takes
over where binding ends, because `DataBindResult` hands out the forest with `DataItem` immutable (R0-0028's row). One
class per pass implements `IBindPass` (the lambda rows become an explicit ordered list of those classes; no
reflection); one binder per non-pass concern: `SpecialNamesBinder` (`Switches`, `IoControl`), `FileControlBinder`,
`ReportSectionBinder` (`Reports`), `ConstantEntryBinder`, `LinkageBinder`, `OoDataBinder`. During procedure binding the
binders read the frozen value slices, append and early-read the fact sets, and use `ProcedureBindState`'s three
shapes, all through `BindSession`; after the group
tail (StorageComputed), when every slice is frozen and every fact set complete, the output is `DataBindResult`, the
immutable record that replaces `DataBinder` on `BoundUnit.Data`, `BoundCompilation.InterfaceData` and
`EmitContext.Data` (§8.1's largest refusal). `DataBinder` keeps only the orchestration that builds the state, runs the
pipeline and returns the result (`DESIGN-module-topology.md` §2.5's "thin orchestrator + named passes", with the
slices and fact sets as the seam it lacked).
*Seam:* `IBindPass` over its declared views (value slices and fact sets), `ProcedureBindState`'s three shapes, and
`DataBindResult`; step 1 (PB2296) carries every field named above. *Steps:* PB2296,
PB2352–PB2361.

**(2) `CobolNet.Editions.Diagnostics.DiagnosticCatalog`** — R0-0002: `public static readonly DiagnosticDescriptor`
fields beside four string constants (`NotImplemented`, `StrongType`, `RecognizedNotImplemented`,
`DeclinedOptionalElement`); the census's own data-ize rule fires (its measures are on the note). And its twin: `CobolNet.Frontend.Diagnostics.DiagnosticDescriptors` holds descriptors (`CBLnnnn`, `COBOLnnnn`, and twins
whose code is another constant) of a second `DiagnosticDescriptor` record, `(Code,
DefaultSeverity, MessageTemplate)`; and `CobolNet.Editions.EditionCodes` holds the four edition-band codes that
`ConstructRegistry`, `CobolWordRule`, `VersionConformancePass` and the frontend's `CobolErrorListener` all read.
*Target: data plus a generator, keeping every identity.* One data file,
`src/Cobol.Net.Editions/Diagnostics/diagnostics.json`, holds every row of the three. The row schema is the union of the
records, so nothing a descriptor carries is lost: `catalog` (`DiagnosticCatalog`, `DiagnosticDescriptors` or
`EditionCodes`, the generated type the row lands in), `name` (the C# field name, the identity the callers use), `code`,
`id`, `severity`, `message` (the editions record's title, or the frontend record's message template), `citation`,
`suppressKey`, **`permissiveInert`** (`DiagnosticDescriptor.cs` declares it; catalog rows set it,
`DiagnosticCatalog.cs:3333,3344,3355`, and `EditionContext.cs:310` reads it to keep those declined optional elements
inert under `--permissive`, so a schema without it would silently change behaviour the default-mode oracle never
exercises), `annex`, `doc` (the XML summary), and **`twinOf`**: the frontend rows that are parse-layer twins of a
catalog row (the eight `COBOLNETnnnn` fields of `DiagnosticDescriptors`) carry no `code` of
their own but `twinOf: "<catalog>.<name>"`, and the generator emits the reference (`Code =
DiagnosticCatalog.GoToFormatShape.Code`), so the code is written once and a twin round-trips exactly as the source
spells it today. And **`family`**: catalog rows take their code from the private constant `NotImplemented`
(`COBOLNET0899`) or from `StrongType` (`COBOLNET1533`) (`DiagnosticCatalog.cs:44,47`) — the split-code families
the existing test exempts by name (`DiagnosticRegistryDriftTests.cs:44-57`, `SharedCodes`), and the reason `All`
sorts by code, then id. They are a `families` table in the same file (`{ "name": "NotImplemented", "code":
"COBOLNET0899" }` and `StrongType`'s), a member row carries `family: "<name>"` instead of a `code`, and the generator
emits the two private constants from the table and `Code = NotImplemented` in each member, so each family's code is
written once. **`suppressKeys`** is the same shape for the suppression keys: rows share the two PUBLIC constants
`RecognizedNotImplemented` (`"recognized-not-implemented"`) and `DeclinedOptionalElement`
(`"declined-optional-element"`) (`DiagnosticCatalog.cs:51,57`), which callers outside the catalog name
(`DeclinedFacilityDriftTests.cs:143`). They are a `suppressKeys` table in the same file (`{ "name":
"RecognizedNotImplemented", "key": "recognized-not-implemented" }` and its sibling), a row's `suppressKey` names a
table entry (never a literal), and the generator emits the two public constants under today's names and
`SuppressKey = RecognizedNotImplemented` in each row, so each key is written once and no caller changes. `EditionCodes` becomes generated from its four rows, so the edition band is single-sourced in the same
file. One generator, `scripts/gen-diagnostics.ps1`, on the pattern this repository already runs
(`gen-constructs.ps1` → `Constructs.g.cs` and `ConstructRegistry.g.cs`; `gen-cobol-words.ps1` → `CobolWords.g4`),
emits `DiagnosticCatalog.g.cs` (one generated partial per band: edition band, digit capacity, directives, §12, §13,
§14, §15, OO, report writer, declined), `DiagnosticDescriptors.g.cs` and `EditionCodes.g.cs` under today's names — so
no caller changes and the three identities (field name, user-visible code, stable `Id`) are preserved — with `All` as a
generated collection expression sorted as today (by code, then id: `DiagnosticCatalog.cs:6877-6878`), and renders
`docs/DIAGNOSTICS.md` from the same rows, so the renderer leaves `DiagnosticRegistryDriftTests` and that test keeps what
only a test can do: rows ↔ `.g.cs` ↔ `.md` agree, no two rows share a code except through `twinOf` or a `family`
(the two sanctioned sharings: a family's members are exempt exactly as `SharedCodes` exempts them today, and the
test reads the families table instead of its hand list), every `twinOf` names a row, every `family` names a
families entry, every `suppressKey` names a `suppressKeys` entry, and no `COBOLNETnnnn` literal is emitted outside the generated files. The two
descriptor records stay two types: they serve two reporting channels with different contracts (a positional message
template in the front end's bag; a title, a citation and the suppression and permissive behaviour in the edition
catalog), and the duplicated RULE — code allocation — is unified by the one file. PB1708's lesson is honored as stated
there: layer 1 ("modular, data-driven tables") is this change; an entry edit touches one row and the generated files;
each type still initializes in one static constructor, and under the ordered whole-population gate that coupling can
change the gate's order, never what runs. *Seam:* the generated fields under today's names, and the row schema.
*Steps:* PB2297, PB2362–PB2364.

**(3) `CobolNet.Runtime.CobolIntrinsics`** — R0-0003 (its partials: `Dec`, `Exact`, `Float`, `NumvalLocale`,
`RealArgs`, `Select`, `Text`, `TwoPi`; `Intrinsics/` under the root namespace, R0-0229); its members are emitted text
(census reach `emitted-text`).
*Responsibilities.* The partials are ARMS, not families: `Dec`, `Float`, `Exact` and `RealArgs` are the two bodies
every numeric function has (`RenderNum` routes on `AnyRealArgument`), the measured defect shape of PB2, PB13, PB14,
PB28, PB32 and PB33 — a rule corrected in one arm. The families are `Select` (MAX/MIN/ORD-*), `Text`, `NumvalLocale`
(the NUMVAL family), `TwoPi` (trigonometric argument reduction), the statistical and arithmetic functions, and
`CobolDate` (already its own type, R0-0051).
*Target types,* one per §15 family ("Each intrinsic function definition specifies: the name…, the type…, the
arguments…, the returned value", §15.1): `NumericFunctions`, `TrigonometricFunctions`, `SelectionFunctions`,
`NumvalFunctions`, `TextFunctions`, `DateTimeFunctions` (from `CobolDate`), each owning BOTH arms of each function
adjacent behind one entry per function; the shared argument coercions become `NumericArguments`. *Seam:* one static
entry per function on its family type, reached by emitted code through a `RuntimeMembers` handle (9); every rename
lands as one explained oracle difference (§4 item 1). *Steps:* PB2298, PB2365–PB2370.

**(4) `CobolNet.Binding.ReferenceResolver`** — R0-0004 (its partial and nested types).
*Responsibilities, from the members.* Qualification ("Qualification is the specification of superordinate names
from the hierarchy to which a user-defined name belongs", §8.4.2.2.1); subscripts ("Subscripts are used when
reference is made to an individual element within a table of like elements", §8.4.2.3.1) including ALL and index-name
subscripts and the D10 residue that still renders a C# segment at bind time (`RenderSegment`, PB2151); reference
modification ("Reference modification defines a unique data item by specifying an identifier, a leftmost position,
and a length", §8.4.3.3.1); the item-to-`Place` construction (the ONE builder, `RenamesPlaceBuilderDriftTests`),
which the backend calls today (`ResolveItem`, 24 emit-time calls); the special counters (LINAGE-COUNTER, PAGE-COUNTER,
LINE-COUNTER); table positions; object properties (the partial); and the closed answer `RefResolution`
(`RefResolutionDriftTests`).
*Target types:* `NameResolution` (§8.4.2.2 over `SymbolTable`, candidates in, uniqueness decided), `SubscriptBinder`
(§8.4.2.3 → `BoundExpr` positions: PB2151's landing place), `RefModBinder` (§8.4.3.3), `PlaceBuilder` (item +
positions + window → `Place`), placed in `compiler.model` and run at bind time, so every `Place` the backend needs is
already in the bound tree and the backend never calls it, `CounterResolver`, `ObjectPropertyResolver`;
`ReferenceResolver` stays the funnel that composes them and answers `RefResolution`. *Seams:* `RefResolution` and
`PlaceBuilder.Build(…) → Place`. *Steps:* PB2299, PB2371–PB2375.

**(5) `CobolNet.Binding.Procedure.IntrinsicBinder`** — R0-0005 (one file; its hand `case Bound…` arms are counted
in PB2300); with `IntrinsicArgumentRules` (R0-0020) and `IntrinsicCatalog` (a table written as
code).
*Responsibilities.* FUNCTION-reference binding per §15 family: argument binding and the argument screens, result
typing (`IntrinsicResultType`), the catalog lookup, the keyword-omitted routing below 2002 (§8.4.3.2 SR2, the
sanctioned behavioural read of `DESIGN-version-conformance-pipeline.md` §1.1 item 3), and the per-function windows
(TRIM's argument 2, the FORMATTED-* family, the NUMVAL family, RANDOM, LENGTH and BYTE-LENGTH, the EC-ARGUMENT
functions).
*Target types.* `IntrinsicBinder` keeps the name-to-family dispatch and the routing read; one family binder per
family of (3) (`DateTimeIntrinsicBinder`, `NumericIntrinsicBinder`, `TextIntrinsicBinder`, `NumvalIntrinsicBinder`,
`LocaleIntrinsicBinder`, `ExceptionIntrinsicBinder`); the catalog becomes data (`intrinsics.json` →
`IntrinsicCatalog.g.cs`, the pattern of (2)) carrying arity, domain and codomain, the edition windows
(`IntroducedIn`/`RemovedIn`) and the argument-schema rows `IntrinsicArgumentRules` screens — so a new function is a
row plus one family-binder arm. *Seams:* `IntrinsicSig` (the catalog row) and `IIntrinsicFamilyBinder.Bind(IntrinsicSig,
arguments) → BoundIntrinsicCall`. *Steps:* PB2300, PB2376–PB2381.

**(6) `CobolNet.Validation.VersionConformancePass`** — R0-0006 (the bound arm and the word checks) with its nested
`ParseArm` (R0-0009, the widest fan-out in the census).
*Responsibilities.* The ONE edition gate (D7, `DESIGN-version-conformance-pipeline.md`): the bound arm over
`BoundUnit` (STOP RUN status, CALL forms, prototypes), the parse arm (one `Visit` per construct family firing
`ConstructRegistry.Check` with its `constructs.json` id), the word checks (`reservedWords` and `cobolWords` constructor
parameters; "A COBOL word is a character-string of not more than 63 characters", §8.3.2.1, through `CobolWordRule`),
and the removed and obsolete flags.
*Target types.* The parse arm stays ONE tree walk, and the walk dispatches each context to the gate of the grammar
fragment that defines it — the modules of §8.4 (Identification, Environment with Special-Names, Data, Procedure,
Control flow, I-O, Expressions, OO, Report Writer, Screen, Declined), one `…ConstructGate` type per fragment (`IdentificationConstructGate`, `EnvironmentConstructGate`, `DataConstructGate`, `ProcedureConstructGate`, `ControlFlowConstructGate`, `IoConstructGate`, `ExpressionConstructGate`, `OoConstructGate`, `ReportWriterConstructGate`, `ScreenConstructGate`, `DeclinedConstructGate`), each
owning the checks for its fragment's contexts, plus `CompilationGroupConstructGate` for the core skeleton's; the bound
arm stays `VersionConformancePass`; the word checks (`CobolWords.g4`'s contexts) become `CobolWordGate`. *Why one walk and not eleven:* the diagnostic stream is compared IN ORDER by the oracle (§4.1) and
nothing sorts it (`DiagnosticBag.Diagnostics` returns the insertion list, `Frontend/Diagnostics/DiagnosticBag.cs:14`),
so eleven gates each walking the unit would report a program's diagnostics grouped by gate instead of in tree order,
and the oracle would print DIFFERENT; one walk in today's order, delegating per context, keeps the stream identical by
construction. *Seam:* `Descent IConstructGate<TContext>.Check(TContext context, ConformanceWalk walk)`, registered in
one explicit list. **`Descent`** is the gate's answer to the walk (descend into the children, or not, the gate having
visited what it wants through `walk.Visit(child)`), because the walk prunes today: `VisitDeclarativeSection` visits
ONLY the USE statement of a USE FOR DEBUGGING section compiled without WITH DEBUGGING MODE, never its body, which
keeps the reserved-word funnel off the DEBUG-* register references inside it (`VersionConformancePass.cs:921-937`;
DB103M is the corpus witness), so a seam with no descent decision would add diagnostics to DB103M's stream.
**`ConformanceWalk`** is the walk state, named once and created per walk (one per compilation's parse arm; the
gates are per-walk instances it creates and hold no state of their own, so nothing leaks across compilations): the
`EditionContext`; the debugging posture (`_debuggingModeDeclared`, set by the environment fragment's
`VisitDebuggingModeClause`, `:721-727`, read by the procedure fragment's declarative-section visit and by the word
check, `:2598`, and RESET at each program unit, `:627-630`); the flagged-word set that reports the reserved-word
diagnostic once per word (`_flaggedWords`, `:616`, read by both of the word gate's contexts, `:2600` and `:2639`); and
the over-long-word set that reports COBOLNET1567 once per word (`_overlongWords`, `:618`, `:2524-2544`). The core
skeleton keeps its own gate, `CompilationGroupConstructGate`, which owns the contexts the core still defines after the
regroup (`programUnit` and the §10 shells, `CobolParserCore.g4:62`); its `ProgramUnitContext` check calls
`walk.BeginProgramUnit()`, the walk's one method that resets the per-unit posture, so the second program of a
compilation group never inherits the first one's WITH DEBUGGING MODE. The walk's context-type → gate map is total, pinned by a drift
test over the generated parser's context types (the chain's first step adds it), so a new grammar rule without a gate
fails the build's tests instead of passing unchecked; and `ConstructRegistry.Check` (existing, the one funnel) sits
underneath. **Which gate owns a context:** the gate of the fragment that DEFINES its rule. ANTLR generates one context
type per rule name, so a rule defined in two fragments makes the map ambiguous: `className` was defined in
`CobolExpressions.g4` and in `CobolOO.g4`, and ANTLR kept the first imported definition, so the OO class-name took the
class-condition keyword set (kb/Work PB2292). The two are now two rules — the class-condition operand
`classConditionName` (Expressions) and the OO class-name `className` (OO) — and `GrammarRuleUniquenessDriftTests`
asserts that no rule name is defined in two grammar files, which this map relies on. The map also needs the
identification, environment and procedure fragments the core-defined contexts belong to (§8.4's regroup); the notes'
`blocked_by` carry the order. Why
this partition: a construct's grammar rule (§8.4), its gate and its `constructs.json` row then share one name, so a
§14.9 GAP fix lands in the procedure gate's file only — the shape that makes the next case automatic. *Steps:*
PB2304, PB2385–PB2390, then PB2301 (the bound arm and `CobolWordGate`).

**(7) `src/Cobol.Net.Runtime/Exceptions/ExceptionState.cs`** — one file, three types: `ExceptionEngine` (R0-0014),
`ArgumentSubstitute`, and the static `ExceptionState` the generated code calls, which is a forwarder today
(`ExceptionState.cs:1672-1691`: `private static ExceptionEngine E => RunUnit.Current.Exceptions; public static string?
LastName => E.LastName; …`).
*Responsibilities, by subject.* Raise and dispatch (`DispatchResult`; the PERFORM-frame and declarative interplay with
`PerformFrame`); the last-exception record and its readers (the EXCEPTION-STATUS, -STATEMENT, -LOCATION and -FILE
functions, kb/Work R04–R07 and R14 each fixed one arm here); propagation across activations (CALL, INVOKE and
function boundaries); the I-O merge (EC-I-O against the file status); argument incompatibility (the EC-ARGUMENT
family).
*Target types:* `ExceptionEngine` keeps raise, dispatch and the checking state; `LastExceptionRecord` (the
status–statement–location–file record and its readers), `ExceptionPropagation`, `IoExceptionMerge`,
`ArgumentExceptions` (with `ArgumentSubstitute`); one file per type (§8.2 rule 3). **The static `ExceptionState` is
deleted, not kept** (CLAUDE.md rule 4: no forwarders). The plan already decided its end: P15 Cut 3 item 4a
(owner-ratified 2026-07-16, `DESIGN-runtime-library.md` §6 Q3) deletes the static runtime facades and has emitted code
reach the instances through the run-unit driver's one captured `RunUnit` local, routed through the runtime façade. So
the extraction is the same wave as that change: PB2309 (this split) and PB2349 (Cut 3, which deletes the facade) are
ONE group in `plan_wave.py`'s plan (the notes carry what makes it so; the dry run of §8.7 shows it), and emitted calls name the owning type's member through its `RuntimeMembers` handle (9)
(`ExceptionState.Set(…)` becomes the engine's, `ExceptionState.LastName` the record's); the oracle lists ONE explained
difference — every case that raises, propagates or reads an exception changes its emitted call text, with identical
program output — which §4 item 1 allows and Cut 3's one reviewed re-baseline absorbs. Split after Cut 3, the engine
would rewrite the same emitted calls a second time (the second re-baseline item 4a was ratified to avoid), so neither
lands alone. *Seam:* the engine and its parts, reached from the captured `RunUnit` through `RuntimeMembers` handles.
*Steps:* one, PB2309, grouped with PB2349.

**(8) `src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs`** — a god FILE, not a god type (its measures are on PB2351's
census columns): the seven `[BoundNode]` roots (`BoundExpr`, `BoundOperand`, `BoundBoolExpr`, `BoundCondition`,
`BoundStatement`, `BoundPerformControl`, `BoundSetTarget`) and their records; the folder's other files already split by
verb family. Also in `Bound/` and not nodes: `StatementBinder` (R0-0052, whose parse-context mentions make most of
`Bound → Frontend.Generated`), `MoveClassifier`,
`MoveOverlap` and `BoundStores`' visitor — the §8.1 rows that tolerate `Bound → Binding*`.
*Target.* One family file per root, named for it (`BoundExpr.cs`, `BoundOperand.cs`, `BoundBoolExpr.cs`,
`BoundCondition.cs`, `BoundPerformControl.cs`, `BoundSetTarget.cs`), `BoundStatement`'s leaves absorbed by the
verb-family files that exist (`BoundCall.cs`, `BoundSort.cs`, …) plus the ones the arithmetic, MOVE, control-flow,
pointer and program-shell leaves need; the rule tables in the file (`ImplicitMovePhrase`, `IntoPhraseRules`,
`EcFeatures`) to `compiler.model`; the binders out to `Binding/Procedure` with `StatementBinder`'s fields as a
`StatementBindContext` (R0-0052's note). *Seam:* the node families themselves (the unit §8.5 converts) and
`StatementBindContext`. *Steps* (no census god-class finding names the file, so they are designed records):
PB2347 (`StatementBinder` out, `StatementBindContext`), PB2391 (`BoundStores` out; `MoveClassifier` and `MoveOverlap`
stay, §8.1's bound-tree computations), PB2392 (the rule tables),
PB2393 (the six small families) and PB2394 (`BoundStatement`'s leaves). PB2393 claims the file's §8.2 rule 2–3
findings, so when the census computes those rules (PB2351) the generator does not file a second claim on it.

**(9) `CobolNet.CodeGen.RuntimeApi`** — R0-0007 (`CodeGen/Roslyn/` under `CobolNet.CodeGen`, R0-0218); the ratchet
`RuntimeApiGuardTests` still tolerates bare runtime names outside it, in `NumericRenderer`, `ConditionRenderer` and
`OperandText` (PB2302 carries the measure).
*Responsibilities.* Three member kinds under one name: `nameof` anchors for runtime types; fragment composers
(`BoolNot(operand)`); and compile-time rule calls into the runtime (`CallEcIsProgramOrExternal`) — by subject the
runtime's own subsystems (`Adapt`, `Store`, `Format`, `Register`, `Group`, `Arg`, `Read`).
*Target: `DESIGN-backend-abstraction.md` §2.3's shape, its catalogue renamed* ("one runtime, one ABI catalogue"):
`RuntimeMembers`, the ONE catalogue of `RuntimeMember` descriptors (declaring type, member name, arity or overload
key, `nameof`-anchored, so a runtime rename is a compile error here for both backends), and `RoslynRuntimeApi`, the
Roslyn backend's renderer over it (today's fragment composers, now sourced from the catalogue); a CIL backend adds
`CilRuntimeApi` over the same catalogue. The compile-time rule calls leave the façade for the binder that asks them,
which §8.1 lets read the runtime directly. **One `RuntimeAbi`, and which given yields:** two givens use the name.
`DESIGN-backend-abstraction.md` §2.3 (2026-07-07, an elevation pass, not an owner decision) names its compiler-side
catalogue `RuntimeAbi`; `DESIGN-external-repository.md` (owner-approved 2026-10-05, the later decision) has the
runtime declare `RuntimeAbi.Version` and `RuntimeAbi.CallAbi`, its call-ABI integer, read by the runtime's
`ProgramTable.RegisterModule` — a type in `Cobol.Net.Runtime`, which references no project. With both, every emitter
that has `using CobolNet.Runtime` would see two `RuntimeAbi` types (CS0104) and the implementer would decide which
design yields. The later, owner-approved design keeps the name: `RuntimeAbi` is the runtime's version and call-ABI
type, and backend-abstraction §2.3 is amended in this change set (2026-10-07) to name its catalogue `RuntimeMembers`;
nothing else in §2.3 changes. Draft 1's split of the catalogue into one class per runtime namespace stays withdrawn: a
catalogue of descriptors is a table whose size is the runtime surface's size, not a second responsibility, and
grouping its members by runtime namespace inside the one type is enough to make the runtime's §8.2 Move waves read.
*Seam:* `RuntimeMember`. *Steps:* PB2302, PB2382–PB2384. A runtime extract that renames emitted entries
(`CobolIntrinsics`, `CobolNum`, the `ExceptionEngine` split) needs the catalogue first, and one whose emitted names do
not change (`CobolDec`, `CobolEdit`, `CobolDate`) does not (the notes carry the blockers). The chain edits `RuntimeApi.cs`
and the binding and code-generation files that call it (the note carries the count), so it is not a leaf: it is B.

**The other 43**, from the same record (responsibilities from the finding's subjects and concerns; each row's note
carries its order and blockers):

| Type (R0 id) | Responsibilities today | Target types | Seam each exposes |
|---|---|---|---|
| `Binding.Procedure.OoBinder` (R0-0008) | inline invocation, object view, OO identifier, property object (the four partials) | `InlineInvocationBinder`; `ObjectViewBinder`; `OoIdentifierBinder`; `PropertyObjectBinder` | OoBinder keeps the dispatch over the OO operand forms; each extracted binder exposes its partial's entry Bind(context, BindSession) → Bound… node, unchanged |
| `CodeGen.OoEmitter` (R0-0010) | class units, methods, universal object references (`Univ`), object cells, external classes | `ClassUnitEmitter`; `MethodEmitter`; `UniversalObjectEmitter`; `ObjectCellEmitter` | ClassUnitEmitter.Emit(OoClassUnit, EmitContext) and MethodEmitter.Emit(OoMethodSymbol, EmitContext); the universal object and object-cell renderers are called by both |
| `Runtime.IO.SequentialConnector` (R0-0011) | record I-O (`Record`), LINAGE and page control (`Linage`), the §9.1.13 status machine | `LinageControl`; RecordCodec (fixed, variable, line-sequential) | RecordCodec, one implementation per record form, chosen at open; LinageControl exposes the LINAGE counters and the page advance; the connector keeps position and the §9.1.13 status |
| `CodeGen.Emit.IntrinsicRenderer` (R0-0012) | argument rendering per family (`Arg`, `Int`, `Dec`) | one renderer per intrinsic family of R0-0003 / R0-0005 | IIntrinsicFamilyRenderer.Render(BoundIntrinsicCall, EmitContext), mirroring the family binders |
| `Runtime.IO.FileRegistry` (R0-0013) | the open-file table, keyed and relative access, sharing (`Shared`) | FileRegistry (the open-file table); SharingPolicy (the OPEN Table 19 rows as data); the keyed and relative access helpers move to their connector types | SharingPolicy.Admit(open mode, sharing mode, the file's current opens) → I-O status |
| `Binding.Validation.StatementValidation` (R0-0015) | per-verb statement rules (`Inspect`, `Initialize`, `Write`, `Open`, `Search`) | `InspectRules`; `InitializeRules`; `WriteRules`; `OpenRules`; SearchRules (one per verb family) | each rule type a static Check(the statement's bound node, diagnostics) called by the verb binder that applies it |
| `Runtime.CobolNum` (R0-0016) | the Int128 carrier arithmetic; the `Image` partial (display, binary, checked store) | CobolNum (the Int128 carrier arithmetic); NumericImage (display, binary, checked store) | NumericImage's static image entries over CobolNum's carrier, reached by emitted code through RuntimeMembers |
| `Binding.Procedure.EcBinder` (R0-0017) | exception-name sets and checking state, EXCEPTION PERFORM, omitted arguments (the partials); its hand arms (PB2312 carries the count) | `EcNameSetBinder`; `ExceptionPerformBinder`; `OmittedArgumentBinder`; `RaisingBinder` | the bound exception-name set (EcNameSetBinder's result) that the other three consume; each Bind(ctx, BindSession) |
| `Binding.Procedure.ConditionBinder` (R0-0018) | relation, class, boolean (`Bool`, `Boolean`) and abbreviated (`Partial`) conditions | `RelationConditionBinder`; `ClassConditionBinder`; `BooleanExpressionBinder`; `AbbreviatedConditionBinder` | each returns a BoundCondition leaf (the §8.5 union); ConditionBinder keeps the dispatch |
| `Binding.Procedure.SetBinder` (R0-0019) | every SET format (`Set`); the R0-0053 clone pair | one binder per SET format family | SetFormatSelection (existing) selects the SetFormat; each family binder Bind(SetFormat, ctx) → BoundStatement |
| `Binding.IntrinsicArgumentRules` (R0-0020) | the §15.3 argument screens | the argument schema rows join intrinsics.json (R0-0005); the predicates stay one type | the generated IntrinsicArgumentSchema row of IntrinsicCatalog.g.cs |
| `Runtime.IO.CobolReport` (R0-0021, `ReportWriter.cs`) | control breaks (`Control`), page advance (`Page`), group presentation, SUM counters | `ReportControlBreaks`; `PageAdvance`; `ReportGroupPresentation`; `SumCounters` | CobolReport keeps the INITIATE / GENERATE / TERMINATE entries the emitted code calls; the four are its owned collaborators (no emitted name changes) |
| `Compiler.Oo.OoConformance` (R0-0022) | §9.3.8 conformance (`Mismatch`): prototypes, returning items, interfaces | `PrototypeConformance`; `ReturnConformance`; `InterfaceImplementationConformance` | one Conforms(left, right) → ConformanceResult per §9.3.8 rule family |
| `Binding.PictureAnalyzer` (R0-0023) | the PICTURE string parse, editing phrases, the per-edition symbol rows | `PictureParser`; `EditingPhraseAnalyzer`; the per-edition PICTURE symbol rows as data | PicInfo (existing, the parse result) |
| `CodeGen.CallEmitter` (R0-0024) | the one argument renderer (`ArgText`), call crossings, call sites | `CallArgumentRenderer`; `CallSiteEmitter` | CallArgumentRenderer.Render(argument, CallCrossing) (ArgText's successor; CallCrossing exists) |
| `Binding.BinderDriver` (R0-0025) | the unit forest and scopes, the ordered pass schedule (the `Validation.*Pass.Run` calls) | `CompilationGroupBinder`; ValidationSchedule (both in compiler.pipeline) | CompilationGroupBinder.Bind(tree, edition, directives) → BoundCompilation; ValidationSchedule.Run(BoundCompilation) |
| `CodeGen.Emit.NumericRenderer` (R0-0026) | the numeric visitor; the Dec/Real/Int128 arm choice (`Power` → `CobolDec.Pow` vs `Math.Pow`, PB28) | NumericCarrierRules (the one place the numeric arm is chosen); the visitor stays | NumericCarrierRules.Choose(…) → the NumXCarrier case (an enum today; the NumX re-model of §8.5, PB2411, then makes it the closed carrier) |
| `Binding.Procedure.ExpressionBinder` (R0-0027) | operands (`Operand`), arithmetic expressions, index and literal operands | OperandContextRules to its own file; `ArithmeticExpressionBinder`; `OperandBinder` | OperandBinder.Bind(ctx, operand context) → BoundOperand (the one operand funnel of DESIGN-binder-bound-tree §3.6) |
| `Binding.Model.DataItem` (R0-0028) | a record that grew methods; the most-depended-on type | DataItem (data only); DataItemQueries (category, usage, class); DataItemLayout (sizes, offsets) | DataItem's fields, immutable after binding; the queries and layout as types over it |
| `Frontend.Preprocessor.CopyProcessor` (R0-0029) | text location and case probing, REPLACING matching (§7.2.3; R36), nesting | `CopyTextLocator`; `ReplacingMatcher`; CopyProcessor keeps the nesting driver | CopyTextLocator.Locate(text-name, library-name) → source text; ReplacingMatcher.Apply(text, REPLACING operands) |
| `Binding.Procedure.CallBinder` (R0-0030) | arguments, the target (resolver-driven after slice 7), the statement | `CallArgumentBinder`; `CallTargetResolver`; `CallBinder` | CallTargetResolver.Resolve(ctx) → the call target (the slice-7 resolver's result) |
| `CodeGen.SequentialIoEmitter` (R0-0031) | READ, WRITE, OPEN/CLOSE, RELEASE/RETURN (`Record`, `Read`, `Released`) | `ReadEmitter`; `WriteEmitter`; `OpenCloseEmitter`; `ReleaseReturnEmitter` | Emit(the statement's BoundStatement leaf, EmitContext), one emitter per leaf |
| `Runtime.CobolEdit` (R0-0032) | §13.18.40 editing; the `Float` partial | `CobolEdit`; `FloatEdit` | CobolEdit keeps the emitted entries; FloatEdit is its owned floating-insertion editor |
| `Frontend.Preprocessor.ReferenceFormatProcessor` (R0-0033) | the §6.5 logical conversion (one pass, PB1491) and the line builder (the partials) | `LogicalLineConverter`; `LogicalLineBuilder` | LogicalLineConverter.Convert(source text, reference format) → logical lines (§6.5, one pass) |
| `Runtime.CobolArgAdapt` (R0-0034, `CallAbi.cs`) | one file holds the whole CALL ABI (`CobolPassMode`, `CobolArg`, `BoundaryItem`, `ICobolProgram`); adapters by boundary class | CallAbi.cs split one type per file (CobolPassMode, CobolArg, BoundaryItem, ICobolProgram); ArgumentAdapters, one per boundary class | a boundary-class selector picks an IArgumentAdapter (`BoundaryClass`, a §14.8.2.2 conformance class, was deleted by kb/Work PB165: `BoundaryItem.Description` carries those facts) |
| `CodeGen.Emit.ConditionRenderer` (R0-0035) | the condition visitor; range membership (`Membership` 4), class conditions | `MembershipRenderer`; `ClassConditionRenderer` | Render(a BoundCondition leaf, EmitContext), ConditionRenderer keeping the dispatch |
| `Runtime.CobolDec` (R0-0036) | standard-decimal arithmetic; the unscaled helpers | `CobolDec`; `DecUnscaled` | DecUnscaled, the unscaled-Int128 helpers CobolDec owns; emitted names unchanged |
| `Frontend.Preprocessor.ConditionalCompilationProcessor` (R0-0037) | `>>IF`/`>>EVALUATE` evaluation, `>>DISPLAY` (the partial), the line walk | `DirectiveEvaluator`; `DisplayDirective` | DirectiveEvaluator.Evaluate(expression, compilation variables) → value |
| `Frontend.Generated.CobolParserCoreBase` (R0-0038) | the ANTLR superclass: the lookahead predicates (`…Ahead`, `Locale`) | `LookaheadPredicates`; `LocalePredicates`; EditionWordPredicates (every member that reads the compile edition, §8.4's pin, as one set); the type's namespace becomes its folder's (R0-0222) | static predicate types the grammar's predicate actions call directly (no forwarding member on the superclass); a grammar wave, so after the §4 item 3 token and parse-tree differential (PB2350) exists, and after PB2291 decides which edition reads stay |
| `CodeGen.EcEmitter` (R0-0039) | per-statement checking state, declarative dispatch (`Dispatch`) | `EcCheckingEmitter`; `DeclarativeDispatchEmitter` | EcCheckingEmitter.Emit(EcStatementInfo, EmitContext); DeclarativeDispatchEmitter.Emit(the unit's declaratives) |
| `Runtime.IO.FileConnector` (R0-0040) | the base of the three organizations: open modes (`Open`), the record area | OpenModeMachine (the §9.1.13 transitions); `RecordArea` | OpenModeMachine.Transition(open mode, operation) → I-O status |
| `CodeGen.PlaceRenderer` (R0-0041) | rendering per `Place` kind (`Group`, `Image`); its `_ => throw` arms | one renderer per Place kind family | one visitor over the Place union (§8.5 step 3); each family renderer Render(a Place kind, EmitContext) |
| `CodeGen.GroupImageCodec` (R0-0042) | AsImage/FromImage (`Image`), bit images, image initialization | `GroupImageCodec`; `BitImageCodec`; `ImageInitializer` | the AsImage / FromImage entries (existing) |
| `Binding.Procedure.EvaluateBinder` (R0-0043) | subjects and objects (`Subject`), WHEN phrases | `EvaluateSubjectBinder`; `WhenPhraseBinder` | EvaluateOperandCombinations (existing) pairs subjects and objects |
| `CodeGen.SortEmitter` (R0-0044) | keys, INPUT/OUTPUT PROCEDURE ranges, MERGE | `SortKeyEmitter`; `SortProcedureEmitter`; `MergeEmitter` | Emit(BoundSort or BoundMerge, EmitContext); SortKeyEmitter.Render(keys) |
| `Validation.FlagConformancePass` (R0-0045) | the directive-axis flags; the `FileStatus` partial | `FlagConformancePass`; `FileStatusFlagRules` | FlagState (existing) |
| `Binding.Procedure.SortBinder` (R0-0046) | SORT and MERGE (`Sort`) | `SortBinder`; `MergeBinder` | SortKeyAdmission (existing) |
| `Runtime.IO.IndexedConnector` (R0-0047) | random and sequential access over an unindexed store (PB2129 carries the measure) | KeyIndex (what PB2129 adds); the connector keeps the access modes | KeyIndex: find, insert, remove and ordered scan by key |
| `CodeGen.ReportWriterEmitter` (R0-0048) | groups, controls | `ReportGroupEmitter`; `ReportControlEmitter` | Emit(ReportGroupModel, EmitContext) |
| `Binding.Procedure.InitializeBinder` (R0-0049) | categories (`InitializeCategories` in `Bound/` is a rule table), REPLACING | the INITIALIZE category rows as data; `ReplacingPhraseBinder` | InitializeCategories (existing rows) and ReplacingPhraseBinder.Bind(ctx) → the REPLACING phrase node |
| `Binding.Procedure.ProcedureTableBuilder` (R0-0050) | pc indices and ranges; the procedure-name scope (`ProcedureNameUniquenessDriftTests`) | `ProcedureTableBuilder`; `ProcedureScopeRecorder` | the procedure table it returns (pc index → range), unchanged |
| `Runtime.CobolDate` (R0-0051) | calendar arithmetic (`Of`, `To`), FORMATTED-* rendering (`Datetime`), parsing | `CobolDate`; `DateTimeFormatter`; `DateTimeParser` | emitted entries unchanged; the formatter and parser are owned collaborators |
| `Binding.Bound.StatementBinder` (R0-0052) | dispatch and composition root; its fields; the `SubscriptSegments` partial dies with PB2151 | StatementBinder moves to Binding/Procedure; StatementBindContext (its fields) | StatementBindContext, passed to the verb binders in place of StatementBinder's fields |

### 8.4 Grammar structure

**Measured.** One lexer grammar, `Grammar/Core/CobolLexer.g4` (1,301 lines, 548 token rules, the modes `PICMODE` and
`COMMENT_MODE` beside the default, the channel `ABSENT_DEBUG_LINE`); one parser grammar, `Grammar/CobolParserCore.g4`
(2,001 lines, 185 rules), importing ten fragments under `Grammar/Core/`: `CobolExpressions` (81 rules), `CobolData`
(87), `CobolSpecialNames` (32), `CobolReportWriter` (36), `CobolIO` (115), `CobolControlFlow` (41), `CobolOO` (23),
`CobolScreen` (31), `CobolDeclined` (13) and `CobolWords` (2, generated from `tests/version-matrix/cobol-words.json`
by `scripts/gen-cobol-words.ps1` and committed as a build INPUT, single-sourced with the lexer word set by
`CobolWordsDriftTests`). The core's own sections, by its headers: the identification division (lines 85–428), the
environment division and configuration section (429–635), the procedure division header, declaratives, body and
statement dispatcher (636–1026), the shared arithmetic rules and ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE
(1027–1200), MOVE, CALL, ENTRY, ENTER, CANCEL, SET (1201–1843), ACCEPT, DISPLAY, GOBACK, RAISE/RESUME and the reusable
exception phrases (1844–2001).

**Edition reads in the grammar: a rule over bodies, not a list of spellings.** Most semantic predicates are lookahead
disambiguation (`{boolExprAhead()}?`, `{!numericLiteralIsLeftOperand()}?` …). **The rule:** EDITION STATE is the
parser's `Edition` (`Parsing/CobolParserCoreBase.cs:22`, which the front end sets, `Pipeline/Frontend.cs:498`, to the
compile edition for the program and to whatever edition a fragment parse is given), any `EditionInfo` value (a
parameter, or a fixed one: `DirectiveExpressionFragment.cs:45` parses every directive expression at
`EditionInfo.Latest` on purpose and gates the `>>` facility separately, so "the compile edition" is not what every
parse reads), and every lexer field the front end primes from one
(`_caseMappingEdition` and `_reservedNonNames`, set by `TokenRetypes.PrimeLexer` from `edition.Year` and from
`ReservedNonDataNames(edition, …)`, `TokenRetypes.cs:47-51`). An EDITION READ is a member of `CobolParserCoreBase`, of
the lexer grammar's `@members`, or of the retype stage (`TokenRetypes`, which primes the lexer and decides the re-lex)
whose body reads edition state or calls another edition read, to a fixpoint; an edition-read SITE is any parser or
lexer predicate `{…}?` or action `{…}` body, any lexer override (`NextToken`, `Emit`), any parser listener, any
retype-stage member, or any front-end DRIVER member that hands edition state to a lexer or parser or decides a
re-lex from it, that calls one or passes edition state on: the pipeline's parse-again loop (`Pipeline/Frontend.cs:425-463`,
calling `LexesDifferentlyFrom` at `:437` and `PrimeLexer` at `:463`) and the fragment parser (`FragmentParse.Parse`,
`Parsing/FragmentParse.cs:62,72,93`, and its callers `SubscriptExpressionFragment.cs:31` and
`DirectiveExpressionFragment.cs:45`), both added by Draft 6 after the fifth refuter (G4). Names play no part, so a read under a new name, in any of the three stages, is
caught the day it is written. Applied to the parser at `3d44e5f22` (*text scan, provisional*: the third refuter's
transitive scan), the rule finds these members: `is85`, `is2002`, `is2014`, `is2023` (`:59-62`);
`setLocaleAhead` (`:190`, the `LC_` escape of SET LOCALE); `reservedHere` (`:344-345`, `IsReservedAt(Edition.Year)`);
`facilityWord` (`:557-563`) and `screenPositionAhead` (`:288-298`, COL, COLS and COLUMNS), both through `reservedHere`;
`keywordContinuesHere` (`:437`) through `IsKeywordReadingHere` (`:467`); `userWordHere` (`:371-375`); the action
`gatedDeclaration` (`:389-392`, through `userWordHere`) and `NoteGatedOffender` (`:401-404`, called by the
authoritative pass's syntax-error listener); and `ReservedUserWordViolation` (`:545-554`, the COBOLNET0901 message, not
a predicate); `DialectLevel` is the edition's setter, not a read. And these sites: five spelled `{isXXXX()}?` —
`| {is2023()}? LOCATION` (`CobolControlFlow.g4:66`), `| {is2002()}? validationClause` (`CobolData.g4:379`),
`({is2002()}? validateValidPhrase)?` (`CobolData.g4:828`), `| {is2023()}? applyCommitClause` (`CobolIO.g4:334`) and
OPEN's `{is2002() || retryPhraseAhead()}?` (`CobolIO.g4:393`); `{reservedHere("CRT")}?` and `{reservedHere("CURSOR")}?`
(`CobolSpecialNames.g4:33-34`); the five `{facilityWord(…)}?` statement arms (`CobolParserCore.g4:929-933`);
`{setLocaleAhead()}?` (`CobolParserCore.g4:1475`); DISPLAY's operand loop `{!screenPositionAhead()}?`
(`CobolParserCore.g4:1898`: at `--std=85` `DISPLAY A COLS` takes COLS as a second operand, from 2002 it ends the loop,
kb/Work PB260); the `reservedGatedWord` action `{ gatedDeclaration(…); }` (`CobolWords.g4:218`, which decides the
words the re-parse retypes); the 71 `{!keywordContinuesHere()}?` alternatives of the generated `CobolWords.g4` — 86
grammar sites — and the one listener. Applied to the lexer and the retype stage at `d4eb89eac` (the fourth refuter's
F6), it adds the lexer members `ApplyEditionSpelling` (`CobolLexer.g4:136-150`, the Annex C spelling, reading
`_caseMappingEdition`) and `PreviousTokenCouldBeDataName` (`:63-64`, the subscript trigger, reading `_reservedNonNames`)
at the sites `NextToken` (`:318`) and the reference-paren action (`:222`), and the retype stage's `ReservedNonDataNames`
(`TokenRetypes.cs:66`), `PrimeLexer` (`:47-52`) and `LexesDifferentlyFrom` (`:58-59`). All but the five spellings read
word status (is this word reserved at this edition, and how is it spelled there) — `setLocaleAhead` included, whose own
comment calls LOCALE and USER-DEFAULT 1985 user words (`CobolParserCoreBase.cs:186-187`) — which the superset parse
cannot avoid: a
word that is a user-defined word in an earlier edition must still parse as one there. The grammar's comments give the
same reason for four of the five spellings (LOCATION, VALID, APPLY and COMMIT, RETRY); the fifth, `validationClause`,
gates a construct by edition. D7's statement in `COBOLNET_DESIGN.md` ("There is no parse-time `{isXXXX()}?` edition
predicate") was not what the code does; that document now says what the code does, and kb/Work PB2291 adjudicates
every member and site the rule finds against D7's purpose (a word-status read the superset parse cannot avoid, or a construct gate that
belongs in `VersionConformancePass`). Draft 1's "zero" and its correction of §3 R1 and of the `architecture-review`
skill stay withdrawn; that skill sentence describes the code.

**Target: one fragment per division or facility; the core is the compilation group.** The rule: a grammar rule
lives in the fragment of the clause or statement whose general format defines it, so the `spec-grammar-conformance`
audit's address space (clause → rule) equals the file layout (clause → fragment). New fragments: `CobolIdentification`
(§11), `CobolEnvironment` (§12; `CobolSpecialNames` folds in, because §12.3.1 lists SPECIAL-NAMES as a paragraph of
the configuration section), `CobolProcedure` (§14's division header, declaratives, sentences, the statement
dispatcher, the shared arithmetic rules, and the nucleus verbs the core holds today). `CobolData` (§13),
`CobolControlFlow`, `CobolIO`, `CobolOO`, `CobolReportWriter`, `CobolScreen`, `CobolDeclined` and `CobolExpressions`
keep their families. The core keeps the §10 compilation-group skeleton (`compilationUnit`, `compilationGroup`, the
program, class and interface shells and end markers) and the import list, and nothing else. The parse arm of §8.3 (6)
dispatches by the same fragments; the regroup is kb/Work PB2410. What ANTLR's import rules give and withhold, and the design's use of each: a parser
grammar may import parser grammars, their rules merge, the importing grammar's rule wins over an imported one of the
same name, and imported fragments may reference each other's rules — that is how the ten fragments already work. When
two IMPORTED grammars define the same rule, ANTLR keeps the first one it finds, depth-first in import order, so the
import list is load-bearing for a duplicate: `className` was defined in `CobolExpressions.g4` (the class-condition
keywords and `cobolWord`) and in `CobolOO.g4` (`cobolWord`), and the Expressions definition won, so CLASS-ID, END CLASS,
INHERITS and OBJECT REFERENCE accepted the class-condition keyword set (kb/Work PB2292, landed: the class-condition
operand is now `classConditionName`). Regrouping changes the import list, so the regroup (PB2410) runs after PB2292, and
`GrammarRuleUniquenessDriftTests` asserts that no rule name is defined in two grammar files — so the next duplicate fails at the gate
instead of being decided by file order. A lexer grammar's import appends the imported rules after the importing
grammar's own, and the lexer's precedence among equal-length matches follows rule order, so a split lexer would move
token precedence with the import order; **the lexer stays one grammar**, its modes' DFA start states pinned by
`LexerDfaCacheDriftTests`.

**Kept, each with its pin:** no left-edge semantic predicate in a lexer mode (`LexerDfaCacheDriftTests`); the edition
reads are what the rule above computes — the grammar drift test APPLIES the rule (it computes the members' fixpoint
from the superclass's syntax and the call sites from every predicate and action body of the parser grammars and every
parser listener) and compares the result with the set PB2291's verdict records, per file, so a new read under any
name (a sixth spelling, a new `reservedHere` site, a new member, a new action or listener call) fails until PB2291's
verdict records it (PB2291 lands the assertion with its verdict, and PB2333 moves those members as one set, so the pin
names one type); `Generated/` is a build output,
never committed (`GenerateIfNewer.ps1`); the generated `CobolWords.g4` is committed because it is an input,
single-sourced from its JSON; the token-stream and parse-tree differential (§4 item 3, not yet built) is the oracle of
EVERY grammar wave, so no grammar wave runs before it exists. **The grammar leads, placed:** PB2143 (DISPLAY's inline
sending set, blocked by 83 differing error trees) is the differential's first customer and lands only with it (its `blocked_by` names PB2350, as PB2144's, PB2145's
and PB2146's do); PB2144
(the arithmetic Format-2 shape error diagnosed two ways) is a Unify inside the shared arithmetic rules as they move to
`CobolProcedure`; PB2145 (`invokeArgument` copies the `callBy*` bodies) makes `CobolOO` reference `CobolProcedure`'s
one argument rule; PB2146 (the identifier-only sending shape written three times) is one rule in `CobolExpressions`;
PB2147 (a removed spelling never swept against the corpus) becomes a step of the grammar-wave template, whose input
population is the differential's; PB2151 (D10's second half) is a binding wave, the prerequisite of §8.3 (4), not a
grammar change; PB2292 (the duplicate `className`, landed) precedes the regroup.

### 8.5 Closed hierarchies → C# 15 unions

**Measured.** Seven `[BoundNode]` roots with generated exhaustive visitors (`BoundVisitorGenerator`,
`Cobol.Net.Compiler.SourceGen`): `BoundExpr` 13 leaves, `BoundOperand` 11, `BoundBoolExpr` 8, `BoundCondition` 14,
`BoundStatement` 97, `BoundPerformControl` 5, `BoundSetTarget` 2. Nine visitor implementations over five roots
(`BoundStatement` ×3, `BoundOperand` ×3, `BoundBoolExpr`, `BoundCondition`, `BoundExpr`), none over
`BoundPerformControl` or `BoundSetTarget`. Hand dispatch still in place (*text scan, provisional*, and regex-dependent:
two scans give 469 arms in 60 files and 519 in 65): `case Bound…`, `is Bound…` and `Bound… =>` arms, densest in
`EcBinder` (76 by both scans), `IntrinsicBinder`, `ConditionBinder`, `IntrinsicArgumentRules`, `ConditionRenderer` and
`VersionConformancePass`; and between 48 and 74 `_ => throw` or `default: throw` arms by the same two scans
(`PlaceRenderer` 4 in both). The ratchet below makes the census count them once, so the number stops depending on a
regex. Other closed hierarchies, each pinned today by a drift test or a `throw`: `Place` (18 sealed kinds and three
abstract decorators; `PlaceDenotedItemDriftTests` says every kind has an answer), `RefResolution` with `RefOutcome` and
`DeferredShape` (`RefResolutionDriftTests`), `AccessSegment` (fixed, dynamic, cell), `WindowCoding` and
`CellWindowCoding`, `AllCount`, `BoundEoOperand` (2), `MoveKind` and `MoveStore`, `SetFormat`, `IntrinsicDomain` and
`IntrinsicCodomain`. **The numeric carrier is not a hierarchy today**: `NumXCarrier` is an enum
(`internal enum NumXCarrier { Scaled, UnsignedWide, Sdidi, Binary64 }`, `CodeGen/Emit/EmitCore.cs:188`) computed from
three mutually exclusive bools on the record struct `NumX(string Expr, int Scale, bool Dec, bool Real, bool U, bool
Approximate, int Digits)` (`EmitCore.cs:178-183`), and the enum's totality is already pinned by
`IntrinsicCarrierAgreementDriftTests`.

**The generator's two outputs, and which one a union replaces.** `BoundVisitorGenerator` emits two things from each
`[BoundNode]` root. (a) The **dispatch**: the `I{Root}Visitor<T>` interface and the `Accept<T>` switch
(`BoundVisitorGenerator.cs:96-120`). A union's checked switch replaces exactly this. (b) The **structural walk**,
`BoundStatementTree`: `StatementChildren` (the statements nested in a statement) and `OwnValueParts` (every
`BoundExpr`, `BoundCondition`, `BoundOperand` and `BoundBoolExpr` a statement holds directly, read from EVERY property
of every case through the semantic model, `:126-244`), which walks THROUGH the two container roots
`BoundPerformControl` and `BoundSetTarget` by the generated `Parts_<root>` helpers. `EcBinder` builds the ambient
EC-ARGUMENT-FUNCTION gate on it (`EcBinder.cs:1000,1034`; §15.3 item 14), and it is generated because of kb/Work PB26,
a landed wrong-answer, silent defect: the gate was a hand-maintained switch over statement kinds, so the same FUNCTION
reference raised in COMPUTE and was silent in STRING. A switch is total over CASES, not over a case's FIELDS: the day a
statement record gains an operand property, a hand switch misses it and `FUNCTION LOG10(0)` there raises nothing —
PB26 again. So (b) is never hand-written: it stays generated from the type structure for every root, union or not,
and `[BoundNode]` stays as the marker of the walk's roots (the partition `EcArgumentFunctionGateDriftTests:96-115`
pins, `BoundNodeRootPartition_IsExplicit`), whatever mechanism dispatches over that root.

**Which, in what order — each one R4 wave under §4** (the dispatch results do not change, so diagnostics and emitted
C# are identical by construction; the SDK that provides the feature is R64's trailing decision, §5.5, and nothing
below starts before it):
1. The small roots with no visitor IMPLEMENTATION: `BoundPerformControl` and `BoundSetTarget`, whose generated
   dispatch (a) has no implementer and is retired in the same wave, and `BoundEoOperand`, which is no `[BoundNode]`
   root — the mechanics proven on the smallest families. The two are the walk's CONTAINER roots, so they keep
   `[BoundNode]` and the generator keeps emitting their `Parts_BoundPerformControl` and `Parts_BoundSetTarget` walkers
   (b): the wave changes the generator to read a root's cases from its union declaration instead of its derived
   classes (its property classification uses `IsDerivedFrom`, `EcArgumentFunctionGateDriftTests.cs:218-272`, which the
   wave converts with it), and `BoundNodeRootPartition_IsExplicit` keeps its seven names: its root predicate is
   `t.IsAbstract && …` (`:100`), which a union root that is not an abstract class fails, so the wave changes the
   predicate to read the `[BoundNode]` attribute, never the set.
2. `Place` (18): `PlaceRenderer`'s four throw arms and every `is …Place` chain become checked switches.
3. `RefResolution`, `AccessSegment`, `WindowCoding`, `AllCount`, `MoveKind`, `SetFormat`.
4. The visitor-covered roots, smallest first: `BoundBoolExpr` (8), `BoundOperand` (11), `BoundExpr` (13),
   `BoundCondition` (14); each wave retires that root's generated `I…Visitor<T>` and turns its visitor classes into
   switches.
5. `BoundStatement` last; when it converts, the generator emits no dispatch (a) at all, and it keeps emitting the
   structural walk (b), `StatementChildren`, `OwnValueParts` and the `Parts_` walkers, from the seven `[BoundNode]`
   roots' union cases, so `Cobol.Net.Compiler.SourceGen` stays (a source generator targets `netstandard2.0` by the
   Roslyn host's rule) with one job, the walk. Draft 3's hand-written total switch is withdrawn: it was PB26's
   mechanism. At every point between, each root's DISPATCH is EITHER a union OR a visitor, never both (one mechanism
   per root), and its WALK is always the generator's.

**The numeric carrier is a re-model, not a conversion, and it is not an R4 step.** The defect class (one rule
corrected in one arm: PB2, PB13, PB14, PB28, PB32, PB33) lives in the flag record, so the target is `NumX` itself: a
closed carrier of four cases (scaled, unsigned-wide, standard-decimal, binary64) replacing the exclusive `Dec`, `Real`
and `U` flags, with `Approximate` legal only on the binary64 case, so an illegal flag combination stops being
representable and a missing arm is a compile error. `NumX` carries C# text (`Expr`) through every renderer, so the
change re-types every numeric path that builds or reads a `NumX` (`EmitCore.cs:178-188`, `NumericRenderer`,
`IntrinsicRenderer`, `ConditionRenderer`, `OperandText`, `RuntimeApi`, `StatementEmitter` and thirteen verb emitters).
It is its own note, PB2411, after the `NumericCarrierRules` extract of R0-0026 (PB2321, which keeps the visitor and
only gathers the arm choice into one place): a code-generation wave between trains under the partition, done before the SDK with a
sealed record family and a total switch, and converted to a union by the step-1 mechanics once the SDK lands.

**Before the SDK (R3, no C# 15 needed):** a ratchet drift test over the hand arms per file (the `RuntimeApiGuardTests`
shape), failing on any increase, so a new consumer of a visitor-covered root is a visitor; it drives the hand arms
toward zero as consumers convert and leaves the R4 waves a smaller surface. Nothing about the feature's syntax is
assumed here; the wave that lands step 1 pins it against the SDK the owner chose.

### 8.6 What is given

Not reopened by §8 or by any wave: the owner-approved `DESIGN-external-repository.md` (2026-10-05) — its slices
PB2097–PB2104 are open, and its §12 table (all three columns: the type changed, the change, and the callers updated
in the same change) is the in-flight file set a wave never touches until the slice lands (`ProgramEmitter`, `ProgramTable`, `ProgramRegistry`, `DataBinder.Unique` and `DataItem.Sanitize`,
`CallEmitter.ArgText`, `RecordStructEmitter`, `GroupImageCodec`, `BindSession.Repository`, `BinderDriver.Bind` and
`BindUnitData`, the program and function tables, `PrototypeSignatures`, `ObjectRefDescriptor`,
`PicInfo.RestrictedPrototypeName`, `OoClassTable`, `OoClassSymbol`, `OoRepositoryScope`, `OoExpansion`,
`OoNameResolution`, `RoslynBackend.Compile`, `BackendOptions`, `CompilerDriver.Options`, `Directory.Build.props`,
`Cobol.Net.Runtime.csproj`, the 14 characterization snapshots); the text model (R51, R52, R53; `COBOLNET_FILES_DESIGN`
D29); P15 Cut 3 (the runtime namespace flip at v1.0, and its item 4a: the static runtime facades deleted, emitted code
reaching the instances through the captured `RunUnit`, one reviewed re-baseline); the §0.5 deep dives as the subsystem
SSOTs whose seams §8.1 formalizes (`COBOLNET_PIPELINE_DESIGN` D1 and D7; `DESIGN-codegen-backend` §2.2 and §2.3;
`DESIGN-backend-abstraction` §1 and §2.3 — §8.3 (9) implements §2.3 with its catalogue named `RuntimeMembers`, and
§1.3's table gains the L6 row (the leak PB2293 records), the two amendments §8 makes to a given; the first because the owner-approved external-repository design, the later decision, owns
`RuntimeAbi`; `DESIGN-binder-bound-tree` §3.1;
`DESIGN-runtime-library` §2; `DESIGN-edition-framework` §2.1; `DESIGN-version-conformance-pipeline` §1.1); every
landed owner decision R00–R69; the oracle and the baseline as the measures; the four-edition mission (D13) and
typed-native data (no byte substrate). The `ICodeGenBackend` seam stays (`CodeGen/ICodeGenBackend.cs:54`), so a CIL
backend drops in behind it; §8.1's refusal of `codegen → binding` and §8.3 (1), (4) and (9) are what make that true for
the backend's input and for the runtime surface.

### 8.7 The wave plan's shape, and the R3 size estimate

**Shape** (a wave is one mechanism, one kind, proven by §4). **The orders collapse** (owner, PB2118 question 5,
2026-10-07, "Drop it"): every wave is "between fix-lane trains under the file-set partition" unless a NAMED gate holds
it, and the named gates are the only ones there are — PB2118 (satisfied: the owner approved §8 on 2026-10-07), PB2417
(the drift test and the edges file land before any Move/rename or Extract), PB2350 (the grammar differential before any
grammar wave), an open external-repository slice whose file set the wave's files meet (by note: the note names the
slice in `blocked_by`), PB2151 and PB2129 where a note's sites need them, and v1.0 for Cut 3 (PB2349, with the
`ExceptionEngine` split PB2309; R69 §1). "Leaves first" stays as the ORDER the planner prefers, never a gate:
1. **Delete** — the census-measured items already filed (PB2119's notes; the test-only judgment below).
2. **Move and rename** — PB2417 first (seen failing once); then the runtime's folder-equals-namespace waves (after them
   the runtime's layer table is measured and written into the edges file), the front end's three folders, the
   editions' multi-type files, the CLI root flip (PB2414), the runtime control and I-O folder moves (PB2248–PB2251), the
   binding and code-generation folder renames (PB2239–PB2242), and the editions, front-end and compiler root flips
   (PB2412, PB2413, PB2415), each when its file set is free.
3. **Extract, Unify and data-ize** — the leaves' (`CobolDec`, `CobolEdit`, `CobolDate`; `DebugLineCarrier`;
   `DiagnosticCatalog`, the frontend catalog and `EditionCodes` as one data file; the runtime I-O, control and
   report-writer extracts; the preprocessor and parser-superclass extracts), then binding's and code generation's by
   the notes' `blocked_by` order: the moves out of `Bound/` and the `BoundTree.cs` family split, the parse-arm split of
   §8.3 (6) after the grammar regroup, the `ReferenceResolver` → `DataBinder` hand-off that removes §8.1's largest
   refusal, `RuntimeApi` to `RoslynRuntimeApi` over `RuntimeMembers` (ratchet to zero first) and then the runtime
   extracts that rename emitted entries (`CobolIntrinsics`, `CobolNum`), Cut 3 with the `ExceptionEngine` split at
   v1.0, the rest. The tests re-home is a wave of this kind's sort too, under the partition (PB1754 amended).
4. **R4**: the SDK decision, then §8.5's order, then §5.5's analyzer waves one rule each.

**The file-set partition** (owner, PB2118 question 5; R69 §2; mechanical, one rule in one place). A wave's file set
is COMPUTED, never listed (PB2118 Draft 8; the seventh refuter, J2, found PB2296's hand list missing 16 callers and
naming a file that does not exist, and Draft 7's "the brief computes callers with the rewriter's dry run and
`where.py`" was no mechanism: `where.py` maps spec clauses to files, and the brief is rendered after the check). It is
every site its notes name, whatever the score (a bare type name included; Draft 7 kept only `fix_clusters`' scores of
2 and up, J3), plus every file under a folder its sites name, on the current tree, plus every file the MEMBER INDEX
finds using or emitting what a note moves or changes: the note's `**Moves or changes:**` names, the documentation ids
in its code spans, and every word of its target paragraph that is a member of a type the note names or its sites
declare (Draft 9: PB2296's hand-listed line left out `DataBinder.Options`, which its target moves and 15 files use;
the eighth refuter, K1), else every type its Sites declare (a folder, a class's files). The index is the census's own
Roslyn walk (`tools/ArchCensus --member-index`, recorded by `scripts/arch/member_index.py --record` as frozen evidence
`<sha>.members.json.gz`: per symbol, the authored product and test files that use it; per type and member, the files
that declare it; and, for every type and member of the EMITTED SURFACE, the authored files whose string or
interpolated text writes its name, because `catch (ProgramReturn)` in `DispatchEmitter.cs` is C# text and binds no
symbol; K1). Every index carries `treeKey`, the hash of what the walk read (the tracked C#, project and solution files
of `src/` and `tests/`, and the host's sources), and the planner plans only on THE index of its own tree
(`member_index.current`: a committed or cached record with that key, else one regenerated into
`<coord>/member-index/`, else a refusal with `--no-index-regenerate`): a stale index cannot see a type or file a
landing added (PB833's `OfdRegionLocks.cs` inside PB2251's site folder; K1), and Draft 8's "an older index only widens
a set" was false. The readers bounded by time (the dispatch guard's record, `work.py check`) use the newest committed
record plus a text re-scan of every file changed since it and the folder expansion. For PB2296 step 1 the planner's
set is 73 files (Draft 8: 66), and holds every one of `DataBinder.Options`'s six users the refuter found outside it
(`SetBinder.cs`, `InitialStateBackground.cs`, `PhysicalModel.cs`, `ConditionRenderer.cs`, `IntrinsicRenderer.cs`,
`NumericRenderer.cs`); PB2249's holds the nine code-generation files that write the runtime signals as text. The dispatcher, `scripts/orchestrator/plan_wave.py --cluster` (`file_set_collisions`, the ONE
implementation), checks it against (a) the in-flight work, read from sources a branch classification cannot drop
(`inflight_file_sets`): every worktree's changed files, committed or not, and every unlanded branch's, each counted
only while it still differs from `origin/main`, plus every group of the dispatch ledger
(`<coord>/inflight-groups.json`, written when a wave is planned for dispatch, and by the dispatch guard for every
write-capable HAND dispatch it admits, an attended Fable or Mythos author or a direct Agent call, with the open notes
its prompt names and their computed file sets, so the planner sees it before its first edit; J3) whose notes are
still open — Draft 6
read only the branches `prune_worktrees.classify` called UNLANDED or CHECK, which drops a dispatched branch with no
commit (MERGED) and one that edits only existing files (LANDED; the sixth refuter's lead); and (b) the open
external-repository slices' in-flight set (`work.slice_file_set`): every file `DESIGN-external-repository.md` §12
names for a row not yet landed, in ALL three columns (Draft 6 read only "Today", which missed the twelve caller
files of "Callers updated in the same change"; H4), from every name in a code span, not only the first (`: GroupRepository`), a type
mapped to each file declaring it, a member to the file declaring it (a one-word or widely declared name is reported,
not guessed), and the `tests/` and `docs/` paths the table names; a collision defers the wave, with the collision
named, never the train.

**The planner's set is the ADMISSION estimate; the landing check is the GUARANTEE** (PB2118 Drafts 9–10). Three refuter
rounds each found a new hole in the computed set (hand lists, then the index's blind spots, then its staleness), so a
remaining miss must stop a landing, never land silently. `scripts/orchestrator/landing_check.py` runs in
`scripts/push-main.sh`, the one way a commit reaches `main`, before a CI run is spent. It reads the notes a landing
WORKS ON (the ids leading its commit subjects, ranges expanded, and the notes whose `status` it changes; a commit that
only mentions a note does not work on it) and compares the landing's ACTUAL diff (`git diff --name-only --no-renames
origin/main...<rev>`: a moved file counts at its old path too, and the planner's admission reads diffs the same way,
through one reader, `plan_wave.changed_files`) with
(1) its declared file set, when it lands R3 work: the set the dispatch ledger recorded when the work was planned or
admitted, or the set computed now for work never recorded; a changed file that existed on `main` and lies outside it
stops the landing; and (2) every other in-flight branch's actual diff and every dispatched group's whole declared
set until its notes are terminal, started or not (`inflight_file_sets`), when either side is R3 work: a shared file
stops the LATER of the two by dispatch time (the ledger's `at`, one millisecond apart per group so a wave never ties,
else the branch's first commit), which re-plans and rebases onto the earlier once it lands, and the earlier one lands
with a warning naming the branch that must rebase. An in-flight branch is R3 work because its DISPATCH says so, never
by its commit wording (implementers commit `WIP checkpoint: …`): a train manifest that lists it, the dispatch-ledger
groups whose declared files its actual diff meets, then the ids leading its subjects or whose status it flips. The
landing's OWN members are excluded by its train manifest (`<coord>/scratch/train<label>-manifest.json`, which the
rolling wave has its lander write first) and by content (a file whose version on the member IS the landing's: the
lander re-commits its clusters); a branch whose commits the landing contains is the landing itself; anything else
that shares a file and was dispatched earlier stops it, naming how it was identified and how to bind a member it did
not recognise. An aborted dispatch's entry never expires on a clock; it is released by hand (`plan_wave.py --release
<wave|group|note>`). Fix-lane work against fix-lane work is a train's ordinary merge and is not checked. The check
costs 7.6 s on a real range with 33 worktrees (the ninth refuter measured 64 s for Draft 9: the per-worktree reads now
run on a thread pool, and a branch's register changes are one patch, not two reads per note). A STOP is
`push-main.sh`'s exit 3. It is enforced where every landing passes, not by the server: CI has no worktrees and no
dispatch ledger to compare with, so the server-side check is the existing `ci-gate`, and the forbidden-commands hook
refuses a bare push to `main`. The domain is
the campaign's code roots and extensions (`model_rules.json` `campaign`) without `.md`: a document both sides edit is
a text merge the rebase resolves. Its self-test (`--self-test`, a CI step, 20 cases) plants an out-of-set edit, a
two-branch overlap, a rename each way, a `WIP checkpoint:`-only R3 worktree, a train against its own implementer
(recorded, unrecorded, re-resolved with and without its manifest, and beside a stranger) and a started wave's
untouched declared file, in a scratch repository, and fails with the check disabled.

The register
verifies alone what it can (`python scripts/spec/work.py check`): every path an open PB2119 note names exists in
the COMMITTED tree, read from the git index and never the filesystem, so a built checkout and CI's decide alike (a
file the wave creates is marked `(new)`; a generated parser file under `Generated/` is a build output no wave edits,
and a note naming one must also name the `.g4` it comes from — PB2197, found by CI at the R1 landing), every name it moves or changes is one the member index knows (a typo or a
member deleted since fails: the first run found PB2197's two lexer tokens, deleted by 61c1c04dd after the census), an
open PB2119 note whose declared sites name a file of set (b) reaches an open slice note through `blocked_by`, and
every Move/rename and Extract note reaches PB2417. The CALLERS' collision with set (b) is the planner's alone,
recomputed at every plan: callers change with every landing, so a `blocked_by` written from them would be a hand list
that drifts (on today's index 77 R3 notes have a caller in set (b) that their sites do not name; the planner defers
each while its slice is open). No folder
rule survives: Draft 5's allow-list (`R3_HELD_FOLDERS`, `R3_OWNER_ADMITTED`) is deleted (CLAUDE.md rule 4). Both checks
were seen failing on a planted collision, then green (PB2118 Draft 6). Their first real run found eleven notes the
folder rule never saw: five Delete notes on slice files (PB2170, PB2175, PB2178, PB2183, PB2228) and six designed waves
(PB2396, PB2398, PB2404, PB2406, PB2407, PB2421); each now names its slice. Draft 7's corrected slice set found six
more (PB2155, PB2160, PB2180, PB2311, PB2314, PB2323); each names the last slice that edits its file, in §21's order.
`test_plan_wave.py` sections 9d and 11 hold both readers: the two in-flight misses and the refuter's four slice plants
(`BindSession.cs`, `GroupRepository.cs`, `OoConformance.cs`, `NonCobolActivatorReturnTests.cs`) each fail Draft 6's
code and pass this one. A grammar wave needs §4 item 3 built
(PB2350); a runtime wave that renames an emitted member lists its oracle differences in the brief before it runs.

**The test-only judgment (67 findings, R0-0062–R0-0125, R0-0197, R0-0198, R0-0200).** A product member only tests
use is one of three things, and the Delete program decides each family by this rule: (a) **a table's enumeration**
(`All`, `Rows`, `Catalog`, `Owners`, `Level3Rows`, `StandardModes`, `LiteralOnlySlots`, `MixedRules`, `TableCount`,
`RangeCount`, `MappingCount`, about twenty of the 67 by name) is the drift tests' observation surface over data and
KEEPS — the census's `test-only` rule gains that exclusion so it stops reporting them; (b) **a seam that exists for
observation** (`ResetForTests`, `ClearCache`, `HostConfiguration`) moves to the test project as a helper over product
state where the product allows it, and is otherwise renamed to say it is a seam; (c) **the rest is dead product code a
test keeps alive** and is deleted with the tests that test only it, as the findings' own target says. R2's Opus
reviewer applies the rule; a Sonnet wave executes (c).

**The R3 size estimate, in the planner's units.** Every number below is `plan_wave.py --cluster PB2119 --dry-run`'s own
output (an implementer GROUP is at most five notes on one file chain, `model_rules.json` `wave.max_notes_per_group`; a
TRAIN is five groups, `wave.train_size`; the model is what `choose_model` routes), never a sum of targets. Because the
planner groups NOTES (`plan_wave.py:217-221`), §8.3's step orders are notes, so these counts include them. The
planner plans only what is unblocked and collides with nothing, and chains a note's successors inside one wave, so the
whole campaign is measured by running it, treating the notes it planned as landed, and running it again until it plans
nothing (a scratch driver that sets those notes' status in memory; the planner is unchanged), with `--no-branches`
(this branch names every note, and the unlanded-branch scan would otherwise group them as one finisher; so set (a) of
the partition is only the dispatch ledger, empty in the driver's scratch coordination directory, and only the slices'
set (b) defers). "Waiting" counts every open PB2119 note the run
never plans, whatever its title (Draft 5's driver missed PB2293 and PB2349 and said 113 for 115; the fifth refuter,
G6). Re-run for Draft 9 (2026-10-07) on this branch, with each group's file set COMPUTED (every site at every score
plus the member index's callers, its emitters and the target paragraph's members, on the index of this tree; J2, J3,
K1); the numbers equal Draft 8's: the six files PB2296 gained and the emitters PB2249 gained meet no file of the open
slices that the notes' other callers did not already meet (230 open PB2119 notes: 103 Delete, Move and Unify, PB2290, and 126
Extract-program notes, PB2293, PB2296–PB2347, PB2349–PB2351, PB2352–PB2421); R2's findings revise every row, and the
owner sees this table before R3 starts.

| Gates assumed cleared (cumulative) | Groups | Trains | Planner waves | Planned | Waiting | Groups holding Extract-program notes |
|---|---|---|---|---|---|---|
| none (today) | 57 | 12 | 1 | 67: 65 Delete, 2 Unify | 163 (every Move/rename and Extract note waits on PB2118 and PB2417; the Delete and Unify notes whose sites or computed callers meet an open slice's files) | 0 |
| PB2118, the approval: **the between-trains plan** | 68 | 14 | 2 | 80: 65 Delete, 3 Unify, PB2417, 9 Extract, 2 other | 150 (76 on a file of an open slice, 42 of them Extract and 24 Move/rename, nearly all through a computed caller; the rest PB2151, PB2350, PB2129, v1.0, or a chain behind one) | 9 |
| + the slices PB2097, PB2099–PB2101, PB2103, PB2104 | 136 | 28 | 4 | 200 | 30 | 66 |
| + PB2151 | 148 | 32 | 5 | 216 | 14 | 78 |
| + PB2350 (with PB2291, PB2292) | 156 | 35 | 6 | 227 | 3 | 86 |
| + PB2129 | 157 | 35 | 6 | 228 | 2 (PB2309, PB2349: v1.0) | 87 |
| + v1.0 (R69 §1, Cut 3) | 158 | 35 | 6 | 230 | 0 | 88 |

Draft 6's row for the approval planned PB2155, PB2160, PB2180 and PB2314 between trains while slices 7 and 8 edit
their files (the Callers column it did not read; H4). Draft 7's planned 141 notes on approval because its file sets
held only the sites the notes name; Draft 8's computed sets hold their callers too, so 61 of those wait for the slices
(the slices' §12 set names `BinderDriver.cs`, `DataBinder.cs`, `ProgramEmitter.cs` and the OO files most binding and
code-generation waves call into), and the whole campaign needs six planner waves where Draft 7's needed three: the
same notes, but a dependent whose blockers the planner put in different groups waits a wave ("blocker … is not planned
in this wave"), and the groups now form differently because a cluster's files are its computed sets. The approval's between-trains plan is therefore
smaller, and the slices are the gate that opens the binding and code-generation waves. `PB2173` (a Delete in `OoBinder.cs`) waits on the slices too, but only in the
planner: its census member's signature names `DataItem`, and the planner's site resolution counts the parameter type's
file (kb/Work PB2423; an over-deferral, never a missed collision).

The routing is the planner's today: area `architecture` routes to Opus, while R69 §4 says Sonnet for census-measured
deletions and rewriter-driven moves (the Move notes say so in their Model line) — kb/Work PB2294.

| Not yet notes | What files them |
|---|---|
| the 67 test-only findings, less R0-0081, R0-0082 and R0-0198 (rule (c): deleted whole with PB2189/PB2190) | R2's Opus reviewer, by the rule above |
| §8.2 rules 2–3 (178 multi-type files, 22 misnamed; `BoundTree.cs` already claimed by PB2393) | the census's columns (PB2351), then `file_census_notes.py` |
| the tests' re-home | R2's tests, scripts and CI reviewer, then `file_census_notes.py`; a wave under the partition (PB1754 amended) |

**The calendar is the gates, not the chains.** With every gate clear the planner needs six waves for the whole
campaign, because it chains each note's successors inside one wave (`after:`) and only a blocker in another successor
chain costs a wave (`BoundCompilation`'s move, PB2400, waits for the OO, rule-family and `DataBinder` chains); the
calendar is therefore set by the named gates: the approval, the external-repository slices (2, 3, 7 and 8 gate
`DataBinder`; 8 gates the OO symbols; the slices' §12 file set defers whatever else meets it), PB2151 (the subscript
step and `StatementBinder`), PB2350 (the grammar regroup and every grammar wave) and v1.0 (Cut 3 with the
`ExceptionEngine` split). The longest chain is `DataBinder`'s eleven steps (PB2296, PB2352–PB2361), its last behind
`ReferenceResolver`'s `PlaceBuilder` step (PB2375). On approval the between-trains plan is 68 groups in 14 trains over
two planner waves, PB2417 first, with 9 groups holding Extract-program notes; the binding and code-generation waves
Draft 5 held for GAP near zero now wait only for the slices whose files their callers meet. Two groups of one wave
can still share a computed caller, because the partition checks in-flight work and not the wave's own other groups
(kb/Work PB2424).

What the estimate does not contain, by design: R2's findings (17 subsystems in shards × 5 dimensions, §3), which set R3's real
size; the R4 waves (§8.5: 5 union steps plus §5.5's analyzer rules); and any wave list.
