## Entry 1713 — 2026-09-25 16:07 PDT — Golden lane 2 tooling: lane-unique slugs (PB1591), read-only writer/fixer roles

Golden lane 2 (the 117 CONFORMS-test-needed rows + 3 DNS witnesses, 120 rows in 11 files) drafted and validated on a
pin of main dac7c9d67. Its misc-p2 writer returned 12 rows, NONE from its 13-row input: they were golden lane 1's
misc-p2 rows, read from the committed `adjudication/golden-lane-1/scratch/in/` copy that shares the bare slug. The
tally read 119/120 ("one row dropped"); only a per-slug set comparison (overlap 0, foreign 12) showed the file wholly
wrong. Filed as kb/Work PB1591 (half). Fixed here: `build_lane1_inputs.py` now REQUIRES a lane prefix and names
slugs `<prefix>-<family>[-pN]`; misc-p2 is re-run as `gl2-misc-p2`, its foreign drafts quarantined. Open: the
integration step must refuse a report row outside its slug's input (applied by hand in lane 2's landing).
Also: the wf_lane1_draft template's writer and fixer now run as `cobol-adjudicator` — they write only under the
scratchpad, so the read-only hook enforces what the prompt said.

Validation (10 good files): 63 drafts pass; 4 suspected compiler defects, all already registered (PB1531 ×3 —
EC-IMP- suffixes accepted; PB1523 — a GLOBAL elementary REDEFINES subject fails the C# backend); 2 draft errors
(files named with the reserved words PF/RF; the validator's rename reproduces the .out exactly). The goldens land
in a separate train.

