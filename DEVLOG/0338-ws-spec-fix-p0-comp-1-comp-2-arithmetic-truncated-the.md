## Entry 338 — WS-SPEC fix (P0): COMP-1/COMP-2 arithmetic truncated the fraction

`StoreArithmeticResult` (the converge point for **all** COMPUTE/ADD/SUBTRACT/MULTIPLY/DIVIDE result stores)
unconditionally called `ApplyScalingAndRounding`, which scales to the receiver's `FractionDigits` — 0 for a
PIC-less COMP-1/COMP-2 float — so `COMPUTE WS-F = 3.14159 * 2` stored `6` and `1.0/3.0` stored `0`. The MOVE path
already guarded floats (`MoveNumericToNumeric` skips scaling for Comp1/Comp2); mirrored it — a COMP-1/COMP-2
receiver bypasses `ApplyScalingAndRounding` + `WouldOverflow` and `EncodeNumeric`s the full IEEE value.
CLI-verified (via MOVE to a display field, to sidestep the separate raw-COMP-2-DISPLAY formatting bug):
`1.0/3.0` → `033333333`; `3.14159*2` → `0628318`. Test: `SpecFixTests.Compute_IntoFloatReceiver_PreservesFraction`.
8th backlog fix. (Still open in the CompFloat cluster: raw `DISPLAY` of a COMP-2 formatting + the COMP-4 token.)

