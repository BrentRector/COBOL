## Entry 451 — Phase B1: OO slice 3b — INVOKE SUPER (an override calls the base class's method)

Built `INVOKE SUPER "m"` — the override-calls-base pattern that completes slice 3's inheritance story. A subclass
method that overrides a base method can call the base's version: `DOG.SPEAK` does `INVOKE SUPER "SPEAK"` (runs
`ANIMAL.SPEAK`, prints `ANIMAL`) then prints `DOG` → output `ANIMAL/DOG`, with **no infinite recursion**.

**Mechanism:** SUPER's receiver is `this` (`ldarg.0`) and the BASE class's method is called **non-virtually**
(`call`, not `callvirt`) so it runs the base's exact method rather than re-dispatching to the override. `INVOKE SELF`
stays deferred (COBOL0112) — it needs a sibling method to call, i.e. multi-method classes (a later slice).

- `BoundInvokeStatement.IsSuper` / `IrInvoke.IsSuper`; `CallBinder.BindInvoke` detects the `objectReference` SUPER
  alt (restructured so SUPER skips the data-reference target and the NEW-factory branch); `Binder.LowerInvoke`
  threads it; `EmitInvoke` resolves `ResolveInheritedMethod(_currentBaseType, name)` and emits `ldarg.0` + the
  marshalled `ManagedPointer[]` args + `call` (vs `callvirt` for a normal instance INVOKE). USING/RETURNING on a
  SUPER call work (verified: `INVOKE SUPER "CALC" USING N RETURNING R` → the base contributes via the shared ABI).

**Adversarial review (Agent tool):** verified correct — non-virtual call semantics (no recursion), `_currentBaseType`
lifetime (fresh per-module emitter), 3-level SUPER chains (C→B→A printed `A B C`), IL stack balance, the BindInvoke
restructure (SUPER never reaches `ResolveData("")`), and SUPER-with-args. **One LOW finding fixed:** SUPER in a class
with no INHERITS base surfaced as a misleading **COBOL0600** "internal compiler error / please report this" at the
wrong line. Now detected cleanly at compile time — `ClassUsesSuper` scans a base-less class's IR for a SUPER invoke
and reports **COBOL0115** ("INVOKE SUPER is not valid in class 'X' — it has no INHERITS FROM base"); `EmitInvoke`
skips emitting a null-base SUPER (the compile already failed via COBOL0115) so no COBOL0600 noise. (The rarer
SUPER-to-a-base-method-that-doesn't-exist still surfaces descriptively; a full bind-time check needs cross-unit
method info — a later refinement.)

Conformance: `tests/conformance/2002/oo_super.cob` (`ANIMAL/DOG`). Regression: OoTests updated/added —
`Invoke_Super_CallsBaseMethod`, `Invoke_SelfTarget_FailsLoudly` (COBOL0112), `Invoke_SuperInRootClass_FailsLoudly`
(COBOL0115, asserts no COBOL0600).

Guard ALL GREEN — **1204 unit / 527 integration / 364 NIST** (+oo_super conformance, net +OoTests). `guard-fast.sh`.

