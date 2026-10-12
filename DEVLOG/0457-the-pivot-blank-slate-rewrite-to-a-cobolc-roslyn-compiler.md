## Entry 457 — THE PIVOT: blank-slate rewrite to a COBOL→C# (Roslyn) compiler; byte substrate abandoned (COBOL.NET / G1)

The biggest course-correction in the project. While extending OO typed-native object data (subclass own data,
lifting COBOL0113), an adversarial panel surfaced that whole-record operations on a typed record still routed
through the per-instance byte `ProgramState` — reading stale init bytes and ignoring typed mutations. I started to
fix it the usual way (give the subclass a correctly-sized shared `State`). The owner stopped me, hard and
repeatedly, escalating across three messages:
1. *"Why are we not mapping COBOL CLASS object layouts to .NET object layouts? In .NET subclasses add fields to a
   base class."* — I was sharing the base's State instead of extending it.
2. *"What the hell is this byte State? A COBOL record in a COBOL class is simply a .NET record in a .NET class."*
3. *"Stop with all legacy byte state support. Move entirely to .NET representations. Tests need not run during the
   transition but must pass 100% after. Stop this piecemeal changeover. I keep falling back to the legacy
   implementation."* — then, decisively: *"Re-architect, re-design, totally rewrite it if useful. The best
   possible COBOL to .NET implementation."*

**Honest record of the misstep (for the article series):** the migration had been built as a *gated* typed path
(`EnableTypedFields`, default OFF) bolted onto the byte engine, with a permanent byte "fallback." I kept reaching
back into that fallback whenever an operation lacked a typed cell — exactly the piecemeal creep the owner had
forbidden more than once. Measuring it made the foundation undeniable: flipping typed-native on as the only path
regressed **360 of 364 NIST programs** (mostly LOUD `COBOL0600 "typed location reached a byte-window operation"`),
proving the byte `ProgramState` is load-bearing everywhere — the substrate, not a detail.

**Decisions (owner, via an explicit fork):** (a) **blank-slate full rewrite** — new compiler, keep only the specs
+ test corpus as the conformance oracle (the owner chose this over my safer "re-architect in place" recommendation,
accepting the longer road / higher regression risk); (b) **backend = generate idiomatic C# source via Roslyn** (a
COBOL record → a C# `record struct`, elementary → native field), the clearest "best .NET implementation." Recorded
as memory `feedback_complete_dotnet_migration_no_byte`; the architecture SSOT is `docs/COBOLNET_ARCHITECTURE.md`.

**What reuses vs is rebuilt:** REUSE only the front-end (ANTLR grammar + lexer/parser/preprocessor — a declarative
ISO-derived spec artifact) and the clean, oracle-verified typed runtime substrates (`CobolNum`/`CobolString`/
`ManagedPointer`). REBUILD everything from the parse tree onward, typed-native, no `ProgramState`/byte-offset
layout/`PicRuntime` byte-window APIs. The legacy `CobolSharp.Compiler` byte engine is kept ONLY as a differential
oracle until cut-over (G8). Per the advisor: get the thinnest end-to-end slice running FIRST (no design doc / no
workflow until a program runs); port the proven PC/dispatch control-flow design (DEVLOG 259–260) rather than
re-derive it; defer the unbounded data cases (REDEFINES/RENAMES, whole-group-alphanumeric, file serialization).

**G1 LANDED — the skeleton works end-to-end.** New project `src/CobolNet` (exe `cobol`): `Frontend` (reuses the
preprocessor + ANTLR parser → parse tree), `CSharpEmitter` + `CodeWriter` (parse tree → readable C#),
`RoslynBackend` (Roslyn `CSharpCompilation` → assembly + `.runtimeconfig.json`, framework refs from the TPA list),
`Program` (CLI). Slice 1 emits `DISPLAY` of literals + `STOP RUN`/`GOBACK`. `hello.cob` → C# → Roslyn → runs:
`HELLO, WORLD!` / `FROM COBOL.NET`. The generated `hello.g.cs` is a clean `internal static class Program { Main }`.
Every seam (preprocess→parse→emit→compile→run) is now real and de-risked. `ZeroTokenRewriter` made public for the
front-end reuse. NEXT: G2 data division → typed C# fields/record-structs/arrays; G3 core verbs on typed values;
G4 control-flow engine; G5 drive NIST to green.

