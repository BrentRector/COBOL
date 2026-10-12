## Entry 453 — Phase B1: OO goes TYPED-NATIVE (ADR §7) — object data → per-instance .NET fields (SLICE 1, char-first)

**Owner course-correction.** I had started the multi-method "keystone" on the *byte* substrate and even floated
"keep object data on the per-instance `ProgramState` byte image" as a recommended default. The owner pushed back,
hard and correctly: *"We long ago said give up the byte substrate and migrate completely to .NET data types
everywhere possible. Why did you fail to follow the plan and design?"* The ADR (`DATA_MODEL_ARCHITECTURE.md` §7)
already settles it — COBOL OO objects → real .NET classes, **object instance data → a per-instance typed record**,
methods → .NET methods, `INVOKE` → `callvirt`. My error was anchoring on the *current implementation state*
(`EnableTypedFields` default-OFF, "corpus byte-identical") as if the gate were the design intent; it is a
migration-safety mechanism for the **legacy** corpus, and OO is a brand-new subsystem with no legacy corpus to
preserve — so it must be born typed-native. Recorded as memory `feedback_oo_typed_native_not_byte`. Owner also
relaxed the cadence: *tests may break temporarily mid-migration; the bar is ALL green at completion.*

**What landed (SLICE 1 — character object data, the smallest typed-native vertical).** Ride the existing
`EnableTypedFields`/classifier pipeline rather than build a parallel one (singular pattern):

- **OO is an always-on consumer of the typed pipeline.** `Binder.CollectTypedFields` gate changed from
  `!EnableTypedFields` to `!EnableTypedFields && !module.IsClass` — a CLASS flips its object data to typed-native
  fields regardless of the global migration gate (mirrors the always-on `CollectPointerFields`/`CollectObjectRefFields`).
  `IsClass` is now tagged inside `Binder.Bind(tree, isClass)` *before* `CollectTypedFields` runs (it was set in
  `Compilation` only *after* `Bind` returned). A temporary `classCharOnly` restriction flips only CHARACTER object
  data for now (numeric/record/array object data follow once their per-instance materialize+arithmetic sites land);
  it auto-relaxes when the global `EnableTypedFields` flip arrives.
- **Per-instance typed fields.** The typed-field machinery was static-only (mirroring the static `ProgramState`).
  Added the instance dimension on the same `StateIsInstance` flag the byte `State` already rides: the typed field
  DEFINITION and its VALUE-init in `EmitProgramState` emit an instance field + store through the receiver for a
  class; and the three typed value-access primitives in `CilDataEmitter` (`EmitTypedLoad`/`StorePrefix`/`StoreSuffix`,
  flat case) route through one new chokepoint **`EmitTypedFieldOwner`** (pushes `ldarg.0` when `StateIsInstance`) —
  the typed analogue of the byte engine's single `EmitLoadBackingArray` State chokepoint.
- **Proof.** `oo_hello`'s `01 MSG PIC X(13)` now emits as `instance String _T_MSG` on the GREETER class (verified
  via Mono.Cecil), output still `HELLO, WORLD!`. The adversarial gate that a single-object test cannot give — a
  **two-object** independence check (`oo_instance_data.cob`: `b1.BUMP/b2.BUMP/b1.BUMP` → `---/---/XYZ`; a shared
  static field would print `---/XYZ/XYZ`) — is in the conformance corpus + mirrored in `OoTests.TypedObjectData_IsPerInstance`.
  This catches the #1 silent-corruption risk (static-field share across instances).
- **Drive-by fix:** `Compilation.EmitAssembly` crashed on a bare `-o prog.dll` output path —
  `Path.GetDirectoryName` returns `""` (not null) for a directory-less path, so `?? "."` missed it and
  `Directory.CreateDirectory("")` threw. Now treats an empty dir as `.`.

The 5 existing `oo_*` conformance tests stay green (`oo_hello`/`oo_inherit` now on typed strings;
`oo_method_perform`/`oo_method_args` still byte via `classCharOnly`; `oo_super` has no object data). NEXT: numeric
object data (per-instance typed `long`/`decimal` — instance-ize the `CilLocationEmitter` materialize + arithmetic
sites), then multi-method classes + `INVOKE SELF`, then INHERITS-typed + subclass typed data.

