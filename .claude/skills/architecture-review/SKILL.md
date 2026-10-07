---
name: architecture-review
description: Use when running (or preparing) the comprehensive, whole-codebase architecture review and restructuring of the WiseOwl COBOL compiler - grammar, preprocessor, lexer and parser drivers, binding, validation, lowering, code generation, runtime, editions and diagnostics - the PROMPT.md §4 comprehensive pass. Carries the project's plan (docs/rearchitecture/DESIGN-architecture-review.md), its behavior-neutrality oracles, subsystem map, known god classes, the model tiers, and the COBOL-specific rules a restructure must keep. R0 and R1 are STARTED by owner decision R69 (2026-10-06); the rest is sequenced in the design's §2.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:architecture-audit` (Skill tool) before reading on. If the plugin is
> not loaded (a cloud session receives no project marketplace), Read
> `tools/claude-skills/skills/architecture-audit/SKILL.md` instead (`git submodule update --init tools/claude-skills`
> if the path is missing). Its phases, contract, wave kinds, model tiers and anti-patterns are the procedure. THEN
> apply this overlay: it carries only what is specific to WiseOwl COBOL, and wins on conflict. Pinned:
> **brent-tools 1.19.1** (`tools/claude-skills`, kb/Work/PB1699).

# Architecture review (WiseOwl COBOL)

**The owner decisions are kb/Work R64 (2026-09-30) and R69 (2026-10-06); never re-ask them.** Project and
assembly names may change; the `tests/` layout is in scope; the preview-SDK question trails the review. The start
is SPLIT (R69): R0 and R1 run now; leaf and Delete waves run between fix-lane trains in partitioned subsystems; R2
and R3 over binding and code generation wait for GAP near zero. PB1754 is the decision card.

**The plan is `docs/rearchitecture/DESIGN-architecture-review.md`.** Follow it (CLAUDE.md rule 2); correct it in the
same change when implementation proves it wrong (rule 5). Phases: R0 baseline and oracle · R1 target architecture ·
R2 review fleet · R3 restructuring waves · R4 modernization · R5 close. The work items are the `PB1754` cluster in
`kb/Work` (R0: PB2115 census · PB2116 oracle · PB2117 performance baseline; R1: PB2118; Delete: PB2119). The legacy
engine's deletion was the `PB2108` cluster (landed), implemented from the plan's PHASE-15 section, not from this skill.

## Preconditions specific to this project

- **There is no legacy engine to refactor.** kb/Work PB2110 deleted it (`docs/rearchitecture/LEGACY-ARCHIVE.md` names the
  tag); the census and every wave cover `src/Cobol.Net.*` (`.agent-fleet.json` indexes that tree only).
- **The approved `DESIGN-external-repository.md` is given, not reopened.** Its slices PB2097–PB2104 are in flight;
  R1 designs around them and a wave never touches a file an in-flight slice names.
- **The fix lane is partitioned**, never paused: a wave's brief declares its file set, and it is checked against the
  in-flight trains' file sets before dispatch; a collision defers the wave, never the train.

## Dispatch (R69 §4 and PB2120)

Every review item is `process_only: true`, which `work.py next` and the fix lane never rank, so a review wave is a
CAMPAIGN wave (PB2120): `python scripts/spec/work.py next --cluster PB1754` lists the cluster in `blocked_by` order,
`python scripts/orchestrator/plan_wave.py --cluster PB1754 ...` plans it (rendered specs, `check_practices.py`), and
`orchestrate.ps1 -Cluster PB1754` runs it between fix-lane waves; the quota meter and the Workflow rolling wave as
usual (the `workstream` skill, rule 9). PB2118 is held for the owner's per-dispatch Mythos approval. Model tiers:
- **Mythos 5.1** authors R1 (PB2118) and runs a second adversarial round only if the Opus refuter cannot break the
  design; EACH dispatch needs the owner's explicit approval, asked as a bare question (MANDATORY-PRACTICES P1).
- **Opus** (`cobol-implementer`, `cobol-refuter`, `cobol-lander`): R0 tooling, the R1 refuter, R2 reviewers, extract
  and unify waves, landers; `model_rules.json` routes `^architecture` and `^build/(ci|legacy)` there.
- **Sonnet**: census re-runs, the prose and register sweeps (`cobol-clerk`), move-and-rename and analyzer waves
  driven by a Roslyn rewriter or a code fix, and Delete waves whose items the census measured dead. The oracle proves
  a mechanical wave; a Sonnet agent that meets a judgment returns `NEEDS-OPUS`.

## The oracle (the design's §4) — what "behavior-neutral" means here

1. **Emitted C#.** For every program the suites compile (corpus goldens, NIST CCVS, version-matrix construct
   samples, at every edition), the emitted C# is byte-identical to the R0 capture (PB2116), or each difference is
   listed and explained.
2. **Diagnostics.** Every negative fixture and diagnostic golden gives the same codes, severities, positions and
   messages.
3. **Grammar or lexer changes:** the token-stream differential (the PB1715 form: type, channel, offsets, line,
   column, text and final mode, over every lexer input the suites use) plus a parse-tree shape differential.
4. **Program output.** The whole-population gate (`build-local.ps1 -Mode implementer`/`-Mode lander`), the Linux
   gate (`scripts/linux-gate.sh`), CI through `push-main.sh`, and the GnuCOBOL differential at close.
5. **Performance** against the R0 baseline (PB2117, which is also kb/Work A6's instrument): the whole-population
   gate time and the compile-throughput benchmark (`brent-tools:performance-diagnosis`).

## Subsystems (R2's review fleet runs one agent per subsystem × dimension)

Preprocessor (`Cobol.Net.Frontend/Preprocessor`) · lexer and grammar (`**/*.g4` and lexer actions) · parser drivers
and the syntax tree · data binding (`DataBinder*`) · procedure binding (`Binding/Procedure`) · reference resolution
(`ReferenceResolver`) · validation and edition gating (`Validation`, `VersionConformancePass`) · lowering and the
bound tree (`BoundTree`) · Roslyn code generation (`CodeGen`) · OO (`Oo`, `OoBinder`, `OoEmitter`) · runtime values
and numerics · runtime I/O · runtime control and exceptions (`ExceptionState`) · editions and diagnostics
(`Cobol.Net.Editions`, `DiagnosticCatalog`) · CLI · tests, scripts and CI (layout and duplication only).

**Dimensions:** the four of `PROMPT.md` §4 (architecture · full code · performance · duplication and efficiency),
plus modern-C# conformance. Every agent also applies the project review overlay (`.claude/skills/review`).

## Known god-class candidates (R0 re-measures; this list is a starting point, not the census)

Measured at `e36c9c9da` (2026-10-06, `wc -l`; PB2115 replaces this with the Roslyn census): `DataBinder` (8,322 lines in
`DataBinder.cs`, plus `.Reports` 5,122, `.Switches` 2,146, `.Odo` and `.Linkage` partials) · `DiagnosticCatalog`
(6,881, a data table written as code) · `ReferenceResolver` (3,074) · `IntrinsicBinder` (3,014) ·
`VersionConformancePass` (2,643) · `ExceptionState` (2,359) · `RuntimeApi` (2,249) · `BoundTree` (2,087) ·
`OoEmitter` (1,981) · `OoBinder` (1,841) · `IntrinsicArgumentRules` (1,841). The remaining GAP rows land in the first
four (R69 §2), which is why R1 runs before zero GAP.

## Rules the restructure must keep

- **Typed-native only.** No byte `ProgramState` substrate anywhere in `src/Cobol.Net.*`; bytes only at the file
  boundary or a mixed-USAGE REDEFINES codec.
- **The phase seams.** No C# text in a bound node; semantics live in binding, not in renderers. The
  `ICodeGenBackend` seam stays, so a CIL backend can still drop in.
- **Grammar.** No left-edge semantic predicate in a lexer mode (`LexerDfaCacheDriftTests`); post-1985 features stay
  in `Core/*.g4` behind `{isXXXX()}?`; the generated parser is a build output, never committed. The additive-only
  caution for the shared grammar lifts when PB2110 lands; PB2114 unifies what it forced.
- **Registries.** Explicit registration over reflection discovery. Static state is a coupling hazard: PB1708
  measured how a static registry's initialization couples tests. `DiagnosticCatalog` restructuring must keep the
  catalog → `docs/DIAGNOSTICS.md` generation and every code's identity.
- **Citations travel with the code.** A moved rule keeps its `§`/GR comment, and `audit_code_citations.py --check`
  stays at zero.
- **No wrappers, shims, forwarders or aliases** (CLAUDE.md rule 4). A rename or a delete changes every caller in the
  same change. That includes `docs/`, `kb/Work` code sites, briefs and drift-test path literals, found with
  `scripts/spec/where.py` and a grep for the old name.
- **Deletions are measured, never deduced** (R69 §3, PB2119): the note for a Delete wave records how the item was
  measured dead; a deletion that changes behavior is a `kind: defect` note for the fix lane.
- **The existing drift tests are the inherited architecture tests.** Query them before touching a file
  (`python scripts/spec/drift_rules.py <files>`). A new boundary gets its own drift test, seen failing once.

## Findings and waves

- **R2 findings** are `kb/Work` notes of `kind: analysis`, `area: architecture`, `cluster: ["PB1754"]`, each with
  its code site, the rule it breaks, a scenario, the target and the wave kind (extract · unify · move and rename ·
  data-ize · delete).
- **R3 waves** run as rolling waves (`.claude/skills/workstream`), with groups computed by
  `fix_clusters.py`'s site index over those notes through `plan_wave.py --cluster <lead>` (PB2120; a note's
  `cluster:` names its campaign, `blocked_by:` orders it).
- **A defect found is a separate `kind: defect` note** for the fix lane, never fixed inside a refactor.

## .NET and C#

Today: `net10.0`, C# 14 (`LangVersion` 14), and `global.json` pins SDK 10.0.100 (`latestMinor`); the .NET 11 SDK
is installed as a preview. R4 targets the latest STABLE .NET at the time it runs, per
`brent-tools:dotnet-engineering` → `references/modernization.md`. `Cobol.Net.Compiler.SourceGen` keeps
`netstandard2.0`, because Roslyn hosts generators there.

**The .NET 11 alternative** (the design's §5.5, owner 2026-09-29):
- **C# 15 unions** for the closed hierarchies (the bound tree and the dispatchers), so the compiler proves
  exhaustiveness where a `default: throw` does today. R1 (PB2118) decides which hierarchies and in what order.
- **The JIT gains**, adopted only if measured against the R0 baseline.
- **Set aside:** runtime async (2 files use async), NativeAOT for the CLI (in-process Roslyn plus dynamic loading),
  and SIMD for packed/zoned decimal (file-boundary only, unless profiling says otherwise).
