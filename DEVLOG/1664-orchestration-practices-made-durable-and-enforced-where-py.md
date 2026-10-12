## Entry 1664 — 2026-09-23 09:58 PDT — Orchestration practices made durable and enforced; where.py; where the fleet's tokens go; OWNER DECISION R41 adjudication first

**Owner, 2026-09-23:** "These improvements should be always used going forward. We don't want to forget best
practices. Additional opportunities to not rerun the same work multiple times should be found and implemented" —
"the overhead seems huge for the actual progress we make" — "All these best practices must be durably recorded for
all future to must use."

**What was fragile.** Wave 58 was dispatched from specs rendered by a chain of scratchpad scripts
(`make_w58_specs.py` → `make_w52_specs.py` → `make_w48_specs.py`) plus `GATE-WAIT.txt` and a tail file, all in ONE
session's scratchpad. The practices adopted on 2026-09-22 (BelowNormal implementer gates, never the whole assembly,
blocking waits, the STOP file, pipelined landing, the compiled-program cache) were live in this wave only because
that scratchpad survived a reboot. The repo briefs carried some of them, not all: `lander-train-brief.md` never
mentioned pipelining, and `registrar-brief.md` had neither STOP nor the blocking-wait rule.

**What changed.**
- `.claude/skills/workstream/templates/MANDATORY-PRACTICES.md` — every practice, by role, each with its reason and
  its measurement. The ONE place they live.
- `templates/dispatch-spec-implementer.md` + `make_dispatch_specs.py <groups.json>` — dispatch specs are rendered
  from the repo template, never hand-written.
- `check_practices.py` — fails when a brief or a rendered spec drops a practice. Proven to fail first: RED (6) on the
  pre-change briefs, and RED on the hand-rendered wave-58 spec (no `where.py`, no code-site rule); GREEN after.
- Every brief now opens by pointing at the practices file; `lander-train-brief.md` gains the pipelined-landing rule.
- `SKILL.md` names the file, the generator and the check as the only route.
- `scripts/spec/where.py <clause> [rule]` — where a clause is implemented, from the § citations the code already
  carries (they ARE the index): ranked files with the cited lines, plus the tests, notes and docs naming it. 0.4 s.

**Where the tokens go (measured, not estimated).** All 81 implementer/finisher transcripts of waves 45–57
(session a700c052), attributed per assistant message to the tool call it made:

| Activity | Token share |
|---|---|
| searching the codebase (grep / rg / find) | 31.7 % |
| reading files (sed / cat / head, Read) | 14.5 % |
| editing | 13.4 % |
| probe compile/run | 9.5 % |
| git | 7.9 % |
| gate + build + waiting | 9.3 % |
| cite / register / generator scripts | 7.6 % |

By role over the same session: implementers 75 %, finishers 14 %, landers 8 %, registrars 2 %, batteries 0.4 %
(3.09 B tokens including cache reads). **The landing and registration machinery is not where the cost is. Re-surveying
the codebase is: 46 % of every implementer's tokens.** Hence P6 (start from the note's code sites / `where.py`) and P7
(every lead carries its repro and code site so the registrar and the next implementer do not re-discover them).

No compiler change; no gate owed (docs + orchestration scripts). Measurement script: `.claude/skills/workstream/measure_costs.py`.

**OWNER DECISION R41 (09:35 PDT, `kb/Work/R41.md`): ADJUDICATION FIRST.** Offered rebalance ~50/50 (recommended),
adjudication first, or keep defects-first; the owner chose adjudication first. Most of the quota now goes to the
lane-3 adjudication fleets until the unadjudicated mass (~1,991 rows of GAP 2,060) is mapped; the fix lane takes only
wrong-answer and crash defects; supersedes the 2026-09-13 "defects to zero first" order. Both lane-3 templates now
honour the graceful STOP file (`args.stopFile`) and `check_practices.py` covers them. The first burst started at
09:52: 68 dossier-fed subject files (§14 + §12) against pinned tree `E:\CobolSharp-pins\adj-d1` at `68bbdb00f`.

