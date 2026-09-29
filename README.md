# WiseOwl COBOL

A COBOL compiler for .NET, built from the ISO/IEC 1989:2023 standard, with correct support for every earlier edition
(1985, 2002, 2014). It compiles standard COBOL to idiomatic, typed-native C# — a COBOL record is a .NET record
struct, an elementary item is a native field — which Roslyn builds into an ordinary .NET assembly.

The standard is the specification: every rule of ISO/IEC 1989:2023 is a row in a traceability inventory, and each
row closes only when a spec-derived test proves the compiler conforms. Behavior the standard leaves to the
implementor is documented, with its rationale, in [docs/CONFORMANCE.md](docs/CONFORMANCE.md).

## Quick start

```bash
git clone https://github.com/BrentRector/COBOL.git
cd COBOL
dotnet build CobolSharp.sln

# compile a COBOL program (the source is a positional argument)
dotnet run --project src/Cobol.Net.Cli -- hello.cob -o hello.dll
dotnet hello.dll
```

Select an edition with `--std 85|2002|2014|2023` (default 2023). A construct the selected edition does not have is
rejected; `--permissive` turns such rejections into warnings for migration work.

A NuGet distribution (`WiseOwl.COBOL`, a .NET global tool providing the `cobol` command) and a documentation site are
planned; see [docs/rearchitecture/DESIGN-USER-DOCUMENTATION.md](docs/rearchitecture/DESIGN-USER-DOCUMENTATION.md).

## Status

As of 2026-09-26 (measured, not estimated):

- **Traceability:** 4,347 normative rules; every one adjudicated against the standard; 3,286 closed (75.6 %) and
  1,061 open. Clause 15 (intrinsic functions) is fully closed.
- **Tests on every change:** 9,046 conformance tests, 29,491 unit tests, 33 characterization tests — plus the NIST
  CCVS suite, a per-edition version matrix, and a GnuCOBOL differential run per batch. CI runs on Windows and Linux.
- **Declined facilities** (documented non-support, each with a compile-time warning, as §4.2.6 permits): the
  Message Control System, commit and rollback, the VALIDATE facility, and screen handling. Optional modules and
  processor-dependent elements are dispositioned in [docs/CONFORMANCE.md](docs/CONFORMANCE.md).

## What it covers

- **All four editions** — COBOL-85, 2002, 2014 and 2023 — each with its own syntax and semantics, and gating
  diagnostics where an edition lacks a construct.
- **The language** (current standing per rule in [docs/CONFORMANCE.md](docs/CONFORMANCE.md) — conformance is
  measured, not claimed): the Identification, Environment, Data and Procedure divisions, including the compiler
  directing facility and conditional compilation, COPY/REPLACE, national and boolean data, dynamic-capacity tables,
  typed and strongly-typed items, exception handling (declaratives and `EC-` exception conditions), sequential,
  relative and indexed files, SORT and MERGE, the report writer, inter-program communication (CALL, nested and
  recursive programs, function and program prototypes), user-defined functions, object orientation (classes,
  interfaces, methods, properties, factories), locales and culturally sensitive collation, and every intrinsic
  function of Clause 15.
- **Arithmetic:** native, and standard-decimal (decimal128 intermediates) as selected by the OPTIONS paragraph.

## Architecture

```
COBOL source
  → Preprocessor      reference format, COPY / REPLACE, compiler directives
  → Lexer / Parser    ANTLR4, one grammar per subsystem
  → Binder            names, types, storage, every syntax and general rule
  → Bound tree        the backend-neutral program
  → C# emission       idiomatic, typed-native C#
  → Roslyn            a .NET assembly, linked to the typed-native runtime
```

```
src/
  Cobol.Net.Cli/                 the `cobol` command
  Cobol.Net.Frontend/            preprocessor, grammar, parser, compile-time expressions
  Cobol.Net.Compiler/            binder, bound tree, C# emitter, Roslyn backend
  Cobol.Net.Compiler.SourceGen/  source generator for the bound-tree visitor
  Cobol.Net.Editions/            per-edition construct registry and gating
  Cobol.Net.Runtime/             the runtime library compiled programs use
  CobolSharp.*                   the legacy engine, kept only as a differential oracle until it is retired
tests/
  Cobol.Net.Tests.Conformance/   spec-derived programs with expected output, per edition, plus negatives
  Cobol.Net.Tests.Unit/          unit and drift tests
  Cobol.Net.Tests.Characterization/
  nist/                          the NIST CCVS programs and expected output
```

## Building and testing

Requires the .NET 10 SDK, PowerShell 7+, Python 3.14+ and Java 21+ (ANTLR parser generation).

```bash
dotnet build CobolSharp.sln
pwsh scripts/build-local.ps1 -Mode implementer   # build + every Conformance, Unit and Characterization test, likely reds first
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for the full workflow, the standard-citation rule and the Contributor License
Agreement.

## Community

- [Contributing](CONTRIBUTING.md) · [Code of Conduct](CODE_OF_CONDUCT.md) · [Security policy](SECURITY.md)
- Report wrong output, compiler crashes and conformance findings with the issue forms.

## License

Business Source License 1.1 — Copyright (c) 2026 Brent Rector. Non-commercial use is permitted; each version
converts to the Apache License 2.0 four years after release; commercial licenses are available. See
[LICENSE](LICENSE).
