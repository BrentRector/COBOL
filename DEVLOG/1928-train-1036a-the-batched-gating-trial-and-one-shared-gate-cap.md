## Entry 1928 — 2026-10-07 20:28 PDT — Train 1036a: the batched-gating trial and one shared gate cap (PB2514, PB2515, PB2523); PB2151 completes the legacy retirement

**Gate throughput — PB2514, PB2515, PB2523 (landed).** The implementer gate cap was read per process from
`COBOLNET_GATE_SLOTS`, so at 16:13 PDT gates holding caps of 1 and 3 shared one FIFO queue and head-of-line blocked each
other, and `gate_slot.py status` showed `1 of 1` while three gates held slots. The cap is now ONE shared setting,
`<common dir>/cobol-gate-slots/settings.json`, written only by `gate_slot.py set-cap N --until ISO --why --by` (locked,
atomically replaced) and re-read on every poll; an expired setting falls back to `DEFAULT_SLOTS = 1` and a malformed
one stops the gate `NOT RUN`. `COBOLNET_GATE_SLOTS`, `configured_slots` and `--slots` are deleted with every caller.
The owner's batched-gating trial (16:25 PDT, "Yes, trial until Saturday") is the shared implementer scope `leg1`: while
it is set (`set-implementer-scope leg1 --until …`, expiring Sat 2026-10-10 10:00 PDT), an implementer gate runs leg 1
only, names every leg-2 case NOT RUN and prints `LEG 1 ONLY (batched-gating trial, PB2515): GREEN|RED`, so a plain
`GATE: GREEN` match never fires. The lander ignores the scope; its whole-population gate is each change's population
check, a red train is attributed per cluster (MANDATORY-PRACTICES L12), and `scripts/orchestrator/train_measure.py`
records each train for the decision. PB2523 runs the audits first: in implementer mode a red audit ends the gate
before the build. CLAUDE.md's Testing paragraph and the `cobol-implementer` agent now describe the trial (the owner's
decision covers those operator edits). The cap-3 measurement is in PB1720: wave 1034 ran 16 implementer gates at cap 3,
10.5 gates/h against about 9.5 at cap 1, mean wait 8.9 against 10.2 min, mean run 6.1 min; 8 of 16 were red first
run and 4 of those were audit-only (3 DRIFT RULES INDEX, 1 DOC CITATIONS) after a full run, none contention-shaped. The
host is CPU-bound, so the extra slots buy little; PB2523 is what removes the audit-only reruns. PB2522 is carried
unfixed. Pinned by `GateSlotDriftTests`, `GateLegDriftTests` and `TrainMeasureDriftTests`.

**PB2151 — D10's second half; the legacy retirement is complete (landed).** No position travels between the binder and
code generation as C# text any more. Every subscript, reference-modifier position, view and bit offset, cell component
ordinal and ADDRESS OF displacement is a typed `Position`, and one `PositionOffset(origin, (index − 1) × stride terms)`
node carries the offset law; only `CodeGen.PositionRenderer` renders it. The design was corrected in
DESIGN-binder-bound-tree section 3.9: the origin is a general `Position`, because the cursor and CORRESPONDING origins
(`k + (o) − e`, `L − e + c`) are not `Displacement + long Constant`. `RenderSegment`, the D18 fragment re-parse, the
digit-string hoist test, the SR6 proof's text evaluator `ConstIndex` and the binder's last render call are deleted.
`PositionCarrierDriftTests` keeps them out. With PB2151 the retirement cluster PB2108 is fully landed, 10 of 10.
**The train merge.** Main's PB2094 had added a string component-ordinal base (`CellOrdinalBase`, `" + CobolPtr.DynBaseOf(p)"`)
to the window walk and the whole-table cursor while PB2151 was in flight. It is now the typed node `PositionPointerDynBase`,
composed into the ordinal origin by `ReferenceResolver.OrdinalOrigin` and carried by the walk's scope and by
`ViewCursor.OrdinalBase` until a dynamic-capacity element cell opens its own scope. It renders the same text, and the
design section names it. PB1071's slot-write carrier type, PB244's composition use and PB2094's area base were kept over
rendered positions.

**Oracle.** `compare_oracle.py` against 9879d9e40069: DIFFERENT, 18 of 7,476 cases, emitted C# only, in PB2151's two
explained classes and none other (classified mechanically): an integer subscript written `+N` or with leading zeros
renders as its value (NC132A–NC245A, the pb2113 corpus), and a D18 temporary is named for its source line rather than
`_L1` (pb17_function_subscript). Re-recorded as 84a4542e6dd8.

