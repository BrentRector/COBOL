## Entry 214 — 2026-05-29: SM suite baselined — 12 clean baselines locked into the guard (149 NIST guarded)

With the COBOL-85 source-text-manipulation feature set complete (COPY, COPY REPLACING, REPLACE,
multi-library COPY, text-word matching), SM is at 12/17 CLEAN. Captured a baseline for each clean
program and added them to `scripts/guard.sh`. Three of them (SM103A/SM106A/SM203A) write their
CCVS report to the shared `print-file.txt` rather than `<lc>.txt`, so the baselines were taken
from there; the guard already tries all three report locations, and all 12 MATCH.

The guard now regression-guards **149 NIST programs (95 NC + 42 IF + 12 SM)**, ALL GREEN
(1000 unit / 336 integration). The remaining 3 SM fails are in other subsystems, not COPY:
SM104A reads a file produced by SM103A (a CCVS producer/consumer pair — file orchestration,
belongs with the SQ/IX/RL file work) and SM105A/SM205A exercise SORT with copied descriptions
(the sort produces empty/wrong output — belongs with the ST suite). SM301M/SM401M are flagging
modules (no CCVS report), excluded by design.

