## Entry 249 — Exclude CCVS column-7 'E' (multi-unit CLOSE … UNIT) lines → SQ110M

SQ110M is the `CLOSE … UNIT` twin of SQ109M (Entry 248): at record 196 it has `CLOSE SQ-FS3 UNIT` on
a **column-7 'E'** indicator, paired with an 'F' replacement line (`MOVE "CLOSE UNIT DELETED" TO
RE-MARK`) — the multi-volume tape feature again, but UNIT rather than REEL. Executing it closed the
file mid-loop (196 of 649 records written). Added 'E'/'e' to the indicator-column exclusion set.
Surveyed the suite: 'E' appears ONLY in SQ110M (the CLOSE UNIT block), so the exclusion is safe.
Deliberately did NOT exclude 'F' — although it is the UNIT replacement here, 'F' is also used as
ordinary code across the IC suite (`,ICnnnA` continuation cards), so it must stay a normal line (its
default), which correctly keeps SQ110M's replacement too.

Result: **SQ110M CLEAN** (0 FAIL*, deterministic), baselined. **NIST baselines 194 → 195** (… **38
SQ** …). Full guard ALL GREEN: 1000 unit / 348 integration (347+1 skip) / 195 NIST, 0 regressions.
SQ session arc this run: 33 → 38 baselined (+SQ106A/107A/109M/110M/214A). Remaining SQ FAIL*: SQ116A
(variable-record REWRITE), SQ124A (CLOSE REEL status 07 + WRITE-status), SQ105A/SQ114A (runtime hang).

