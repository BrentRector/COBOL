## Entry 248 — Exclude CCVS column-7 'H' (multi-reel CLOSE … REEL) lines → SQ109M

SQ109M writes 750 records then reads them back, but only 325 were written. Cause: at record 325 the
write loop has `CLOSE SQ-FS1 REEL` on a **column-7 'H'** indicator line — the CCVS marker for the
optional multi-reel/multi-volume tape feature. We executed it, and since CobolSharp has no multi-reel
support, `CLOSE … REEL` closed the whole file → the remaining 425 WRITEs hit a closed file.

The CCVS pairs each 'H' block with an 'I' replacement line (e.g. `MOVE "CLOSE REEL DELETED" TO
RE-MARK`). The 'H' lines carry the statement's terminating period, so deleting them makes the
following 'I' line become the controlling `IF`'s body — the intended no-multi-reel program. Added
'H'/'h' to the indicator-column exclusion set in `ReferenceFormatProcessor.ConvertFixedToFree`
(alongside the existing D/S/Y/P/J), so 'H' lines become comments and 'I' lines (normal) are kept.
Surveyed all NIST programs: 'H' appears only in SQ109M, ST112M, ST132A — every occurrence is a
`CLOSE … REEL` block, so the exclusion is safe and on-pattern.

Result: **SQ109M CLEAN** (0 FAIL*, deterministic), baselined. **NIST baselines 193 → 194** (… **37
SQ** …). Full guard ALL GREEN: 1000 unit / 348 integration (347+1 skip) / 194 NIST, 0 regressions.
Remaining SQ FAIL*: SQ110M (reads 196/649 — a DISTINCT record-count loss, no REEL), SQ116A
(variable-record REWRITE), SQ124A (CLOSE REEL status 07 + WRITE-status), SQ105A/SQ114A (runtime hang).
SQ session arc this run: 33 → 37 baselined (+SQ106A/107A/109M/214A).

