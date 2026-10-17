# WiseOwl COBOL

WiseOwl COBOL compiles standard COBOL to .NET. It implements ISO/IEC 1989:2023 and supports the 1985, 2002 and 2014
editions, each with its own syntax and semantics. The compiler translates a program to typed-native C#, in which a
COBOL record is a .NET `record struct` and an elementary item is a native field, and Roslyn builds that C# into an
ordinary .NET assembly.

The standard is the specification. Every normative rule of ISO/IEC 1989:2023 is a row in a traceability inventory,
and a row closes only when a test derived from the standard proves the compiler conforms. Where the standard leaves a
choice to the implementor, [docs/CONFORMANCE.md](docs/CONFORMANCE.md) records the choice and its reasons.

## What you need

| Tool | Version | Why |
|---|---|---|
| .NET SDK | 10.0.100 or a later 10.0 release (`global.json`) | builds the solution; runs compiled programs |
| PowerShell | 7 or later (`pwsh`) | the build regenerates the ANTLR parser through a PowerShell step; the gate scripts |
| Java | 21 or later | runs the checked-in ANTLR 4.13.2 jar when a grammar changes |
| Python | 3.14 or later | the gate driver, the specification tools, the work register |
| Git | a current release | |

Windows and Linux both work as hosts. On Windows, WSL with Ubuntu lets you run the Linux legs of the test suite
before you push, and GnuCOBOL 3.2 inside WSL enables the differential comparison. Both are optional.

## Get the code and build it

```bash
git clone https://github.com/WiseOwlSoftware/COBOL.git
cd COBOL
git config merge.renormalize true
dotnet build Cobol.Net.sln
```

Every tracked text file is LF on every platform, and `.gitattributes` enforces it; the `merge.renormalize` setting
keeps a merge from reporting conflicts that are only line endings. The first build regenerates the parser from the
grammars under `src/Cobol.Net.Frontend`, so it takes a few minutes; later builds regenerate it only when a grammar
changes.

The build leaves the compiler at `src/Cobol.Net.Cli/bin/Debug/net10.0/cobol` (`cobol.exe` on Windows). Build the
whole solution, not one project: the tests run against the compiler the solution build produced.

## Compile and run a program

Put this in `hello.cob` (fixed reference format: code in columns 8 to 72):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. HELLO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GREETING PIC X(20) VALUE "Hello from COBOL".
       PROCEDURE DIVISION.
           DISPLAY GREETING
           STOP RUN.
```

Then compile it and run it:

```bash
cobol hello.cob -o hello.dll      # or: dotnet run --project src/Cobol.Net.Cli -- hello.cob -o hello.dll
dotnet hello.dll
```

The compiler writes four files beside the output: `hello.dll`, `hello.runtimeconfig.json`, the runtime library
`Cobol.Net.Runtime.dll`, and `hello.g.cs`, the C# it generated. Read the `.g.cs` file to see how your program maps
to .NET.

The options you will use most:

| Option | Effect |
|---|---|
| `--run` | runs the program after a successful compile |
| `--std 85`, `2002`, `2014` or `2023` | selects the edition (default 2023); a construct newer than the edition is rejected, and so is one the edition removed |
| `--permissive` | accepts a construct the targeted edition removed, with a warning in place of the rejection, for migration work; a construct newer than the edition is still rejected |
| `--source-format fixed`, `free` or `auto` | the reference format the source starts in (default fixed) |
| `--copy DIR` | a COPY library directory; repeat it for several |
| `--sign-encoding ibm` or `ascii` | the overpunched-sign convention for DISPLAY numerics (default ibm) |
| `--flag-extensions` | warns at every nonstandard extension the compiler accepts |
| `--nist NAME` | the NIST CCVS placeholder preprocessing |

`cobol --help` lists them all. `cobol check-batch manifest.json` parses, edition-checks and binds many sources in one
process without emitting code.

## Run the tests

The gate builds the solution and runs the tests in an order that puts the ones your change is most likely to break
first:

```bash
pwsh scripts/build-local.ps1      # or: bash scripts/build-local.sh
```

Read the last line it prints, `=== BUILD-LOCAL GATE: … ===`. `GREEN` is the only pass. Locally the gate runs its
first leg: your change's own tests, the previous run's failures, the host-only witnesses and the cheap tree-wide
sweeps. The whole population runs in CI, on Windows and Linux, for every pull request in the merge queue.

To run one test project yourself, build the solution first, then test with `--no-build`:

```bash
dotnet build Cobol.Net.sln
dotnet test tests/Cobol.Net.Tests.Unit --no-build
dotnet test tests/Cobol.Net.Tests.Conformance --no-build --filter "FullyQualifiedName~NistDifferentialTests"
```

On Windows, `bash scripts/linux-gate.sh` runs CI's Linux legs under WSL before you push. It clones the committed
HEAD into the Linux filesystem and builds and tests it there, the way CI's Ubuntu jobs do.

The test populations:

- `tests/Cobol.Net.Tests.Conformance` over `conformance-suite/`: the per-edition conformance suites, programs
  derived from the standard with their expected output in a folder per ISO clause, and the programs WiseOwl must
  refuse (`conformance-suite/README.md`); the NIST CCVS suite under `tests/nist/`; and the version matrix, which compiles one corpus at all four editions.
- `tests/Cobol.Net.Tests.Unit`: the runtime and compiler units, and the drift tests that keep two copies of one rule
  from diverging.
- `tests/Cobol.Net.Tests.Characterization`: snapshots of diagnostics and emitted C# over a fixed corpus.
- `tests/differential/`: the committed goldens of the Conformance project's `*DifferentialTests` classes, compared on
  every gate and CI run (recorded once, from an oracle since retired).
- `tests/external/`: the GnuCOBOL differential, run once per batch against the pinned GnuCOBOL 3.2 test corpus.

Two more projects in the solution measure rather than test. `tests/Cobol.Net.ArchOracle` runs the architecture
review's behavior-neutrality oracle outside xunit: `scripts/arch/capture_oracle.py` captures the C# and the diagnostics
of every compile the file-driven suites perform, and `scripts/arch/compare_oracle.py` compares a tree's capture with
one it makes on demand of the tree's own base commit, so a restructuring proves it changed no behavior and nothing is
committed. `tests/Cobol.Net.Benchmarks` is the BenchmarkDotNet harness behind `scripts/arch/perf_baseline.py`, the
recorded performance baseline. No test asserts a time.

## Find your way around

```
src/
  Cobol.Net.Cli/                 the cobol command
  Cobol.Net.Frontend/            reference format, COPY and REPLACE, directives, the ANTLR grammars and parser
  Cobol.Net.Compiler/            binder, bound tree, C# emitter, Roslyn backend
  Cobol.Net.Compiler.SourceGen/  the source generator for the bound-tree visitor
  Cobol.Net.Editions/            the construct registry and per-edition gating
  Cobol.Net.Runtime/             the library every compiled program runs on
