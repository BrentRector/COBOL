## Entry 471 — COBOL.NET G0 (step 3/5): split the compiler exe → Cobol.Net.Compiler library + Cobol.Net.Cli (cobol.exe)

G0 step 3 of 5 (`docs/COBOLNET_DESIGN.md` §17 §1.3/§1.5). The greenfield compiler had `Program.cs` (CLI) living
inside the exe project, so tests couldn't reference the compiler without referencing an exe. Split it the way the
proven legacy `CobolSharp.Compiler`/`CobolSharp.CLI` pair is split:

- `git mv src/CobolNet → src/Cobol.Net.Compiler` (LIBRARY now: dropped `OutputType=Exe`/`AssemblyName=cobol`;
  `AssemblyName=Cobol.Net.Compiler`, `RootNamespace` stays the single token `CobolNet`). Holds Binding/, CodeGen/,
  and the new `CompilerDriver`.
- **Extracted `CompilerDriver`** (the library entry point): the source→C#→Roslyn pipeline lifted out of
  `Program.Main`, returning a structured `Result` (`Outcome ∈ {Success, SourceNotFound, FrontendError,
  BackendError}` + the `.g.cs` path + error strings) — no console I/O, no process control, so it is unit-testable.
- **New `src/Cobol.Net.Cli`** exe (`<AssemblyName>cobol`, produces cobol.exe): a thin shell — `Program.cs`
  (Main + Run) over `CompilerDriver`, mapping the outcome to POSIX sysexits (66/65/70), plus the extracted
  `CliOptions` record.
- `Frontend.cs` `git mv`'d into the front-end project (`Cobol.Net.Frontend/Pipeline/Frontend.cs`) where it belongs
  — it orchestrates the front-end and uses only front-end types.
- Solution: renamed the `CobolNet` entry → `Cobol.Net.Compiler` (GUID kept) + added `Cobol.Net.Cli` (new GUID,
  full Debug/Release×AnyCPU/x64/x86 config map + src-folder nesting).

NOTE: the §17 §2 emitter decomposition is deliberately NOT done here — G2 rebuilds the emitter on a bound tree, so
the doomed parse-tree-walk `CSharpEmitter` is moved as-is and decomposed INTO the §2.2 structure during G2 (build
it decomposed rather than decompose-then-rewrite).

Verified: solution build clean (0/0); `cobol.dll` from the CLI project compiles+runs a program end-to-end
(`HELLO FROM COBOLNET`) with `Cobol.Net.Compiler.dll` + `Cobol.Net.Runtime.dll` deployed alongside; exit codes
correct (missing source → 66, parse error → 65); legacy oracle guard ALL GREEN (unchanged by this split). NEXT:
G0 step 4 (add the new test projects `Cobol.Net.Tests.Unit` + `Cobol.Net.Tests.Conformance`), then step 5
(scripts/CI/sln), then G2.

