## Entry 1833 — 2026-10-01 12:54 PDT — Fleet tooling after wave 1001: Sonnet implementers by argument, LSP diagnostics are acted on, the ledger refreshed, the witness-reuse plan

Wave 1001 ran four Sonnet implementers and one Opus lander train (Entry 1832); this entry records the process changes made around it.

- **`implementer_model` argument.** `wf_rolling_wave.js` hard-coded `model: 'opus'` for implementers, which would have made
  the owner's allowance for Sonnet implementers (5-10 bugs per commit) unusable. The model is now `args.implementer_model`
  (default `opus`); the lander stays Opus. Documented in the workstream SKILL.md args line.
- **Act on the language server's diagnostics (owner, 2026-10-01).** CS8019 hints on in-flight worktrees scrolled past for
  two hours with nobody fixing them. MANDATORY-PRACTICES P13 now says a diagnostic on a file an agent edited is fixed in the
  same change or named in the report; `dispatch-spec-implementer.md` carries an `LSP DIAGNOSTICS` paragraph and
  `check_practices.py` refuses a rendered spec without it. Wave 1001's specs predate the rule. The public skills repo
  carries the same rule (`automating-agent-guardrails`, commit 4acad11, eval Delta +0.50), labelled not yet validated.
- **Conformance ledger.** The published page was two days stale (2026-09-29, GAP 818). `gen_ledger.py` regenerated it at
  `b62408c2` (GAP 710) and the trend series gained that point.
- **Witness reuse.** Probing the owed rows (state GAP, resolving verdict) found 59 in 39 subject clusters, 9 of them Annex A.1
  documentation rows. A row's `test-ref` is a list, so one test can witness many rows; the queued batch re-cites existing
  tests first and writes a golden only where none names the clause (variable-length data items, DISPLAY directive).
- **Watchdog.** `stall_watch.py` exited at launch when the agents' transcripts were still empty, then was stopped by the
  30-minute default background limit; it needs `timeout: 7200000` and a launch after the first agent transcripts exist.
- **Measured.** The workflow (4 implementers + 1 lander) took 1 h 55 min and 2.77 M subagent tokens; the weekly meter went
  70 % to 73 %.

