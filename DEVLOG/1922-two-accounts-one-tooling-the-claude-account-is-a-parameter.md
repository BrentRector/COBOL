## Entry 1922 — 2026-10-07 15:22 PDT — Two accounts, one tooling: the Claude account is a parameter of the coordination scripts (PB2478–PB2483)

The owner moved every Opus and Sonnet dispatch, the orchestrator loop and the landings to a second Max 20x account
(claude@wiseowl.com, `CLAUDE_CONFIG_DIR=C:\Users\brent\.claude-acct2`) on 2026-10-07 13:17 PDT, keeping the first
account (brent@wiseowl.com) for Mythos work, and at 13:36 asked that account 2's week be spent before its own reset
(Saturday 10:00, not account 1's Sunday 03:00). Every coordination script assumed ONE account, and it cost real time
within the hour: at 14:44 the loop HELD on an estimated session of 122 % while account 2's `/usage` read 24 % (week
6 %, not 24 %); and the loop's own wind-down STOP (`scratch\STOP`, shared by every actor) cut the Mythos session's
sixth R1 refuter at claim H5.

`scripts/orchestrator/account.py` is now the one resolver of the account — config dir, global config, account uuid and
email, project transcript dir, the child's `CLAUDE_CONFIG_DIR`, and the `model_rules.json` `accounts` table that carries
each account's weekly reset — and every script that read Claude's own files uses it (budget.py, next_unit.py,
ledger_plan, usage_report.py, orchestrate.ps1 and start-session.ps1 `-ConfigDir`, the build-guard hook, tooling_check).
`budget.py` keeps readings and telemetry per account (`user.account_uuid`); an unstamped reading anchors no account.
The calibration was refit ×4 from account 2's one measured interval (13:34 → 14:54: 6 % week, 24 % session), which the
estimate now reproduces; the implementer showed the account filter alone was NOT the cause (account 2's own telemetry
gave 25 points under the old constants), so the constants were account 1's guesses. A second account-1 interval refits
them there (lead). STOP is scoped (PB2483): the owner's global `scratch\STOP` stops everything; a fleet's own
`STOP-<scope>` (the loop's `STOP-loop`) stops only that fleet; `make_dispatch_specs.render()`, `wf_rolling_wave.js`,
the lane-3 workflows, MANDATORY-PRACTICES P3 and `check_practices.py` carry both. `account-profile.ps1` seeds a config dir
from `accounts.seed` (the hand-built account-2 dir lacked the Chrome flags, so the operator session had no Chrome
tools); the ledger artifact URL is per account (`ledger_state.py mark-published --url`), and `gen_ledger.py` renders to
`<coord>\ledger.html` by default; `mailbox.py` enforces the two-session mailbox's message shape (an operator message had
carried a mangled `\b` path) and the supervisor posts `publish` messages.

Gates: implementer GREEN (run 20261007T220141Z-0ef402; run 1 was RED only on the known PB1953 leg-2 selection
recurrence, appended there), Linux gate GREEN, semgrep verify PASS; self-tests account 16/16, budget 46/46, mailbox
27/27, plan_wave 65/65, test_orchestrate 135/135. Report `w1033t-PB2478-report.md`. Owed after landing: stamped meter
readings from each account, `account-profile.ps1` for account 2 (owner, with no account-2 session running), the
account-2 ledger URL recorded, the mailbox README pointed at `mailbox.py`.

