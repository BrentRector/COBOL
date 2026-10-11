## Entry 974 — 2026-07-22 09:40 PDT — F3-in-a-method increment M1: the dead binder/data plumbing (byte-identical)

First implementation increment of the §9.10 design (DEVLOG 973). Lands the binder/data plumbing, still 0899-rejected
(`F3StagedInMethodStub` untouched) so `table.F3Handlers` is empty for a class ⇒ everything is inert / byte-identical:
- `OoMethodBinding.HandlerStartPc`/`HandlerCount` — the method's contiguous sub-range of the class's appended handler
  pc-space (default 0 = no F3 PERFORM).
- `ProcedureTableBuilder._f3HandlerMethod` (parallel to `_f3Handlers`, exposed as `F3HandlerMethods`) — the owning
  `OoMethodScope` per appended handler (null in a program unit), filled by `AddF3Handler` from `ctx.CurrentMethodScope`.
- `StatementBinder.BindMethodRoster` — appends `table.F3Handlers` to the class `bound` (mirroring the program path at
  `Bind():163`), stamps each method's `[HandlerStartPc, HandlerCount]` via `StampMethodHandlerSlices` (a `scopeToMethod`
  map + a LOUD contiguity assertion), and passes `F3HandlerBasePc`/`F3HandlerOwners` to the class `BoundProgram` (null
  when no handlers).

Gate: build 0/0, characterization 33/33 byte-identical, 182 OO/F3/Method/Class unit tests green. NEXT = M2 (the
scope-parameterized `__RunUse`/`__RunF3`/`__EcPerform` machinery — the byte-identity-critical refactor).

