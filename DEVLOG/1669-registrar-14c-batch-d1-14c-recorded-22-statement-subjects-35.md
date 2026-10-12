## Entry 1669 — 2026-09-24 08:41 PDT — Registrar 14c: batch d1-14c recorded (22 statement subjects), 35 notes filed (PB1160–PB1199, 5 folded), GAP 1955 → 1900

**What.** Registrar 14c registered lane-3 adjudication batch d1-14c: 22 statement subjects (RAISE through WRITE, the
ROUNDED phrase, the RETRY phrase, RETURNING items and the scope of statements), adjudicated on the pinned tree
`adj-d1` @68bbdb00f. The registrar was stopped by the STOP file while waiting for 14b to reach main; a finisher
merged its checkpoint branch onto 14b, took origin/main's traceability inventory and RE-RECORDED the batch with
`record_verdicts` on the merged tree (never merged as JSON hunks), and folded five drafts into the notes 14a/14b had
already filed for the same mechanism. **GAP 1955 → 1900** (+55 closed of 4,348).

**Folds (one note per mechanism, CLAUDE.md rule 8).** 14a and 14b filed the same mechanisms concurrently, so five
drafts were merged into the landed notes (rows, body and every reference rewritten, including the batch's notes
field before the re-record): PB1169 → **PB1072** (SUPPRESS WHEN decode), PB1161 → **PB1148** (the PERFORM
dispatcher's GO-TO-next-paragraph return), PB1176 → **PB1140** (SORT national short-record fill), PB1170 and PB1172
→ **PB1139** (SORT/MERGE statement-level syntax rules; PB1087, 14a's SAME-clause model, added to its cluster). Five
merge conflicts in PB1036/PB1052/PB372/PB994/PB995 (14b and 14c both extended them) were resolved as a union of
`inventory_rows` with both extension sections kept. Net: **35 notes filed** (PB1160, PB1162–PB1168, PB1171,
PB1173–PB1175, PB1177–PB1199), 15 extended, 5 folded; PB1200–PB1209 unused.

**Registrar 14c registered lane-3 adjudication batch d1-14c — 22 statement subjects (RAISE through WRITE), 218
records, GAP 1955 → 1900.** Verdicts: CONFORMS 118 (55 closed their row, 63 are CONFORMS-but-untested and go to the golden
lane's next input set), DIVERGES 57, PARTIAL 37, NOT-IMPLEMENTED 6. The refuters re-examined 141 rows and overturned
25: 16 to PARTIAL, 6 to DIVERGES, 2 restated as CONFORMS with a corrected test-ref, 1 to NEEDS-OWNER-DECISION. Every
overturn went toward MORE defect, none toward less. 23 rows were deliberately NOT recorded because only the owner may
stamp DOCUMENTED-NON-SUPPORT (D13). They are the owner question **PB1198**.

**Notes: forty drafted (PB1160–PB1199), thirty-five filed after the five folds above, one per mechanism, clustered by code site.** Fifteen existing notes were
extended rather than duplicated. The ones that change what a user's program does:

- **Wrong answers (MAJOR):**
  - **PB1148** (PB1161 folded): `PERFORM M2` where M2 does `GO TO M3` and M3 is the next paragraph returns to the PERFORM. The
    out-of-line dispatcher's fall-off test treats that GO TO as falling off the end of M2. The registrar re-probed it
    on main: it prints `BACK-IN-M1`, where the right output is `M3` then `M4`.
  - PB1160: a RESUME within a GLOBAL declarative's scope is not a CONTINUE.
  - PB1163: RETRY FOREVER gives up after one attempt on a lock held by another run unit.
  - PB1166: the RETURNING PICTURE identity check ignores currency strings and DECIMAL-POINT IS COMMA. A Z9,99 item
    reads 1234.
  - PB1168: REWRITE's size tests, on three arms.
  - PB1072 (PB1169 folded): SUPPRESS WHEN SPACE, SUPPRESS WHEN ZERO and a short literal never suppress, and a WRITE gets a false
    '22'.
  - PB1174: a table SORT ignores OCCURS DEPENDING ON.
  - PB1140 (PB1176 folded): SORT's space fill uses alphanumeric spaces where the record is national.
  - PB1178: `STOP RUN WITH ERROR STATUS 4294967296` exits 0.
  - PB1184: a zero-length UNSTRING sender takes ON OVERFLOW.
  - PB1185: UNSTRING takes a national figurative delimiter from the alphanumeric collating sequence.
  - PB1186: SUPPRESS is keyed by report, not by group.
  - PB1187: TERMINATE's report footing shows the current control values instead of the prior ones.
  - PB1190: after one ADVANCING write, a varying record is written with no length check.
  - PB1191: the line-sequential trailing-space strip is keyed wrong.
  - PB1194: REWRITE on a file that is not open gives '51' instead of '49'.
  - PB1195: REWRITE does not propagate SAME RECORD AREA to out-of-line records.
  - PB1196: a floating-point receiver ignores ROUNDED MODE entirely.
- **Crashes (MAJOR):**
  - PB1175: a table SORT over a REDEFINES group aborts at run time.
  - PB1179: every STRING INTO a national receiver aborts at run time.
  - PB1192: no organization has an externally-defined boundary. A relative key of 999999999999999999 gives '00',
    then OverflowException at CLOSE.
  - PB1199: `DISPLAY LINE-COUNTER OF R1` aborts at run time. This came from an adjudicator's side lead, and the
    registrar re-probed it.
  - The extension to PB1033 adds `RETRY 4294967297 TIMES`, which fails in the backend with CS0221.
- **Legal source rejected:**
  - PB1162: `RETRY (N) TIMES` and `A B-AND (B)`. The lexer's subscript trigger does not look at the edition.
  - PB1164, PB1165 and PB1167: three RETURNING-conformance arms.
  - PB1193: record-level GLOBAL in the FILE SECTION is dropped.
  - PB1197: RAISE identifier-1 is narrowed by the grammar.
- **Under-rejections:** PB1139 (PB1170 and PB1172 folded), PB1171, PB1173, PB1180, PB1181, PB1182, PB1183 and PB1189. PB1177 is an
  analysis note.

**Extended:**
- PB1033: the RETRY literal crash, and the STRING POINTER cast to `long`.
- PB812: ROLLBACK SR2 on inverted procedure ranges.
- PB259: the ROLLBACK twin of EC-FLOW-COMMIT.
- PB758: END-SORT and END-MERGE.
- PB995: SORT SR5, the USING twin.
- PB994: SORT SR9.
- PB996: two collating-message citations, and `Signals/StopRun.cs` citing §14.9.43.
- PB1036: SORT GR9's FILE-OPEN sentences.
- PB397: STOP SR1 placement.
- PB867: RAISE when checking is off leaves EXCEPTION-OBJECT set.
- PB374: facilityWord gates MCS on reservation rather than on introduction.
- PB937: arm 1's premise re-derived under §4.2.6.
- PB1052 and PB1055: cluster pointers.
- PB372: the batch's dossier gaps. In all 22 subjects, sites that implement a rule without citing its clause were
  invisible to the dossier.

**Checked against trains 58 and 59 before filing.**
- PB481's data-name-shape screen already fixes finding 29 (the OCCURS key in a multi-01 SD, now COBOLNET2024 or
  COBOLNET2270) and finding 31 (the table-SORT key subscript). The registrar re-probed finding 29 and read finding 31's fix in the PB481 diff (`SortBindTable` now calls `ScreenDataNameShape`); both are recorded as leads.
- PB510's renaming of the figurative tokens did NOT fix the dropped ALL. `sr2c` still prints `[0........ ]`, so
  PB1180 stands.

**Owner questions:**
- PB1198:
  - (1) Should an `mcs-only` derived-verdicts selector stamp the 22 Annex A.3 4) RECEIVE and SEND rows?
  - (2) Does GR-14.9.51.4-24 join the family of unpopulatable-antecedent rows?
- Owner visibility is also flagged inside three notes:
  - PB1163: the A.1-166 value.
  - PB1178: whether the ERROR indication or the STATUS value takes precedence.
  - PB1192: key<1 returns '34' under GR29 b) but '24' under GR33 b).

