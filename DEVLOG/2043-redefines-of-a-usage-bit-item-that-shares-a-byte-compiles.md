## Entry 2043 — 2026-10-14 18:42 PDT — REDEFINES of a USAGE BIT item that shares a byte compiles, and a redefiner no longer splits a bit run (PB3166)

The repro `w1078e/p5.cob` (a GROUP-USAGE BIT group redefined by another, after a bit leaf) failed in the Roslyn
backend with CS0103 "'RSUB' does not exist". The re-probe showed the cause is wider than the note: any redefined USAGE BIT
item, leaf or group, that shares a byte with a same-level bit neighbour (ISO 8.5.1.6.3) did, and the same redefinition after
a character item compiled.

Mechanism 1. A Tier-B redefines class is one string backing holding the canonical's own bits (13.18.44.4 GR1) and the
canonical has no field of its own name. The bit-run law still counted it a run member, so `PhysicalModel.BuildPhysicals`
emitted the backing as a byte field beside the run's slice (an image one byte too wide) and `GroupImageCodec.BitCarrierOf` /
`EmitRunMemberFromBits` named the missing field. The backing is now the run member's field and both sites read and write
it (`BackingOf`, `CobolBits.ReadWindow` / `WriteWindow`), for the record `AsImage` / `FromImage` and a bit group's
`AsBits` / `FromBits` alike.

Mechanism 2, found in `BitLayout` while reading it. `RunsOf` ended a run at a redefining sibling and began another after
it, where the placement walk keeps its predecessor across the redefiner. `05 B PIC 1(6) VALUE B"101010". 05 B2 REDEFINES B
PIC 1(2). 05 C PIC 1(2) VALUE B"11".` therefore laid C at bit 6 for LENGTH and windows but imaged it a byte later, losing
its VALUE (C read 00, the byte 0xA8 instead of 0xAB). `RunsOf` now scans the non-redefining siblings only.

Goldens: `2002/pb3166_redefines_bit_item_in_shared_byte` (values derived from 13.18.44.1/.4 and 8.5.1.6.3, not from a run),
`negative/pb3166-redefines-bit-item-below-2002`; `BitRunImageDriftTests` rows BRD11 and BRD12. Design doc updated
(`COBOLNET_DATA_MODEL_DESIGN.md`, the run section). No inventory row claimed or closed. Sibling sweep: the OCCURS DEPENDING
holder, a table element struct and an FD record round trip were probed and agree.

