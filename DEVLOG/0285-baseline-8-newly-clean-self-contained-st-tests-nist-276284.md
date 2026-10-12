## Entry 285 — Baseline 8 newly-clean self-contained ST tests (NIST 276→284)

Follow-up to 284. Tested each newly-clean ST test standalone from a clean output dir to separate
self-contained tests from chain consumers. Baselined the 8 self-contained, non-vacuous ones:
ST101A (9/9), ST106A (1/1), ST131A (15/15), ST132A (6/6 — had been a runtime timeout before the
section-PERFORM fix), ST133A (18/18 — had been a vacuous 000-of-000), ST134A (4/4), ST135A (9/9),
ST136A (5/5). Guard ALL GREEN: **284 NIST** (… + 40 IX + 14 ST), 0 regressions.

Correction to 284: of the previously-vacuous trio, only **ST133A** was de-vacuumed by the section-PERFORM
fix. **ST109A, ST112M, ST122A are STILL 000-of-000** (clean = 0 FAIL*, but vacuous) — a *separate* bug, not
the section-PERFORM one; not baselined, logged as a lead.

Remaining ST (leads): chain consumers ST105A/107A/114M (consume TF001 built by the baselined ST104A/ST106A —
order after them), ST103A (← ST101A + ST102A NO_OUTPUT), ST117A (← ST116A NO_OUTPUT builder), ST121A
(← ST120A); still-vacuous ST109A/112M/122A; FAIL* ST111A/124A/126A/139A/140A/144A/146A/147A; NO_OUTPUT
ST102A/110A/113M/116A/120A/123A/137A(rc=127); timeout ST115A; ST301M flagging (excluded).

