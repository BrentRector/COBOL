---
name: architecture-review
description: Use when running (or preparing) the comprehensive, whole-codebase architecture review and restructuring of the WiseOwl COBOL compiler - grammar, preprocessor, lexer and parser drivers, binding, validation, lowering, code generation, runtime, editions and diagnostics - the PROMPT.md §4 comprehensive pass. Carries the project's plan (docs/rearchitecture/DESIGN-architecture-review.md), its behavior-neutrality oracles, subsystem map, known god classes and the COBOL-specific rules a restructure must keep. Only after the owner starts it (kb/Work PB1754).
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:architecture-audit` (Skill tool) before reading on. If the plugin is
> not loaded (a cloud session receives no project marketplace), Read
> `tools/claude-skills/skills/architecture-audit/SKILL.md` instead (`git submodule update --init tools/claude-skills`
> if the path is missing). Its phases, contract and anti-patterns are the procedure. THEN apply this overlay: it
> carries only what is specific to WiseOwl COBOL, and wins on conflict. Pinned: **brent-tools 1.15.1**
> (`tools/claude-skills`, kb/Work/PB1699).

# Architecture review (WiseOwl COBOL)

⛔ **Do not start without the owner.** kb/Work **PB1754** holds the start decision and three more (preview SDK
or not; whether project/assembly names may change, since the NuGet id `WiseOwl.COBOL` is public; whether `tests/`
layout is in scope). Ask them one at a time, as bare questions, when the review is about to begin.

**The plan is `docs/rearchitecture/DESIGN-architecture-review.md`.** Follow it (CLAUDE.md rule 2); correct it in the
same change when implementation proves it wrong (rule 5). Phases: R0 baseline and oracle · R1 target architecture ·
R2 review fleet · R3 restructuring waves · R4 modernization · R5 close.

## Preconditions specific to this project

- **P15 has deleted the legacy `CobolSharp.*` engine** (~46,000 lines). Never refactor it.
- **No subsystem redesign is in flight.** For example, PB1708's gate work must be closed.
- **The fix lane is paused, or partitioned** away from the subsystems in the current wave.

## The oracle (the design's §4) — what "behavior-neutral" means here

1. **Emitted C#.** For every program the suites compile (corpus goldens, NIST CCVS, version-matrix construct
   samples, at every edition), the emitted C# is byte-identical to the R0 capture, or each difference is listed and
   explained.
2. **Diagnostics.** Every negative fixture and diagnostic golden gives the same codes, severities, positions and
   messages.
3. **Grammar or lexer changes:** the token-stream differential (the PB1715 form: type, channel, offsets, line,
   column, text and final mode, over every lexer input the suites use) plus a parse-tree shape differential.
4. **Program output.** The whole-population gate (`build-local.ps1 -Mode implementer`/`-Mode lander`), the Linux
   gate (`scripts/linux-gate.sh`), CI through `push-main.sh`, and the GnuCOBOL differential at close.
5. **Performance** against the R0 baseline: the whole-population gate time and the compile-throughput benchmark
   (`brent-tools:performance-diagnosis`).

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

Measured at `edda57d7f`: `DataBinder` (7,448 lines in `DataBinder.cs` alone, plus the `.Reports` 3,165, `.Switches`
1,936, `.Odo` and `.Linkage` partials) · `DiagnosticCatalog` (5,612, a data table written as code) ·
`IntrinsicBinder` (2,826) · `ReferenceResolver` (2,660) · `VersionConformancePass` (2,535) · `ExceptionState`
(2,201) · `BoundTree` (1,985) · `RuntimeApi` (1,949).

## Rules the restructure must keep

- **Typed-native only.** No byte `ProgramState` substrate anywhere in `src/Cobol.Net.*`; bytes only at the file
  boundary or a mixed-USAGE REDEFINES codec.
- **The phase seams.** No C# text in a bound node; semantics live in binding, not in renderers. The
  `ICodeGenBackend` seam stays, so a CIL backend can still drop in.
- **Grammar.** No left-edge semantic predicate in a lexer mode (`LexerDfaCacheDriftTests`); post-1985 features stay
  in `Core/*.g4` behind `{isXXXX()}?`; the generated parser is a build output, never committed.
- **Registries.** Explicit registration over reflection discovery. Static state is a coupling hazard: PB1708
  measured how a static registry's initialization couples tests. `DiagnosticCatalog` restructuring must keep the
  catalog → `docs/DIAGNOSTICS.md` generation and every code's identity.
- **Citations travel with the code.** A moved rule keeps its `§`/GR comment, and `audit_code_citations.py --check`
  stays at zero.
- **No wrappers, shims, forwarders or aliases** (CLAUDE.md rule 4). A rename changes every caller in the same
  change. That includes `docs/`, `kb/Work` code sites, briefs and drift-test path literals, found with
  `scripts/spec/where.py` and a grep for the old name.
- **The existing drift tests are the inherited architecture tests.** Query them before touching a file
  (`python scripts/spec/drift_rules.py <files>`). A new boundary gets its own drift test, seen failing once.

## Findings and waves

- **R2 findings** are `kb/Work` notes of `kind: analysis`, `area: architecture`, `cluster: ["PB1754"]`, each with
  its code site, the rule it breaks, a scenario, the target and the wave kind (extract · unify · move and rename ·
  data-ize).
- **R3 waves** run as rolling waves (`.claude/skills/workstream`), with groups computed by
  `tools/claude-skills/skills/agent-fleet/references/fix_clusters.py` over those notes.
- **A defect found is a separate `kind: defect` note** for the fix lane, never fixed inside a refactor.

## .NET and C#

Today: `net10.0`, C# 14 (`LangVersion` 14), and `global.json` pins SDK 10.0.100 (`latestMinor`); the .NET 11 SDK
is installed as a preview. R4 targets the latest STABLE .NET at the time it runs, per
`brent-tools:dotnet-engineering` → `references/modernization.md`. `Cobol.Net.Compiler.SourceGen` keeps
`netstandard2.0`, because Roslyn hosts generators there.

**The .NET 11 alternative** (the design's §5.5, owner 2026-09-29):
- **C# 15 unions** for the closed hierarchies (the bound tree and the dispatchers), so the compiler proves
  exhaustiveness where a `default: throw` does today.
- **The JIT gains**, adopted only if measured against the R0 baseline.
- **Set aside:** runtime async (2 files use async), NativeAOT for the CLI (in-process Roslyn plus dynamic loading),
  and SIMD for packed/zoned decimal (file-boundary only, unless profiling says otherwise).
