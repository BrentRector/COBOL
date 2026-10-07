---
title: Build System
area: compiler
status: draft
last_updated: 2026-07-23
related_files:
  - Directory.Build.props
  - Directory.Packages.props
  - global.json
  - nuget.config
  - Cobol.Net.sln
  - src/Cobol.Net.Frontend/GenerateIfNewer.ps1
  - src/Cobol.Net.Frontend/Invoke-Antlr4CSharp.ps1
  - scripts/guard.sh
  - scripts/guard-fast.sh
tags:
  - cobolsharp
  - compiler
---

# Build System

## Platform & language
.NET 10 / C# 14. `Directory.Build.props` sets `TargetFramework=net10.0`, `LangVersion=14`, `Nullable=enable`,
`ImplicitUsings=enable`, and `TreatWarningsAsErrors=true` (so warnings on machine-generated C# would fail the build;
generated ANTLR files carry `NoWarn=CS3021`). `global.json` pins the SDK to `10.0.100` with `rollForward: latestMinor`.

## Central package management
`Directory.Packages.props` (`ManagePackageVersionsCentrally=true`) is the single version source; project `.csproj`s
reference by name only. Pinned versions include: `Antlr4.Runtime.Standard 4.13.1`, `Mono.Cecil 0.11.6` (future CIL
backend), `Microsoft.CodeAnalysis.CSharp 4.14.0` (Roslyn), `System.CommandLine 2.0.9` (CLI), plus xunit/Test.Sdk.

## ANTLR regen at build
`Generated/` is a **gitignored build output** — a fresh checkout always regenerates, so **`java` + `pwsh` are build
prerequisites** on every platform. The csproj `EnsureGeneratedFiles` target (`BeforeTargets=CoreCompile`) runs
`GenerateIfNewer.ps1 -PackageName $(AntlrNamespace)`; the namespace `CobolNet.Frontend.Generated` is single-sourced
from the csproj `<AntlrNamespace>` property.
- **`GenerateIfNewer.ps1`** — staleness check: regenerates when any `Grammar/**/*.g4` is newer than the generated
  lexer/parser (or they're absent). A failed generation **fails the build** (no stale-parser fallback).
- **`Invoke-Antlr4CSharp.ps1`** — runs `antlr-4.13.2-complete.jar` with `-Dlanguage=CSharp -no-listener -visitor`.
  Portability fix: each grammar is generated **from its own directory with a bare filename** (ANTLR mirrors relative
  dirs using the *platform* separator). Lexer generates first; the parser's `-lib` inputs (imported `Core/*.g4` +
  `CobolLexer.tokens`) stage under `obj/antlr-lib/`.

## Solution project list (`Cobol.Net.sln`)
Greenfield `Cobol.Net.*`: **Frontend**, **Editions** (shared lowest leaf), **Compiler**, **Compiler.SourceGen**
(source generator), **Cli** (exe `cobol`), **Runtime**, and tests **Unit / Conformance / Characterization**. The legacy
engine is deleted from `main` (`docs/rearchitecture/LEGACY-ARCHIVE.md`). See
[[kb/Architecture/Module Overview]].

## Guard & generator scripts (`scripts/`)
- **`guard.sh`** — the full serial gate: build CLI → unit → integration → the 364-program NIST regression, in a
  per-run snapshot dir (snapshots the built compiler + NIST inputs so a concurrent rebuild can't swap DLLs mid-run —
  the "phantom regression" class).
- **`guard-fast.sh`** — the parallel fast gate, proven equivalent to `guard.sh` via `guard-verify.sh`. Self-contained
  suites run fully parallel; file-I/O suites run serially within-suite. Mis-grouping can only produce a RED, never a
  false GREEN.
- **`gen-*.ps1` generators** (each with a drift test): `gen-cobol-words.ps1` (→ `CobolWords.g4` + `CobolLexerWordSet.g.cs`),
  `gen-reserved-words.ps1` (→ `ReservedWords.Table.cs`), `gen-constructs.ps1`, `gen-vcr.ps1`, `gen-diagnostics-doc.ps1`.

## Quick build / test
```bash
dotnet build                    # build all projects (regenerates ANTLR if stale)
dotnet test                     # unit + conformance + characterization
bash scripts/guard.sh           # full regression gate incl. NIST
```

## Key concepts
- .NET 10 / C# 14; nullable + implicit usings; warnings-as-errors; SDK pinned in `global.json`.
- Central package management (`Directory.Packages.props`) = one version source.
- `Generated/` is a gitignored build output; ANTLR regenerated at build; failed regen fails the build.
- Portable regen: per-grammar, own-directory, bare-filename; package name = MSBuild `<AntlrNamespace>`.
- Two guard gates + `gen-*.ps1` generators, each drift-tested against its JSON source.

## See also
- [[kb/Compiler/Phases]] — the grammar that gets regenerated.
- [[kb/Architecture/Module Overview]] — the assemblies being built.
- [[kb/Modernization/Tasks]] — CI / guard discipline in the plan.

## Backlinks
- [[kb/Compiler/MOC]] · [[kb/Index]] — link here.
