## Entry 044 — 2026-03-14: THE BINDER — Full Pipeline Wired End-to-End

### The Milestone

NC101A now compiles through the **complete pipeline**:
```
COBOL Source → Preprocess → ANTLR4 Lex → Parse → SemanticBuilder
→ ReferenceResolver → SemanticModel → Binder → IrModule
→ CilEmitter → Mono.Cecil → .NET assembly (.dll)
```

The output is a real 2048-byte .NET assembly with runtimeconfig.json. It doesn't run yet
(needs assembly entry point set, and statement lowering produces placeholder runtime calls),
but the pipeline is end-to-end connected.

### What Was Built

**SemanticModel**: facade over all semantic pass results. Exposes DataRecords,
ParagraphsInOrder, ResolveData/Paragraph/Section/File, PicDescriptors, StorageLocations.
The binder never re-derives — it just asks.

**Binder**: walks parse tree with resolved symbols, produces IrModule.
- BuildRecordTypes: DataSymbol → RecordLayoutBuilder → IrRecordType + StorageLocations
- CreateParagraphMethods: each paragraph → IrMethod
- ProcedureLoweringVisitor: parse tree walker that emits IR instructions
  (MOVE, DISPLAY, PERFORM, GO TO, STOP, ADD, IF, EXIT, OPEN, CLOSE → IrPerform,
  IrReturn, IrRuntimeCall)
- CreateEntryPoint: Main method that calls first paragraph

**Compilation.cs**: phases 4-6 wired (SemanticModel → Binder.Bind → CilEmitter.EmitAssembly).

### What's Still Placeholder

- Statement lowering emits IrRuntimeCall("CobolRuntime.Move") etc. — not yet resolved
  to actual PicRuntime methods with StorageLocation arguments
- IF conditions emit sequential statements, not IrBranch with basic blocks
- No assembly entry point set (MissingMethodException on run)
- No actual data in the emitted assembly (records defined but not populated)

### Process

Grammar files accidentally renamed .g4 → .txt during commit — fixed immediately.
NC101A verified after every change.

---

