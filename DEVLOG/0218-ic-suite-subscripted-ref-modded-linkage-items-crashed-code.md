## Entry 218 — 2026-05-29: IC suite — subscripted/ref-modded LINKAGE items crashed code generation

IC106A (and other IC programs) hit an internal compiler error: `EmitLoadBackingArray: unexpected
StorageAreaKind 'LinkageSection'`. A LINKAGE-section item has no backing array in `ProgramState`
— it is reached through a `CobolDataPointer` populated from the CALL USING arguments — and
`EmitLocationArgs` handled that for a plain item (case → `EmitLinkageLocationArgs`). But the
element-address (`EmitElementAddress`, table subscripting), reference-modification
(`EmitRefModAddress`), and ODO-group (`EmitOdoGroupLocationArgs`) paths all loaded the base array
via `EmitLoadBackingArray`/`…OrExternal`, which throws for LINKAGE. So passing a *table* or
ref-modded item via USING (IC106A passes two tables + an index) crashed codegen.

Extracted the LINKAGE param-matching into `FindLinkageField` and a reusable
`EmitLinkageBufferAndOffset` that pushes `[CobolDataPointer.Buffer, pointer.Offset + relOffset]`
— the (array, runtime-base-offset) pair the address-composition code expects. Routed the element,
ref-mod, and ODO base computations through it for a LINKAGE base. The LINKAGE base offset is a
runtime value (the caller's argument position), unlike the compile-time constant for
WorkingStorage, so it must be pushed as `pointer.Offset + relOffset`, not `Ldc_I4`.

IC COMPILE_FAIL 14→10, CLEAN 8→9. Guard ALL GREEN (1000 / 336 / 149).

