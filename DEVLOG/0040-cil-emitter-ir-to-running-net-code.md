## Entry 040 — 2026-03-14: CIL Emitter — IR to Running .NET Code

User provided concrete Mono.Cecil CIL emission design, instruction-by-instruction.
Implemented CilEmitter that maps the full IR instruction set to CIL:

- IrModule → AssemblyDefinition (Console module)
- IrRecordType → ValueType with SequentialLayout (COBOL records)
- IrGlobal → static fields on program type
- IrMethod → static methods with auto-allocated locals for IrValues
- IrBasicBlock → NOP-labeled IL regions
- Each IR instruction maps to 1-3 CIL opcodes

**Concrete MOVE A TO B example:**
```
IR:   v1 = loadfield A ; storefield B, v1
CIL:  ldsfld Program::A ; stloc.0 ; ldloc.0 ; stsfld Program::B
```

Not yet wired into the pipeline — needs the binder pass to produce IR from bound trees.
The remaining gap: Binder (bound tree → IR), then wire CilEmitter into Compilation.cs.

Pipeline so far: Preprocess → Lex → Parse → Symbols → Types → [Binder → IR → CIL → .dll]

---

