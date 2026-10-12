## Entry 975 — 2026-07-22 09:52 PDT — F3-in-a-method increment M2: scope-parameterized F3 machinery (byte-identical refactor)

Second increment of the §9.10 design. The pure REFACTOR that lets the F3 handler machinery emit at either scope
(class member for a program, method-local for an OO method) from ONE source — kept program-path byte-identical:
- `EcEmitter.EmitPerformInterceptor` SPLIT into `EmitEcPerformMember` (`__EcPerform` — always a class member: it reaches
  a handler only via `RunTopFrame`→the Matcher, never `__RunF3` directly, so it is class-callable even when `__RunF3` is
  method-local) + `EmitRunF3(w, asLocal)` (member for a program, local for a method). `EmitPerformInterceptor` now just
  calls both — identical output order.
- `DispatchEmitter.EmitRunUseBody(w, ecModel)` extracted from the member `__RunUse` emission, rendering
  `dispatchState.DispatchName` (the **C3 correction** — the former hardcoded literal `__Dispatch` would name a
  nonexistent method inside a class; for a program `DispatchName == "__Dispatch"`, so byte-identical).
- `DispatchEmitter.EmitDispatchMethod` generalized with an optional second contiguous handler case-range
  (`handlerFromPc`/`handlerToPc`, default −1 = off); the per-pc case emission extracted to a local `EmitCase` shared by
  both ranges. A program's `__Dispatch` passes no handler range (its main loop `[0..Count−1]` already covers its
  appended handlers) ⇒ byte-identical.

All still called ONLY by the program path (the method-local calls arrive in M3). Gate: build 0/0, characterization 33/33
byte-identical, 245 F3/EC/USE-declarative unit tests green. NEXT = M3 (`EmitMethod` wiring: the per-method F3 context,
method-local machinery, the two-range dispatch, the class-member frame-aware `__IoCheckEc`/`__EcPerform` [C1], and the
method-entry frame FLOOR [C2] — all gated on `HandlerCount>0`, still unreachable until M4).

