## Entry 291 — Substitute the XXXXX065 record-count X-card → ST115A/116A/117A SORT chain (NIST 296→297)

ST117A is the last ST near-miss: a procedural BIG-SORT that verifies a native-collating sort of every record
in a file the ST115A→ST116A chain builds and sorts. It was blocked one level up: ST115A's file-build loop
(`ADD 1 TO WRK-DU-04V00 … IF WRK-DU-04V00 GREATER THAN XXXXX065 GO TO write … GO TO loop`) compares the
counter against the **unsubstituted** X-card placeholder `XXXXX065` — NIST's "4-digit integer for the NUMBER
OF RECORDS the program is to build". Left as the literal token it is a bogus bound, so the loop never
terminates (ST115A ran away; an earlier ST117A attempt grew an 8.2 GB report before I killed it). ST117A also
`DIVIDE XXXXX065 BY 51 GIVING NUMBER-OF-SETS`, so the value MUST be a multiple of 51. Fix: substitute
XXXXX065 → **204** (= 51×4, four sets) in `NistPreprocessor`.

Substitution hazard caught before it shipped: the 5-char string "XXXXX065" is ALSO embedded inside a
baselined IX106A test-data literal — `"…XXXXXXXXXX065ALTKEY1…"` (ten X's + 065, i.e. preceded by 'X' and
followed by 'A'). A plain `.Replace("XXXXX065","204")` would corrupt that literal and break IX106A. So the
substitution is a token-boundary regex `(?<![A-Za-z0-9])XXXXX065(?![A-Za-z0-9])` — it matches only the
whitespace-bounded standalone operand in ST115A/ST117A, never the embedded substring. Verified: IX106A still
matches its baseline; ST117A passes 1/1 ("NO TEST(S) FAILED") and its 204-record dump is deterministic across
runs. ST115A (204-record builder, 000-of-000 canonical comment) and ST116A (the SORT, no CCVS report) join
the guard as NO_OUTPUT producers ahead of ST117A; only ST117A is baselined. Guard ALL GREEN:
1000 unit / 347 integration / **297 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 40 IX + 27 ST),
0 regressions. Remaining ST near-misses: ST121A (producer-ordering, 9 FAIL*) and ST137A (rc=127);
ST301M/ST302M are flagging modules (excluded).

