## Entry 212 — 2026-05-29: COPY REPLACING — debug lines participate, comment lines do not (SM206A CLEAN)

PST-TEST-009 (SM206A) copies KP008 — `PERFORM FAIL.` / a `D` debug line `THIS IS GARBAGE.` /
`SUBTRACT 1 FROM ERROR-COUNTER.` — with `REPLACING ==FAIL. THIS IS GARBAGE. SUBTRACT 1 FROM
ERROR-COUNTER. == BY ==PASS. ==`. SM206A has no `WITH DEBUGGING MODE`, yet the expected result is
`PERFORM PASS.` — so the debug line's content must participate in the match. That is correct
COBOL-85: COPY/REPLACE is text manipulation performed before the debugging-mode determination, so
a debug line is source text for matching (only an ordinary comment line is not a text word — see
Entry 210, REP-TEST-6, which needs comment lines skipped).

The normalizer renders comment lines as `*> …` and debug lines as `*> DEBUG: …`. The REPLACE
tokenizer now skips only the `*> DEBUG:` prefix (tokenizing the content) while still dropping
ordinary `*>` comment lines whole. The match then spans the debug line and the replacement
consumes it, yielding `PERFORM PASS.`. SM206A → CLEAN; SM CLEAN 10→11. Guard ALL GREEN
(1000 / 336 / 137).

