## Entry 532 — 2026-06-10 02:30 PDT — Serial SEARCH (ISO §14.9.37 Format 1) — implemented + differential-locked; the cluster advances to the wave-4 resolver gaps

**SEARCH Format 1 complete per §14.9.37.4 GR5–8:** scan from the index's CURRENT setting; per pass — past-end →
AT END, else the WHEN conditions in SOURCE order (first true wins); none → the search index and the in-step
varied item (GR8: a same-table VARYING index BECOMES the search index; another table's index or a data item
increments in step) advance by 1. Bound as `BoundSearch(IndexField, Count, AlsoVaried: BoundSetTarget?, AtEnd,
Whens)` — the varied item reuses the SET-target model and the shared `AugmentSetTarget`. **Emission uses C#
LABELS, not a `while`** — a GO TO inside a WHEN/AT END body emits `__pc = k; break;`, and a loop would swallow
that `break`; with labels it exits the DISPATCHER case (transfer-of-control out of SEARCH, GR5c/6b) and a body
that completes jumps past the search. SEARCH ALL (Format 2 — needs OCCURS KEY capture) and the non-ISO NOT AT
END stay loud by name. New `SearchDifferentialTests` (5 facts: current-setting start, past-end AT END, WHEN
order, VARYING-in-step, GO-TO-out). **414 conformance + 15 unit green.** No instant NC greens — the SEARCH
cluster's programs advance to their NEXT blockers exactly as the diagnosis mapped (the wave-4 reference-resolver
gaps: subscripted Tier-B REDEFINES views [GAP-1, needs the B2 offset×OCCURS fixes first], NEXT SENTENCE,
COMPUTE forms) — the union rule again: SEARCH was necessary, not sufficient.

