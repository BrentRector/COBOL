# The legacy byte engine — archive pointer

The CobolSharp byte engine is not on `main`. Owner decision R69 (`kb/Work/R69.md`) deleted it in PHASE 15 Cut 2
(`kb/Work/PB2110.md`). This page says where it is preserved and how to run it. History lives in `DEVLOG.md`.

## Where it is

The annotated git tag **`legacy-byte-engine-final`** marks the last commit of `main` that contains the engine. At that
tag the tree holds:

| Path | What it is |
|---|---|
| `src/CobolSharp.Compiler/` | the legacy compiler: front end bindings, byte-engine semantics, the Cecil IL emitter |
| `src/CobolSharp.Runtime/` | the byte `ProgramState` runtime its programs call |
| `src/CobolSharp.CLI/` | the `cobolsharp` command-line driver |
| `tests/CobolSharp.Tests.Unit/` | its unit tests |
| `tests/CobolSharp.Tests.Integration/` | its integration tests |

All five are members of `CobolSharp.sln` at the tag. The shared front end (`src/Cobol.Net.Frontend`) and the editions
leaf (`src/Cobol.Net.Editions`) at the tag are the ones the engine builds against.

## How to run it (WSL or Linux)

```bash
git clone https://github.com/BrentRector/COBOL.git cobol-legacy
cd cobol-legacy
git checkout legacy-byte-engine-final
dotnet build CobolSharp.sln
# compile: writes PROG.dll, its runtimeconfig and a copy of the runtime library beside it
dotnet src/CobolSharp.CLI/bin/Debug/net10.0/cobolsharp.dll -o out/PROG.dll prog.cbl
dotnet out/PROG.dll
```

`cobolsharp --help` lists the options at the tag: `--standard default|cobol85|cobol2002|cobol2014|cobol2023` and
`--nist [name]` for the NIST CCVS placeholders. The guard scripts at the tag drive `cobol`, not the engine (PB750,
PB2109), so a spot check against the engine runs its CLI directly as above.

## What it is for

Nothing in the build, the tests or CI depends on it. It is evidence for a question about what the engine did, never
an authority on what COBOL means: the ISO standard decides (CLAUDE.md rule 1).
