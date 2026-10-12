## Entry 1418 — 2026-08-31 12:31 PDT — The registration wave: 131 defective inventory rows were invisible to the work register, and the fix is a frontmatter back-link with a gate on it — plus sixteen rows dispositioned on a licence read at its source, seven CONFORMS flips refused, and one cross-reference error found in the standard itself

The owner's burn-down directive aims at a number, and the number and the work list had nothing holding them
together. `tests/version-matrix/traceability-inventory.json` says which spec rules this compiler does not yet
implement correctly. `kb/Work/` — the ONE work register, CLAUDE.md rule 8 — says what is left to do. Measured on
the cluster-B tree at `1884ba1e`: of **138** rows verdicted PARTIAL / NOT-IMPLEMENTED / DIVERGES, **131 were
invisible to `work.py next`**. A defect the register cannot see cannot be ranked, cannot be scheduled, and reads
to every later session as work nobody owes — while the burn-down counted it the entire time.

Re-running the coverage predicate FRESH on this tree was the first act, because clusters A and B had landed
since the 131 was measured and the honest expectation was that some rows had been covered. **The delta was
zero: still exactly 131.** Cluster A and B's notes are `landed`, and a landed note covers nothing — which is
itself the shape of the problem, not a coincidence.

### The mechanism, and why the OTHER direction was wrong

The 131 was measured by scraping a `PB\d+` token out of each row's prose `notes` field and asking whether the
note it named was live. That direction is wrong twice over. A row's notes are forensic narrative, so a note id
appears in one for reasons that are not ownership; and — the half that matters — a note that LANDS keeps naming
its rows, so a landed note whose rows were never re-verdicted is invisible to a row→note scrape. The event most
worth failing on is precisely the one it cannot see.

So the link is the NOTE→ROW direction, as a new register field: **`inventory_rows:`** in a note's frontmatter,
listing the `rule-id`s that note owns. It is the SSOT for ownership; the prose "Rows:" line beside it is
narrative. `scripts/spec/work.py`'s module docstring now documents it.

**The gate is `tests/Cobol.Net.Tests.Unit/DefectiveRowCoverageDriftTests.cs`.** Two checks and a self-test:
every row whose verdict the schema marks non-resolving is claimed by a note whose status is not terminal; every
`inventory_rows` claim names a rule-id the inventory really has (a typo'd claim satisfies nothing while LOOKING
like ownership, so both checks must notice it — and both do). Nothing in it carries its own copy of a
vocabulary: "defective" is derived from `inventory-schema.json`'s `resolves` flag, so adding a verdict extends
the gate with no edit here; and only `landed`/`retired` are terminal, every other status counting as live, so a
status added to `work.py` tomorrow fails SAFE.

⛔ **The consequence to know before landing anything: when a note flips to `landed`, its rows must be
re-verdicted in the same change set, or the battery goes red.** That is the point.

