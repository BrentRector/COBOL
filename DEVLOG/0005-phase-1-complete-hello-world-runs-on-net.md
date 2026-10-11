## Entry 005 — 2026-03-13: Phase 1 Complete — "Hello, World!" Runs on .NET

**Session**: #2 (first implementation session)
**Time**: ~1 hour elapsed in this session
**Cumulative**: ~2 hours across 2 sessions (Session 1: planning, Session 2: implementation)

### What Was Built

The entire Phase 1 compiler pipeline, from nothing to a working COBOL-to-.NET compiler:

1. **Solution scaffolding**: 5-project .NET 8 solution (Compiler, Runtime, CLI, Tests.Unit, Tests.Integration)
2. **Source text abstraction**: SourceText with line/column tracking, SourceLocation, TextSpan
3. **Lexer**: Free-form COBOL tokenizer with ~100 keyword mappings, case-insensitive matching, string/numeric literals, free-form comments (`*>`), operators, figurative constants
4. **AST**: Full node hierarchy — CompilationUnit, ProgramNode, divisions, 8 statement types, 8 expression types
5. **Parser**: Recursive descent with operator precedence for COMPUTE, COBOL-style conditions (GREATER THAN OR EQUAL TO, etc.), error recovery (skip to period)
6. **Semantic analysis**: Symbol table, data-name resolution, PICTURE parsing (9/X/A/V/S with repeat counts)
7. **Runtime**: CobolProgram base class, CobolField with byte[] storage + decimal computation, DISPLAY/MOVE/ADD/SUBTRACT
8. **CIL code generator**: Mono.Cecil emission — one class per PROGRAM-ID, field initialization from VALUE clauses, full procedure division emission, Main entry point
9. **CLI**: `cobolsharp compile <file> [-o output]`
10. **Tests**: 43 tests (39 unit + 4 integration), all passing
11. **CI**: GitHub Actions (Ubuntu + Windows matrix)

### The Five Bugs (Mistakes That Were Made)

These are worth documenting in detail because they represent patterns of AI-generated code errors:

**Bug 1: C# Ternary Type Coercion Trap**
```csharp
object value = hasDot ? decimal.Parse(text) : long.Parse(text);
```
This looks correct — if there's a decimal point, parse as decimal; otherwise as long. But C# ternary expressions require both branches to have a common type. Since `long` is implicitly convertible to `decimal`, the compiler silently promotes the `long` branch to `decimal`. The boxed `object` always contained a `decimal`, never a `long`. This is a genuinely subtle C# gotcha — both branches are individually correct, but the ternary combining them introduces a silent type conversion.

**Fix**: Replace with explicit if/else to prevent implicit conversion.

**Bug 2: Dead Code — Lexer Level Number Recognition**
The lexer's `ReadWord()` method had code to recognize level numbers (01-49, 66, 77, 88). But `ReadWord()` only fires when the first character is a letter or hyphen. Numbers always route to `ReadNumericLiteral()` first. The level number code in `ReadWord()` could never execute. This is a *design* error — I (Claude) placed the level number recognition in the wrong lexer method.

**Fix**: Moved level number recognition to the parser as a context-sensitive check. This is actually the architecturally correct place for it, since `42` is a valid numeric literal in COMPUTE but a level number in DATA DIVISION. The lexer shouldn't make this decision.

**Bug 3: Parser Scope Terminator Blindness**
Statement parsers (DISPLAY, MOVE, ADD) read operand lists "until period or next statement keyword." But `ELSE`, `END-IF`, `END-PERFORM` aren't statement keywords — they're scope terminators. Inside an IF body, `DISPLAY "Hello" ELSE DISPLAY "World"` would cause DISPLAY to consume `ELSE` as an operand expression, which then error-recovered badly.

This is the kind of bug that's invisible with simple test cases but breaks immediately with nested structures. The fix was trivial (add scope terminator checks), but the root cause is a failure to think about how individual parsers interact with the overall statement-nesting structure.

**Bug 4: CIL DISPLAY Stack Corruption**
The original DISPLAY emitter tried to build an `object[]` array and call the base class `Display(params object[])` method. The IL stack manipulation was wrong: it pushed `Ldarg_0` (this) then `Pop`'d it, with confused comments about the stack state. This is a classic danger of writing raw IL — there's no compiler checking your stack discipline.

**Fix**: Completely rewrote DISPLAY to use `Console.Write` per operand + `Console.WriteLine()` at the end. Simpler, correct, and matches COBOL semantics more directly.

**Bug 5: CIL Field Init Argument Order**
`MoveNumeric(decimal value, CobolField target)` is static. The emitter pushed `CobolField` first, then `decimal`. CIL is stack-based — arguments must be pushed in parameter order. Reversed arguments mean the decimal gets interpreted as a CobolField pointer and vice versa, causing a type safety violation at runtime.

### Observations on the AI Development Process

**Speed**: Building a complete (minimal) compiler pipeline in ~1 hour is fast by any measure. The plan's 6-phase structure with detailed task breakdowns made this possible — there was no time wasted deciding what to build next.

**Error patterns**: All 5 bugs were in the code generation / IL emission layer. The lexer, parser, and semantic analyzer worked correctly on first pass (once the ternary bug was fixed). This suggests the AI is more reliable at abstract/structural code (AST manipulation, recursive descent parsing) than at low-level details (IL stack manipulation, C# type coercion edge cases). This matches intuition — IL emission requires precise reasoning about invisible state (the evaluation stack), which is harder for probabilistic models.

**Test-driven correction**: All bugs were found by the test suite, not by manual inspection. This validates the decision to write comprehensive tests alongside the implementation. Without tests, bugs 1, 2, and 3 would have been invisible until later phases.

### What's Next

Phase 2: Core Data & Arithmetic. Starting with full PICTURE clause support (the most complex parsing challenge in COBOL), then USAGE, data hierarchy, full MOVE semantics, arithmetic statements, IF/EVALUATE, and PERFORM.

---

