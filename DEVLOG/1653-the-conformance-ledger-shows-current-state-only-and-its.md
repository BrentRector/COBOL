## Entry 1653 — 2026-09-22 23:13 PDT — The Conformance Ledger shows current state only, and its "since battery" delta is measured, not guessed

Owner: "Clean up the COBOL artifact. It has far too much historical dev log like info." The ledger's one
hand-written section, `ledger-in-flight.md`, had grown to **73 KB** of dated campaign narrative ("Where the campaign
stood on …" for every day since 2026-09-13, plus two cards of owner questions and open builds, several of them
long since landed) — a second DEVLOG inside the status page, which its own header forbids. It is now **2.9 KB**: one
panel naming what is landing and what is being implemented, a pointer to `kb/Work/` for open owner decisions (with
the standing ISO → GnuCOBOL → IBM/Micro Focus precedence), and a pointer to `DEVLOG.md` for history. The page went
from 107 KB to 38 KB; every number on it is still computed.

**A generator defect the cleanup exposed.** The GAP tile read "-1506 since battery #85" and the trend chart put
battery #85's mark on an August point. `render()` took the FIRST trend point carrying any battery flag — the oldest
— and labelled it with the CURRENT battery's number; `trend_svg()` did the same. The series records a point only when
the generator happens to run, so even "the latest point before the battery" can predate whole trains (it gave -136).
Fixed at the root in `scripts/spec/gen_ledger.py`: `gap_at(sha)` measures the GAP of the inventory AS COMMITTED at
the battery's tree (2198 at `0abe2c80f`), so the tile now reads **-68 since battery #85**, which is right; the chart
mark is placed only on a point recorded at that battery (`battery_point`), never on a stand-in. Published as v63.

