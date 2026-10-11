## Entry 365 — M2: CALL … RETURNING into WORKING-STORAGE — the runtime gap of Entry 354 FIXED (unblocks UDF)

Entry 354 deleted the spurious caller-side CBL3304, found `CALL "ADDER" USING … RETURNING WS-R` then crashed at
runtime (RT0001 buffer access out of range, **bufferLength=0**), and reverted — the callee's RETURNING LINKAGE
item was not wired to the caller's passed pointer. This session **root-caused and fixed it**.

**The gap.** A LINKAGE item resolves its storage through `CilLocationEmitter.FindLinkageField`, which maps a
LINKAGE offset to the owning parameter's `_linkage_<name>` `CobolDataPointer` field — but it iterates only
`SemanticModel.ProcedureUsingParameters`. The PROCEDURE DIVISION RETURNING item is **not** a USING parameter, so
no range matched → the `else` branch pushed a **null buffer** → the callee wrote to a 0-length area. Entry 354's
attempt added the RETURNING name to `module.UsingParameterNames` (which creates the `_linkage_` field + the
`args[count] → field` Entry mapping) but **not** to the location-resolution path — so the field existed yet
`FindLinkageField` still returned null. That asymmetry is exactly why it kept crashing.

**The fix** (surgical, RETURNING-only — programs without a RETURNING item are untouched):
- `Binder.PopulateModuleMetadata`: after the USING names, append `ProcedureReturningItem.Name` to
  `module.UsingParameterNames` (so the trailing `_linkage_<R>` field is created and the Entry maps
  `args[usingCount] → _linkage_<R>`, matching the caller pushing RETURNING at `args[usingCount]`).
- `CilLocationEmitter.FindLinkageField`: after the USING loop, also test the `ProcedureReturningItem`'s LINKAGE
  range and return its `_linkage_<R>` field — so a reference into the RETURNING item resolves to the caller's
  pointer instead of a null buffer.
- `BoundTreeValidator`: removed the spurious **CBL3304** (the caller's RETURNING *target* is an ordinary receiving
  item in the caller's own storage — ISO §14.9.4.3 places no LINKAGE restriction on it; the callee-side
  requirement is CBL3109).
- `EndToEndTestBase`: added a dialect-aware `CompileMultipleAndRun(DialectMode, …)` overload (the callee's
  `PROCEDURE DIVISION … RETURNING` is `is2002()`-gated).

Deliberately NOT added to `ProcedureUsingParameters`, so the USING-only validation (CBL3108) and any arity logic
reading that list stay correct.

**Verified END-TO-END** (compile + run, per the lesson of 354): `CALL "ADDER" USING WS-A WS-B RETURNING WS-R`
with `ADDER` computing `L-R = L-A + L-B` → `RESULT=0042`. Test `CallReturning_IntoWorkingStorage_WritesResultBack`.

This is the **high-leverage unblocker for WS-2002-UDF** — a user-defined function returns its value through the
same RETURNING mechanism (`docs/SPEC_FIX_RECIPES_M1.md` #9 now resolved).

Guard ALL GREEN: **1047 unit / 450 integration / 364 NIST**, 0 regressions.

