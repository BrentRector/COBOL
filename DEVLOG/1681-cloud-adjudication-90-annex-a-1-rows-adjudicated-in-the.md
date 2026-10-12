## Entry 1681 — 2026-09-24 14:46 PDT — Cloud adjudication: 90 Annex A.1 rows adjudicated in the cloud; routines bill the plan, web/--cloud sessions bill the credit; handoff PB1522

**What.** The owner asked what COBOL work best fits the $250 cloud-session credit (expires 2026-11-04). Answer:
adjudication (R41), cheapest GAP per dollar and read-only. A calibration session adjudicated 10 Annex A.1 `DOC-A.1-*`
rows, then wave 1 ran four parallel cloud sessions over 80 more — all against pinned main, each with an independent
refuter over its CONFORMS verdicts, results on `claude/adj-cal-1` and `claude/adj-a1-w1-s1..s4` (never main).
**90 rows: 7 CONFORMS · 47 DIVERGES · 18 PARTIAL · 8 NOT-IMPLEMENTED · 10 NEEDS-OWNER-DECISION;** refuters overturned
3 of 10 CONFORMS (A.1-72, -81, -113 → PARTIAL). Dominant mechanism (≈ 69 rows, harm silent): the code fixes an
implementor determination that `docs/CONFORMANCE.md` never files under its own `DOC-A.1-N` key. Also surfaced:
6 wrong-answer, 2 crash rows (indexed OPEN OUTPUT on a full device → unhandled IOException; dynamic-capacity table
growth → OutOfMemoryException), 7 under-rejects. Not yet recorded in the inventory — the registrar is PB1522 step 2.

**Measured cost and billing.** ~11–19 min and **~$0.34 per row** (one session reported $6.73 for 20 rows, 99 % cache
hits). ⛔ Sessions launched by **RemoteTrigger routines bill to the plan, not the credit** — six routine runs (~$30)
left it at $250 — while a session started **from the claude.ai/code web page** drew it at once ($250 → $249);
`claude --cloud` qualifies too but needs an interactive terminal. Routine sessions also never appear in the web
sidebar.

**Handoff.** Because a cloud VM cannot read the owner's local memory, the whole plan now lives in the repo:
`kb/Work/PB1522.md` (state, the billing finding, how to COMPUTE the remaining 118 rows, the registrar step, what a
cloud session must not do) and `.claude/skills/workstream/templates/cloud-adjudicate-brief.md` (the per-batch brief:
per-row checkpoint appends, a push every 5 rows, the refuter, the delivery shape). A cloud orchestrator runs wave 2
as parallel subagents inside ONE credit-billed session; landing on main stays with the local orchestrator
(`push-main.sh`).

