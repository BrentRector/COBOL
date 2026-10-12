## Entry 466 — COBOL.NET G0 (step 1/5): extract the reusable front-end into its own assembly (Cobol.Net.Frontend)

Began executing the design's G0 (`docs/COBOLNET_DESIGN.md` §17) — the project reorganization that must precede
G2 so the bound-tree emitter lands into the final no-god-class structure and the new test projects (which G2's
validation needs) exist. Step 1 of 5: **lift the front-end out of the legacy byte-engine assembly.**

Reconnaissance first (verified against the live tree, not assumed): the front-end dirs (`Parsing/`,
`Preprocessor/`, `Diagnostics/`, `Common/`, `Generated/`, `Grammar/`, `ANTLR4/` + the two ANTLR `.ps1` scripts)
have **zero** `using` references into the engine layers (`Semantics`/`IR`/`CodeGen`/`FlowAnalysis`) and don't
reference `CobolSharp.Runtime` — so no dependency cycle. Every front-end top-level type is `public` and there is
**no cross-assembly `internal` access** from the engine into the front-end (the only `internal` token in those
dirs is a word inside a diagnostic message string) — so moving the files to a new assembly breaks nothing. The
ANTLR generation scripts are `$PSScriptRoot`-relative (survive the move); java 23 is present (a triggered regen is
safe); the generated namespace is hard-pinned to `CobolSharp.Compiler.Generated` in `Invoke-Antlr4CSharp.ps1` and
stays constant (§1.4 — the cosmetic namespace rename is the G8 big-bang, NOT now).

Executed: `git mv` the 7 front-end dirs + 2 scripts → `src/Cobol.Net.Frontend/` (history preserved); created
`Cobol.Net.Frontend.csproj` (`<AssemblyName>Cobol.Net.Frontend`, `<RootNamespace>CobolNet.Frontend`, the ANTLR
`EnsureGeneratedFiles`/`CleanGenerated` targets + the `Antlr4.Runtime.Standard` ref carried verbatim); stripped the
ANTLR targets/grammar items out of `CobolSharp.Compiler.csproj` and added it a `Cobol.Net.Frontend` ProjectReference
(the legacy engine now consumes the extracted front-end like any other client). **`src/CobolNet` is a pure
repoint** — its only `using`s are the front-end namespaces (`Generated`/`Diagnostics`/`Parsing`/`Preprocessor`),
zero engine types — so its `CobolSharp.Compiler` reference was swapped straight to `Cobol.Net.Frontend`: **the
greenfield compiler no longer references the legacy byte-engine assembly at all** (the stated G0-step-1 goal). The
CLI + both legacy test projects keep their `CobolSharp.Compiler` ref and get the front-end transitively. Added the
new project to the solution.

Verified: `dotnet build CobolSharp.sln` clean (0 warn / 0 err); the new `cobol` compiler still compiles+runs a
program end-to-end through the relocated front-end (`HELLO FROM COBOLNET`) — proving the parser/preprocessor are
wired correctly across the new assembly boundary; and the legacy oracle guard stays **ALL GREEN: NIST 364 MATCH /
0 regressions, 1204 unit, 535 integration**. (Namespaces are intentionally unchanged — `CobolSharp.Compiler.*`
through G0–G7; the rename is G8.) NEXT: G0 step 2 (rename the runtime project → `Cobol.Net.Runtime`), then 3
(split + rename the compiler/CLI), 4 (add the two new test projects), 5 (scripts/CI/sln), then G2.

