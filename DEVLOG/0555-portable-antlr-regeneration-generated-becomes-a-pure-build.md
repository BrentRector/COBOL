## Entry 555 — 2026-06-10 19:25 PDT — Portable ANTLR regeneration; Generated/ becomes a pure build output (owner directive)

The owner rejected the DEVLOG-554 band-aid: "fix the paths such that MSBuild regenerates on either Linux or
Windows — that is the proper fix, not checking in generated files." Root cause of the platform split, found and
killed: **ANTLR mirrors a grammar's relative directory under `-o`, keyed on the PLATFORM path separator** —
`Core/CobolLexer.g4` is a bare name on Windows (flat output) but a nested path on Linux (output lands under
`<out>/Core/`), so the lexer-tokens hand-off to the parser's `-lib` silently missed ONLY on Linux. The portable
pipeline (`Invoke-Antlr4CSharp.ps1` rewritten): each grammar generates FROM ITS OWN DIRECTORY with a BARE
filename (flat everywhere), and the parser's `-lib` inputs — the imported Core/*.g4 plus CobolLexer.tokens —
are STAGED into `Generated_temp/lib/` (no source-tree writes). `GenerateIfNewer.ps1` now PROPAGATES the
generator's exit code (the other half of the 554 break: a failed regen exited 0 and the build continued on the
stale parser); the csproj target paths are forward-slashed and a java-missing condition fails loud with an
actionable message.

**`src/Cobol.Net.Frontend/Generated/` left the repository** (git rm --cached + .gitignore): it is a build
output — a fresh checkout always regenerates, making `java` (jar vendored in ANTLR4/) and `pwsh` build
prerequisites on every platform. WSL got both user-local (`~/.local/bin`, no sudo — pwsh 7.4.6 + Temurin
JRE 21 via /e/tmp/wsl-tools.sh). Verified THREE ways from scratch (Generated/ deleted before each): Linux/WSL
build regenerates + 648/648 conformance no-build; Windows build regenerates + 648/648; legacy guard ALL GREEN
(364 NIST / 1204 unit / 536 integration). Memory `feedback_commit_generated_parser` rewritten to the new
doctrine.

