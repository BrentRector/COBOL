## Entry 039 — 2026-03-14: IR Layer — CIL-Friendly Intermediate Representation

User provided a concrete IR design: simpler than CIL, richer than COBOL, stable enough
for multiple backend passes.

**IrModule**: per-program container with types, methods, globals.
**IrType**: IrRecordType (COBOL records with explicit-layout fields carrying byte offset
and size) and IrPrimitiveType (int32, int64, decimal, string, bool, void, byte[]).
**IrMethod**: per-paragraph with parameters, locals, and basic blocks.
**IrBasicBlock**: linear instruction sequence with explicit terminators.
**IrValue**: SSA-ish virtual registers with monotonic IDs via IrValueFactory.

**Instruction set**:
- Data movement: IrLoadField, IrStoreField, IrMove, IrLoadConst
- Arithmetic/logic: IrBinary (Add/Sub/Mul/Div/Eq/Ne/Lt/Le/Gt/Ge/And/Or)
- Control flow: IrBranch (conditional), IrJump, IrReturn
- Calls: IrCall (general), IrPerform (COBOL paragraph → method call)
- Runtime: IrRuntimeCall (DISPLAY, file I/O, intrinsic functions)

**Design decision**: each COBOL paragraph becomes its own IrMethod. PERFORM becomes
IrPerform/IrCall. This makes CIL emission straightforward — each IrMethod maps to a
MethodDefinition, each IrValue maps to a CIL local via liveness analysis.

CIL emission uses Mono.Cecil 0.11.6. Next step: the Cecil emitter that takes IrMethod
and produces MethodDefinition body.

---

