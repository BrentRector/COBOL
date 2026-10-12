## Entry 184 — 2026-05-28: INITIALIZE must initialize every OCCURS occurrence — NC201A 100% (21→ remaining)

**Session setup:** the build was blocked by `NU1507` — a `demeanor` GitHub Packages
source had been added to the *global* NuGet config since the last session, and with
central package management + `TreatWarningsAsErrors` two unmapped sources are an
error. Added a repo-local `nuget.config` that `<clear/>`s inherited sources and pins
nuget.org (CobolSharp's only feed), making the build reproducible without touching
global state.

**Bug (NC201A PFM-TEST-F4-24, "manipulating subscripts", GR10(d)):** a
`PERFORM ... VARYING PFM-F4-24-A(S1) FROM 10 BY PFM-F4-24-C(S2) UNTIL ...` whose body
mutates S1/S2. Expected S1=4, A(4)=80; got S1=2, `COMPUTED=8224`. `8224` = `0x2020`
= two ASCII spaces read as an `Int16`.

**Root cause** — `INITIALIZE FILLER-A` failed to zero the `PIC S9(3) COMP OCCURS 10`
array. `DataMovementLowerer.InitializeDataItem` resolved the *whole* 20-byte array as
one location and emitted a single `IrPicMoveLiteralNumeric`; the COMP encoder handles
2/4/8-byte widths only, so 20 bytes fell through and the storage kept its space fill.
With `A(2)/A(3)` left at 8224 (> 70) the VARYING loop exited after one iteration. (The
augment and runtime subscript re-evaluation were correct all along — disproving the
session-state's "COMP subscript multiplier" theory.)

**Fix** — Made INITIALIZE OCCURS-aware (ISO 1989:1985 14.x: INITIALIZE applies to
*each* occurrence of a table element). `InitializeDataItem` now carries a subscript
path: when an item (group or elementary) has `Occurs.MaxOccurs > 1` it iterates
1..MaxOccurs, recursing per occurrence; the elementary leaf is resolved per-occurrence
by synthesizing a constant-subscript `BoundIdentifierExpression` and reusing
`LocationResolver`'s compile-time offset folding (which also narrows the PIC to a
single element). Handles nested/multi-dim OCCURS and group OCCURS uniformly.
NC201A: 2→0 FAIL*, now 59/59. Guard green.

---

