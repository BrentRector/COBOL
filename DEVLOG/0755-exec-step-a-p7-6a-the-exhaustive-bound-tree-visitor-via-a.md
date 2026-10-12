## Entry 755 — 2026-07-11 00:40 PDT — EXEC STEP A / P7 §6a: the exhaustive bound-tree visitor, via a Roslyn source generator (after a regex-script misstep the owner caught)

**What landed (6a — dispatch machinery, no consumers converted yet):** a new `src/Cobol.Net.Compiler.SourceGen`
project — a Roslyn **incremental** source generator (`BoundVisitorGenerator`, `netstandard2.0`, `IsRoslynComponent`,
referenced by the compiler with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`). It marks all **7**
bound roots `[BoundNode]` (`BoundStatement`/`BoundExpr`/`BoundCondition`/`BoundOperand`/`BoundBoolExpr`/
`BoundPerformControl`/`BoundSetTarget`), discovers every sealed leaf through the **semantic model**
(`INamedTypeSymbol.BaseType` chain), and emits `BoundVisitors.g.cs` = 7 `I{Root}Visitor<out T>` interfaces (120
`Visit` overloads total) + a `BoundVisitor` static class with 7 exhaustive `Accept<T>` extensions. The `[BoundNode]`
attribute is self-emitted via `RegisterPostInitializationOutput`. Correctness net: `BoundVisitorGeneratorTests`
(reflection — each interface's Visit-params == the compiled non-abstract descendants of its root; 7 cases green). **No
drift test and no committed artifact** — a source generator regenerates off the live type graph every build, so it
*cannot* drift; adding a leaf without a `Visit` becomes a compile error in every implementing consumer. Compiler +
unit battery green (`-p:EmitCompilerGeneratedFiles=true` confirmed 120 leaves / 7 roots; 266 unit + 7 new).

**The misstep (owner-caught, logged per [[feedback_transparency]]):** I first built this as a PowerShell
`gen-bound-visitors.ps1` that **regex-scanned the C# for `record X : Root`** and committed a `BoundVisitors.g.cs` +
a drift test — reflexively matching the repo's `gen-constructs.ps1`/`gen-cobol-words.ps1` discipline. It was wrong,
and it showed: the non-greedy regex immediately mis-captured the base-less helper `BoundCallArg` by running across a
`;` into the *next* record's `: BoundStatement`, and a prose "record area" matched too. I patched the regex — but the
owner (correctly) flagged the whole approach as contraindicated: *"Writing a custom generator script instead of
leveraging existing tooling seems contraindicated… we mandate commercial quality code maintainable for decades."*
Parsing C# with a regex to emit C# IS the hand-rolled-parser anti-pattern the Path-A "leverage the tooling" mandate
exists to kill. The canonical tool for C#→C# is a Roslyn source generator reading the semantic model — which is also
exactly what PHASE-07 Step 6 prescribed before I deviated. Replaced the script with the generator; deleted the pwsh
script, the committed `.g.cs`, and the drift test.

**Why the `gen-*` pwsh scripts staying is NOT a two-mechanisms violation ([[feedback_singular_pattern]]):** those
scripts generate from JSON data into BOTH C# *and* ANTLR `.g4` grammar — a Roslyn source generator cannot emit `.g4`.
Different job (data+grammar → artifacts) vs the visitor's job (C# → C#), so the best tool differs; the singular-best
tool per job is honored.

**As-built design choices (kept CURRENT in `docs/rearchitecture/PHASE-07-*.md §6a` per the doc-sync rule):**
`Accept` is an extension-method `switch` with a `_ => throw` (records aren't `partial`; the throw satisfies CS8509 and
is unreachable-by-construction since the switch regenerates from the live leaf set — the compile-time exhaustiveness
guarantee lives in the *interface*). The five error nodes each get their own `Visit` — no `IBoundError` collapse
(explicit is strictly more exhaustive; a consumer wanting uniform handling calls one shared helper). OPEN Q1 resolved
(source generator; the hand-written-abstract-visitor fallback was unneeded). Docs swept: PHASE-07 STATUS + §6a +
files-list, resume-prompt RESUME-AT banner (→ 6b), memory to follow. **NEXT: 6b — convert `EmitStatement` to
`IBoundStatementVisitor<bool>`, delete the loud `default`; then renderers/analyses 6c–6f, battery-green each.**

