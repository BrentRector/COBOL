## Entry 467 — COBOL.NET G0 (step 2/5): rename the runtime project → Cobol.Net.Runtime (+ subsystem re-fold)

G0 step 2 of 5 (`docs/COBOLNET_DESIGN.md` §17 §1.5). Renamed the greenfield runtime project
`src/CobolNet.Runtime` → `src/Cobol.Net.Runtime` (`git mv`, history preserved); `<AssemblyName>` →
`Cobol.Net.Runtime`, dropped the redundant `<TargetFramework>` (Directory.Build.props owns net10.0). Re-folded
to mirror the data-model subsystems: `CobolString.cs` → `Text/`, `StopRun.cs` → `Control/` (`Numeric/` already
held CobolNum/NumProfile/CobolRounding). **`<RootNamespace>` stays `CobolNet.Runtime`** so the namespace is
unchanged — generated programs' `using CobolNet.Runtime;` and the emitter's emitted import are untouched; only the
*assembly file* is renamed (the cosmetic namespace rename is the G8 big-bang).

Tracked the assembly-name change at its three consumers: `RoslynBackend` (the `Cobol.Net.Runtime.dll` path
constant it metadata-references + deploys beside each compiled program, ×2), `CobolNet.csproj` (the
ProjectReference path), and `CobolSharp.sln` (the project entry — same GUID kept). A repo-wide grep confirmed
nothing else (no script/CI/test) references the runtime by name.

Verified: `dotnet build` clean (0/0); the renamed `Cobol.Net.Runtime.dll` is produced beside the compiler AND
correctly deployed beside a freshly-compiled program, which runs end-to-end exercising both runtime substrates
(`CobolString` width/justify + `CobolNum` DISPLAY imaging) — proving runtime resolution survives the rename. Legacy
oracle guard re-run (the new-compiler subgraph the rename touches is outside the guard, but the rule is the rule):
ALL GREEN. NEXT: G0 step 3 (split + rename the compiler/CLI: `CobolNet` → `Cobol.Net.Compiler` library +
`Cobol.Net.Cli` exe producing `cobol.exe`, extracting `CompilerDriver`).

