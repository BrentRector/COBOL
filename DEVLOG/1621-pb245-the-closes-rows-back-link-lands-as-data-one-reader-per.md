## Entry 1621 — 2026-09-20 11:45 PDT — PB245: the `closes_rows` back-link lands as DATA — one reader per language, two gate arms proved red, 485 notes back-filled from the inventory's own history

⭐ **THE OWNER'S 2026-09-19 DECISION, IMPLEMENTED WHOLE.** A landing had no step that recorded which
traceability-inventory rows it CLOSED. `inventory_rows` is a note's claim while it is OPEN and it empties as its
rows are re-verdicted, so the link vanished at the moment of landing — and "is this row still real?" could only
be answered by measuring the compiler again. It was measured once, on one clause family: of fourteen §15 rows
holding the GAP open, THIRTEEN closed CONFORMS on first re-measurement, on mechanisms seven landings had already
shipped. `kb/Work/PB245` is that measurement; this entry is its mechanism.

**The field.** `closes_rows:` names the rows a note's landing closed; a landing that closed none writes
`closes_rows: []` AND a `closes_rows_reason:` sentence, because silence is the thing being replaced. ⚙ Spelled
with an underscore rather than the decision's `closes-rows`: every multiword key in this frontmatter is
snake_case (`inventory_rows`, `blocked_by`, `spec_refs`), and `kb/Work.base` addresses a property as
`note.<key>`, where a hyphen would at best need quoting and at worst read as an operator. `kb/Work.base` gains that column plus a `Landed — rows closed` view (a view of FINISHED work — not a
worklist, rule 8).

**The gate.** `ClosesRowsBackLinkDriftTests` (Unit, beside `DefectiveRowCoverageDriftTests`) is the decision's
two arms: a landed `kind: defect` note states what it closed, and every row ANY note names as closed is one
the inventory itself still counts as OK today — its computed `state`, which is strictly stronger than
'the verdict resolves' and is the number the burn-down reports (they coincide on all 4,348 rows today).
⛔ Both were proved RED on the live register before either was believed — stripping
`kb/Work/PB205`'s field named that file and quoted the repair; adding the still-PARTIAL `SR-13.18.60.3-4` to it
produced *"closes_rows names 'SR-13.18.60.3-4', whose verdict is 'PARTIAL' — that row is NOT closed"* — then
restored. The class also carries a fabricated-register self-test driving both predicates in both directions
(landed/retired vs. every live status; defect vs. the three other kinds; a typo'd id; an UNADJUDICATED row,
which is not "closed" either).

**The back-fill is derived, not asserted.** `scripts/spec/backfill_closes_rows.py` walks the inventory's own
257-commit history and treats a row that moved from a non-resolving verdict (or none) to a resolving one as a
row that landing closed. 2,006 such transitions exist; **425 are left UNATTRIBUTED** rather than divided,
because a train naming five notes and a row naming none cannot be split without putting a row on a note that did
not close it. Result: **1,972 attributions across 485 notes**, every one filtered to a row the inventory
still counts as OK, so a regressed row is never claimed. Idempotent (a second run: 484 kept, 1 added), insert-only over 485 files,
and byte-faithful — `PB261` and `PB283` carry a stray lone CR that makes git treat them as binary, and a
normalizing writer showed a 200-line diff for a one-line change until the writer was made to detect and re-use
each file's own line ending. `--self-test` covers 19 cases and was itself proved to FAIL on a broken writer
(12/17 at the time).

**The reader was repaired first, because the new gate would have inherited its bug.** `kb/Work/PB875` lands in
the same change set: both readers of this register failed OPEN on a list WRAPPED across two lines — Python
truncated it, C# discarded it entirely, and `work.py check` called the note well-formed either way. There is now
ONE reader per language (`work.py parse_frontmatter`, `tests/_shared/WorkRegister.cs` — the gate's private
parser is deleted), a value that cannot be finished is an ERROR CODE with the key left ABSENT rather than
silently empty, and a runaway is bounded by the next key (the first cut let `tags: [cobolsharp, work, defect]`
become three members of `inventory_rows`; the fixture caught it). `tests/version-matrix/
work-frontmatter-parity-cases.json` holds 12 shapes and the C# gate RUNS `work.py parity --json` rather than
assuming it. Measured old-vs-new over all 939 notes: exactly ONE difference — `PB205`, read as 4 rows where it
names 12, and as NONE by the gate that enforces claims. The back-fill now writes wrapped lists on purpose, so
three hundred notes carry the shape that used to be dropped.

**⚠ THE SCOPE-HONESTY SWEEP IS MEASURED, AND IT IS NOT ZERO.** `--suspects` counts rows still carrying a
non-resolving verdict whose forensic `notes` name a note that has already reached a terminal status, across the
WHOLE inventory rather than PB245's one clause family: **75 rows**. That is a measurement and deliberately not a
gate — a landed note named in a row's prose is not proof the row is stale, which is why the enforced back-link
runs NOTE→ROW — but it is where the rot hides, and the next golden round can now be aimed rather than sampled.

**Also swept:** `tests/_shared/TraceabilityInventory.cs` extracts the inventory + verdict-vocabulary loader the
two coverage gates were about to hold two copies of, so "defective" and "resolving" stay DERIVED from
`inventory-schema.json`'s `resolves` flag in one place. Docs current in the same change set: `kb-sync`, the
fix-lane implementer / lander / lander-train / implementer briefs (the field is written in the commit that
lands), the registrar brief (a wrapped list is legal now and an unterminated one is reported by name), plan §0 +
§9, `docs/DOC_INDEX.md`, and CLAUDE.md rule 8's frontmatter enumeration.

**GAP unchanged at 2,366** — nothing was re-verdicted; this records what the history already said. Gate: the
filtered drift run plus the FULL Unit assembly. Notes landed: `PB245`, `PB875`. Codes used: none (COBOLNET2164–
2166 stay free).

