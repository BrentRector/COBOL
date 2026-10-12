## Entry 1703 — 2026-09-25 02:16 PDT — Register: PB1580–PB1584; ledger v74 (GAP 1254); night's close

Register-only checkpoint after trains 60–62 and golden lane #2 landed (no compiler change).

- **New notes:** PB1580 (golden-lane `integrate.py` pools every overturned verdict's refs, withholding goldens from rows
  the refuter upheld, and records refs the refuter never judged — two landers worked around it by hand), PB1581 (wrong
  answer — native binary64 arithmetic never raises the size error on overflow/underflow; the PB1566 range check belongs
  on the arithmetic carrier too), PB1582 (rejects legal source — COBOLNET0836 refuses every factory METHOD-ID named NEW;
  depends on the held PB1548 branch), PB1583 (ConflictMarkerDriftTests opens tracked files without FileShare and reds
  when the Unit and Conformance legs run concurrently), PB1584 (an over-range SYMBOLIC CHARACTERS ordinal draws both
  COBOLNET2427 and COBOLNET1670 — IntegerOperandPass ignores its own slot-table row).
- **The night in numbers:** GAP 1515 → 1254 (golden lane #2: 165 rows over three batches; trains 60–62: twelve clusters;
  the R43 derived selectors alone −78). 36 new kb/Work notes (PB1549–PB1584), most found by the golden lane's refuters
  and the implementers' sibling sweeps. PB1548 (standard class BASE; ~130 OO goldens corrected to INHERITS FROM BASE) is
  built and held for the next train.
- **Ledger** artifact v74 published; trend point for ab810eb97 appended.

