## Entry 1071 — 2026-07-28 04:05 PDT — Figure D.3, where the border weight IS the notation

D.3 is drawn. It is not a flowchart, which is why neither existing generator covered it: it is a nested
schematic in which the BORDER WEIGHT carries the meaning. The printed figure distinguishes four entities purely
by how their rectangle is drawn and says so in its own Legend, and box-drawing happens to have exactly those
weights — `┏━━┓` heavy for a compilation group or run unit, `┌──┐` light for a unit, `┌╌╌┐` dashed for an
element. So the distinction survives rather than being described in a note.

Everything the figure says survives with it: the containment chain (compilation group ⊃ compilation unit ⊃
source unit ⊃ source element), the example hierarchy P-1 / P-1-1 / P-1-2 / P-1-2-1 / P-1-2-2, the mirrored
runtime structure, and the point of the whole diagram — the `To other run unit` and `From other comp. group`
arrows on the flanking units, which are what say that a compilation group's modules need not land in one run
unit and a run unit need not come from one compilation group.

**I wrote this generator without the collision guard, and it immediately produced garbage.** The two label
bands landed on the same row and merged into `Fromtotherucomp.tgroup` — silently, because nothing was checking.
I had written that guard twice already this session and watched it catch six defects, and still left it out of
the third file. Added; it then caught the real layout error straight away, which was that `P-1-2-1` and
`P-1-2-2` are seven characters each and P-1-2 was thirteen columns wide. The figure is wider now because the
labels decide the width, not the other way round.

Annex D is eight of nine. **One remains: D.6, an example page layout.**

Gates: sweep 484/484 · publishable at 47,142 lines · 3,811 links, zero dangling. Lint red at 1.

