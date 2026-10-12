## Entry 1432 — 2026-09-02 17:43 PDT — The conformance ledger becomes a GENERATED view — a refresh is a script run and a publish, never a retyping — and the workstream doctrine gains its ledger step and a chunked fresh-adjudicate stage

Owner, 2026-09-02: "plus you should be keeping the artifact up to date." The ledger had last been refreshed at
battery #41; three landings had since moved GAP 3636 → 3618 and DOCUMENTED-NON-SUPPORT 36 → 333 before the page was
touched. The lapse had a structural cause — the page was rewritten by hand from numbers an agent recomputed each time —
so the fix is structural: `scripts/spec/gen_ledger.py` renders the page deterministically from the inventory, the
`kb/Work` frontmatter (through `work.py`'s own loader, not a copy), `audit_annex_a1.py --json`, CONFORMANCE.md §5, plan
§0's CURRENT battery bullet and `docs/rearchitecture/evidence/ledger-trend.json` — the GAP trend as DATA, seven points
each re-derived from that commit's own inventory via `git show`, a battery always getting a point even when it moved
nothing so the "last gated here" mark cannot land on the wrong commit. The only hand-written part is
`ledger-in-flight.md` (narrative, headed "no count the generator can measure; not a work list — rule 8"). `--check`
detects staleness on the `gen_conformance_notes.py` contract (absent is not stale) and was proven in all three branches.
Two design corrections over the obvious version: the page is stamped with the last commit that touched a MEASURED
input, not HEAD (a DEVLOG-only commit must not make a correct page read stale), and the rendered HTML is a gitignored
build output.

The page published now reads 693 closed of 4,311 (16.1%), GAP 3,618, 1,284 adjudicated, CONFORMS 833 (675 closed /
158 owed a golden), DOCUMENTED-NON-SUPPORT 333 (18 / 315 owing a witness), and says mechanically that battery #41 on
`2acbd842` is still the last comprehensive run with four src/tests-touching commits since — the "#42 owed" tile. The
resolved-but-unwitnessed band, 473 rows, is the largest lever on the board and is exactly what the landing queue
carries (witness clusters A, B1, B2 and the golden lane).

Doctrine kept current in the same change set: the `workstream` skill's landing steps end with the ledger refresh
(`gen_ledger.py` then an Artifact publish to the existing URL), and its lane-3 template's fresh-adjudicate stage is
chunked to the same four-wide budget as its refute stage — the version that ran batch 1's adjudicators sixteen-wide
was the shape the owner's instruction forbids.

