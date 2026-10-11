## Entry 1406 — 2026-08-31 00:54 PDT — PB164 lands WHOLE: the arm fell, the lanes were the work — and a wrong answer hiding inside the wave's own new code

For four waves this note carried a remaining half called "the mixed-usage REDEFINES codec". The design
scout inverted the premise before a line was written: V59 had ALREADY made Tier-B REDEFINES windows hold
true bytes. There was no codec missing. There was a rejecting arm standing in front of one that already
worked, and a handful of emit lanes that had never been asked to serve a windowed float, a windowed
unsigned-wide COMP-5 or a windowed index. So the landing is a DELETION plus lane completion:
`ComputeTier`'s float/COMP-5/BINARY-*/INDEX reject is gone, `RedefinesTier.ByteCanonical` and
`StorageForm.TierCWindow` are gone (zero fan-out, confirmed by build), and `ForceStringCanonical` — the
second gate, the EXTERNAL/BASED/ADDRESS-OF path — now admits every `HasImageByteForm` numeric. NATIONAL
is refused with the honest RESIDUE-11 reason instead of a stale one; USAGE BIT and the pointer/object
classes stay out because they genuinely have nothing to put in a byte cell.

The lanes were the actual labour: `NumericRenderer`'s windowed-float arm ordered ahead of the native
float arm, the windowed unsigned-wide arms (`ParseImageU` / `ParseImageU128`, new in `CobolNum` and
`RuntimeApi`), float-receiver stores in `ArithmeticEmitter` and `MoveEmitter` re-encoding through
`NumFormatImageFloat`, float seeds through the ONE `RawValueAsFloat` recipe, the canonical promotion
widened in `StorageFormPass`. And `SetEmitter` had TWO windowed-index arms, not one — `StoreSetTarget`
and `AugmentSetTarget` — the second found only because the golden probe threw CS1503 on a raw long going
into a window. The two-arm dispatch shape, for the sixth recorded time.

The gate's first run went red on exactly one fixture, and it was the right red. The negative
`pb151-based-comp-leaf` pinned the OLD posture — a BASED record with a COMP leaf draws COBOLNET1695 — and
the second-gate widening makes that shape WORK. So the fixture was pinning a rejection of legal source:
§13.18.5.3 SR1/SR2 restrict a BASED subject only to "shall not be of class object" and "shall not be a
dynamic-length elementary item or a variable-length group", and §14.9.3.3 SR1 asks only for the BASED
clause. It was deleted and replaced by a POSITIVE golden, `2023/stepd_based_comp_leaf`, which allocates a
COMP + COMP-3 + COMP-1 record, windows the same cell through a second BASED `PIC X(8)` entry, and reads
all eight bytes with `FUNCTION ORD` — FF FE, 12 3D, 3F C0 00 00 — then writes bytes back through the X
view to prove the codec is an involution. Deleting a negative fixture costs something, though: COBOLNET1695
lost its only witness, and a diagnostic with no witness is how a residue boundary moves unobserved. Hence
`pb164-based-national-leaf`, a BASED record with a `PIC N(4)` leaf, which is exactly where the residue
really is.

The owed review fleet then ran a full round over the diff. Twenty-five confirmed findings collapsed to
eleven distinct defects, all fixed in-change; twelve were killed on refutation. Five probes were written
and all five reproduced before anything was touched. Refuter corrections were binding: F1's real root was
`OperandText.NonTextBytes`, not the arm the finder named, and its live sibling `RedefViewPlace` would have
been left broken by a fix at the reported site; F1's MOVE/compare/INSPECT triggers were REFUTED and
deliberately not "fixed"; F2's callers were refuted down to `FUNCTION CONVERT` alone and its clause
corrected to §15.19.3 r7; F4's severity was corrected DOWNWARD by its refuters, because ISO mandates no
initial value for a plain EXTERNAL item (§11.9.10.4 GR7; §14.6.2.3.3 "undefined"), so what looked like a
wrong answer was a consistency defect. And one finder's corroborating citation was FABRICATED — it praised
a GR4 reference that does not exist in the golden it named — which is a reminder that a fleet's
agreement is evidence only after the agreement is read.

The wave's real catch came in through a finding filed as MINOR. Its second half asked whether the
`FormatImage` / `ParseImageU128` pair round-trips a 16-byte UNSIGNED item at or above 2^127. It does not:
`FormatImage(UInt128)` funnelled into the SIGNED lane, whose §14.9.25.4 GR6b absolute-value rule then
negated a REINTERPRETED BIT PATTERN. An all-`X"FF"` 16-byte window read back as `…0001` instead of
2^128−1, and the re-encode did not return the bytes it was handed. That is a silent wrong answer, in code
this very campaign added two waves ago, found by asking a round-trip question rather than by running a
round-trip test — a self-inverse test would have passed. Fixed with a `BinaryBytes` helper that keeps the
unsigned lane unsigned, pinned at the three values that discriminate it (2^128−1, 2^127, 2^127+1), and
observed in both directions by the golden's UW/UB legs.

