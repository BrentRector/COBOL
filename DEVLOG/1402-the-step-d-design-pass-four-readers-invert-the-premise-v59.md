## Entry 1402 — 2026-08-30 14:46 PDT — The Step D design pass: four readers invert the premise — V59 already built the byte windows, and what remains is a widening with its lanes, its spec-required Tier-D arm, and three live defects found on the way

PB164's last codegen half was framed for two phases as "the confined byte[] codec" — CobolByteImage, a
TierCWindow with Read/Write, a GroupImageCodec byte path. The deep-dive itself said Step D "NEEDS
RE-BASING against reality before implementation," so before writing code I ran a four-reader
understanding scout (classification/layout, the window lanes, the observable blast radius, the spec
derivation — 4 agents, each returning file:line-referenced findings). Their unanimous headline: THE
PREMISE IS STALE. Since V59, a Tier-B class's geometry is byte-form end to end — ElementaryImageWidth
returns StorageWidth for every pinned NumericByteForm, the offsets and the class width advance in those
units, the backing seeds from byte images, and a promoted member reads/writes through
FormatImage/ParseImage windows, pinned by RedefinesClassificationTests and the v59_byte_image golden.
The stale sentence at the top of ComputeTier ("still models windows over the ZONED digit-image
representation") contradicted the StoreAsImage loop's own comment twelve lines below it, and it is
almost certainly where the byte-codec framing came from — a stale comment supplying a false premise to
a design, exactly the failure its own file warns about.

The spec reader made the derivation one-directional: §13.18.44.4 GR1 associates storage AT THE BIT,
GR2 grants every entry's name unconditioned reference to it, §13.18.60.4 GR2 makes representation a
function of the USAGE clause alone, §13.18.44 and Annex A.2 hold no undefined-result escape, and
§14.6.13.2 r2's EC-DATA-INCOMPATIBLE presupposes the alias wrote the item's real storage. One
representation per item, REDEFINES views included — which CONFORMANCE items 205/208 already promise.
It also caught §12.4.6.4.4 GR2 being cited for the wrong proposition (the clause derives same-record-area
FROM redefinition, and its only representational content argues FOR bytes) and a PHANTOM clause —
§8.8.4.1.1, cited 18 times including inside the design SSOT's own TOTAL-rule premise; §8.8.4.2.3 SR2 is
the real rule (registered PB182).

The observers reader measured the blast radius instead of estimating it: 1457 corpus files swept with a
proper tokenizer — exactly ONE explicit observer of a non-DISPLAY REDEFINES window (v59_byte_image,
which is byte-identical under the plan and becomes the regression pin), one SAME-RECORD-AREA carrier
(ST134A, green), zero corpus pressure on the Tier-C arm, and 507 multi-01 FD/SD implicit classes none
of which yet carries a non-DISPLAY leaf — the latent blast radius that gets its own golden. And the
scout surfaced three LIVE defects on shipping Tier B, registered before this entry: PB179 (pointer/
object leaves classify Tier B with ZERO-WIDTH windows — §13.18.44.3 SR12/SR14 require the missing
Tier-D rejection; ComputeTier's "no such items exist" comment was stale), PB180 (ACCEPT stores
DisplayTextWidth device characters into a StorageWidth byte window), PB181 (the CALL boundary reads a
byte-form window through the text reader, corrupting BY REFERENCE aliasing over class members).

The re-based Step D is now WRITTEN in DESIGN-data-model §2.3 as an ordered, implementable plan:
Tier-D first, the live lane defects, then dissolve ComputeTier arm 1 with the float arm-order /
unsigned-wide / Length-0 seed lanes completed (each with a byte-level pin — the Tier-B mark has been
dead behind the reject and is unproven), the second gate widened deliberately (ForceStringCanonical —
the two-arm shape's eighth instance), NATIONAL rejected with the corrected reason, the truth sweep, and
hand-authored discriminating goldens for a corpus that supplies none. ComputeTier's stale comment is
corrected in this change set. Implementation is the next campaign; the design is the deliverable here.

