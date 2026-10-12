## Entry 1309 — 2026-08-18 06:35 PDT — Battery #8 (PB88 + PB80/PB89): green once the two flips and the one lost result are attributed; the guard re-observes a vanished program

**The battery for the PB88 + PB80/PB89 batch (tree `3793750a`):** Conformance **4675/4675** · Unit **4196/4196** ·
Characterization 33/33 · legacy guard Unit 1203/1203 + Integration 503/503 · differential 1323 cases. Two things
kept it from ALL GREEN, both explained:

- **Two differential flips, both PB88's and both in the direction the differential exists to confirm** —
  `syn_misc:3789` (STRING WITH POINTER over a non-integer, §14.9.43.3 SR7) and `syn_misc:3903` (UNSTRING
  DELIMITER IN / COUNT IN without DELIMITED BY, §14.9.48.3 SR7) went WE_ACCEPT_THEY_REJECT → AGREE_REJECT: GnuCOBOL's
  permissive default already rejected both, and COBOLNET1651 now does too. The two baseline rows are flipped in
  this commit, attributed here.
- **One lost NIST result in the legacy guard — `SQ201M` had NO verdict line at all** (352 MATCH, audit missing=1).
  Its single-program group emitted neither stdout nor a byte of stderr; re-run alone through the same group runner
  it is MATCH, so the 353/0 baseline stands. The gap it exposed is real, though: `guard-fast.sh` step 3b re-observed
  only programs that had SAID "NO-VERDICT" — a program whose group vanished entirely was reported by the audit and
  never re-taken. `LOST` is now the NO-VERDICT set ∪ (population − observed), through the same serial re-observation;
  proved on a synthetic results file (a vanished B2 and a NO-VERDICT C3 both re-observed).

Housekeeping: DEVLOG 1308's stamp was written from an estimate (06:20) — corrected to the measured 06:11.

