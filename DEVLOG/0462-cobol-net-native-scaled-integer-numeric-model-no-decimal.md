## Entry 462 — COBOL.NET: native scaled-integer numeric model (no decimal/BigInteger) — owner-directed

Three owner corrections reshaped the numeric core: (1) arithmetic must operate on ALL COBOL representations per ISO;
(2) don't use BigInteger by default; (3) `decimal` is software, not hardware-native. The resulting model — locked in
`docs/COBOLNET_ARCHITECTURE.md` + memory `feedback_complete_dotnet_migration_no_byte`:

**Every COBOL fixed-point datum is a native `long` holding its UNSCALED value** (all digits; the decimal point is
compile-time scale metadata) — the literal COBOL definition of fixed-point. So arithmetic is hardware integer math,
and the DISPLAY image (unscaled digits, zero-padded, no point) falls out for free. `COMP-1`/`COMP-2`→`float`/`double`;
`COMP-5`→native int by width (binary-wrap). Escape hatch for 19–38-digit pictures = the fixed-size `Int128` value type
(far cheaper than `BigInteger`); `decimal`/`BigInteger` essentially never used. The key reframe (advisor): representation
changes only the capacity discipline + external byte image, not the in-memory value — so it's 3 models, not 6.

`CobolNet.Runtime` is now native-`long`: ported `NumProfile` (digits/scale/sign/usage→capacity: DigitCount / Packed /
BinaryCapacity) + `CobolRounding` (8 ISO modes); a new `CobolNum` with `Rescale` (scale-align), `Store` (scale→round→
truncate-to-capacity→sign), `Divide` (scaled quotient at a working scale), `FormatDisplay`/`FormatUnsignedDisplay`,
and the 8 modes via integer round-divide. The earlier BigInteger `CobolDecimal` port was backed out per the directive.

The emitter is now scale-tracked: a `NumX(expr, scale)` flows through `Num`/`Combine`/`Fold` — `+`/`−` align to the
larger scale (exact widening), `*` adds scales, `/` produces a quotient at the receiver's scale; every store routes
through `CobolNum.Store(value, scale, profile)` (one canonical path). MOVE/COMPARE/DISPLAY rescale via the same engine;
each numeric item emits a static `NumProfile`. Verified end-to-end incl. a scaled-money program: `PRICE=01250` (12.50),
`TOTAL=0005000` (12.50×4=50.00), `AVG=01666` (50.00÷3 truncated=16.66), scale-aligned `IF PRICE > 10.00`. Integer
tests unchanged. Deferred (coverage-grows): ROUNDED-phrase wiring, ON SIZE ERROR, signed-DISPLAY overpunch, COMP-5
wrap, float-mixed arithmetic, `Int128` widening.

