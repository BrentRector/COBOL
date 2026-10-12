## Entry 1229 — 2026-08-08 13:39 PDT — The R17+R19+R20 battery lands ALL GREEN — the grammar change moved nothing

The comprehensive battery the last session left in flight completed just after this session opened:
every leg green — Conformance 4275/4275 zero skipped (11 m 52 s), Unit 4086/4086, characterization
33/33, guard-fast ALL GREEN with NIST 353 MATCH / 0 REGRESSION and a CLEAN audit, and the GnuCOBOL
differential at 1323 cases, 0 per-case flips. The differential was the leg the handoff said to watch
— R17 rewrote the signed floating-point literal into one token across every lexer region — and it
moved nothing: not one of 1,323 real programs changed its accept/reject verdict.

Battery reference advanced to the bc739114 tree (the docs-only handoff commit e922c011 landed after
the battery's build phase, so the measured code tree is R20's). The R00-audit orphan wave continues
from the top of work.py next: R21, the CURRENT-DATE / FORMATTED-CURRENT-DATE clock-seam bypass.