**Proved failing before it was believed** (`feedback_green_gates_arent_evidence`), on the real tree, twice:
blanking `PB247`'s claim produced `AR-15.87.3-3: verdict PARTIAL, and no open kb/Work note names it in
inventory_rows` (1 of 4311 examined); replacing it with a typo'd `AR-15.87.3-33` fired BOTH checks at once
(`PB247.md: inventory_rows names 'AR-15.87.3-33', which is not a rule-id in the inventory`). Restored, green.

### The sixteen reconciliations — and the eight refused

Sixteen rows' entire content is an Annex A.4 optional module this implementation does not claim, so they are
**DOCUMENTED-NON-SUPPORT**, not defects. The licence was read at its source rather than inherited — the
inherited-citation failure mode CLAUDE.md rule 1 spends its length on:

* **A.4.1** (`cite.py --check`, OK): the module list's "associated syntax rules, general rules, other rules,
  exception conditions, and I-O status values are also optional, even if not explicitly listed" — the clause
  that carries a licence from the named FORMAT to the rules hanging off it, which is what these rows need.
* **A.4.2 item 1** ("ACCEPT statement, format 3: screen"), **item 9** ("DISPLAY statement, format 2: screen"),
  **item 10** (the EC-SCREEN conditions); **A.4.3 item 4** ("COMMIT statement 14.9.7").
* `docs/CONFORMANCE.md` §5's disposition table (`A.4.2 … Not claimed`, `A.4.3 … Not claimed`) and §4 items 2
  and 4 — the §4.2.7 user documentation the verdict actually rests on.

⚠ **A cross-reference error in the STANDARD, found on the way and worth recording:** A.4.2 item 9 reads
"DISPLAY statement, format 2: screen **(14.9.10)**", but §14.9.10 is the DELETE statement and DISPLAY is
§14.9.11. Checked against the canonical PDF (page index 1004) as well as `specs/ISO_COBOL.md` — **both read
14.9.10**, so the transcription is faithful and the error is the standard's; no repair is owed. Every other
cross-reference checked in A.4.2/A.4.3 resolves (item 24 → §14.9.39 SET, A.4.3 item 4 → §14.9.7 COMMIT, item 5
→ §14.9.36 ROLLBACK). The element is named unambiguously by its text, so the licence holds; only the number is
wrong.

**GAP is unchanged: 3641 → 3641.** All sixteen rows carry an empty `test-ref`, so DOCUMENTED-NON-SUPPORT
records the verdict and the row stays open for the witness the schema requires — "a test proving we diagnose …
rather than silently miscompile". That debt is registered, not implied: `PB260` (thirteen screen rows) and
`PB261` (three COMMIT rows) own it, and `PB260` carries the rows' own measured evidence that the posture gap is
real — `DISPLAY screen-name-1` parses as a one-operand Format-1 DISPLAY, `DISPLAY SG COLUMN 5` binds as a
three-operand one while `DISPLAY SG COLUMN NUMBER 5` is a clean parse error, and the four EC-SCREEN names sit in
`ExceptionCatalog` with zero setters and zero readers.

**Eight of the twenty-four proposed reconciliations were REFUSED.** Seven proposed flipping a row to CONFORMS on
the ground that a named fix had landed and the code now read correctly. That is not a licence — the contract
requires a recorded owner decision or a §4 licence — and the asymmetry decides it: recording CONFORMS on an
unverified read over-claims conformance, which is the single failure the inventory exists to make expensive,
while registering the row costs one note. There was also a mechanical bar: six of the seven named no `test-ref`
at all, so the flip would have produced six CONFORMS-but-untested rows — GAP unchanged, defect count six lower,
nothing on disk any better. They are `PB258`, one note because they are ONE mechanism: **there is no back-link
from a landed fix to the inventory rows its landing invalidates** — the same mechanism a different family of the
same fleet found independently over fourteen §15 rows (`PB245`). The eighth, plus the ACCEPT format row, are
`PB259`: two rows held at a defective verdict *for visibility*, one of which says so in its own notes —
"the row stays PARTIAL to keep it visible". That is a work register improvised inside a verdict field. The
register now does that job, so the verdict is free to say what is true.

### What landed

**37 new notes, PB225–PB261**, from the grouping fleet's drafts with the partition corrected against the fresh
predicate — 33 mechanism notes plus `PB258`/`PB259`/`PB260`/`PB261`. Corrections applied, each re-verified
rather than inherited: the catch-all family's duplicate notes for `GR-8.8.4.4.4-3` and `SR-12.3.7.3-11` dropped
in favour of the 8-11-12 family's; `GR-14.9.10.4-L2.2` removed from the DELETE note (already owned by open
`PB55`); the three unassigned FMT rows bucketed by mechanism (`FMT-14.9.4.2` → the `ADDRESS OF` note, whose
mechanism is the row's own "STILL OPEN" sentence; `FMT-14.9.5.2` → the program-prototype-name note, since
CANCEL's missing third format alternative IS `program-prototype-name-1`; `FMT-14.9.1.2` → `PB259`).

**One correction the contract did not name**, found by sweeping the drafts for a shared mechanism rather than a
shared file: two families independently wrote up `EcBinder`'s `node is BoundMove` hoist — one over
`GR-14.9.2.4-6`/`GR-14.9.8.4-1`/`GR-14.9.8.4-3`, the other over `GR-14.9.12.4-3`. One mechanism, two notes, is
the anti-pattern; merged into `PB230`, with the DIVIDE-specific analysis kept as a section.

**A flag correction, because the register would have swallowed them:** two notes came back flagged `silent`
alone, and `silent` is not a HARM flag — `work.py` would have filed them and then never ranked them, the exact
disappearance the two-flag predicate used to cause. Both are failures to raise or diagnose a condition the spec
requires, so both take `under_rejects`.

**`inventory_rows` added to 13 existing live notes** (PB55, PB142, PB144, PB152, PB153, PB156, PB158, PB160,
PB165, PB167, PB189, PB190, PB196), covering 25 rows they already owned in prose. Every claim was verified
against the note body first, and the verification is why the field is worth having: six of the twenty-five were
named nowhere in their note's "Rows:" line — they were claims about a *mechanism* described three paragraphs
down, true but unreadable by anything. The patch is byte-level so each file keeps its own line endings (PB189
and PB190 are CRLF; the rest LF).

### Verdict

`work.py check`: **317 items, all well-formed**. Actionable **29 → 54** (+25; eight of the new notes are
`process_only` evidence gaps and correctly do not rank). Frontmatter coverage: **122 defective rows, 0
unclaimed, 0 claims naming no row**. Wave-local gate GREEN — Conformance 116/116 (`~Display|~Accept`), Unit
**5106/5106**, Characterization 33/33. Battery #40 was NOT run and now accrues this wave too.

`work.py next` re-ranks over the TRUE defective set from here, and the burn-down order re-derives from it —
harm first, which is what the register was for.

