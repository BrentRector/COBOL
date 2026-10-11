## Entry 551 — 2026-06-10 16:25 PDT — OCCURS DEPENDING ON live (wave-2 agent + serial integration) → 88/95; NC235A EXCEEDS the legacy golden

**The first wave-2 family integrated.** The ODO agent delivered `Binding/OdoModel.cs` (OccursSpec carrier,
OdoGroupPlace, the post-build resolution/validation pass, SearchBound) + a 9-fact differential file, and 14
anchored integration edits — applied with ZERO anchor failures and ZERO build errors. Design per §13.18.38:
tables ALLOCATE at the maximum (integer-2 — §8.5.1.8 physical capacity fixed; the old code stored integer-1,
the root of NC235A's 1-element arrays); the GR8 semantics ride two seams — a group operand containing an ODO
table wraps in `OdoGroupPlace`, whose `SendingImage()` is the current-count slice (OperandText read side) and
whose `ReceiveInto()` splices over exactly the current extent when data-name-1 is OUTSIDE the group (GR8a; INSIDE
⇒ full-width, GR8b) — the same store seam INSPECT's store-back uses (GR7); serial and binary SEARCH bound by the
CURRENT count (§14.9.37.4 GR4/GR9, `BoundSearch.DependCount`); `CobolTable.OdoExtent` clamps benignly (the
COBOL-85 no-EC policy).

**NC247A GREEN (the string[] CS0029 program — INSPECT/UNSTRING over ODO groups). NC235A now EXCEEDS the golden:**
it PASSES IDX-TEST-F2-9/F2-12, which the LEGACY deleted (its SEARCH ALL over an ODO table with a condition-name
WHEN fell through to DE-LETE) — the golden encodes 011 OF 013 with 2 deletions; the conforming run is 013 OF 013
(§14.9.37 F2 + §13.18.38 GR7). Spec-pinned alongside NC236A (golden re-baselines at G8). **88/95 byte-match +
2 spec-pinned-exceeding + 2 no-golden-by-design = only NC105A and the NC107A/108M DECIMAL-POINT pair remain
actionable in the NC suite. 580 conformance + 15 unit green.** The sort-merge/keyedio/call families are parked in
`/e/tmp/wave2-hold/` and integrate next, one at a time.

