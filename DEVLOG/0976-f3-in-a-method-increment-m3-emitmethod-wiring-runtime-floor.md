## Entry 976 — 2026-07-22 10:04 PDT — F3-in-a-method increment M3: EmitMethod wiring + runtime floor (C1/C2), still gated

Third increment of the §9.10 design — the emit-side wiring + the runtime frame FLOOR (C2), all gated on
`HandlerCount>0` / `bound.Ec.HasF3Perform`, which stay false for every EMITTED class (an F3-in-method class is still
0899-rejected via `F3StagedInMethodStub`, so it never reaches the emitter) ⇒ byte-identical.
- **Runtime (C2):** `ExceptionEngine._floor` + `RaisePerformFloor()`/`RestorePerformFloor(old)`; `RunTopFrame`'s walk
  now stops at `_floor` (default 0 ⇒ programs/CALL unchanged). Facade forwards for the floor +
  `PerformDepth`/`TrimPerformTo` added.
- **OoEmitter.EmitTypeHalf (C1):** the class-member `__IoCheckEc` + `__EcPerform` (`EmitEcPerformMember`) now emit
  frame-aware when `bound.Ec.HasF3Perform` — `ecState.UnitHasF3Perform` is set from that class-level signal for the
  duration of the two emissions (it was cleared for the class at BeginUnit; per-method later). Byte-identical when no
  method has F3.
- **OoEmitter.EmitMethod:** per-method F3 context (set `UnitHasF3Perform` + `DeclCount=0` + `F3HandlerBasePc`=class
  base, restored after); method-LOCAL `__useActive` (sized to the class total H — the ONE HandlerUseId formula) +
  `__RunUse` + `__RunF3` (via the M2 scope-parameterized emitters); the two-range `__MDispatch` (real slice + handler
  sub-range); and the F3-method entry FLOOR wrapping the `__MDispatch` call (`RaisePerformFloor`/`RestorePerformFloor`
  + a defensive `TrimPerformTo`, matching the CALL boundary).

Gate: build 0/0, characterization 33/33 byte-identical, 340 F3/EC/OO/INVOKE/Method unit tests green. NEXT = M4
(delete the `F3StagedInMethodStub` early return → un-reject; the `PerformFormat3MethodBehaviorTests` matrix + the P1-P6
probes; comprehensive gate + GnuCOBOL differential).

