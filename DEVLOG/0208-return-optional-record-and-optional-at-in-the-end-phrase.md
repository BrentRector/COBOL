## Entry 208 — 2026-05-28: RETURN — optional RECORD and optional AT in the END phrase (COMPILE_FAIL 4→2)

SM105A/SM205A failed on `RETURN SORTFILE-1E END PERFORM …`: two grammar gaps. `RETURN fileName
RECORD` required the RECORD keyword (these statements omit it), and `returnAtEndPhrase` required
the literal `AT` before `END` (ISO §14.9.39 marks AT optional). Made `RECORD?` optional and `AT?`
optional in both the AT END and NOT AT END branches. Both programs now compile and run
(SM105A → 6 FAIL*, SM205A → 8 FAIL* — value work remains). COMPILE_FAIL 4→2 (only SM206A's
multi-line pseudo-text REPLACING and SM208A's copybook string literal remain). Guard ALL GREEN
(1000 / 336 / 137).

