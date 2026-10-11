## Entry 487 — COBOL.NET G2-1c: reference modification (the last small G2 tail)

Reference modification `item(start:length)` (ISO §8.4.2.4) — read and write, the last open G2 tail. New
`CobolString.RefMod` (substring read; lenient clamp/pad, the strict EC-BOUND-REF-MOD a later dialect option) +
`CobolString.SpliceInto` (slice write, preserving the field width). New `RefModPlace(Inner, Start, Length)`:
`Read()` = `CobolString.RefMod(inner, start, len)`, `Write(rhs)` = `inner = CobolString.SpliceInto(inner, start,
len, rhs)` — so the universal `Place` contract handles ref-mod transparently in DISPLAY / MOVE source / comparison
(read) and MOVE target (write), with no per-verb special-casing except that a MOVE into a slice passes the raw
source image (SpliceInto left-justifies / space-fills / truncates to the slice length).

`ReferenceResolver` now classifies each `(…)` suffix — a depth-0 `SUB_COLON` is ref-mod, else a subscript — so an
item can be both subscripted and ref-modified; the ref-mod start/length render through the same `RenderSegment`
(literals, data-names, relative expressions), cast to `int` at the call site. Restricted to alphanumeric/edited
items for now (ref-mod of a numeric item's image is deferred); the parsed-arithmetic `refModSpec` form is loud.

Verified: full-solution build clean Debug (0/0); Conformance 140 green (10 new ref-mod tests: literal/variable
start+length, to-end, slice write exact/truncate/space-fill, MOVE-from-slice, comparison — one spec-pinned where the
legacy DISPLAY trailing-trim quirk shows on a space-filled slice) + Unit 3 green; legacy oracle untouched. **All
small G2 tails are now done** (figurative, signed-DISPLAY, level-88, class conditions, ref-mod). RESUME AT → G5
(file I/O) + G6 (whole-group MOVE) for the first full NC program through the differential harness.

DEVLOG 487.

