## Entry 1684 — 2026-09-24 16:11 PDT — Cloud registrar-1 lands: every inventory row now adjudicated at least once; 22 recorded (GAP 1664 → 1652), R42 made structural, 126 A.1 rows held on PB1535

**What.** Wave 2 of the cloud adjudication (PB1522) ran as ONE credit-billed claude.ai/code orchestrator started from the
web page with both repos (session_019puPmD9TXHu1VpiamtCr3s): six parallel adjudicator subagents over the remaining 118
rows (~15 min each), a refuter per batch with CONFORMS rows (9 attacked, 1 overturned: SR-5.5-2 → PARTIAL, PB1413), then
a registrar over all 11 `claude/adj-*` branches — 208 distinct rule-ids = exactly the 208 verdict-less inventory rows.
Adjudicated histogram: DIVERGES 79 · NEEDS-OWNER-DECISION 71 · PARTIAL 31 · CONFORMS 15 · NOT-IMPLEMENTED 12.
**Cost: the credit went $249 → $191 — ~$58 for 118 rows plus the registrar and orchestration (~$0.50/row).**

**Recorded.** `batch-nondoc.json` 11 rows (PARTIAL 5, DIVERGES 3, NOT-IMPLEMENTED 2, CONFORMS 1) and `batch-r42.json`
11 A.1 rows → DOCUMENTED-NON-SUPPORT under owner decision R42 (DOC-A.1-3, 4, 11, 27, 28, 41, 45, 83, 91, 172, 199),
each on the negative golden that diagnoses its declined construct. GAP 1664 → 1652, no record dropped. Stamping the
R42 rows by hand turned `AnnexA1RegisterDriftTests` red, so the registrar made R42 STRUCTURAL instead: two kind-DOC
arms on `screen-handling-only` (keyed on the item's own words, so device items 1, 2, 5, 111 stay out) and one on
`commit-and-rollback-only` in `inventory-schema.json`, pinned in `DerivedVerdictDriftTests`, with CONFORMANCE.md's §7
denominator and §5 counts updated. Evidence and scripts are under `adjudication/registrar-1/`.

**Held.** `batch-doc.json` — 126 A.1 rows (DIVERGES 76 · PARTIAL 26 · CONFORMS 14 · NOT-IMPLEMENTED 10). The schema
defines a DOC DIVERGES as "§7 documents one thing and the compiler does another" and gives every DOC verdict except
DOCUMENTED-NON-SUPPORT a `docs/CONFORMANCE.md#DOC-A.1-N` anchor the drift gate resolves; 122 of these items have NO §7
row (the adjudication brief wrongly called that DIVERGES). Trial-applied once: 4 of 50 gate tests red, inventory
restored. Owner question PB1535 — recommended: leave them verdict-less now, and write the determination rows (PB1522
step 3), after which the batch records as-is. 60 further NEEDS-OWNER-DECISION rows stay verdict-less, each claimed by
an owner note (PB1536, PB1198, PB1099, PB468, PB579, PB1151, PB1255, PB1517–PB1519).

**Filed.** PB1523 GLOBAL bridge duplicate member → CS0102 backend crash (contradicts the recorded GR-13.18.27.4-3
CONFORMS) · PB1524 EC-OO-RESOURCE never raised · PB1525 no §4.2.10 extension register · PB1526 FACTORIAL(X/2+1) = 6
under checking (fraction dropped) · PB1527 object reference in a group: MOVE gives 8 spaces vs §7 DOC-A.1-214, STRING
aborts · PB1528 unpositioned screen ACCEPT/DISPLAY ON EXCEPTION → generic parse error · PB1529 CONTINUE AFTER wraps
before clamp · PB1530–PB1533 · PB1534 (41 A.1 determinations no item note owns) · PB1535/PB1536 decisions; 44 owning
notes extended. Leads were re-probed before filing.

**Landing.** Merged locally onto main (the branch was cut from 0caa7d5; only PB1522.md overlapped, auto-merged); the
committed `__pycache__/*.pyc` dropped. Gate on the merged tree: solution build green; filtered
`SpecTraceabilityInventory|DefectiveRowCoverage|DerivedVerdict|AnnexA1|WorkRegister` **50/50**; `work.py check` clean
(1474 items); the full matrix runs in push-main's CI. Plan §0: every row adjudicated at least once, GAP 1652, 186
verdict-less by design.

