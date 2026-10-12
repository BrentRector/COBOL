## Entry 414 — S4: numeric ARITHMETIC on typed `long` fields — ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE/REMAINDER (byte-identical)

The large, byte-identity-critical increment: COBOL arithmetic now runs natively on typed-native numeric fields. The
byte arithmetic runtime (`PicRuntime.AddNumeric`/`MoveAccumulatedToField`/`MultiplyNumeric`/`DivideNumeric`/
`ComputeCobolRemainder` …) is a **read-modify-write on a byte window**: the receiver array is passed and mutated in
place. So a typed receiver needs two new bookends around the unchanged runtime call:

- **prologue** (`EmitTypedNumericReceiverPrologue`): allocate a scratch `byte[width]`, encode the field's current
  `long` into it via the SAME codec the byte field uses (`EncodeNumeric`), and push `(scratch, 0, width, pic)` as the
  receiver args. Materializing the *current* value is what makes a read-modify-write receiver (`ADD x TO y`) correct,
  and it's harmless for a write-only `…GIVING` receiver (the op overwrites it) — so one uniform prologue serves both.
- **epilogue** (`EmitTypedNumericReceiverEpilogue`): after the op has written the result into the scratch window,
  `DecodeNumeric` it back to a `long` and store into the field. The decoded value is the field's truncated digit
  image (< 10^digits) — exactly what the byte path holds — so DISPLAY and downstream arithmetic stay byte-identical.

A single `EmitWithReceiver(dest, emitRest)` wrapper in `CilArithmeticEmitter` applies this: for a typed numeric dest
it brackets `emitRest` with prologue/epilogue; for a byte dest it emits the destination args and `emitRest` exactly as
before — so the (flag-OFF) corpus byte path is **unchanged verbatim**. Applied to all twelve op cells (PicAdd/Sub/
Mul/Div + their *Literal forms, the three Accumulated-to-target ops, ComputeStore, and — inlined, because its dest is
pushed mid-arg-stream — CobolRemainder). Senders switched from `EmitLocationArgsWithPic` to the materializing variant
in the arithmetic ops **and** in the two value-producing leaves that feed user arithmetic: `IrAccumulateField`
(`ADD A TO B` accumulates A) and `IrLoadNumeric` (COMPUTE/GIVING expression operands). Factored the shared
"materialize current numeric → scratch" out of the sender path so the prologue reuses it (DRY).

New flip test `NumericField_Arithmetic_ByteIdentical` (ADD…TO, SUBTRACT…FROM, MULTIPLY…GIVING, DIVIDE…GIVING,
COMPUTE with typed operands, DIVIDE…REMAINDER) — **12 flip tests, all flag-on≡flag-off**. Guard **ALL GREEN: 1196
unit / 492 integration / 364 NIST**. With this, the first numeric class (unsigned-integer DISPLAY) is fully native
across VALUE/MOVE/DISPLAY/COMPARE/class-condition/arithmetic. NEXT: widen the classifier's numeric flip beyond
unsigned-integer-with-VALUE (signed + scaled → `decimal`), then OCCURS → `CobolTable<T>`.

