## Entry 526 — 2026-06-10 01:02 PDT — The SET statement + index machinery (ISO §14.9.39 F1–F2, §13.18.60 USAGE INDEX) → SIX new NC greens (22 total)

**The table-handling wave, part 1.** The fresh corpus sweep (after 525) read: 16 GREEN, 4 DIFF, 75 blocked — and the
single largest blocker cluster was the SET statement (18 programs' FIRST blocker). Implemented the COBOL-85 SET
surface per the spec + design (§3.5 index model, §12.3 dispatch-by-target-kind) — NOT scoped to what the corpus
uses: **Formats 1–2 complete** (index-name / USAGE INDEX item / integer item receivers; arithmetic-expression /
index-name / item senders; UP/DOWN BY; multi-receiver with the GR2/GR3 once-evaluated sender), **Format 4 TO TRUE**
(was already in), and every other format failing LOUD BY NAME with its edition (F3 switches → SPECIAL-NAMES
subsystem; F5/F7–F10 pointers/objects → 2002 subsystems; TO FALSE → the 2002 FALSE phrase, §14.9.39 SR7).

- **Bound nodes:** `BoundSetTo` / `BoundSetUpDown` over a `BoundSetTarget` kind hierarchy (`SetIndexTarget` = the
  C# long occurrence-number field; `SetPlaceTarget` = an index/integer data item), plus `BoundIndexRef` — an
  index-name read as its occurrence number, now valid in EVERY numeric-expression position via the new ONE
  `RefExpr` dataReference→BoundExpr mapping (it replaced 4 duplicated resolve sites; index-names also flow into
  relation conditions per §13.18.38 through `FieldOperand`).
- **The §3.5 model pays off:** an index IS its 1-based occurrence number (a long), so §14.9.39 GR2's cross-table
  occurrence-number conversion is the identity; SET TO → assign, UP/DOWN BY → `±=`, numeric receiver → the
  occurrence number through its own PICTURE store (GR2c), index receiver → unchanged copy (GR2b). EC-RANGE-INDEX
  awaits the EC model — COBOL-85 HAS no exception conditions, so the unchecked store IS the 85 semantics.
- **USAGE INDEX data items** (§13.18.60): a PIC-less USAGE INDEX entry binds as an elementary long
  (`PicInfo.IndexItem`, new `Usage.Index`). **Bug found by NC131A:** `01 I-DATA-GROUP USAGE IS INDEX.` with a
  subordinate is a GROUP whose usage merely INHERITS (GR1) — children aren't known at entry-bind time, so a new
  post-build `ResolveIndexItems()` pass demotes child-bearing entries back to groups and makes PIC-less leaves
  under them index items.

**Result: NC121M, NC123A, NC131A, NC137A, NC141A, NC248A all byte-match the golden → 22 NC programs locked in.**
The rest of the cluster advanced to their NEXT blockers exactly as mapped (SEARCH, PERFORM VARYING out-of-line,
subscripted-reference forms, GO TO section-names). New `SetIndexDifferentialTests` (8 facts: every Format-1/2
receiver×sender kind, once-evaluated sender, USAGE INDEX round-trip, index-in-relation). **322 conformance + 15
unit green.** A 6-agent diagnosis workflow mapped the remaining blocker groups in parallel (CMPL_FAILs = de-editing
MOVE + DECIMAL-POINT IS COMMA + BLANK WHEN ZERO + ODO; full findings in the session log) — they are the next waves.

