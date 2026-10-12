## Entry 240 — File-I/O wall, part 4: RL + IX free wins baselined (NIST baselines → 181)

The FILE-CONTROL/READ/USE grammar (237) and FILE STATUS/REWRITE (238) fixes were not SQ-specific, so
the relative (RL) and indexed (IX) suites benefited for free. Surveys: **RL CLEAN=5, FAIL*=6,
COMPILE_FAIL=12; IX CLEAN=1, FAIL*=1, COMPILE_FAIL=25.** Baselined the already-CLEAN ones —
RL101A RL201A RL209A RL210A RL302M and IX302M — into `tests/nist/valid/` + `scripts/guard.sh`
(deterministic MATCH confirmed). **NIST baselines 175 → 181** (94 NC + 42 IF + 12 SM + 4 IC + 23 SQ +
5 RL + 1 IX). Full guard ALL GREEN.

IX/RL COMPILE_FAILs (37 total) are indexed/relative-specific PARSE forms not seen in SQ — dominant:
`no viable alternative at input 'INVALID'` (×8, INVALID KEY phrase placement), `… 'EQUAL'` (×6,
START … KEY IS EQUAL relational), and several RECORD KEY / ALTERNATE KEY data-name forms. These are
the next focused pass (each a distinct grammar/spec item). The SQ/RL/IX FAIL* + RUNTIME tail
(indexed/relative runtime correctness) follows.

Session arc on the file-I/O wall: SQ 2→75 compiling / 0→23 baselined; RL 5 + IX 1 baselined; NIST
baselines 148→181 across DEVLOG 237–240 (+ IC 16→20 and the spec/cleanup work in 231–236).

