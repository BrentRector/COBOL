## Entry 447 — Phase B1: OO COBOL slice 1 — CLASS / NEW / INVOKE / OBJECT REFERENCE end-to-end (the first running OO program)

Implemented the **OO semantic + CIL-emit vertical** (MASTER_PLAN Phase B1 / Stage-4 OO; the turnkey
`docs/OO_IMPLEMENTATION_DESIGN.md` §6.6). The grammar was already landed (DEVLOG 439); this is the missing
semantic/emit half. **A representative OO program now compiles to real .NET classes and runs:**
`INVOKE GREETER "NEW" RETURNING G` then `INVOKE G "SAYHELLO"` → `HELLO, WORLD!`.

**The model (ADR §7 / design §4): a CLASS is an INSTANCE .NET reference type with a per-instance `ProgramState`.**
Each object owns its own `State` field; OBJECT data lives there; the COBOL method is a public instance method
invoked via `newobj`/`callvirt`. The whole byte + typed data engine works per-instance with ONE addressing change
(load `this.State` instead of the static `State`).

**What landed (one cohesive vertical, built incrementally, guard-green throughout):**
- **Routing** (`Compilation.cs`): `CollectProgramContexts` also collects `group.classDefinition()`;
  `ExtractProgramIdFromContext` resolves a class name from its CLASS-ID paragraph; Phase 4 tags the `IrModule`
  (`IsClass` + `ClassMethodName`). A class routes through the existing `BuildSemanticModel`/`Binder` unchanged (the
  ANTLR visitor recurses into the OBJECT data + the method's procedure), so a single-method class compiles to a
  normal `IrModule` — no new `ClassSymbol` needed for slice 1.
- **Instance emission** (`CilEmitter.EmitClassModule`): a non-sealed reference type; `EmitProgramState` flips the
  `State` field + `InitializeState` to instance and emits a public `.ctor` (the COBOL `NEW` factory) instead of the
  `.cctor`; `DefineMethodSignature`/`EmitDispatchHelper` emit instance paragraph methods + an instance `Dispatch`
  (arg-index shift + receiver before each self-call); the COBOL `METHOD-ID` becomes a public instance method that
  runs the method's paragraphs through the instance dispatch loop. **One chokepoint flag** —
  `EmissionContext.StateIsInstance` — switches every `State` load between static (`ldsfld`) and instance
  (`ldarg.0; ldfld`); default **false**, so the whole non-OO corpus is **byte-identical**.
- **INVOKE** (`BoundInvokeStatement` → `IrInvoke` → `EmitInvoke`): NEW → `newobj <class>::.ctor` + store into the
  RETURNING object reference; instance → `ldsfld <recv>; callvirt <class>::<method>`. **Cross-type resolution is
  static at emit time** (no runtime reflection): class units are emitted BEFORE program units, so a program's
  INVOKE resolves the class type + method by name from the shared assembly module.
- **OBJECT REFERENCE storage** (mirrors the pointer-field subsystem): `USAGE OBJECT REFERENCE class` →
  `CobolCategory.ObjectReference` (+ reuse the existing `UsageKind.Object`), no WORKING-STORAGE bytes
  (`StorageLayoutComputer` skips it), a `static <class> _OBJ_<name>` field (`Binder.CollectObjectRefFields`,
  always-on). The declared class is captured on the `DataSymbol` to type the field + resolve INVOKE targets.
- **REPOSITORY `CLASS class-name`** parsed (gated `{is2002()}?` hook on `repositoryEntry`); class-names in the
  compilation group are whitelisted in the reference resolver so an INVOKE class-name target is not flagged CBL3128.
- **Conformance** (same commit): `tests/conformance/2002/oo_hello.cob` (NEW + instance method → `HELLO, WORLD!`)
  and `oo_method_perform.cob` (a method using PERFORM / PERFORM TIMES over its own paragraphs, mutating per-instance
  data → `N=1/N=2/N=3`).

**Adversarial review (3-lens panel: instance-flip safety · INVOKE/OBJREF correctness · semantic-regression).**
Verdict: slice-1 happy path correct, **non-OO corpus byte-identical (no regression)** — the OO surface is islanded
behind empty collections, a false-defaulting `StateIsInstance`, and `{is2002()}?` gates inert in cobol85. It caught
**two real latent landmines the trivial conformance test missed**, both fixed in this commit:
- **(major)** `EmitLocalStorageDefaultsSnapshot` loaded the static `State` (`ldsfld`) — invalid IL the moment a CLASS
  declares LOCAL-STORAGE. Routed both CilEmitter `State` loads through one instance-aware `EmitLoadState` helper.
- **(major)** the PERFORM family (`EmitPerform`/`EmitPerformTimes`/`EmitPerformThru` + `EmitParagraphDispatch`)
  emitted **receiver-less** calls to instance paragraph methods / `Dispatch` → bad IL for any OO method using
  PERFORM. Threaded `StateIsInstance` into `CilControlFlowEmitter` (an `EmitSelfReceiver` no-op for programs).
  The new `oo_method_perform` conformance test now exercises + pins this path.
- **(hardening)** `UsageMapper.FromUsageKeyword`'s `_ => UsageKind.Object` default (now that `UsageKind.Object` is
  live) → `_ => UsageKind.Unknown`, so a stray usage keyword can't masquerade as a zero-storage object reference.

**Scope notes (deferred to later OO slices, documented in the design doc):** USING/RETURNING args (slice 2);
INHERITS + SELF/SUPER (slice 3); FACTORY/static (slice 4); PROPERTY (slice 5); polymorphism + EC-OO (slice 6).
Slice 1 is single-method-per-class; typed-native OBJECT fields (when `EnableTypedFields` flips on for classes) and
object references HELD BY a class are future per-instance work (the `_OBJ_`/typed fields are static today, correct
for the driver-program INVOKE site but a future cross-instance concern). Multi-method classes need a richer IrModule.

Guard ALL GREEN — **1204 unit / 511 integration / 364 NIST** (+2 OO conformance vs the pre-OO 509). `guard-fast.sh`,
byte-identical to serial.