Two structural results are worth more than their findings. F4 became a one-seeder consolidation:
`CallInitialImage` now routes byte-form leaves through `GroupImageCodec.ImageInitOf`, and a THIRD copy of
the same seeding in `PtrEmitter` was deleted — `ImageInitOf` with `useValues` is THE seeder. And F8, the
hand-rolled `{ Category: Numeric, IsFloat: false, Usage: Display }` union, was not merely widened where it
was wrong (`UdfBinder`'s group-RETURNING screen drew COBOLNET1510 on a COMP-5 group leaf — probed first,
then fixed to `ElementImageCapable`, and the function now returns `N=[0009] T=[ABC]`); every remaining
copy was CLASSIFIED and the classification LOCKED. `DisplayUsageUnionDriftTests` inventories all 21
occurrences across 9 files with the reason beside each: seven are spec-required (§8.4.3.3.3 SR1's
reference-modification restriction, §14.9.48.3 SR4's UNSTRING receiver), ten are load-bearing because the
question really is "is this zoned DISPLAY?", three are out-of-wave and registered, and one is an open
derivation registered rather than swept. A new copy can no longer appear unnoticed, and a removed one
forces a decision.

The verdict pass flipped NOTHING, and that is the honest outcome. GR-14.9.3.4-3 was re-adjudicated and
deliberately HELD at PARTIAL: GR3 asks for "the number of bytes required to hold an item as described by
data-name-1", and for a record carrying a NATIONAL, USAGE BIT or pointer-class leaf no such number is
computed at all — a national position is two bytes over a byte-addressed cell, which is a GR3-shaped
residue, not somebody else's. The R40 precedent governs: a named residue keeps PARTIAL. GR-14.9.3.4-9's
residue was re-stated as the pointer-class leaf specifically rather than "PB164's cell model", and
GR-14.9.11.4-4 gained a second named residue. GAP stays 3659. CONFORMANCE.md items 207, 211 and 208 now
say the landed truth: a REDEFINES view over a float / INDEX / COMP-5 item reads and writes the same bytes,
because the item's window IS its storage (§13.18.44.4 GR1/GR2).

Registered on the way out, notes before narrative: PB184 (a group VALUE is not distributed into byte-form
leaves — `01 GV VALUE "40537". 05 GB PIC 9 COMP-5 OCCURS 5.` leaves every occurrence zero, where
§13.18.63.4 GR5 initializes the area "without consideration for the individual elementary or group items
contained within this group"), PB186 (a latent signed decode of an unsigned wide sort key, preserved out
of four findings the refuters otherwise killed), PB187 (the OO universal-crossing reconciler), PB188 (the
CCVS-leniency seed's digit-count-versus-byte-width mismatch), PB189 (the subscripted dynamic-element
DISPLAY gap, promoted out of a prose paragraph where `work.py next` could never see it), PB190 (an
inventory row holding a GAP open on three shapes two landings appear to have closed — to be MEASURED, not
deduced), and a fourth missing screen appended to PB177. Chasing PB184's citation found that the phrase
"without consideration for the individual elementary items" appears in NO clause of the standard: both
real occurrences read "elementary or group items", and the paraphrase has been inherited across a dozen
sites. The one in `GroupValueSlicer` — the file this wave's finding lives in — was corrected here; the
rest belong to PB182's sweep.

One friction, logged because it cost real work: `scripts/hooks/fleet_active_build.py` denies every
`dotnet` verb to a subagent working alone, because its own-transcript exclusion never matches an agent's
`agent-*.jsonl`. That is the THIRD self-block mode of the same guard after PB101 and PB103, it hit two
agents in this session, and because a caller writes its transcript on every tool call the 120-second
window never clears — the block is permanent, not transient. So this landing's inventory-drift check and
one three-shape probe could not run; they are named in PB185 and in PB190 rather than quietly skipped. The
guard is fail-open by construction everywhere else; this is its one fail-closed path, and a third bolted-on
string comparison is not the fix.

Wave gate GREEN on the landed tree: Conformance 1429/1429 · Unit 5035/5035 · characterization 33/33
(filter `~Corpus|~Negative|~Redefines|~TierC|~Record|~Move|~Call|~Image|~Physical|~Storage|~Drift|~V59|~External|~Based|~Accept|~Set`).
PB164 closes WHOLE — the island's final boundary is variable-length groups and the pointer/object-class
leaves, which have no character image to give. Battery #38 accrues: PB164 arm-1 plus the fleet fixes F1–F11.

