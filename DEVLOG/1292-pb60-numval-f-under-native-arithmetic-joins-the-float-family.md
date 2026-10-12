## Entry 1292 — 2026-08-17 21:48 PDT — PB60: NUMVAL-F under native arithmetic joins the float family — the sweep RenderFloat's own comment asked for

The row PB56 reassigned to PB60 (RV-15.69.4-2 — "if native arithmetic is in effect, the returned value is an
approximation of the numeric value represented by argument-1"). Measured before (probe p13, `--std 2023`, native):
`DISPLAY FUNCTION NUMVAL-F("5E+30")` printed `170141183460469231731687303715.884105727` (the Int128 saturation
sentinel of a ×10⁹ rescale), `IF FUNCTION NUMVAL-F("5E+30") = FUNCTION NUMVAL-F("9E+30")` was TRUE, a COMP-2
receiver got 1.7E+29, and `NUMVAL-F("1.5E-12")` was 0 in a COMP-2, in a DISPLAY and in `> 0`. RenderFloat's
comment records the identical symptom for EXP10 and credits the fix to PB13 — "never swept to the three NUMVAL
arms in the same file", the refuter wrote, and for this member that was exactly right.

**The determination.** §15.69.4 r2 grants native NUMVAL-F an approximation and §15.4.1 leaves a native returned
value's representation to the implementor; the compiler's documented determination for an approximated returned
value (item 92) is the float family's — binary64 wherever no fixed-point scale governs the render, quantized only
by a fixed-point arithmetic receiver. So: `CobolIntrinsics.NumvalFDouble` — ONE correctly-rounded `double.Parse`
of the scan's canonical `[-]digits E exp` (never a multiply chain), the same `NvfScan` and reject projection as
the Int128 and SDIDI twins — renders in a receiver-less or float-receiver context; a fixed arithmetic receiver
keeps the exact Int128 parse. **And so does a MOVE sender**, which is the one place I departed from the float
family and the reason a new `ReceiverContext.MoveSender` discriminator exists: my first cut routed MOVE senders
through binary64 too, and the unit theory caught it — `CobolFloat.ToScaled(1.5e-8, 9)` multiplies then truncates
and lands 14, so `MOVE FUNCTION NUMVAL-F("1.5E-8") TO PIC V9(9)` would have stored 0.000000014 where the exact
route stores 15, and a 20-digit argument would have lost every digit past the 17th. NUMVAL-F is the one family
with BOTH an exact form and an approximation license; where the receiver's scale is known the exact form wins.
The float family does not consult the flag — its value has no exact form.

**After.** `5E+30` / `1.5E-12` / `1.5` in DISPLAY; EQ FALSE; GT TRUE; COMP-2 receivers hold 5E+30 and 1.5E-12;
COMPUTE into a fixed receiver and MOVE unchanged and exact (MVS 0.000000015, MV20 all twenty digits). Golden
`pb60_numvalf_native_channels` (13 rows) matched first run; unit `NumvalFDoubleTests` (12 cases — the nearest
binary64 for seven shapes, comma mode, the shared reject, ±Infinity past binary64, and the decimal-identity
theory against the Int128 projection). Gates: Unit 2696 green across the intrinsic/Dec/scanner/traceability
filters · Conformance 1198/1198 (Intrinsic|Corpus|Arithmetic|Numeric|Move|Condition|Float|SpecTraceability) ·
characterization 33/33 · SpecTraceability 10/10. RV-15.69.4-2 → CONFORMS (GAP 4143 → 4142); PB60 at 13 of its
15 listed rows. Item 92 and the intrinsics design bullet carry the determination.

**Recorded honestly, not hidden:** the refuter's UNDERFLOW instance — `COMPUTE R = FUNCTION NUMVAL-F("1.2345E-10")
* 100000000000000` into `PIC S9(9)` giving 0 — still gives 0. It is the item-92/D1 native determination for every
approximated function inside a fixed-point arithmetic expression (the value lands at max(receiver scale, 9)
fraction digits before the multiply, exactly as `FUNCTION SQRT` would); a higher working scale is not free (the
D1 engine's per-operator scale sums would overflow the product). The row note says so. And a MOVE of a value
past the Int128 carrier at the receiver's scale — `MOVE FUNCTION NUMVAL-F("5E+30") TO PIC V9(9)` → 884105727,
the sentinel's low digits, exactly as `MOVE FUNCTION EXP10(40)` does — is a MOVE-landing defect shared with the
whole float family, registered as **PB77** rather than patched here.

