## Entry 1781 — 2026-09-29 13:43 PDT — PB1754: the comprehensive architecture review planned (design doc, skills); pin claude-skills v1.15.0

**The request.** The owner asked for a skill that will, "at some point in the future", run "a full, 100%,
end-to-end, architectural review of the entire COBOL compiler codebase, grammar and C# lexer/parser/code generator".
It should use the latest review techniques, eliminate duplicate code, refactor god classes and the file-system
layout, names and classes along proven architectural designs, and bring all C# to the latest .NET and C#: "Create
the plan for all this and design/update appropriate skills in anticipation." This is the comprehensive pass that
`PROMPT.md` §4 already required once the design settles. Nothing is dispatched; the owner decides the start.

**Measured first (at `edda57d7f`).**
- About 164,000 lines of current C#: Compiler ~103,600, Runtime ~39,200, Front end ~12,600, Editions ~7,900.
- About 8,600 lines of ANTLR grammar.
- The legacy `CobolSharp.*` projects, ~46,000 lines, are excluded because P15 deletes them.
- God-class candidates: `DataBinder` (7,448 lines in one file, plus the `.Reports` 3,165 and `.Switches` 1,936
  partials and more), `DiagnosticCatalog` (5,612, a table written as code), `IntrinsicBinder` (2,826),
  `ReferenceResolver` (2,660), `VersionConformancePass` (2,535), `ExceptionState` (2,201), `BoundTree` (1,985) and
  `RuntimeApi` (1,949).
- The target today is net10.0 with C# 14 (LangVersion 14); the .NET 11 SDK is installed as a preview.

**The plan: `docs/rearchitecture/DESIGN-architecture-review.md`.**
- **Scope** and **preconditions:** a settled design, P15 done, the oracle built, and a quiet fix lane.
- **Phases:**
  - R0: the baseline census and the behavior-neutrality oracle;
  - R1: the target architecture, adversarially reviewed, with dependency edges enforced by architecture tests;
  - R2: a review fleet, 16 subsystems × the four PROMPT.md §4 dimensions plus modern C#, whose findings become
    kb/Work notes;
  - R3: behavior-neutral waves (extract, unify, move and rename with every caller, data-ize);
  - R4: analyzer-driven .NET/C# modernization;
  - R5: close.
- **The §4 neutrality contract every wave proves:** byte-identical emitted C# across the corpus, NIST and the
  version matrix; identical diagnostics; token and parse-tree differentials for grammar changes; all gates on both
  OSes; no performance regression.
- **A .NET 11 alternative, at the owner's request:**
  - C# 15 unions for the closed hierarchies (compiler-enforced exhaustiveness where `default: throw` stands today);
  - the JIT gains, adopted only if measured against the R0 baseline.
- **Set aside, with measurements:** runtime async (only 2 files use async), NativeAOT for the CLI (in-process
  Roslyn plus dynamic assembly loading) and SIMD for packed decimal (file boundary only).

**PB1754** (kind: decision, status: owner) holds the four owner decisions, to be asked when the review starts:
when; a preview SDK or stable only; whether project/assembly names may change (the NuGet id `WiseOwl.COBOL` is
public); whether `tests/` layout is in scope.

**Skills:**
- the public `architecture-audit`, the general campaign method, in claude-skills v1.15.0;
- the public `dotnet-engineering` → `references/modernization.md`: upgrade the pins alone, analyzers at
  `latest-all`, one feature per wave through `dotnet format` code fixes or the Roslyn rewriter, kept only if neutral
  and not slower, and the `netstandard2.0` exception for generators stated;
- the project overlay `.claude/skills/architecture-review`: the subsystem map, known god classes, the oracle, and
  the rules a restructure must keep.

`PROMPT.md` §4 and `DOC_INDEX.md` point to the plan. The pin moves 1.14.1 → 1.15.0 in the five project skills and
PB1699.

