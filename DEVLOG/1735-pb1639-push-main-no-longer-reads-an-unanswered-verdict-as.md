## Entry 1735 — 2026-09-27 05:26 PDT — PB1639: push-main no longer reads an unanswered verdict as red

Landing ca28783e3 ended with "⛔ CI IS RED ON ca28783e3035 — AND IT IS ALREADY ON MAIN", yet that main run
(36318722784) concluded success. The script's single `gh run view --json conclusion` read came back empty (a
transient API failure; job enumeration failed in the same breath), and `!= success` turned the silence into red. It
now re-reads for up to two minutes and reports `⚠ UNVERIFIED` (exit 3) when no verdict arrives: a missing observation
is neither a negative nor a positive. I confirmed main green by hand (`gh run list`: 36318722784 completed/success)
before landing anything on top.

