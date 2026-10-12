## Entry 448 — Phase B1: OO slice 2 — INVOKE … USING / RETURNING (per-instance state is now observable)

Built OO slice 2 (turnkey `docs/OO_IMPLEMENTATION_DESIGN.md` §6.7): a COBOL OBJECT method now takes USING
arguments and returns a RETURNING value, so a method can mutate per-instance OBJECT data and hand a result back.
**The headline proof:** two independent `ACC` objects each accumulate their OWN balance — `INVOKE A1 "ADDTO"
USING 10` → `A1=0010`; `INVOKE A2 "ADDTO" USING 100` → `A2=0100`; `INVOKE A1 "ADDTO" USING 5` → `A1=0015` (A1
kept its 10, untouched by A2). That is the per-instance `ProgramState` model demonstrated end-to-end.

**The key reuse: an OO method is an instance "Entry" on the SAME ManagedPointer[] ABI as CALL.** Every OO method
is now `public void Method(ManagedPointer[] args)`; a no-arg method (slice-1 SAYHELLO) just takes an empty array.

- **Instance "Entry" method** (`CilEmitter`): factored the LINKAGE-field creation out of `CreateEntryMethodSignature`
  into `CreateLinkageFields` (now also called by `EmitClassModule`); added `EmitMapArgsToLinkage` (the shared
  `args[i] → _linkage_<name>` binding prologue). `EmitClassMethodBody` now emits `void Method(ManagedPointer[] args)`,
  maps args → the method's USING/RETURNING LINKAGE fields, then runs the instance `Dispatch`. The method's RETURNING
  item is the trailing LINKAGE param (already last in `UsingParameterNames`), written through the trailing
  by-reference pointer and read back by the caller — identical to `CALL … RETURNING`.
- **INVOKE marshalling** (`BoundInvokeStatement`/`IrInvoke`/`CallBinder.BindInvoke`/`Binder.LowerInvoke`/
  `EmitInvoke`): the instance INVOKE now carries USING arg locations + a RETURNING location, marshalled into a
  `ManagedPointer[]` exactly like `EmitCallProgram` (each USING arg BY REFERENCE via `ManagedPointer.CreateByReference`,
  RETURNING as the trailing by-reference element), then `ldsfld receiver; <args[]>; callvirt <class>::Method(args)`.
  Slice-2 USING args are data references (BY REFERENCE, the COBOL default); BY VALUE/CONTENT + literal args + a
  value-returning .NET method shape are later refinements. NEW still `newobj ..ctor` + store into the `_OBJ_` field.

Self-reviewed against the slice-1 adversarial findings (the instance machinery was already panel-reviewed in 447):
IL stack balance in the inline arg-array build (receiver underneath the array, balanced for 0/1/N args ± RETURNING);
the `CreateLinkageFields` refactor is byte-identical for a normal program (NIST IC CALL suite green); Cecil `Ldarg`
with a `ParameterDefinition` correctly emits `ldarg.1` for the `args` of an instance method; the array-index↔
LINKAGE-param order matches (RETURNING last). All confirmed by the passing tests, especially the 2-object
independence test.

Conformance (same commit): `tests/conformance/2002/oo_method_args.cob` (USING/RETURNING + the two-object
per-instance-independence proof → `A1=0010 / A2=0100 / A1=0015`). Slice-1 `oo_hello`/`oo_method_perform` stay green
under the new (empty-args) ABI.

Guard ALL GREEN — **1204 unit / 513 integration / 364 NIST** (+1 OO conformance). `guard-fast.sh`.

