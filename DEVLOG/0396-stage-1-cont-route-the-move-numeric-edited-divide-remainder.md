## Entry 396 — Stage 1 cont.: route the MOVE / numeric-edited / DIVIDE-REMAINDER paths through CobolNum; retire the legacy decimal rounding logic

Completes the value-level numeric scale/round migration. `PicRuntime.ApplyScalingAndRounding` — the helper behind
the remaining non-arithmetic paths (numeric MOVE, MOVE-to-numeric-edited, ACCEPT-to-numeric, MOVE-of-literal,
DIVIDE-REMAINDER; 8 call sites) — now delegates to `CobolNum.ScaleAndRound` (the BigInteger core). Its former
inline decimal logic, and the now-orphaned `RoundToIntegerByMode` + `NearestTowardZero`, are removed. So **every**
value-level numeric scale/round in the runtime now flows through `CobolNum`: arithmetic stores via `TryStore`
(Entry 395), MOVE / numeric-edited / remainder via `ScaleAndRound` (this entry). `ApplyScalingAndRounding` survives
only as a 3-line `decimal → CobolNum → decimal` adapter, so the 8 byte-path call sites are unchanged; it does no
capacity/SIZE-ERROR check (those paths truncate on encode — the arithmetic path owns SIZE ERROR via `TryStore`).

This landed green on the **first** guard run — the Entry-395 sign-layering correction is exactly why: `ScaleAndRound`
returns the signed rounded value, the MOVE-to-plain paths keep their existing caller-side `Math.Abs` for an unsigned
target, and the numeric-edited paths keep the sign for `FormatNumericEdited` to render. (The `.ToDecimal()` on the
result throws only for degenerate >28-digit-scale fields — exactly where the former `value × 10^scale` decimal
multiply also overflowed, so behavior is unchanged and the test corpus has no such field.)

Guard **ALL GREEN: 1144 unit / 481 integration / 364 NIST** — no regression; the entire NIST MOVE/edited/remainder
corpus is now byte-identical through `CobolNum`. Next: Stage 0's `PicDescriptor` → `FieldShape` (compile-time) +
`NumProfile` (runtime) lossless split (ADR M6), then the `IrDataSlot`/`ByteWindowSlot` + `Span<byte>` scaffolding.

