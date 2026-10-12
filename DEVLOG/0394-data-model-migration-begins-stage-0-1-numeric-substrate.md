## Entry 394 — Data-model migration begins: Stage 0/1 numeric substrate (CobolDecimal/NumProfile/CobolNum) + differential oracle; 2 oracle-surfaced legacy spec bugs fixed

The .NET-native data-model migration (ADR `docs/DATA_MODEL_ARCHITECTURE.md`, plan §0.5) starts with its **first
concrete slice** — the value-level numeric substrate, which ADR §13 names the riskiest correctness component and
mandates be **proven first with a differential oracle**. The owner re-confirmed the gating decision this session:
the numeric substrate is **`BigInteger`, not `decimal`**, for 19–31-digit values and all intermediates (ADR §12
Open Q#1, settled DEVLOG 393).

**New, purely additive (`src/CobolSharp.Runtime/Numeric/` — nothing in the compiler pipeline calls it yet, so the
guard stays green by construction):**
- **`CobolRounding`** — the eight ISO ROUNDED modes as an enum whose values equal the legacy `PicRuntime.Round*`
  int constants (so the typed↔byte cast is the identity while both pipelines coexist).
- **`CobolDecimal`** — an exact base-10 fixed-point carrier `Unscaled × 10^−Scale` backed by `BigInteger`: the
  substrate that holds COBOL's full 1–31 digit range and unbounded intermediates that `decimal` (28–29 digits)
  silently loses. Value semantics normalized across scale (a deliberately plain `readonly struct`, not a
  `record struct`, so `Equals` is by value), lossless `decimal` interop via `GetBits`, exact `+`/`−`/`×`, and the
  single rounding primitive (`RescaleTo`/`RoundedQuotient`) implementing all eight modes on the exact rational.
- **`NumProfile`** — the compact runtime numeric descriptor (digits / scale / leading-&-trailing-P / signed /
  truncation-discipline / length) the ADR §9 says the runtime should be handed instead of a full `PicDescriptor`
  per op; plus a `FromDescriptor` bridge used during the migration.
- **`CobolNum`** — `ScaleAndRound` + `TryStore`: scale → round (8 modes) → capacity-check (DigitCount /
  PackedDecimal `2n−1` / BinaryCapacity, signed two's-complement vs unsigned-magnitude) → SIZE ERROR, **never
  throws** (ON SIZE ERROR via the `TryStore` bool). The extracted-and-corrected successor to `PicRuntime`'s
  `ApplyScalingAndRounding` + `WouldOverflow` + the PROHIBITED guard.

**The Stage-1 differential oracle (`CobolNumDifferentialTests`, +`CobolDecimalTests`):** `TryStore` is proven
bit-for-bit identical to the legacy byte pipeline (reached via `AddNumericLiteral` into a zeroed receiver) across a
grid of **field × value × all 8 modes** — DISPLAY/COMP/COMP-3/COMP-5, signed & unsigned, leading-P & trailing-P,
and the 10–18-significant-digit mid-range — staying inside the legacy path's *faithful window* (≤18 significant
digits, the `long` boundary). Beyond it — 19–31 digits, the 8-byte **unsigned** COMP-5 range `(long.Max, ulong.Max]`,
the COMP-5 signed negative extreme, PROHIBITED — the decimal/long legacy path is common-mode blind (overflows /
decodes to zero / throws), so those are validated against an **independent `BigInteger` / two's-complement
reference**, exactly as the ADR prescribes.

**Two real legacy spec-fidelity bugs the oracle surfaced (ISO-cited, fixed, guard-gated, regression-tested):**
1. **`EncodeComp3` set the packed sign nibble from `value < 0` without consulting `IsSigned`** — so an unsigned
   `PIC 9(n) COMP-3` wrongly retained a negative sign (0x0D) and decoded back negative, while DISPLAY/COMP already
   stored the magnitude. ISO §13.18.40 / §14.9.25 GR8: an unsigned receiver stores the absolute value. Fix:
   `negative = pic.IsSigned && value < 0`.
2. **`WouldOverflow`'s COMP/COMP-3/COMP-5 arms omitted the `/10^TrailingScaleDigits` divide** the DISPLAY arm has,
   so a valid trailing-P value (`PIC 9(3)P` stores 3 digits → max 9990) wrongly raised SIZE ERROR. ISO §13.18.40:
   the overflow digit count is of the *stored unit count*. Fix: mirror the DISPLAY arm's divide in the three arms.

**Process (the project's proven pattern).** Implemented on `main` → **5-lens adversarial review workflow** (19
agents, find→refute) → triaged 14 raw findings: **8 confirmed — all oracle-coverage gaps** (P-scaling, 8-byte
COMP-5, the mid-range digit band, a doc threshold imprecision); **the production `CobolNum`/`CobolDecimal`/
`NumProfile` code and both legacy fixes were verified correct by every lens**; 6 refuted (BINARY-* misclassification
— invalid COBOL; float profile — documented bypass; unused operators — additive by design; etc.). Closed all 8 gaps
by completing the grid + adding the independent-reference and trailing-P known-answer tests, and corrected the
oracle's doc (the legacy faithful window ends at 18 digits, not decimal's 28–29). The review's value was precisely
catching the blind spots that would have let a future regression pass.

Guard **ALL GREEN: 1144 unit / 481 integration / 364 NIST** (+92 unit tests; the two byte-path fixes caused zero
NIST/integration regression). Next: ADR Stage 0 scaffolding continues — split `PicDescriptor` → `FieldShape` +
`NumProfile` (lossless rename, M6) and introduce the `IrDataSlot` sum type with `ByteWindowSlot`, then Stage 1
routes byte-island arithmetic through `CobolNum` behind the differential oracle.

