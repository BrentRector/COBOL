## Entry 1398 — 2026-08-30 12:50 PDT — PB164: DISPLAY of a variable-length group — the documented format is the LENGTH sum's geometry, and the fleet makes the claim honest

GR7 hands the implementor the format; A.1 item 57 makes documenting it REQUIRED. The determination that
costs nothing to remember: a variable-length group displays as what it IS right now — members' images in
order, dynamic members at their current extent — the same extent FUNCTION LENGTH sums under §15.50.4 r7.
The mechanics reuse everything that exists: CurrentImage() emits as AsImage()'s sibling, fixed members
through AsImageOf (the ONE member-image law), a new CobolDynTable.CurrentImage over the same occurrences
SET manages, the dynamic-length carrier read verbatim. One real refactor: IsImageCapable split into the
dynamic-axis guard and ElementImageCapable (the shape half), so a dynamic table's ELEMENT struct gets the
per-occurrence image the composer concatenates — additive, methods emitted superset of methods used.

The landing took three rounds of adversarial checking, and each round earned its keep. The WAVE GATE went
red on pb62 — a fixed-OCCURS nested variable-length group my recursion admitted, emitting a scalar
CurrentImage() call on an array field. My fix discriminated by `Occurs is null` — and the REVIEW FLEET's
CRITICAL (refuters reproduced it with the built compiler) was that a Format-4 dynamic-capacity table ALSO
carries Occurs null, so the same arm re-admitted the dynamic-table-with-runtime-length-element shape and
produced uncompilable C# on legal source that never even referenced the group. The right discriminator
was the mechanism, not the field: `!IsDynamicTable`. Four more confirmed defects fell out: an OCCURS
DEPENDING member rendered at MAXIMUM where LENGTH counts CURRENT — excluded loud, and the exclusion is
architecturally forced (a struct-scoped composer cannot reach an ODO counter declared outside the group;
the LENGTH sum reads it through the operand's access path); a NATIONAL member falsified the "LENGTH(G)
equals the displayed width by construction" claim (LENGTH counts the national member's two bytes per
position, the image displays one character) — the CLAIM was corrected everywhere and the golden gained a
national member so the L = width + 2 relationship is observed, not assumed; and TierCIsland.Reason's
derived offender mis-blamed in BOTH directions (the dynamic member when an INDEX leaf was the blocker —
a false remedy; INDEX for an item that IS a dynamic table) while promising "DISPLAY alone renders" inside
DISPLAY's own failure message — now imageless-leaf-first, the item's own axes checked, no promises.

The probes then surfaced PB176 — a group with BOTH an ODO table and a dynamic member fails BACKEND
compilation (the ODO sender calls AsImage() on a struct that never gets one) — proven PRE-EXISTING at
4006248f by stash/build/probe/pop, registered rather than silently absorbed. Wave gate GREEN twice over
(Conformance 1333, Unit 5027, characterization 33); GR-14.9.11.4-7 NOT-IMPLEMENTED → PARTIAL with its
residues named and loud; rows -4/-6 narrowed to USAGE INDEX only. PB164's remaining scope: the
mixed-usage REDEFINES codec and the USAGE INDEX owner decision. Battery #36 accrues (PB168 + this).

