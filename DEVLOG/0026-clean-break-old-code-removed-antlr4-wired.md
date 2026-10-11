## Entry 026 — 2026-03-14: Clean Break — Old Code Removed, ANTLR4 Wired

Removed: Lexing/ (3 files), Parsing/ (9 files, ~4200 lines), CodeGen/ (CilEmitter.cs),
Semantics/ (4 files). Removed old unit tests referencing deleted code.

Added: ANTLR4 JAR (2.1MB), Antlr4.Runtime.Standard NuGet 4.13.1, PowerShell generation
scripts, MSBuild build target, Generated/ directory. Compilation.cs rewritten with ANTLR4
pipeline: AntlrInputStream → CobolLexer → CommonTokenStream → CobolParserCore.

First test: `*>` comments not being skipped (COMMENT_START needed `-> skip`).

Build passes. Pipeline works end-to-end for the first time.

---

