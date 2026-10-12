## Entry 533 — 2026-06-10 08:43 PDT — Wave 4: Tier-B layout accounting (B2) + subscripted REDEFINES views (GAP-1) + NEXT SENTENCE → SIX new NC greens (42 total) + a legacy SEARCH hole exposed

**Three interlocking pieces; the SEARCH cluster's gate fell:**
- **B2 — the Tier-B layout accounting bugs (diagnosis Wave 0):** `AssignClassOffsets` (a) gave an inner-REDEFINES
  child its ACCUMULATED offset instead of its TARGET's (redefinition begins at the redefined item's first
  position, ISO §13.18.44 GR1) and let it advance the sibling offset (it occupies the target's storage, adds
  none); (b) accumulated `ImageWidth` without × OCCURS (a sibling AFTER a table landed at occurrence-1's end);
  (c) `cls.Width` took the max member width without × OCCURS (a member table's class image truncated to one
  occurrence).
- **GAP-1 — subscripted Tier-B views:** `RedefViewPlace` generalized from a constant offset to an OFFSET
  EXPRESSION; a subscripted view's window = `ClassOffset + Σ (idxₖ − 1) × strideₖ` over the in-class OCCURS
  levels (outer→inner, same convention as AccessPath) — a redefined table lays its occurrences end-to-end in the
  ONE backing (§13.18.44). Multi-dim falls out (RSV3: two-subscript views green).
- **NEXT SENTENCE (§14.9.19 GR6; archaic F.1, legal at every edition):** `BoundParagraph` now carries SENTENCES
  (the separator-period boundaries are semantic); the emitter labels each inter-sentence boundary
  (`__sentP_K:`) when a paragraph contains NEXT SENTENCE and emits `goto` to the current sentence's end (last
  sentence ⇒ the paragraph fall-through, so a performed range still returns at its exit). `ContainsNextSentence`
  walks every nesting container; a missed container would be a loud backend compile failure (no label), never a
  silent misjump. Generated header gains `#pragma warning disable CS0164`.

**Result: NC125A, NC132A, NC210A, NC231A, NC232A, NC234A byte-match → 42 NC locked in. 426 conformance + 15
unit green, zero regressions** (B2 reshapes every Tier-B layout — the whole-corpus net held). New
`RedefinesSubscriptedViewDifferentialTests` (5 facts: subscripted reads/writes through the backing, 2-subscript
views, sibling-after-table offsets, NEXT SENTENCE both branches).

**⚠ Legacy-oracle hole #4 (the kind the corpus net exists to catch):** NC236A now runs 10/10 CCVS tests but the
GOLDEN marks SCH-TEST-F1-8/F1-10 "TEST DELETED". Ran the LEGACY directly: it produces the DELETED rows from the
SAME preprocessed source — its serial SEARCH with `VARYING index-of-ANOTHER-table` (§14.9.37.4 GR8b) FALLS
THROUGH into the CCVS DE-LETE paragraph (an unimplemented-form skip), and that artifact got baselined into the
golden. The greenfield scan (searched table's own first index drives; the other index varies in step) passes
both tests — the conforming outcome. NC236A therefore gets a SPEC-PINNED test (`SpecPinnedNistTests` — asserts
PASS rows, 010 OF 010, no deletions) instead of a golden byte-match; the golden re-baselines at G8 when the
legacy guard retires. Remaining cluster blockers, as mapped: SEARCH ALL (NC233A/237A/238A), subscripted
condition-names (NC250A/235A), deep qualified-subscript references (NC246A), NC252A's nested-Tier-B backend
fail (B4), ACCEPT forms (NC204M), condition forms (NC174A/254A).