tests/                           the populations above
specs/ISO_COBOL.md               the standard, transcribed to Markdown and searchable
docs/COBOLNET_DESIGN.md          the design, with one deep-dive document per subsystem beside it
docs/CONFORMANCE.md              every implementor-defined choice, with its reasons
kb/Work/                         the work register: one note per defect, analysis or decision
DEVLOG/                          the history, one file per landing
scripts/                         the gate, the specification tools, the generators
```

Start with [docs/DOC_INDEX.md](docs/DOC_INDEX.md), the map of every document.
[docs/rearchitecture/DESIGN-test-build-ci.md](docs/rearchitecture/DESIGN-test-build-ci.md) describes the gate and CI.

The pipeline:

```
COBOL source
  → Preprocessor      reference format, COPY / REPLACE, compiler directives
  → Lexer / Parser    ANTLR4, one grammar per subsystem
  → Binder            names, types, storage, every syntax and general rule
  → Bound tree        the backend-neutral program
  → C# emission       idiomatic, typed-native C#
  → Roslyn            a .NET assembly, linked to the typed-native runtime
```

## How work is done here

- **The standard decides.** Settle every question of syntax, semantics or output from `specs/ISO_COBOL.md`, cite
  the clause and rule, and check the citation: `python scripts/spec/cite.py --check 14.9.25.3 "<quoted text>"`.
  Other compilers and test suites are evidence, never authority.
- **Work lives in `kb/Work/`.** `python scripts/spec/work.py next` ranks what to fix next by what a defect does to a
  user's program. A new defect becomes a note there before anything else.
- **Every change ships with its tests and its documents.** A behavior change adds a test to `conformance-suite/cases/`
  with expected output derived from the standard, and a refused program for each earlier edition that must reject
  it. The documents that describe the behavior change in the same pull request.
- **Every landing carries a DEVLOG entry.** Write `DEVLOG/pending-<slug>.md` with the change; the landing numbers
  and stamps it.
- **One pull request per branch, through the merge queue.** `main` is protected, and the `ci-gate` check must pass.
  Your first pull request asks you to sign the [Contributor License Agreement](CLA.md) once.

[CONTRIBUTING.md](CONTRIBUTING.md) has the complete rules, including what a pull request needs.

## Status

Every normative rule of ISO/IEC 1989:2023 is a row of the traceability inventory,
`tests/version-matrix/traceability-inventory.json`, and a row closes only when a test derived from the standard proves
the compiler conforms. The open rows are the project's one progress number, and the checkout computes it:
`pwsh scripts/session-probe.ps1` prints the rows, the open rows and the corpus sizes for the commit you are on, and
the gate's verdict line prints the tests it ran.

What the compiler declines is documented as §4.2.6 and §4.2.7 require, in [docs/CONFORMANCE.md](docs/CONFORMANCE.md):
its §2 dispositions every processor-dependent element of Annex A.3, its §5 every optional module of Annex A.4, and its
§4 the facilities declined whole. Every element those two tables do not claim in full is named below by its Annex
item, and a unit test (`ReadmeDeclinedSummaryDriftTests`) fails when this list and the tables disagree:

- The Message Control System (A.3 item 4) and commit and rollback (A.3 items 6–7, A.4.3) compile with the warning
  §4.2.6 requires for a processor-dependent element (COBOLNET1578, COBOLNET1579), and the program runs with the
  facility inert. The MESSAGE-TAG usage (COBOLNET1943) and the APPLY COMMIT clause (COBOLNET1709) are refused.
- The VALIDATE statement (A.4.14) compiles with a warning by documented choice under §4.2.7 (COBOLNET1580); the
  facility's data-division clauses (COBOLNET1708) and its EC-VALIDATE exception names (COBOLNET1710) are refused.
- Screen handling (A.4.2; COBOLNET1560, COBOLNET1707), FORMAT and SELECT WHEN file handling (A.4.8, COBOLNET1705) and
  WRITE FILE and REWRITE FILE (A.4.13, COBOLNET1706) are refused with errors, which is how Annex A.4.1 reads for an
  optional module whose support is not claimed.
- ARITHMETIC IS STANDARD-BINARY (A.3 item 2) is refused (COBOLNET0806).
- FLOAT-BINARY-128 (A.3 item 17, which leaves the FLOAT-BINARY clause of A.3 item 12 partial) and the standard decimal
  floating-point usages (A.3 items 19 and 20–21) have no conforming .NET type and are refused (COBOLNET1564); the
  FLOAT-DECIMAL clause (A.3 item 13) compiles with a warning (COBOLNET2424).
- CONTINUE AFTER (A.3 item 8) suspends execution for whole seconds: a fractional interval truncates.
- The RECORD DELIMITER clause (A.3 item 26) compiles with a warning (COBOLNET1778), and the SOURCE phrase of a record
  key (A.3 item 40) is refused (COBOLNET1954).
- No reel or unit device backs the CLOSE and OPEN phrases that name one (A.3 items 28–30 and 33–34): they are accepted,
  act as on a file that is not on a unit, and report I-O status '07'.
- CODE-SET (A.3 item 27) converts the identity code sets and EBCDIC; an alphabet no device encodes (a literal-phrase
  alphabet, or UTF-8 or UCS-4 as the medium's encoding) is refused (COBOLNET1672).
- A file's COLLATING SEQUENCE clause (A.3 items 41–42) orders an alphanumeric record key by any alphabet, LOCALE
  included; a national alphabet defined by literals or by LOCALE, on a national record key, is refused (COBOLNET1584).
- Multiple class inheritance (A.4.10, COBOLNET0849) and method overloading (COBOLNET0822, and so the parametric
  polymorphism of A.3 item 45) are refused; multiple interface inheritance is supported.
- The report writer (A.4.11) and dynamic-length items (A.4.5) are partial, and §5 itemizes what each supports.

A NuGet distribution of the compiler as a .NET tool (`WiseOwl.COBOL`, the `cobol` command) and a documentation site
are planned; see [docs/rearchitecture/DESIGN-USER-DOCUMENTATION.md](docs/rearchitecture/DESIGN-USER-DOCUMENTATION.md).

## What it covers

- **All four editions**, COBOL-85, 2002, 2014 and 2023, each with its own syntax and semantics, and a diagnostic
  wherever an edition lacks a construct.
- **The language**, with the standing of each rule recorded in [docs/CONFORMANCE.md](docs/CONFORMANCE.md): the
  Identification, Environment, Data and Procedure divisions, the compiler directing facility and conditional
  compilation, COPY and REPLACE, national and boolean data, dynamic-capacity tables, typed and strongly-typed items,
  exception handling (declaratives and the `EC-` exception conditions), sequential, relative and indexed files, SORT
  and MERGE, the report writer, inter-program communication (CALL, nested and recursive programs, program and
  function prototypes), user-defined functions, object orientation (classes, interfaces, methods, properties,
  factories), locales and culturally sensitive collation, and every intrinsic function of Clause 15.
- **Arithmetic:** native, and standard-decimal with decimal128 intermediates, as the OPTIONS paragraph selects.

## Community

- [Contributing](CONTRIBUTING.md) · [Code of Conduct](CODE_OF_CONDUCT.md) · [Security policy](SECURITY.md)
- Report wrong output, compiler crashes and conformance findings with the issue forms.

## License

Business Source License 1.1 — Copyright (c) 2026 Brent Rector. Non-commercial use is permitted; each version
converts to the Apache License 2.0 four years after release; commercial licenses are available. See
[LICENSE](LICENSE).
