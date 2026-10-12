## Entry 806 — 2026-07-11 17:52 PDT — P7 Step 9l (BATCH-2b): FieldEmitter splits 5-ways into `DataDivision/` behind the `DataEmitter` facade

**What (the phase doc's 9l; the doc's four named classes + the 5th concern given its own home as the STATUS
ledger foretold).** `Emit/FieldEmitter.cs` (486 lines, five concerns) → `CodeGen/DataDivision/`:
`PhysicalModel` (the memoized physical-field model — the 5th concern; owns the linear-time cache),
`RecordStructEmitter` (types · profiles · index cells · root fields), `GroupImageCodec` (the AsImage/FromImage
pair + the Tier-B INITIAL-image composer), `GroupValueSlicer` (the §13.18.63 group-VALUE positional
distributor), `ValueInitializer` (VALUE/figurative/default initializers). The five are MUTUALLY RECURSIVE by
design (a field's Init is a VALUE initializer; a Tier-B backing's seed is an image init; both walk the physical
model) — the `DataEmitter` FACADE constructs and property-wires the cycle and forwards the public surface, so
the four ad-hoc construction sites (program class ×2, OO class/interface units) stay one-line
(`new DataEmitter(_ctx)`). Ratchet: the 24 redistributed EXACTLY (15+5+2+1+1+0); fragments route in the 9-final
sweep. One transcription hazard caught by the compiler: the anchor-slicer cut `RawValueAsFloat` at a `}` inside
its interpolated string — repaired; the battery proves the split byte-neutral.

**Verify.** Sln Debug clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact) —
verdicts read as separate actions.

