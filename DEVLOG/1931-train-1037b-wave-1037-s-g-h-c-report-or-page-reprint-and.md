## Entry 1931 — 2026-10-07 23:16 PDT — Train 1037b: wave 1037's G, H, C — report OR PAGE reprint and limits, the RECORD clause bound, sum counters as DISPLAY, a literal's comma image, function-identifier arguments, property receivers in implicit moves, the plain WRITE as AFTER ADVANCING 1 (PB2513, PB2519, PB1934, PB1927, PB1943, PB2520, PB1643, PB1930, PB2078 part, PB1027, PB1664)

**G — PB1927, PB2513, PB1934, PB2519, PB1943, PB2520 (all landed).** All five notes reproduced on the implementer's build
before any edit. PB1927: `ReportWriter.PresentOrPageHeadings` had the control-footing arm backwards, reprinting the OR PAGE
headings BELOW the footing's level and skipping those at and above it. §13.18.57.4 GR6 c) 2.'s "not before a control
footing at a lower control level" is elliptical; read with the footing as the referent it skips the headings of the groups
the break leaves open, which GR7 d) 4. contradicts. The heading's level is the referent, recorded as `specs/ISO_COBOL.md`
Addendum D5 (transcribed as printed) and in `docs/CONFORMANCE.md` A.4.11. PB2513: the GR7 d) 2./3./4. limit screens put a
heading below an OR PAGE heading at FIRST DETAIL; `DataBinder.GroupLimits` now walks each heading from the nearest OR PAGE
heading above it (`HeadingReach`, `MinimumLastLine`'s new `seed`), one chain for all three limits. PB2519 and PB1934: the
report line width read only Format 1's integer-1, and now reads `FileModel.RecordClause.Upper` in all three formats
(§13.18.43.4 GR6, GR7, GR18), the reading the record-less FD screen already shared. `ReportLineImage` keeps the column
occupancy out to the page width and cuts only the text at the record, so two items overlapping past the record still raise
EC-REPORT-COLUMN-OVERLAP (§13.18.14.4 GR4). The cut itself stays a documented latitude (DOC-A.1-159). PB1943 and PB2520: a
sum counter is a signed USAGE DISPLAY item (`PicInfo.SumCounterItem`, a determination in A.4.11: §13.18.54.4 GR1 states no
usage). It is reference-modifiable through the one reference-modification tail (`ReferenceResolver.SumCounterFor`), and
LENGTH reads its digit count. The implementer found that image-backing facts recorded for the counter's register item steered
a later `CF-T (3:)` off the numeric-image wrap (CS1503), and fixed it: `Place.OwnsStorageCell` (false for the five views over
engine state) gates `RefModView`, `MarkRefModStoreImage` and `MarkFillImageStorage`; `PlaceStorageCellDriftTests` pins it.
Visible consequence: DISPLAY of a counter shows its signed image (`123D`), so `2002/pb1272_present_when_snapshot` and
`85/pb1454_sum_counter_group_qualifier` were re-derived. Eleven positive goldens and three negatives; no inventory row, no code.

**H — PB1643, PB1930 (landed); PB2078 (part, stays open).** Re-probed: `DISPLAY 1,5` under DECIMAL-POINT IS COMMA printed
`1.5`; CALL BY CONTENT with a function returning an object or a pointer, and `FUNCTION UPPER-CASE(X)`, drew COBOLNET0844
(the note's "alphanumeric already crosses on CALL" did not hold: only INVOKE had the recovery); READ/ACCEPT/UNSTRING/
INITIALIZE/STRING/INSPECT with `NM OF AR(I)` drew COBOLNET0899. PB1643: one text field was both the canonical value and the
character image; `BoundNumericLiteral`/`BoundNumLiteral` now carry `DecimalSeparator` and an `Image`, built only by
`ExpressionBinder.NumericLiteralOperand`/`NumericLiteralExpr` (`NumericLiteralImageDriftTests`). That follows §12.3.7.4 GR14 a),
and DOC-A.1-56 records the display-as-written determination (GnuCOBOL's default). Sibling: INVOKE BY CONTENT literals were
built from raw grammar text (CS1503 under DPC). PB1930: §14.9.4.4 GR8, a single identifier is an identifier; `CallBinder`
binds a sole function-, method- or keyword-omitted-function identifier through its own door in the BY CONTENT, bare and
BY VALUE arms (SR22 screens BY VALUE). Sibling: INVOKE dropped a user function's result place (COBOLNET0828). PB2078: the
claim moved into `MoveBinder.BindMoveOf`, so every implicit move places a run-time-selected property's accessors around its
own store, and STRING INTO and INSPECT identifier-1 joined; the residue (TALLYING counter, POINTER items, plain ACCEPT,
RETURNING, SET) keeps COBOLNET0899, and the note's title now names it. Four goldens, two negatives, one negative renamed
(`...-accept` to `...-inspect-tally`); codes 1677 and 1688 reused, no new code.

**C — PB1027, PB1664 (landed).** Every cell of the note's table reproduced. The note's premise that `-fwrite-after` selects
GnuCOBOL's leading empty line did not hold: default `cobc` 3.2.0 writes `AAAAA LF BBBBB LF` for two plain WRITEs. PB1027: a
WRITE with no ADVANCING phrase on a file that supports vertical positioning now takes the explicit AFTER ADVANCING 1 LINE
path in every device state (§14.9.51.4 GR25), for record and line sequential files; `ImplicitAdvanceIsBefore` and the third,
line sequential arm are deleted. Whether a file supports positioning is a determination (CONFORMANCE §4: a LINAGE clause, or
a WRITE with ADVANCING since OPEN), so plain-only files are unchanged; the owner question it leaves is filed as PB2557.
PB1664: `SelectLineEnd` chooses the line end per OPEN: host newline for a line sequential, LINAGE or report file, LF for any
other record sequential print stream. `PlainWriteAdvancingConformanceTests` (90 cells, mutation-checked), two goldens, six
goldens re-derived. Row GR-14.9.51.4-25 re-verdicted CONFORMS from batch `w1037c-PB1027` (two deleted symbols retired); the
implementer's first Linux gate found three host-dependent goldens and made them count line ends.

**The train.** H wrote its own DEVLOG entry 1930 on its branch; it was dropped and folded in here (main's 1930 is train
1037). Conflicts: the 2002, 85 and negative corpus manifests (whole elements, unioned; counts checked: 85 398 to 401, 2002
774 to 777, 2023 811 to 812, negative 2526 to 2528 with one rename), `docs/DRIFT_RULES.md` (regenerated, 293 drift tests),
and CONFORMANCE DOC-A.1-159, where G and C each rewrote a different sentence of one row (G's row kept, C's line-end sentence
and witness applied to it). The inventory hunk was discarded and C's batch re-applied: **GAP 145 → 145**. Gate (lander mode,
the whole population, first run): `=== BUILD-LOCAL GATE: GREEN — Conformance 11,129/11,129 · Unit 32,621/32,621 ·
Characterization 36/36`, run 20261008T060417Z-f4431f; no red, so no attribution. semgrep PASS before and after. Oracle:
DIFFERENT, 59 of 7,560 cases, every one explained: 32 report programs whose ONLY change is the sum counter's `NumProfile`
(Binary to Zoned, trailing overpunch: G's PB1943); 4 goldens whose source C re-derived (pb1667 x2, pb489_85, pb523_85);
22 added goldens and negatives and 1 renamed negative. Re-recorded as `868aaaf24f0b`. Review of the merged diff: one finding,
a stale comment in `SequentialConnector.WriteRecord` (it still said a plain WRITE on a line sequential LINAGE file does not
reach `WriteAdvancingRecord`), fixed in C's commit; no cluster dropped. Leads filed: PB2553 (a non-digit stored into a sum
counter through a modifier decodes silently to zero), PB2554 (an INVOKE formal's PICTURE uses the caller's DECIMAL-POINT
mode), PB2555 (text past margin R reported as a parse error on the next statement), PB2556 (LINE SEQUENTIAL report file:
'71' documented, '91' likely), PB2557 (the PB1027 owner question); addenda to PB616 (remaining GR14a miscitations), PB1840
(plain-then-BEFORE welds), PB2521 (the counter ruling). No diagnostic codes claimed: COBOLNET3080-3082, 3092-3094 and
3095-3097 returned.

