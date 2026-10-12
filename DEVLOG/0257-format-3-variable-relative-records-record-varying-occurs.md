## Entry 257 — Format-3 variable relative records (RECORD VARYING + OCCURS DEPENDING inside) → RL210A/RL211A

RL210A/RL211A write a relative file with two 01 formats — `RL-VS1R1-F-G-120` (fixed 120) for records
1–200 and `RL-VS1R2-F-G-140` for 201–500, the latter a `RECORD IS VARYING` record (no FD-level
DEPENDING) containing `…125-140 PIC X OCCURS 1 TO 16 DEPENDING ON …121-124`, written with the count = 16
and `RL-GROUP = "ABCDEFGHIJKLMNOP"` (length 124+16 = 140) — then read everything back and verify the ODO
content. They were the long-standing relative format-3 gap (300/500 FAIL*, COMPUTED came back empty).

Diagnosis by instrumentation: the WRITE side was already correct (logged 200 writes of length 120 + 300
of length 140 with content) thanks to the variable-write work in 256. The fault was the **READ buffer
size**. `ResolveReadRecordLocation` reads a varying record into the *largest* 01 (`RL-VS1R2-F-G-140`),
but resolved that location *without* `receiving: true`, so for an 01 containing an OCCURS DEPENDING
table the buffer was sized by the depending item's value — which the program had just `MOVE SPACES`'d to
zero before the READ — truncating the table's bytes (offset 124–139) to nothing. Fix (one line): resolve
the read-into-largest location as a RECEIVING operand (`ResolveLocation(largest, receiving: true)`), so
the buffer uses the record's MAXIMUM length (the same MAX-length receiving rule READ INTO already used,
DEVLOG 246). The ODO content now reads back in full.

Result: **RL210A and RL211A CLEAN** (deterministic, self-contained; previously dropped as a vacuous/
failing baseline). No regression to the simple-DEPENDING varying tests (RL206A/RL207A) or the sequential
varying tests (SQ220A/SQ221A) — the change only widens the read buffer to the declared max. Baselined
RL210A/RL211A after the RL206A→RL207A varying chain (they open OUTPUT, self-contained; the following
fixed-format producer RL209A re-creates TF021). **NIST baselines 210 → 212** (94 NC + 42 IF + 12 SM + 4 IC
+ 39 SQ + **19 RL** + 2 IX). Full guard ALL GREEN: 1000 unit / 348 integration / 212 NIST, 0 regressions.
Remaining relative: RL208A (the 5-record RL207A→RL208A delete/update chain gap).

