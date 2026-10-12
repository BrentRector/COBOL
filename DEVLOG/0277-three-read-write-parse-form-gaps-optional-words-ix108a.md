## Entry 277 — Three READ/WRITE parse-form gaps (optional words) → IX108A/IX211A

Three CCVS forms used optional words the grammar required, blocking compilation:
- **IX108A** — `WRITE rec NOT INVALID MOVE …` (only the NOT INVALID KEY branch, no INVALID KEY branch). The
  five `*InvalidKey` rules (read/write/rewrite/delete/start) required the INVALID branch first; ISO §14.9.x
  makes both branches independently optional. Added a standalone `NOT INVALID KEY? statementBlock`
  alternative to all five.
- **IX211A** — `READ IX-FD1 NEXT AT END …` (NEXT without RECORD). `readDirection` required `RECORD`; it is an
  optional word (ISO §14.9.30) — `(NEXT | PREVIOUS) RECORD?`.
- **IX208A** — `READ IX-FD1 RECORD KEY IX-FD1-ALTKEY1` (KEY without IS). `readKey` required `IS`; optional
  word — `KEY IS? dataReference`.

IX108A/IX211A → CLEAN, baselined. IX208A now compiles but has a 9-FAIL* alternate-key runtime tail (START
GREATER on an alternate key, alt-key READ sequencing) — not yet baselined. Guard ALL GREEN: 1000 unit / 347
integration / **262 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 32 IX), 0 regressions (the three
optionality relaxations only accept previously-rejected forms; every existing baseline parses identically).

