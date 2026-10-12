## Entry 1281 — 2026-08-09 18:06 PDT — The 7a+PB72 battery: green on every internal leg, and GnuCOBOL votes FOR the new refusals

One `bash scripts/battery.sh` run over `e219be59` (artifacts /tmp/battery-20260809-174919): FULL
greenfield Conformance 4354/4354 zero skipped (the five PB72 negatives and the admitted-cells golden in
the population) · Unit 4124/4124 · characterization 33/33 · guard-fast ALL GREEN with NIST 353 MATCH /
0 REGRESSION and a CLEAN audit — so the shared MOVE seam moved and not one internal case regressed.
The differential then did exactly what §0 predicted it might, in the best possible direction: 3
per-case flips, ALL divergence→AGREE fixes, ALL on GnuCOBOL's own syn_move syntax suite — syn_move:90
(alphabetic→numeric), syn_move:155 (alphanumeric-edited→numeric), syn_move:189 (numeric→alphabetic) —
three cases GnuCOBOL rejects that this compiler used to accept and now refuses for the same reason.
The external oracle independently confirms the Table-16 completion. The baseline is regenerated with
exactly those three rows changed (WE_ACCEPT_THEY_REJECT → AGREE_REJECT, verified by diff before
committing), attributed here and in the commit that carries them; a fresh diff against the new
baseline reads 0. §9's reference is updated; next per §0: family 7b (the OperandCategory totalization
+ its GR6c ref-mod sibling), then the BASECONVERT screen set.

