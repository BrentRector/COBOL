## Entry 001 — 2026-03-13: The Beginning

### Context
Starting from a blank repository containing only the 1,261-page ISO/IEC 1989:2023 COBOL
specification PDF. The goal: build a production-quality, fully standards-compliant COBOL compiler
that targets .NET.

### Key Decisions Made

**Why .NET as the target platform?**
We considered several options — LLVM IR, JVM bytecode, native x86/ARM, and .NET CIL. We chose
.NET for several compelling reasons:

1. **Decimal arithmetic is built in.** COBOL lives and dies by exact decimal math. .NET's
   `decimal` type is 128-bit base-10 — it maps almost perfectly to COBOL's `COMP-3`/packed
   decimal. On LLVM or native targets, we'd have to build or import a decimal math library
   from scratch, which is a massive and error-prone undertaking.

2. **Precedent validates the approach.** Micro Focus Visual COBOL and Fujitsu NetCOBOL both
   target .NET in production. This isn't a research experiment — it's a proven path.

3. **Interop story.** .NET assemblies can be called from C#, F#, VB.NET. This means COBOL
   programs compiled by our tool can participate in modern .NET applications, which is the
   entire value proposition of a COBOL modernization tool.

4. **Runtime services for free.** Garbage collection, threading, I/O, string handling, and a
   massive standard library. We don't need to build a runtime from scratch.

**Why C# as the implementation language?**
Same ecosystem as our target. We can reference Roslyn's architecture for design patterns. The
tooling (debugger, profiler, IDE support) is best-in-class.

**Why hand-written recursive descent parser?**
COBOL's grammar is notoriously context-sensitive. `PICTURE` clauses contain characters that are
operators elsewhere. Area A/B rules in fixed-form affect parsing. Inline PERFORM creates scoping
that depends on paragraph ordering. Parser generators (ANTLR, yacc) struggle with these
ambiguities. Roslyn uses hand-written recursive descent for C# for similar reasons — the control
you get is worth the verbosity.

**Why Mono.Cecil for CIL emission?**
System.Reflection.Emit works but has a clunky API and limited PDB support. Mono.Cecil is the
industry standard for .NET IL manipulation (used by Unity, Fody, many others). It gives us clean
APIs for emitting instructions, defining types, and writing debug symbols.

### Architecture Sketch
We defined a 5-stage pipeline:
```
Source → Preprocessor → Lexer → Parser → Semantic Analysis → CIL Code Gen → .NET Assembly
```

This is deliberately traditional. No novel compilation techniques — the novelty is in handling
COBOL's enormous spec surface area correctly.

### The Scale of the Problem
The spec has 2,090 table-of-contents entries across 1,261 pages. For comparison, the C11 spec
is ~700 pages and the C# spec is ~800 pages. COBOL is genuinely one of the largest language
specifications in existence. We broke this into 6 phases with ~60 task groups to make it
tractable.

### What's Next
Phase 1: scaffold the .NET solution and get "Hello, World!" compiling from COBOL to a running
.NET executable. This is the proof-of-concept that validates every architectural decision above.

---
