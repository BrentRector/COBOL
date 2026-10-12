## Entry 1383 — 2026-08-29 08:45 PDT — Battery #32 ALL GREEN, zero differential flips — the PB137–PB140 batch confirmed whole

One `bash scripts/battery.sh` run on the PB140 tree `e79e8e20`: FULL greenfield Conformance 5052/5052 ·
Unit 4612/4612 · characterization 33/33 · NIST 353 MATCH / 0 REGRESSIONS (audit CLEAN) · GnuCOBOL
differential 1323 cases, ZERO per-case flips, totals unchanged 572/474/206/71 — notably the
file_sharing_seq REWB '43' re-derivation (PB140's §9.1.13.7 3 reading) flipped no external verdict. The
population grew by 27 Conformance tests over #31 (PB137 COMMIT/ROLLBACK, PB138 CONTINUE AFTER, PB139
ACCEPT, PB140 the I-O status discipline). ONE legacy-suite intermittent, named and dispositioned per the
no-unnamed-flake rule: CobolSharp.Tests.Integration DataTests.Renames_Through_Synonym failed once in the
parallel Integration leg with NO captured assert message, then passed a clean serial re-run AND a clean
full-suite re-run on the same build — filed as kb/Work PB166 (process-only; the guard's failure-detail
capture gap is part of the note). §0's battery reference now points at #32.

