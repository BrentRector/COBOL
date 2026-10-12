## Entry 410 — S4: the FIRST numeric typed flip — an unsigned-integer DISPLAY item → a native .NET `long` (byte-identical); + owner reaffirms Stage 5

The numeric stage begins. A **standalone unsigned-integer DISPLAY** item with a VALUE (`PIC 9(n)`, no sign / V / P,
`n ≤ 18`) now flips to a native `.NET long` field. The byte-identity contract is subtler than character — a numeric
DISPLAY item's in-memory value (`long`) differs from its on-disk form (digit bytes), so the byte path's `DISPLAY`
returns the **stored digit bytes** (`GetDisplayString` numeric arm) and `MOVE`-literal stores `|value|` truncated to
the low `n` digits (`EncodeNumeric`). The typed path reproduces both **exactly**: `MOVE`-literal truncates the
compile-time literal (`|value| mod 10^n`) and stores the `long` with `ldc.i8`; `DISPLAY` formats via the new
`CobolNum.FormatUnsignedDisplay(long, n)` (the low `n` digits, zero-padded). VALUE init likewise truncates at
compile time. Gated by `EnableTypedFields` (default OFF → corpus byte-identical); restricted to items **with a
VALUE** because an uninitialized numeric field shows spaces on the byte path, which a `long` cannot reproduce
(deferred). Everything beyond MOVE-literal/DISPLAY (field MOVE, arithmetic, COMPARE) stays on the loud
`EmitLocationArgs` guard — arithmetic, the real numeric payoff, is the next (large) increment and is hard-gated on
the `CobolNum` differential oracle (already green).

Changes: `IrTypedFieldDef` gains `IsNumeric`/`NumericInit` (a `long` field + init); `Binder.CollectTypedFields` a
numeric flip branch; `CilEmitter` emits an `Int64` static field + `ldc.i8` init; `CilDataEmitter` numeric cells —
`EmitPicMoveLiteralNumeric` (compile-time truncate → store) and the numeric arm of `EmitDisplayOperand`
(`FormatUnsignedDisplay`). +9 unit tests (`CobolNumFormatTests`, incl. a self-caught fix: the helper renders the
unsigned magnitude of the low digits, not a modular residue, for a defensive negative input) + 1 flip differential
test (VALUE init, high-order truncation, fraction truncation, DISPLAY — byte-identical).

**Owner decision (this tick):** reaffirmed the ADR's **full Stage 5** — the Roslyn C# backend with Cecil as a
byte-exact oracle, default-backend choice deferred (ADR §12 Open Q#2). Cecil/direct-IL remains the primary backend
throughout regardless; the C# backend is additive/later. (The question was whether to drop it / make it
inspection-only given the `Microsoft.CodeAnalysis` dependency + double-compile cost; owner chose to keep the full
optionality.)

Autonomous /loop tick, [[feedback_continue_dont_wait]]. Guard **ALL GREEN: 1196 unit / 490 integration / 364
NIST**. NEXT: numeric **arithmetic** (ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE on typed `long`/`decimal` via
`CobolNum`, byte-identical — the large numeric increment), then numeric field-MOVE/COMPARE, then signed/scaled
variants, then OCCURS.

