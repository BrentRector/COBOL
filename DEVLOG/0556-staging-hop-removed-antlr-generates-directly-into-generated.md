## Entry 556 — 2026-06-10 19:50 PDT — Staging hop removed: ANTLR generates DIRECTLY into Generated/ (owner question answered)

The owner asked why generation staged through `Generated_temp/` and copied. Both historical justifications are
dead under the build-output doctrine: (1) the "protected" `CobolParserCoreBase.cs` is referenced via the
`superClass` option — ANTLR never EMITS a file by that name, so nothing could overwrite it; (2) the
half-written-folder hazard is covered because a failed step now FAILS the build and the next build's staleness
check (parser missing or older than a grammar) regenerates. The only real residue was the parser's `-lib`
directory (imported Core/*.g4 + CobolLexer.tokens) — that staging now lives in `obj/antlr-lib/` (SDK-ignored,
SDK-cleaned, never globbed into Compile). `Generated_temp` is gone from the script, the csproj (its Compile
exclusion + clean step), and .gitignore. Re-verified from scratch on BOTH platforms (Generated/ deleted first):
WSL regen 8 files, Windows regen 8 files, 648/648 conformance.

