## Entry 399 — Stage 0: the character substrate `CobolString` + its differential oracle (the typed-string analogue of `CobolNum`)

With the classifier complete (Phase A+B+C, Entry 398), the migration moves to Stage 0 — the runtime
scaffolding the first Stage-3 typed flip will consume. Character data is the first, cheapest flip (ADR §10),
so the first Stage-0 substrate is the typed-string counterpart of the numeric `CobolNum`: **`CobolString`**
(`src/CobolSharp.Runtime/Text/CobolString.cs`), and — exactly as for `CobolNum` (Entry 394) — it lands with a
**differential oracle** proving it byte-identical to the legacy path *before* anything wires it in. Purely
additive (nothing in the pipeline calls it yet), so the guard stays green by construction.

`CobolString` carries the two responsibilities a typed `string` field needs (ADR §1.2 / §2.5):
- **The COBOL alphanumeric value semantics over `System.String`** — `Store(value, width, justifiedRight)` is the
  receiving value of an alphanumeric MOVE (ISO §14.9.25): exactly `width` character positions, space-filled or
  truncated, left-justified by default / left for JUSTIFIED RIGHT (ISO §13.18.36). `Compare(left, right)` is the
  alphanumeric relation condition with the shorter operand space-extended (ISO §8.8.4.1.2), **ordinal** by
  character value, never the BCL's culture-aware default (ADR §1.2.1 guardrail 1).
- **The `IDataSlot` boundary codec** (ADR §2.5) between a typed `string` and a byte window, under the **Latin-1**
  convention (byte k ↔ U+00kk, ADR R10): `FromWindow` decodes (no trim — raw field content), `ToWindow` encodes
  (one byte per position, space-fill remainder). This matches the existing byte path exactly — `StorageHelpers`
  lays one low byte per character and `CompareFieldToField` decodes with `Encoding.Latin1`.

**The oracle (`CobolStringDifferentialTests`, +9 tests).** MOVE is proven byte-identical to
`StorageHelpers.MoveStringToField` / `MoveStringToJustifiedField` over a grid of source patterns (incl. binary /
LOW-VALUE / HIGH-VALUE / embedded spaces) × receiver widths × {left, justified-right}; `FromWindow`∘`ToWindow`
is proven the identity. Comparison is proven **sign-identical** to `StorageHelpers.CompareFieldToField` over a
grid of base strings × trailing spaces.

**One honest nuance the oracle made me confront (and a deliberate scoping call).** The legacy
`CompareFieldToField` decodes then calls `string.TrimEnd()` — which trims *all* Unicode whitespace — before an
ordinal compare. COBOL's relation condition space-extends the shorter operand with the **space character only**
(`0x20`, ISO §8.8.4.1.2). So on operands with a *non-space* trailing byte (NUL / control), the legacy path and
the COBOL-correct rule diverge (e.g. `"AB" + LOW-VALUE` vs `"AB"`: legacy treats the trailing NUL as making the
left operand greater; COBOL space-extends the right to `"AB "` and `0x00 < 0x20`, so the left is *less*).
`CobolString.Compare` implements the COBOL-correct `0x20` extension directly; the differential grid therefore
restricts trailing whitespace to the COBOL space (where the two agree), and five known-answer `[Theory]` cases
pin the COBOL-correct extension. I deliberately did **not** touch the live `CompareFieldToField` in this slice:
the substrate is additive and unwired, the NIST corpus is green on the current behavior, and the two paths are
reconciled when Stage 3 actually wires the typed comparison (the wiring-time differential check forces it then).
Recorded here so the divergence is visible rather than silently enshrined.

Guard **ALL GREEN: 1184 unit / 481 integration / 364 NIST** (+9 unit; additive — no codegen/NIST change). Next
Stage-0 slices: the `IrDataSlot`/`ByteWindowSlot` IR sum type + `Span<byte>` adapters and the
`PicDescriptor`→`FieldShape`(compile)/`NumProfile`(runtime) split (additive parallel `NumProfile`, per the
investigation), then wire `RecordClassificationPass` into the Binder (no-op until the first flip), then the first
character-data typed flip (PIC X → `string`) consuming `CobolString` at the typed↔byte boundary.

