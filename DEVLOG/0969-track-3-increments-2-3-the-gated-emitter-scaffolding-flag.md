## Entry 969 — 2026-07-22 00:05 PDT — Track ③ Increments 2+3: the gated emitter scaffolding (flag flow + funnel→__EcPerform + __RunF3), all inert/byte-identical

**Increment 2 (gating flag flow) + Increment 3 (emitter scaffolding), committed together as the "gated scaffolding"
checkpoint** — every new emission is gated on a flag that is only true for a program containing an F3 PERFORM, which
is still bind-REJECTED (COBOLNET0899) until Increment 4. So NOTHING new is emitted for any compiled program yet;
byte-identity is provable and characterization-confirmed.

**Increment 2 — the flag flow (inert):**
- `EcFeatures` gains an 8th field `HasF3Perform` (default false) + it is added to `.Any` (so the int-returning
  `__RunUse` the interceptor depends on is emitted even for an F3 PERFORM whose imp-1 uses no OTHER EC feature —
  e.g. `PERFORM CONTINUE WHEN EC-BOUND-SUBSCRIPT CONTINUE END-PERFORM`). `BuildFeatures()` threads `F3Perform`.
- `EcState.UnitHasF3Perform` (new, parallel to `UnitHasF3`/`UnitHasF4`); set in `ProgramEmitter` from
  `Ec?.HasF3Perform`, forced false in `OoEmitter` (F3-in-method is loud-rejected, §9.1-B).
- `BoundProgram` gains `int? F3HandlerBasePc` + `IReadOnlyList<int>? F3HandlerOwners` (both default null — the
  byte-identity gate: a non-F3 unit adds zero synthetic pcs). Both new record params have defaults ⇒ no construction
  site breaks (verified the greenfield `new BoundProgram(...)` sites; the legacy `CobolSharp.Compiler.BoundProgram` is
  a separate type).

**Increment 3 — the emitter scaffolding (gated `UnitHasF3Perform`, DEAD until Incr 4 un-rejects):**
- `EcEmitter.EcDispatchExpr` (the ONE raise-site funnel): emits `__EcPerform(ec,file)` when `UnitHasF3Perform`, else
  the historical `__EcDispatch(...)`/`-3` — a non-F3 unit's text is unchanged (the new branch is prepended above the
  identical middle branch). `fatal` is NOT threaded (the split is realized at the raise site).
- NEW `EcEmitter.EmitPerformInterceptor`: emits `__EcPerform` (consult `ExceptionState.RunTopFrame` first, fall to
  `UnitHasF3 ? __EcDispatch : -3` on no-match) + `__RunF3(u,pc,cu,cpc)` (run imp-2/3 via `__RunUse`, then WHEN COMMON
  imp-4 ONLY on `-1`; `-2`/RESUME-NEXT short-circuits COMMON — §9.6 Q3).
- `DispatchEmitter.EmitUseMachinery`: BOTH gates widened for the no-declarative F3 case — the outer CALL gate
  (`|| Ec is { HasF3Perform: true }`) and the inner `__useActive`/`__RunUse` gate; `__useActive` sized
  `declCount + f3Handlers` (f3Handlers = `Paragraphs.Count − F3HandlerBasePc`, which is 0 until Incr 4 ⇒ renders as
  the identical `declCount`). `EmitPerformInterceptor` wired before the `__IoCheck` early-return so a no-declarative
  F3 unit still gets it. This is the fix for the "no-declarative F3" BLOCKER all three design verdicts caught.

**Deviation from the §9 increment plan (noted):** the `__IoCheckEc` frame-first restructure (§9.5.1) is DEFERRED from
Incr 3 into Incr 4 — it is the byte-riskiest change and is only exercised by an I-O WHEN, so folding it into Incr 4
gets it BOTH the characterization byte-identity gate AND a real behavior test (I/O WHEN preempts a matching USE),
which is strictly better coverage than landing it dead in Incr 3.

**Gate (wave-local — inert scaffolding, shared EC funnel touched but provably gated): characterization 33/33
byte-identical + F3 parse 37 + frame stack 9 + full greenfield Unit sweep + build 0/0.** The comprehensive gate
(full greenfield Conformance + legacy guard + GnuCOBOL differential) is reserved for Increment 4 (the behavior wave,
where the scaffolding first activates). **⏭ NEXT = Increment 4:** the pc-range synthesis (`AddF3Handler` side-list +
`StatementBinder` append + node reshape `BoundExceptionMatch`) + the tier-sorted matcher + `EmitExceptionPerform` +
the `F3Region`/`BoundExitPerform` machinery + `__IoCheckEc` frame-first + drop the 0899 reject (program path) —
the behavior wave.

