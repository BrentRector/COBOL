## Entry 250 — CLOSE … REEL/UNIT: status 07, file stays open (SQ124A)

SQ124A `CLOSE SQ-FS4 UNIT` expected I-O status **07** with the file still open (it then WRITEs a
second record and checks status 00); we gave 00 and the WRITE then failed 48. Cause: `LowerClose`
routed every non-LOCK option (including REEL/UNIT) to `FileRuntime.CloseFile`, fully closing the file.
Per ISO §9.1.13.2 item 6, `CLOSE … REEL/UNIT` (and NO REWIND / FOR REMOVAL) on a non-reel/unit (disk)
medium completes with status 07; and per §14.9.10 the REEL/UNIT phrase leaves the file connector OPEN
(it advances past the current volume — a no-op on disk). The binder already captured
`CloseOption.Reel`/`Unit`, so: added `FileRuntime.CloseReelUnit` (sets status 07 if open, 42 if not;
no close) + `FileStatus.CloseNonReelMedium = "07"`, routed Reel/Unit to it in `LowerClose`, and added
the CIL dispatch. Plain CLOSE and NO REWIND keep the standard close (NO REWIND's 07 on disk is a
deferred nicety — no test needs it).

Result: **SQ124A CLEAN** (0 FAIL*, deterministic), baselined. **NIST baselines 195 → 196** (… **39
SQ** …). Full guard ALL GREEN: 1000 unit / 348 integration (347+1 skip) / 196 NIST, 0 regressions.
SQ session arc this run: 33 → 39 baselined (+SQ106A/107A/109M/110M/124A/214A). Remaining SQ FAIL*:
SQ116A (variable-record REWRITE — "FROM AREA CLOBBERED"), SQ105A/SQ114A (runtime hang).

