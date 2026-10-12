## Entry 468 — Fix the long-standing CI failure: never compile the ANTLR Generated_temp staging folder

The owner reported persistent GitHub CI failures. Root-caused from the run logs: **pre-existing**, failing on
every push for many commits (incl. docs-only ones), independent of today's G0 work — my front-end extraction (467→)
merely relocated the latent bug from `CobolSharp.Compiler` to `Cobol.Net.Frontend`.

Mechanism: `Invoke-Antlr4CSharp.ps1` generates the parser into a `Generated_temp/` staging folder (the lexer lands
in `Generated_temp/Core/`), copies the final files to the committed `Generated/`, then deletes the staging folder —
but ONLY on the full-success path (the copy loop is also non-recursive, so the lexer's `Core/` output is never
copied; `Generated/CobolLexer.cs` is authoritative-committed). On a fresh CI checkout, equal/zeroed mtimes make the
grammar look newer than `Generated/`, so the `EnsureGeneratedFiles` MSBuild target regenerates; on the Linux runner
that regen fails partway (and `GenerateIfNewer.ps1` swallows the function's non-zero return), leaving the staging
folder behind. The first build step still succeeds (it uses the committed `Generated/`), but the NEXT build step's
default `**/*.cs` glob then compiles BOTH `Generated/CobolLexer.cs` and the leftover `Generated_temp/Core/
CobolLexer.cs` → ~hundreds of `CS0102`/`CS0579` duplicate-definition errors. Local builds were green only because
regen is skipped when `Generated/` is already up to date, so the staging folder never appears.

Fix (surgical + robust; the committed `Generated/` is authoritative): exclude the staging folder from compilation
in `Cobol.Net.Frontend.csproj` — `<Compile Remove="Generated_temp\**\*.cs" />` (+ a `None Remove`) — so a leftover
staging folder is harmless regardless of regen outcome; and add `Generated_temp/` to `.gitignore` so a local regen
can never accidentally commit it. Did NOT touch the generation control flow (avoids risking parser-gen behavior).

Verified by REPRODUCING the failure locally: dropped duplicate `CobolLexer.cs`/`CobolParserCore.cs` into a
simulated `Generated_temp/` and built — **0 errors** with the exclusion (would have been the CS0102 storm without
it); `git check-ignore` confirms the staging tree is ignored. Pushed to confirm CI goes green.

