---
title: Module Overview (Assembly Topology)
area: architecture
status: draft
last_updated: 2026-07-23
related_files:
  - docs/rearchitecture/DESIGN-module-topology.md
  - docs/COBOLNET_PROJECT_ORG_DESIGN.md
  - docs/rearchitecture/CRITIQUE-encapsulation.md
  - src/Cobol.Net.Compiler.SourceGen/BoundVisitorGenerator.cs
  - src/Cobol.Net.Cli/Program.cs
tags:
  - cobolsharp
  - architecture
---

# Module Overview (Assembly Topology)

The solution is a **five-assembly greenfield tree** (`src/Cobol.Net.*`) plus a **source-generator project** (the legacy
engine is deleted from `main`: `docs/rearchitecture/LEGACY-ARCHIVE.md`). The one structural addition over the original
four-project split is the leaf **`Cobol.Net.Editions`**, so both Frontend and Compiler can share one
construct/reserved-word catalogue without a cycle.

## Dependency graph (from the live `.csproj` files)
```
                    Cobol.Net.Editions   (LEAF — no refs)
                       ▲            ▲
                       │            │
   Cobol.Net.Frontend ─┘            └── Cobol.Net.Compiler ──► Cobol.Net.Runtime  (LEAF)
        (→ Editions,                          │  ▲
         Antlr4.Runtime)                      │  │ (analyzer-only, netstandard2.0,
                       ▲                      │  └─ ReferenceOutputAssembly=false)
                       └──────────────────────┤
                                              ├──► Cobol.Net.Frontend
   Cobol.Net.Compiler.SourceGen ──────────────┘    Cobol.Net.Compiler.SourceGen
   (source generator)                              (+ Microsoft.CodeAnalysis.CSharp)

   Cobol.Net.Cli (exe → cobol.exe) ──► Cobol.Net.Compiler ──► (System.CommandLine)
```

## Per-project responsibility
- **`Cobol.Net.Editions`** *(leaf)* — the construct/reserved-word catalogue, edition-severity policy, the immutable
  `EditionInfo` value, and the diagnostic-code registry. Pure data + policy; referenced by both Frontend and Compiler.
- **`Cobol.Net.Frontend`** (→ Editions, `Antlr4.Runtime.Standard`) — preprocessor, ANTLR lexer/parser, parse tree,
  frontend diagnostics. Subfolders: `Grammar/`, `Generated/`, `Parsing/`, `Preprocessor/`, `Diagnostics/`, `Common/`,
  `Pipeline/`, `Cst/`, `Expressions/`. See [[kb/Compiler/Phases]].
- **`Cobol.Net.Runtime`** *(leaf)* — the typed-native runtime kernel the *generated* programs bind against. Subfolders:
  `Values/` (CobolNum, NumProfile), `Verbs/`, `Control/`, `Exceptions/` (EC model), `IO/`, `Intrinsics/`. No compiler
  back-reference. See [[kb/Runtime/Execution Model]].
- **`Cobol.Net.Compiler`** (→ Editions, Frontend, Runtime; + SourceGen as analyzer) — binder + bind pipeline + CodeGen +
  Roslyn backend + `CompilerDriver`. Subsystems: `Binding/` (`Model/`, `Bound/`, `Passes/`, `Procedure/`,
  `Validation/`), `CodeGen/` (`Emit/`, `DataDivision/`, `Verbs/`, `Roslyn/`), `Oo/`, top-level `Validation/`.
- **`Cobol.Net.Compiler.SourceGen`** — a Roslyn source generator (`BoundVisitorGenerator.cs`) emitting the exhaustive
  `IBoundVisitor<T>` dispatch so a missing bound-node arm is a **compile error**. Wired analyzer-only
  (`OutputItemType=Analyzer`, `ReferenceOutputAssembly=false`; netstandard2.0). See [[kb/IR/Node Types]].
- **`Cobol.Net.Cli`** (→ Compiler; `AssemblyName=cobol`) — thin `System.CommandLine` shell (`Program.cs` +
  `CliOptions.cs`); parses `--std`/`--backend`, orchestrates files.
- **Legacy `CobolSharp.{Compiler,Runtime,CLI}`** — the byte-engine, kept **only** as a differential oracle (opt-in
  `COBOLSHARP_LEGACY_DIFFERENTIAL=1`), deleted at G8. See [[kb/Modernization/Tasks]].

## Layering / dependency-direction rules
Dependencies flow one way — Editions ← Frontend/Runtime ← Compiler ← Cli — with **no assembly cycle and no
Runtime→Compiler back-reference**. The **bind→emit boundary** is the hard rule: the binder produces the
backend-neutral bound tree and resolves *all* semantics; CodeGen is a pure renderer that must never mutate the
binder's model or re-discover semantics. Shared state travels in `BinderContext`/`EmitContext`, never a god class.
(The historic `StoreAsImage`/`IndexFields` write-backs are the defects the rearchitecture closes.) See
[[kb/Context/Doctrine & Anti-Patterns]].

## Key concepts
- 5 greenfield assemblies + 1 source-gen + 3 legacy (oracle-only).
- `Cobol.Net.Editions` = a new leaf so Frontend + Compiler share ONE catalogue (no cycle).
- Runtime is a dependency-free leaf; Compiler references Frontend/Editions/Runtime + SourceGen (analyzer).
- Folder = sub-namespace; one file per public type.
- Source-generated exhaustive bound-visitor: a missing dispatch arm is a compile error.
- Bind→Emit boundary: binder owns semantics, emitter renders only.

## See also
- [[kb/Architecture/High-Level Design]] · [[kb/Compiler/Build System]]
- [[kb/IR/Node Types]] · [[kb/Runtime/Execution Model]]
- [[kb/Diagrams/IR Graph Overview]]

## Backlinks
- [[kb/Architecture/MOC]] · [[kb/Index]] — link here.

> Doc-vs-tree note: the live tree places the data model under `Binding/Model/`; `DESIGN-module-topology.md` proposes
> promoting it to a top-level `Model/`. The assembly-level graph above is taken from the live `.csproj` files and is
> authoritative; some finer folder targets in the topology design are still in-progress rearchitecture waves.
