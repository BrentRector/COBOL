## Entry 488 — COBOL.NET G6-core: whole-group MOVE / DISPLAY / compare (the AsImage facility, DISPLAY-homogeneous)

The whole-group image facility (COBOLNET_DESIGN §14.4) for the DISPLAY-homogeneous (all-character) case — a group is
treated as alphanumeric (ISO §14.9.24), its image the concatenation of its leaves. A pure-character group
(`DataItem.IsAllAlphanumeric` — every leaf alphanumeric/edited, no OCCURS) gets a generated `readonly string
AsImage()` (concat the leaf images / nested `AsImage()`s) and `void FromImage(string)` (pad to the group width, then
distribute substrings into the leaves) emitted inside its `record struct`. Wired in:
- **DISPLAY group** / **group comparison** → `group.AsImage()` (the existing string-operand path: a group already
  classifies as a string operand);
- **MOVE x TO group** → `group.FromImage(CobolString.Store(sourceImage, width))` (figurative → width-fill);
- **MOVE group → alphanumeric / group → group** → the source's `AsImage()` flows through the same paths.

A **mixed-usage** group (numeric/COMP leaves — a numeric leaf cannot hold an arbitrary character) is the Tier-C byte
island (§4.2) and stays loud, as designed.

Empirical impact (the advisor's "pull real NC programs in"): **NC101A's loud guards dropped 111 → 70** — G6-core
cleared ~40 all-character group MOVEs + the 2 group DISPLAYs; the remaining 66 are mixed-usage group MOVEs (Tier-C) +
3 file I/O (G5).

Verified: full-solution build clean Debug (0/0); Conformance 148 green (8 new whole-group tests: DISPLAY group,
group↔alphanumeric, group→group, nested-group image, group compare, MOVE-SPACES-fill spec-pinned) + Unit 3 green;
legacy oracle untouched. RESUME AT → G5 (file I/O) and the Tier-C mixed-usage group byte path — the last gaps to a
first full NC program through the differential harness.

DEVLOG 488.

