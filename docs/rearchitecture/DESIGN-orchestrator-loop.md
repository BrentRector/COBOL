# DESIGN — The unattended orchestrator loop

Status: PROTOTYPE (built, not yet run against a real session; kb/Work PB1981 items 1-3 plus the supervisor).
Scope: a PowerShell supervisor that runs the fix lane as a sequence of short, FRESH headless `claude -p` sessions,
each doing ONE bounded unit of judgment, and the deterministic scripts that do everything else.

## 1. Why this shape

Measured on 2026-10-04: the orchestrator main thread averaged about 350k tokens of context per model call and was
about 8 % of spend; the agents it dispatched were the other 92 %. The cost of an orchestrator turn grows with its
context, and compaction keeps the context large. A fresh session per unit starts small every time, and its handoff
file is the only state it inherits. Everything that needs no judgment (allocating ids, choosing the next wave,
estimating the quota, checking the inventory did not regress, deciding which unit runs next) is a script, because
local CPU time is free and tokens are not. The LLM is used for judgment only: reading agent reports, resolving a
dropped cluster or a merge conflict, adjudication, and recognising a decision only the owner can make.

The metric is the one the project already has: GAP rows in `tests/version-matrix/traceability-inventory.json`
closed per weekly-quota point.

## 2. Components

| Path | Role |
|---|---|
| `scripts/orchestrator/orchestrate.ps1` | the supervisor loop (section 4); PowerShell 7 |
| `scripts/orchestrator/next_unit.py` | the deterministic choice of the next unit (section 3.2) |
| `scripts/orchestrator/steer.ps1` | the OPERATOR's one way to name the loop's next unit: a validated `handoff.operator.json` (section 4.8) |
| `scripts/orchestrator/units/*.md` | `common.md` (the rules every unit obeys) plus one short prompt per unit type; they reference files, never paste them |
| `scripts/orchestrator/handoff.schema.json` | the contract between a unit and the supervisor (section 5) |
| `scripts/orchestrator/alloc.py` | the one locked allocator for DEVLOG numbers, `PB` ids and `COBOLNET` codes (section 6) |
| `scripts/orchestrator/inventory_ratchet.py` | the closed-rows ratchet (section 7) |
| `scripts/orchestrator/budget.py` | the weekly and session quota estimator (section 8) |
| `scripts/orchestrator/plan_wave.py` + `model_rules.json` | the deterministic wave planner and its routing and cost constants (section 9) |
| `scripts/orchestrator/coord.py` | names the coordination directory, loads `model_rules.json`, reads and writes JSON atomically through `scripts/sharedfile.py` (the one Windows share-retry rule, which `gate_slot.py` uses too; kb/Work PB2564), and holds its one short mutex (`locked(cdir, name)`: the allocator's and the landing lease's) |
| `scripts/orchestrator/landing_lease.py` | the landing lease: one lander on main at a time (section 4.7) |
| `scripts/orchestrator/mailbox.py` | the two attended sessions' mailbox: send, list, take, finish and watch messages, and `operator-session.json` (section 15) |
| `scripts/account-profile.ps1` | seeds a named account's config dir from the default one (section 2.1) |
| `scripts/orchestrator/account.py` | the ONE resolver of the Claude account a script runs as: its config dir, its global config and account id, its row of `model_rules.json` `accounts` (section 2.1) |
| `scripts/orchestrator/watch_agent.py`, `watch-agent.ps1`, `open-watchers.ps1` | the token-free view of running agents in Windows Terminal tabs (section 13) |
| `scripts/orchestrator/test_*.py`, `test_orchestrate_*.ps1`, `testdata/` | the self-tests and their fakes (section 12) |

The coordination directory lives OUTSIDE every git worktree so that no worktree, branch switch or `git clean` can
lose it, and every session and agent sees the same copy. Default `E:\COBOL-coord`; override with the environment
variable `COBOL_COORD_DIR` (every Python tool) or `-CoordDir` (the supervisor, which exports `COBOL_COORD_DIR` to
the tools it runs). Contents:

| File | Writer | Meaning |
|---|---|---|
| `alloc.json` | `alloc.py` | the highest value reserved so far per kind |
| `alloc.lock` | `alloc.py` (through `coord.locked`) | the allocator's lock (atomic create; stale after 60 s) |
| `landing-lease.json` | `landing_lease.py`: every lander (`acquire`, `renew`), `push-main.sh` (re-take, renew, release) | the LANDING LEASE (section 4.7): `{holder, reason, worktree, branch, host, acquired_at, heartbeat_at, expires_at, ttl_min}`, absent when no lander holds main |
| `landing-lease.lock` | `landing_lease.py` (through `coord.locked`) | the lease's read-modify-write lock (the same mutex as `alloc.lock`) |
| `landing-lease.jsonl` | `landing_lease.py` | one line per `acquire`, `takeover` (with the dead lease's holder and why it was dead) and `release` (minutes held, outcome) |
| `readings.json` | the `meter` unit (via `budget.py --record`) | owner-meter readings, a list of `{noted_at, account, weekly_pct, session_pct, session_reset}`; each account reads only its own (section 8) |
| `orchestrate.lock` | the supervisor | one instance only: `{pid, started_at, host}` |
| `STOP` | the owner (`stop.ps1`) | closes work down as soon as possible without losing any: a running unit is wound down (below), then the loop ends; `stop.ps1 -Clear` removes it |
| `checkpoint.json` | the supervisor (`checkpoint.py`) | the running unit's frequent handoff (section 5.1); moved to `logs\` when the unit ends, so a survivor means the supervisor died |
| `milestones.jsonl` | the running unit's model | one line per milestone; moved to `logs\` when the unit ends |
| `STOP-UNIT` | the supervisor | asks the running unit to start no new step, launch no fleet (`dispatch_guard.py` refuses one in a loop unit) and hand off once its current step ends (section 4.3); created for either context cap and for `STOP` |
| `scratch\STOP-loop` | the supervisor | the LOOP's fleet stop (section 4.6), created with `STOP-UNIT` for `STOP` and for the HARD context cap only, never for the soft cap (section 4.3): every implementer and lander the loop dispatched checkpoints, commits and returns `SPLIT`; removed at the start of the next unit |
| `scratch\STOP` | the owner (`stop.ps1 -Global`) | the GLOBAL stop (section 4.6): every agent of every session obeys it, and the loop ends; nothing but the owner's `stop.ps1 -Clear -Global` removes it |
| `handoff.json` | the running unit | its handoff (section 5); the supervisor archives it per unit. One present when a unit STARTS was written by no unit of this run (a hand edit): the supervisor archives it as `logs\<time>-<unit>.handoff.orphan.json` and says so; it steers nothing (section 4.8) |
| `handoff.last.json` | the supervisor | the newest VALID handoff, which the next unit reads and `next_unit.py` decides from: a unit's own, one the supervisor synthesized for a dead unit (section 5.1), or the operator's steer once its unit starts (section 4.8) |
| `handoff.operator.json` | the operator (`steer.ps1`) | the operator's pending steer (section 4.8): validated against `handoff.schema.json`, it wins the next choice over every other handoff and is consumed (archived beside the unit's log, copied to `handoff.last.json`) when its unit starts |
| `scratch\` | the units and their fleets | the fleet scratch directory (`{SCRATCH}`): specs, `groups.json`, the Workflow args, and `reports\`, which `plan_wave.py` reads for finishers; it persists across units |
| `clusters-open.json`, `clusters-half.json` | `plan_wave.py` | the latest `fix_clusters.py --json` views it planned from |
| `clusters-campaign-<lead>.json` | `plan_wave.py --cluster` | the latest campaign view it planned from: the open and half clusters, `deps` and `waiting` (section 9.1) |
| `inflight-groups.json` | `plan_wave.py` (without `--dry-run`), and `scripts/hooks/dispatch_guard.py` for every write-capable Agent call and every Workflow call it admits | the dispatch ledger: every group planned or dispatched by hand whose notes are not all landed or retired, with the files it was planned on and when (`at`); the campaign lane's file-set partition reads it, so a dispatched group is in flight before its agent has a worktree or a commit, and the landing check (`landing_check.py`, run by `push-main.sh`) reads it for a landing's declared set, its dispatch order and an in-flight branch's identity (the groups whose declared files its diff meets); a wave entry never expires and an aborted one is released with `plan_wave.py --release <wave|group|note>` (section 9.1; kb/Work PB2118 Drafts 7–10) |
| `member-index/<treeKey>.members.json.gz` | `plan_wave.py --cluster` (`member_index.current`) | the member index regenerated for the planner's own tree when no committed record has its `treeKey`: the planner never plans on a stale index (kb/Work PB2118 Draft 9) |
| `scratch/train<label>-manifest.json` | each train lander, first (`wf_rolling_wave.js` gives it the JSON) | a train's members: per cluster its notes, branch, worktree, base and head. The landing check reads every manifest: a member of the landing's own train is not in flight against it, and another train's member is identified by its notes (kb/Work PB2118 Draft 10) |
| `units.jsonl` | the supervisor | one line per unit (section 4.5) |
| `OWNER-QUESTIONS.md` | the supervisor | questions only the owner can answer; the loop stops after writing one |
| `logs\` | the supervisor | the raw stream-json of every unit, its archived handoff, and the breaker's notes |

### 2.1 Two Claude accounts (kb/Work PB2478-PB2483)

Owner 2026-10-07 13:17 PDT: the fix lane runs from TWO Claude accounts at once. The OPERATOR session
(`CLAUDE_CONFIG_DIR=C:\Users\brent\.claude-acct2`, account 2) runs the loop, every Opus and Sonnet dispatch and the
landings; the MYTHOS session (the default config dir, account 1) keeps the Mythos work. So the account is a PARAMETER of
every coordination tool, never an assumption:
- **One resolver.** `account.py` is the only code that knows where Claude keeps its own files: the config dir is
  `CLAUDE_CONFIG_DIR` or `~/.claude`; the global config that names the signed-in account (`oauthAccount.accountUuid`) is
  `<config dir>\.claude.json` when the variable is set and `~\.claude.json` when it is not; a repository's transcripts are
  `<config dir>\projects\<repo key>\<session>`. PowerShell callers ask it (`account.py --field config_dir`), they never
  join `$HOME\.claude` themselves.
- **The accounts table** is `model_rules.json` `accounts.list`: per account its name, config dir (null = the default)
  and weekly reset, plus any `quota` keys that differ. A config dir no row names is an error, so a third account is one
  row, and every tool picks it up.
- **Seeding a named account** (kb/Work PB2480): `model_rules.json` `accounts.seed` lists what its config dir carries
  (settings, plugins, skills copied from the default account when missing; the project memory junctioned to the
  default account's; the Claude in Chrome flags in its `.claude.json`; never the credentials).
  `scripts/account-profile.ps1 -ConfigDir <dir>` carries out `account.py --seed-plan` idempotently, and
  `account.py --seed-check` (run by `tooling_check.py` at every session start) says what is missing. The Chrome bridge
  is the named pipe `claude-mcp-browser-bridge-<Windows user>`, per Windows user, so one pairing serves either account;
  the page it reads is whichever claude.ai account the browser is signed into, which the `meter` unit checks.
- **Telemetry is per MACHINE.** Every account's user settings export to the one sink (`otlp_sink.py`, 127.0.0.1:4318,
  `~\.claude\telemetry`), and each `api_request` event names its account in `user.account_uuid`. A consumer that needs
  one account's spend filters on it (`budget.py`, `usage_report.py --account-uuid`).
- **Coordination state is per account where it belongs to the account**: readings (`account` field), the published
  ledger artifact (`ledger-published.json` `accounts.<name>`, section 14). The STOP files are scoped by fleet, not by
  account (section 4.6). The two sessions pass work through `E:\COBOL-coord\mailbox` with `mailbox.py` (section 15).

## 3. Unit types

A unit is one fresh `claude -p` session with one prompt, one model and one exit condition. Units never chain inside
one session.

| Unit | Model | Starts when | Ends when |
|---|---|---|---|
| `wave` | Opus (the orchestrator's judgment; the implementers are routed per group by `model_rules.json`) | nothing pending to land or resume and the budget says `go` | its Workflow returned, the last lander train landed through `push-main.sh`, the ledger refreshed (`python scripts/spec/gen_ledger.py`, `references/landing.md`) and the handoff written |
| `campaign` | Opus | only with `-Cluster <lead>[,<lead>...]`: a wave-type unit is due (a handoff named `wave` or `campaign`, or nothing else fired), a lead's cluster has a ready note and the last wave-type unit was not a `campaign`; the ready leads take turns (section 3.2, section 9.1) | as `wave`; its prompt is `units/wave.md` with `--cluster <lead>` on the `plan_wave.py` call |
| `land` | Opus | finished implementer branches exist with no lander (a previous wave unit ended early), or a handoff says `next_unit: land` | the train landed (or was dropped with reasons) and the ledger rendered to `{COORD}\ledger.html` (section 14) |
| `resume` | Opus | the previous unit died, timed out, was stopped with a Workflow in flight, or handed off `split` | it has classified every pending branch and report (finish, land, re-plan) and handed off the next unit |
| `meter` | Sonnet, with `--chrome` | the newest reading in `readings.json` is older than 3 hours | the reading is appended (`budget.py --record`) and the handoff written |
| `owner-question` | none | a handoff carries `owner_question` | immediately: the supervisor appends the question to `OWNER-QUESTIONS.md` and STOPS; it never guesses and never starts a session for it |

Fable is never selected: the supervisor refuses any model name other than `opus` and `sonnet` (owner 2026-10-02:
Fable only with explicit approval per dispatch, which an unattended loop cannot obtain).

### 3.1 How a Workflow in flight constrains unit boundaries

A Workflow lives inside its `claude` process and dies with it. So a `wave` unit is NOT bounded by its own context:
it stays alive until its lander has landed. Its context stays small anyway, because the Workflow's agents return
pointers (the rolling wave's `summary` is capped at 900 characters and the forensic detail is in the report files),
and the unit polls nothing (it waits for the Workflow's completion signal).

If a wind-down is signalled while a Workflow runs, the unit does NOT end the session: supervising the fleet is its
current step, so it waits for the Workflow to return, writes a handoff that names every unlanded branch, worktree and
report (`next_unit: resume` when any is left), and ends. Whether the FLEET stops is the supervisor's decision, never
the unit's (section 4.3): for the SOFT context cap it sends `STOP-UNIT` only, the fleet finishes its work and its
trains land; for the owner's `STOP` or the HARD context cap it also creates the loop's fleet stop `{FLEET_STOP}`
(`scratch\STOP-loop`), so every implementer it dispatched checkpoints and returns `SPLIT` and every lander finishes or
abandons its train at a cluster boundary. The next unit is then a fresh `resume`.
A kill (after `-GraceMinutes`) is the last resort, and it loses only uncheckpointed agent work: every agent
checkpoints with WIP commits and a stamped `STATUS.md`, so `resume` rebuilds the state from disk with
`status_delta.py`, `prune_worktrees.py` and the report files.

**The alternative, described and NOT built:** run each implementer and lander as its own headless
`claude -p --agent cobol-implementer` process in its own worktree, with the supervisor as the pool. A unit could
then end at any time without losing a running agent, and the orchestrator's session would only plan and judge.
Its costs: the supervisor must re-implement the rolling pool, the same-file successor rule, the train assembly and
the stall ceiling that `wf_rolling_wave.js` already has; the structured return schema becomes a file contract; and
`dispatch_guard.py` cannot see a dispatch it does not mediate, so the brief checks move into the supervisor. It
becomes worth building when measurement shows wave units being killed with work in flight often enough that the
lost agent work exceeds that cost. Open decision D3.

### 3.2 The deterministic choice of the next unit

`next_unit.py` reads the operator's pending steer (`handoff.operator.json`, section 4.8) and the newest valid handoff
(`handoff.last.json`) and applies these checks in order; the first that fires wins:

0. The operator's steer names `next_unit` → that unit, and the JSON carries `"operator": true` so the supervisor consumes
   the steer when it starts the unit. It outranks every rule below, an unanswered `owner_question` included: the
   operator is the one actor that sees the whole machine (the loop, the attended landings, the owner's answers).
   `orchestrate.ps1 -Unit` is a steer like any other (section 4.8).
1. The handoff carries `owner_question` → `owner-question` (stop).
2. The handoff names `next_unit` → that unit; but a handoff naming `land` while another lander holds the landing lease
   (section 4.7) is passed over, and the rules below choose. A wave-type unit (`wave` or `campaign`) names only a WAVE: with
   `--cluster` the campaign rule at the end of this list decides which lane it runs, and the handoff's reason is kept
   in the choice's reason (kb/Work PB2522). Rule 2 used to return the named unit outright, ahead of the campaign rule,
   so a model that wrote `wave` in every handoff starved a ready campaign note indefinitely: PB2151, the legacy
   retirement's last note, sat ready and undispatched 2026-10-07 05:40–14:31 PDT through eight units (three of them
   fix-lane waves) while every supervisor start logged `campaign lane: cluster PB2108, 1 open note(s), 1 ready`. The alternation is the
   supervisor's rule, not the model's. A handoff's `land`, `resume` or `meter`, and an owner question (rule 1), still
   outrank the campaign.
3. The newest meter reading is older than 3 hours (`quota.meter_max_age_hours`), or there is none → `meter`.
4. The repository is dirty (ignoring `.claude/settings.local.json`) or has unpushed commits → `resume`.
5. The last unit failed (as the breaker counts it) or ended `split`, or a branch that `prune_worktrees.classify`
   calls `UNLANDED` received a commit after the last unit started (an agent of a unit that died) → `resume`.
   Older unlanded branches do not trigger it, so an abandoned branch cannot cause a `resume` loop; `plan_wave.py`
   still sees them.
6. The handoff lists `branches_pending` with status `DONE` → `land`, unless a live landing lease is held (section 4.7):
   a `land` unit would only wait for that lander, so rule 7 chooses and its reason ends `land deferred (<branches>):
   the landing lease is held by <holder> …`. The land runs at the first choice after the lease is released or dies.
7. Otherwise → `wave`. (A stale ledger page is not a reason for a unit: a headless unit cannot publish it, section 14.
   The earlier rule 7 compared a repo file that never existed, so it sent every idle loop to `land`.)

**The campaign rule** (`--cluster <lead>`, repeatable: the supervisor's `-Cluster`, section 9.1) decides the lane of
every wave-type choice, from rule 2 or rule 7: `campaign` when a lead's cluster has a ready note (`work.py`'s
`cluster_order`) and the newest `wave` or `campaign` line of `units.jsonl` is not a `campaign`, so campaign waves
alternate with fix-lane waves; otherwise `wave`. Among several ready leads the one whose own last `campaign` line is
oldest (never run counts as oldest; ties in `-Cluster` order) takes the turn, and the JSON names it in `cluster`. A
wave-type choice also carries `campaign` for the lane as a whole: `run`, `between`, `waiting` (no lead has a ready
note), `landed` (every lead's cluster is landed or retired) or `unknown`. A land the landing lease deferred (rule 6,
or a handoff's `land` at rule 2) stays at the end of that choice's reason, and a deferred handoff `land` names no wave:
the campaign rule then treats the wave as rule 7's (`next_unit.py#base_choice` returns the deferral beside the
choice; `test_orchestrate_campaign.ps1` 8f). With `--cluster` EVERY choice but an owner
question carries `campaigns` (`{lead: ready | waiting | landed | unknown}`; the supervisor ends a lead's lane at
`landed`, runs the fix lane alone once no lead is left, and records the map in that unit's `units.jsonl` line) and
`starved`: each lead whose ready note has waited through MORE THAN ONE wave-type unit since its own last campaign
wave, with `hours`, `waves` and `last_campaign_at`, which the supervisor logs as `campaign <lead> ready for N h (W
wave-type unit(s)), last campaign wave at T`. The measure walks `units.jsonl` back from the newest line and stops at
the lead's own campaign line or at a line that did not record the lead `ready`, so a gap in the record (every line
written before the record existed) under-reports and never over-reports. Without `--cluster` the output is
byte-identical to the fix lane's (`test_orchestrate_campaign.ps1` 8b and 8c).

## 4. The supervisor loop (`orchestrate.ps1`)

Parameters: `-DryRun` (prints the budget decision, the unit it would run and the exact `claude` command line, then
exits; it starts nothing and, past a hold, says what it would run after it), `-ClaudeExe` (the executable; a test
seam pointing at a fake that emits canned stream-json, not a wrapper; a `.ps1` or `.cmd` is launched through its
shell), `-CoordDir`, `-RepoDir` (default the repository containing the script), `-MaxContextTokens` (the SOFT
context cap, default 200000) and `-HardContextTokens` (the HARD cap, default 500000; section 4.3), `-MaxUnits` (default unlimited), `-PermissionMode` (default `bypassPermissions`), `-GraceMinutes` (default 30; a `wave` or `campaign`
unit gets three times this, because a lander train must be allowed to finish), `-BorrowDays` (passed to
`budget.py`), `-Unit` (the first unit: written as the operator's steer through `steer.ps1`, section 4.8; a dry run writes it to a temporary file it removes), `-Cluster <lead>[,<lead>...]` (the campaign lanes, section 9.1: comma-separated because `pwsh -File` passes `-Cluster A,B` as one string; each validated at start), `-ConfigDir` (the Claude account the loop spends, section 2.1: default `account.py`'s resolution of `CLAUDE_CONFIG_DIR`; an unknown dir exits 2 before anything starts; every child runs with exactly that account's `CLAUDE_CONFIG_DIR`, unset for the default account; the log header and every `units.jsonl` line name the account), `-Watch` (section 13), `-Python`, and the
test seams `-TelemetryDir` (passed to `budget.py`), `-WorkDir` (the register `next_unit.py` and `work.py` read for `-Cluster`), `-IdleCloseSeconds` (default 20, section 4.6), `-CheckpointSeconds` (default 300, section 5.1) and `-FastFailSeconds` (default 120; a unit under it fails only without a `done` handoff, because the `meter` unit legitimately takes about 40 s) and `-BackoffBaseSeconds`
(default 60). Exit codes: 0 stopped (`STOP`, `-MaxUnits`, `stop-week`, `-DryRun`), 3 another instance runs,
4 circuit breaker, 5 an owner question is waiting, 2 `-Cluster` names no cluster any kb/Work note names (or `-Unit campaign` without `-Cluster`).

Each iteration, in this order:

1. **Single instance.** `orchestrate.lock` is created atomically holding the PID. An existing lock whose PID is a
   live process refuses the start (exit 3); a lock whose PID is gone is stale and is taken over.
2. **STOP.** `STOP` in the coordination directory (or the owner's global `scratch\STOP`) ends the loop (exit 0), and it does so ASAP WITHOUT LOSING WORK
   (owner 2026-10-04: "close down work asap when needed without losing any"). Between units the loop ends at once;
   a hold or a backoff is interrupted within a minute; a RUNNING unit is wound down by the supervisor itself, not left
   to finish: it creates `STOP-UNIT` and the loop's fleet stop `scratch\STOP-loop` (section 4.6), every agent it dispatched checkpoint-commits its WIP and returns
   `SPLIT`, the unit writes a handoff (`next_unit: resume`, naming every branch), and only then does the loop end. A
   unit that ignores the wind-down for `-GraceMinutes` is killed (the last resort, losing only uncheckpointed agent
   work). The owner runs `pwsh scripts/orchestrator/stop.ps1` (`-Status` to watch, `-Clear` to run again); the loop
   deletes nothing of the owner's, so `STOP` stays until cleared and the logon start honours it.
3. **Circuit breaker.** Three consecutive units that end in under two minutes or with a nonzero exit or an invalid
   handoff stop the loop (exit 4) and write `logs\BREAKER-<time>.md` plus an `OWNER-QUESTIONS.md` entry naming the
   three units and their logs. Between failures the loop backs off 1, 2, 4 minutes (exponential, capped at 30).
4. **Budget.** `python scripts/orchestrator/budget.py --json` decides: `go` continues; `hold-session` sleeps until
   the session reset it reports; `hold-day` sleeps until 03:05 America/Los_Angeles the next day; `stop-week` ends the
   loop. Sleeps wake every minute to honour `STOP`.
5. **Next unit** (section 3.2): the operator's steer first (validated again here: a hand-edited invalid one is moved to
   `logs\<time>-handoff.operator.invalid.json` and said, never ignored in silence), then the last handoff and the
   deterministic checks.
6. **Run it**: `claude -p <prompt> --model <unit model> --permission-mode <mode> --permission-prompts none
   --output-format stream-json --verbose --session-id <fresh GUID>` from the repository root (plus `--chrome` for
   `meter`). The prompt is `units/common.md` followed by `units/<unit>.md` (`units/wave.md` for a `campaign` unit), with the substitutions `{COORD}`,
   `{HANDOFF}`, `{STOP_UNIT}`, `{PREV_HANDOFF}`, `{SCRATCH}`, `{FLEET_STOP}`, `{GLOBAL_STOP}` (section 4.6), `{TASKS_DIR}`, `{CLUSTER_ARG}` (` --cluster <lead>` for a `campaign` unit, empty otherwise) and `{BORROW_DAYS}` (the supervisor's `-BorrowDays`, so the
   `wave` unit's `plan_wave.py --from-budget` sees the same allowance the supervisor's gate used). The stream goes to `logs\<time>-<unit>.jsonl`,
   stderr to `logs\<time>-<unit>.stderr.txt`.
   While it runs, the supervisor reads every `assistant` event's `usage` (counting each message id once: the
   stream repeats a call's usage on each content block) and keeps the running context estimate =
   `input_tokens + cache_read_input_tokens + cache_creation_input_tokens` of the LATEST call (the size of the
   context the model just read). Only the unit's OWN messages count: a subagent's messages stream into the same log
   tagged with `parent_tool_use_id`, and that transcript is bounded by its own `maxTurns`, so the supervisor records
   them as `peak_subagent_context` and never winds the unit down for them (a lander subagent legitimately reaches
   200k+ while the unit that dispatched it sits near 100k). It applies the context policy of section 4.3: past
   `-MaxContextTokens` it creates `STOP-UNIT` only; past `-HardContextTokens`, or when the owner creates `STOP`, it also
   creates the loop's fleet stop `scratch\STOP-loop`, and only after that does the grace period run, after which it kills
   the process tree. The unit's session carries `COBOL_LOOP_UNIT=<unit type>` (`coord.py` `LOOP_UNIT_ENV`), so
   `dispatch_guard.py` knows a loop unit and refuses its fleet launch once `STOP-UNIT` exists.
   **The supervisor, not the model, decides when the unit is over.** The prompt is the first stream-json message
   (`--input-format stream-json`) and stdin stays open, because a one-shot `claude -p` waits for background tasks after its
   model ends a turn only up to `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS` (600 s) and then terminates them (wave 1017,
   2026-10-04: the unit wrote "I'm waiting for the Workflow" and ended its turn, `stderr` said "Background tasks still
   running after 600s; terminating", and an eight-agent fleet died at 653 s; the `resume` unit found the line). With
   stdin open the session never enters that exit wait, and a background task's completion notification wakes the
   model after `end_turn` (probed twice: ended 16.8 s, woken 41 s; and with the ceiling set to 5 s, woken at 30 s and
   still alive, so the ceiling does not apply to an open stdin). The supervisor
   tracks `result` and `system/background_tasks_changed` events and closes stdin when the model has ended a turn, no
   background task runs and the stream has been quiet for `-IdleCloseSeconds` (20; a task's completion event arrives
   just after its list empties, so the close waits). `units/wave.md` also tells the model to wait with foreground
   calls on the Workflow's task-output file (`{TASKS_DIR}`), a second line of defence, never the only one.
7. **Validate the handoff** against `handoff.schema.json` (`Test-Json -SchemaFile`). Missing or invalid counts as a
   failure for the breaker, and the next unit is `resume`.
8. **Record** one line in `units.jsonl`: `{unit, reason, model, session_id, started_at, ended_at, duration_s,
   exit_code, handoff_outcome, next_unit, failed, log, calls, input, output, cache_read, cache_creation,
   peak_context, peak_subagent_context, cost_usd, stop_unit_sent, fleet_stop_sent, wind_down (null, context-soft,
   context-hard or stop), fleet_at_wind_down, killed}`, plus, with `-Cluster`, `cluster` (the lead a `campaign` unit ran)
   and `campaigns` (each lead's lane state at this choice), which `next_unit.py` reads back for the campaign turns and
   the starvation measure (section 3.2). This is the per-unit cost record PB1981 item 9 refits the cost
   law from. An owner question in a valid handoff then stops the loop (exit 5).
9. **Loop**, until `-MaxUnits` units have run.

### 4.1 What every unit prompt says

- Load the `session-start` skill first; read the previous handoff the prompt names.
- Before EACH new step, check for `STOP-UNIT`; if it exists, finish the step, write the handoff and end (for a
  `wave` with a Workflow in flight: section 3.1).
- End by writing `{HANDOFF}` that validates against `handoff.schema.json`.
- A decision that only the owner can make ends the unit with `outcome: owner-question` and an `owner_question`;
  never guess (CLAUDE.md rule 7, the owner-decision protocol).
- Forbidden to every unit: the WSL lifecycle commands `wsl --terminate`, `--shutdown`, `--update`, `--export`,
  `--unregister` (they kill every other session's Linux gate). Land only through `bash scripts/push-main.sh`.
- A `wave` unit loads the `workstream` skill before the Workflow call (`dispatch_guard.py` refuses it otherwise),
  runs `plan_wave.py`, which renders the specs with `make_dispatch_specs.py` and runs `check_practices.py`, then
  starts `wf_rolling_wave.js` with the args file `plan_wave.py` wrote.

### 4.2 Permission mode under a headless session

The repository's hooks stay the real guard under any `--permission-mode`: `forbidden_commands.py` refuses the
forbidden git and shell shapes, `dispatch_guard.py` refuses an unchecked dispatch, `status_guard.py` refuses a
commit with a stale `STATUS.md`. `--permission-prompts none` makes anything that would prompt a denial instead of
a hang. The default mode is `bypassPermissions` (decision D1, owner 2026-10-04: allowed for the COBOL work). With prompts
gone, the hooks above are the only guard, which is why the WSL lifecycle commands are blocked by `forbidden_commands.py`
(decision D4) and why a hook's block is the rule speaking, never something a unit routes around.

### 4.3 The context policy (graceful stop; kb/Work PB2597)

The same protocol the workflows use for their agents: a file, checked before each new step, never a kill. A cap exists
because a unit's cost per call grows with its context, so past some size a fresh unit is cheaper than continuing. But a
fleet costs far more than the unit that supervises it: on 2026-10-08 wave 1038's unit did an inline planner fix
(PB2575) before planning, passed the old single cap of 150,000 (151,282 at 00:06:39), got `STOP-UNIT` and the loop's
fleet stop, then STARTED its eight-implementer Workflow anyway (00:06:56), and all eight returned `SPLIT` before any
work (00:10:03). So the policy has two levels, and the fleet stop belongs to the higher one only:

| Level | Fires at | The supervisor creates | Kill timer | What happens |
|---|---|---|---|---|
| soft | `-MaxContextTokens`, default 200,000 | `STOP-UNIT` | none | the unit starts no new step and launches no fleet; a fleet already running finishes and its trains land; then the unit hands off |
| hard | `-HardContextTokens`, default 500,000 | `STOP-UNIT` and `scratch\STOP-loop` | `-GraceMinutes` (three times for `wave`/`campaign`) | as for the owner's `STOP`: agents checkpoint and return `SPLIT`, the unit hands off `resume` |

Three mechanisms keep a just-paid fleet from being thrown away:
- **The soft cap never stops a fleet.** A unit supervising a running Workflow makes one call per wait (about every ten
  minutes), so its own context is cheap next to the fleet's; the soft cap lets the fleet finish. It needs no special
  case for the race in which the cap fires as the launch happens: whichever wins, no fleet stop is sent. The supervisor
  records whether a fleet (`local_workflow` or `local_agent` background task) was running when the wind-down fired
  (`fleet_at_wind_down`).
- **No fleet launch after the signal.** `dispatch_guard.py` (rule 0) refuses a `Workflow` call and an implementer or
  lander `Agent` call from a loop unit (the supervisor exports `COBOL_LOOP_UNIT`) while `STOP-UNIT` exists; the unit
  prompts say to check `STOP-UNIT` immediately before the call. An attended session shares the coordination directory
  but never carries the variable, so the loop's signal never refuses its dispatches.
- **The fleet launch comes first** (`units/wave.md`): a wave unit does nothing but plan before its Workflow runs; a
  defect found on the way becomes a kb/Work note, and one that makes the plan unusable is that unit's whole job (fixed
  on a branch and handed off without a launch).

The numbers, measured from `units.jsonl` and the unit logs on 2026-10-08 (the unit's own messages, section 4 step 6):
- context when the fleet launched, 21 wave, campaign and land units since 2026-10-04: 70,337 to 113,385 (wave 1038's
  151,282, after its inline side work, is the outlier the policy removes);
- final peak of the units that completed normally on 2026-10-07: at most 137,718 (wave, 20:30; the trend rose from
  117,318 and 118,732 the same day as prompts and skills grew), land units at most 120,303; two wave units of
  2026-10-04 and 2026-10-05 reached 152,067 and 164,175 and still completed their fleets;
- growth while and after supervising a fleet: up to about 94,000 (wave 2026-10-05 02:12, 70,337 to 164,175);
- a fresh unit starts at about 55,000 to 66,000 (the meter units' peaks), and the model's context window is 1,000,000
  (`modelUsage.contextWindow` in every unit's `result` event).
So the soft cap is 200,000, about 1.45 times the largest normal peak (headroom for the trend), and the hard cap is
500,000: three times the largest peak ever measured, half the window, leaving room for a wind-down; a fleet launched just
under the soft cap still has more than three times the largest measured supervision growth before the hard cap.
Re-measure both from `units.jsonl` (`peak_context`, `wind_down`, `fleet_at_wind_down`) when the prompts or the model
change.

### 4.4 Replacing the daily-resume cron

The session-only cron (it lived inside one interactive session and died with it) is replaced by the supervisor
itself: `hold-day` sleeps until 03:05 the next day and continues, and `hold-session` sleeps until the session
reset. The supervisor survives as long as its PowerShell window. To survive a reboot, a Windows Task Scheduler task
can start `orchestrate.ps1` at logon; the single-instance lock makes a duplicate start harmless. The task is not
registered by this change (open decision D2).

### 4.5 Failure modes and the guard for each

| Failure | What it would cost | Guard |
|---|---|---|
| two supervisors run at once | double dispatch, id collisions, two landers racing `push-main.sh` | `orchestrate.lock` with a live-PID check |
| a unit crashes at start (auth, a bad flag, a hook refusing everything) | the loop spins and burns quota | the circuit breaker: three fast or failed units stop the loop and write an owner note |
| a unit ends without a handoff | the next unit starts blind | schema validation; an invalid handoff is a failure and the next unit is `resume`, which rebuilds state from disk |
| a unit's context grows without bound | each call costs more than a fresh start | `STOP-UNIT` at the soft cap `-MaxContextTokens` of the unit's own messages (not its subagents'); the fleet stop and, after the grace period, a kill only at the hard cap `-HardContextTokens` (section 4.3) |
| a wind-down throws away a fleet the unit just launched | a whole fleet's launch cost for nothing (wave 1038: eight implementers SPLIT before any work) | the soft cap sends no fleet stop; `dispatch_guard.py` refuses a fleet launch from a loop unit after `STOP-UNIT`; the wave unit launches before any side work (section 4.3) |
| the operator's instruction is ignored | the loop runs a unit the operator ruled out (2026-10-07 23:53: a `resume`, then a `land` that would have raced an attended landing) | one steering path, `steer.ps1`, validated, outranking every handoff; a hand-written `handoff.json` is archived and said (section 4.8) |
| a unit ends with a Workflow in flight | the agents die with the process | the wave unit stays alive until its lander landed; on a cap it stops the fleet gracefully first (section 3.1) |
| a unit guesses an owner decision | a wrong irreversible landing | `owner_question` in the handoff stops the loop |
| the quota runs out mid-week | the owner's other work (the TENET project) is starved | `budget.py`: `hold-day` at the cumulative daily allowance, `stop-week` at the weekly cap |
| the 5-hour session window runs out mid-unit | a unit killed at the limit | `hold-session` at the soft stop (`session_soft_stop_pct`, 97 % since 2026-10-07) before a unit starts |
| two sessions allocate the same id or code | a renumbering pass (five collisions in one day, 2026-09) | `alloc.py`: one lock, reservations outside every worktree |
| two landers (the loop's and an attended one, or two trains) rebase and gate at once | the one that pushes second rebases, renumbers its DEVLOG entry and re-gates (≈ 30 min of the whole machine each time); the R1 lander lost three times on 2026-10-07 and ended SPLIT with nothing landed | the landing lease (section 4.7) |
| a landing reopens a closed inventory row | silent conformance regression | `inventory_ratchet.py` (section 7) |
| the owner wants it stopped | losing agent work if the process were killed | `STOP` (`stop.ps1`): a graceful wind-down of the running unit and its fleet, a kill only after the grace period |
| a unit's model ends a turn with a fleet in flight | a one-shot `claude -p` terminates background tasks 600 s later and the fleet dies (wave 1017) | the supervisor holds stdin open and closes it only when idle with no background task |
| a unit runs a WSL lifecycle command | every other session's Linux gate dies | the unit prompts forbid it; `forbidden_commands.py` is the hook-level guard (open decision D4: add these shapes there) |

### 4.6 Scoped stop files (kb/Work PB2483)

On 2026-10-07 13:26 the operator moved the loop to account 2 (`stop.ps1`); the supervisor's wind-down created the one
shared `scratch\STOP`, and the OTHER session's in-flight refuter, whose brief also named `scratch\STOP`, read it and
split with a claim undecided. One session's stop must never abort another session's work, so a stop is SCOPED:

| File | Who creates it | Who obeys it |
|---|---|---|
| `<coord>\scratch\STOP` (GLOBAL) | the owner (`stop.ps1 -Global`, or by hand) | every agent of every session, and the loop (it ends) |
| `<scratch>\STOP-<scope>` (FLEET) | the fleet's own dispatcher: the supervisor's wind-down (`STOP-loop`), an attended session winding down its wave (`STOP-w<wave>`) | only the agents whose dispatch names it |

- **One definition.** `coord.py` `global_stop()` and `fleet_stop(scratch, scope)` name the files for every Python tool;
  `orchestrate.ps1` and `stop.ps1` use the same two names (`$GlobalStop`, `$FleetStop`), and `test_orchestrate_winddown.ps1` proves
  they agree (the supervisor's wind-down is exactly what `stop.ps1 -Clear` removes).
- **Every dispatch names both.** `plan_wave.py --stop-file` (default `<scratch>\STOP-w<wave>`; the wave unit passes
  `{FLEET_STOP}`, the loop's) writes `stop_file` into `groups.json` and the Workflow args together with `global_stop`;
  `make_dispatch_specs.py` renders both into every spec (and renders nothing without `stop_file`); `wf_rolling_wave.js`
  refuses args without them; the lane-3 workflows take `stopFile` and `globalStopFile`.
- **The check.** `check_practices.py` refuses a template without the `{global_stop}`/`{stop_file}` placeholders, a
  rendered spec that does not name a `scratch\STOP` and a `STOP-<scope>` file, and a groups file whose `stop_file` is
  not a `STOP-<scope>` file; MANDATORY-PRACTICES P3 states the rule and the agent definitions repeat it.
- **Clearing.** The supervisor removes only `STOP-loop` at a unit's start; `stop.ps1 -Clear` removes the loop's files,
  and only `-Clear -Global` removes the global one.

### 4.7 The landing lease: one lander on main at a time (`landing_lease.py`, kb/Work PB2537)

`push-main.sh` refuses a HEAD that is not a descendant of origin/main, so it serialized the PUSH. A landing's cost
comes before that step: the final rebase, the whole-population lander gate, the Linux gate, the oracle and CI, about
50 minutes. Every train the loop landed inside that window made a waiting lander rebase, renumber its DEVLOG entry and
re-gate; on 2026-10-07 the R1 lander (approved 17:42 PDT) lost to trains 1034 and 1034b, re-gated three times and ended
SPLIT at its turn cap with every gate and CI green and nothing on main. Owner 19:30 PDT: "this is stupid. we cannot
keep running r1 repeatedly for zero gain".

- **The lease** is one file, `<coord>\landing-lease.json` (section 2), so every session and every worktree sees the
  same one. It records the holder (who), the reason (what is landing), the WORKTREE that holds it, a heartbeat and an
  expiry. Its owner is the worktree: a lander that resumes in the same worktree re-takes its own lease, and push-main
  needs no argument to know whether its caller holds it. Every change is a read-modify-write under
  `coord.locked(cdir, "landing-lease.lock")`, and every read (`check`, `status`, `next_unit.py`) takes the same lock, so
  no reader holds the file open while a writer replaces it (Windows refuses that). A file that is not a lease is never
  guessed free or held: every verb exits 2 with the reason, and push-main dies on it. The exit codes are the contract
  push-main acts on: 0 own/done, 1 (`check` only) free, 4 held by another worktree, 2 could not answer.
- **Acquire before the final rebase and the gates** (lander-train-brief and lander-brief step 2b, golden-lander-brief
  step 4): `acquire --holder … --reason … --wait-min 9` takes a free lease, re-takes the caller's own, or takes over a
  DEAD one (its expiry passed, or its worktree no longer exists; logged as a `takeover`). While another worktree holds a
  live one it polls, and after the wait it prints `WAIT` and exits 4; the lander re-issues it and neither rebases nor
  gates meanwhile. Bringing the clusters in and verifying them happen before it, so they still overlap the previous
  train's CI (the reason pipelining exists, MANDATORY-PRACTICES L3).
- **Renew, expire.** The expiry is 30 minutes after the last heartbeat. A lander renews at every later step and before
  every blocking wait (each under 10 minutes); push-main renews every minute in a background loop that ends with it,
  because its CI wait is the longest step. A lander that died stops renewing, and its lease is taken over 30 minutes
  later; a lander whose renew prints `LOST` was taken over, and acquires, rebases and re-gates again.
- **push-main.sh is the backstop.** Before it spends a CI run it re-takes the lease (or takes it, when the caller
  took none: a registrar's docs landing, the operator's own push), and REFUSES the landing with exit 4 while another
  worktree holds it, so an unleased landing can no longer move main under a leased one. Its renewer retries through a
  transient failure and stops only on `LOST`, logging to a file push-main prints if the lease is gone later. It checks
  the lease again before the push to main (free: it takes it back; taken over: exit 4 with the renewer's log) and
  releases it when it exits, landed or not, saying so loudly when the release fails. A re-run that finds its sha
  already on main (an earlier run landed and was killed before its trap) releases its worktree's lease too. A lander
  that stops before push-main runs `release --outcome …`; only the holder's worktree can release or renew.
- **The directory must be absolute.** `coord.coord_dir()` refuses a relative coordination directory: the default is a
  Windows path, which on Linux is a relative name, and a lease created inside one checkout would bind no one else.
- **The loop** (`next_unit.py`, section 3.2 rules 2 and 6) starts no `land` unit while a lease is live: the land is
  deferred, the reason names the holder, and the loop runs a wave or a campaign instead. `stop.ps1 -Status` prints
  the lease line (`landing lease: free`, `held by …`, or `DEAD (…)`), and `landing_lease.py status` is the same line.
- **Tests.** `landing_lease.py --self-test` (asserted arm by arm by `LandingLeaseDriftTests` in every Unit run) plants
  two landers and shows the second waits and gates only after the first releases; takes over an expired lease and one
  whose worktree is gone; refuses a renew or release by a non-holder; re-takes a resuming holder's lease; admits exactly
  one of six parallel acquires; answers push-main's `check`; exits 2 on a file that is not a lease; and runs
  next_unit's `choose` in process to show no `land` unit starts while a lease is live. The bash layer
  (push-main's exit mapping, renewer and trap) has no automated test: it needs gh and a remote; it was exercised by
  hand on a harness of its own lines (held elsewhere, corrupt, released and taken over mid-run). `test_orchestrate_campaign.ps1` (8c) proves next_unit's deferral
  and the `stop.ps1 -Status` line.

### 4.8 Steering the loop: the operator (kb/Work PB2596)

The attended OPERATOR session (section 2.1) sees what no unit can: an attended landing in flight, the owner's answer to
a question, a plan the owner changed. On 2026-10-07 at 23:50 it wrote its instruction (run a wave; hold push-main) into
`handoff.json`; the supervisor deletes that file at every unit start and `next_unit.py` reads only `handoff.last.json`,
which still held the synthesized handoff of a failed `meter` unit, so the 23:53 restart ran `resume`, whose handoff named
a `land` unit that would have raced the operator's R2 lander at push-main. There is now ONE steering path:

- **`pwsh scripts/orchestrator/steer.ps1 -Unit <wave|campaign|land|resume|meter> -Reason "<why>" [-Summary "<instruction>"]`**
  writes `handoff.operator.json`: `{unit: operator, outcome: done, next_unit, next_unit_reason, summary}`, validated
  against `handoff.schema.json` (which requires `next_unit` and its reason for unit `operator`) before it lands, written
  atomically. `-Show` prints the pending steer, `-Withdraw` removes it, and `stop.ps1 -Status` shows it.
- **Precedence**: it wins the next choice over every unit's handoff, a synthesized one included, and over every
  deterministic check (section 3.2 rule 0). It may be written while a unit runs: that unit's own handoff, copied to
  `handoff.last.json` when it ends, does not displace it.
- **One choice**: when the supervisor starts the unit it chose, it archives the steer as `logs\<time>-<unit>.handoff.operator.json`
  and moves it to `handoff.last.json`, so the unit reads the operator's instruction as its previous handoff
  (`{PREV_HANDOFF}`). A dry run reads it and consumes nothing. A stop (`stop.ps1`, `-Clear`) leaves it pending.
- **No second way**: `orchestrate.ps1 -Unit` writes its first unit through `steer.ps1`; a hand-edited invalid steer is
  set aside as `logs\<time>-handoff.operator.invalid.json` with a log line; a `handoff.json` found when a unit starts
  is archived as `.handoff.orphan.json` with a log line. Writing `handoff.last.json` by hand is not a path: the next
  unit to end overwrites it.

## 5. The handoff (`handoff.schema.json`)

Required: `schema_version` (1), `unit` (a unit type, or `operator` for the operator's steer, section 4.8, which also
requires `next_unit` and `next_unit_reason`), `outcome` (`done` · `split` · `failed` · `owner-question`), `summary`
(at most 900 characters; the detail lives in files the handoff points at).
Optional: `next_unit` (one of the unit types, or null to let the deterministic checks choose) with
`next_unit_reason`; `branches_pending` (`{branch, worktree, report, status}`, status one of the rolling wave's
`DONE` · `SPLIT` · `DISCHARGED` · `BLOCKED`); `workflow` (`{run_id, state}` with state `landed` · `stopped` ·
`none`); `landed` (`{commits, gap_before, gap_after, devlog_entry}`); `meter_reading` (the reading appended, for
the `meter` unit); `owner_question` (`{question, context}`), required exactly when `outcome` is `owner-question`;
`notes_touched` (kb/Work ids); `synthesized` (true when the supervisor, not the unit's model, wrote it) and
`checkpoint` (the path of the checkpoint it was built from). Additional properties are refused, so a misspelled key
fails validation instead of being ignored.

### 5.1 Frequent handoffs: the checkpoint, the milestones and the synthesized handoff

A handoff written only when a unit ends is lost with the unit (wave 1017 ran 11 minutes, was terminated, and its
successor rebuilt the state from stderr and git; owner 2026-10-04: "we need more frequent handoffs written"). Three
mechanisms, in order of how little they trust the model:
1. **`checkpoint.json`, written by the supervisor** (`checkpoint.py write`) at unit start, every `-CheckpointSeconds`
   (default 300), whenever the set of background tasks changes (a Workflow starts, a train lands, a gate ends; at most
   every 10 s) and at the unit's end. It holds the stream counters (calls, context, cost), the running background-task
   count, and every linked worktree's branch, head, commits ahead of `origin/main`, uncommitted-file count (the owner's
   `settings.local.json` and `STATUS.md` excluded) and `STATUS.md` headline, plus the unit's last milestones. No model
   is involved and a failed write never ends the unit.
2. **`milestones.jsonl`, written by the unit's model** at every milestone (`units/common.md`): one line, `{at, what}`
   plus `shas` or `branches`. It carries what the supervisor cannot see: the decision and its reason.
3. **The synthesized handoff** (`checkpoint.py synthesize`): when a unit ends with no valid handoff (a crash, a kill,
   the background-task ceiling, a malformed file) the supervisor writes one from the last checkpoint, the five last
   milestones and the worktrees AS THEY ARE NOW: `outcome: split`, `next_unit: resume`, `synthesized: true`,
   `branches_pending` for every worktree that holds work. The unit still counts as failed for the breaker, and
   `units.jsonl` records `synthesized (<what was wrong>)`. If the SUPERVISOR dies mid-unit (a reboot), `checkpoint.json`
   is still on disk at the next start (a finished unit's checkpoint is moved to `logs\`, never left), and the supervisor
   turns it into the missing handoff before choosing the first unit.
Every unit's checkpoint and milestones are kept beside its log: `logs\<time>-<unit>.checkpoint.json` and
`.milestones.jsonl`.

## 6. The allocator (`alloc.py`)

`python scripts/orchestrator/alloc.py devlog | pb N | code N` prints the allocated value or the range
(`PB1982-PB1986`, `COBOLNET2799-COBOLNET2801`); `peek <kind>` prints the next free value without reserving, with
the truth-source detail. `--coord DIR` (or `COBOL_COORD_DIR`) and `--repo DIR` select the coordination directory
and the repository.

- **One rule per kind, in one place.** The next value is `max(truth, reserved) + 1`, where truth is read from the
  repository on every call:
  - `devlog`: the first line-start `## Entry NNN` of `DEVLOG.md` (the file is descending, so that is the top);
  - `pb`: the highest `kb/Work/PB<n>.md`;
  - `code`: the ceiling of BOTH scans: every `COBOLNET\d{4}` in `src/**/*.cs` (outside `bin`/`obj`) and every
    quoted `"COBOLNET\d{4}"` in `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs`. This is the rule
    `session-probe.ps1` used to compute itself; it now calls `alloc.py peek code --probe` and prints that line, so
    the rule exists once.
- **The lock** is `coord.locked(cdir, "alloc.lock")`, the coordination directory's one short mutex (the landing lease
  uses the same one, section 4.7): an atomic create (`os.open` with `O_CREAT | O_EXCL`) holding the PID and time.
  A lock older than 60 seconds is stale (the allocator holds it for milliseconds) and is broken by an atomic rename
  to a unique name, so two breakers cannot both win. A waiter retries for 30 seconds, then fails loudly.
- **Reservations** persist in `alloc.json` (`{"devlog": n, "pb": n, "code": n}`), written to a temporary file and
  renamed, so a crash never leaves it half-written. A reservation is never returned: an unused id is a gap, which
  costs nothing, while a reused one costs a renumbering pass.
- Truth wins over a stale reservation file and a reservation wins over a truth that has not caught up (an id handed
  to an implementer whose note has not landed yet). That is exactly the in-flight-worktree case `workstream`
  SKILL §4 names.
- `seed <kind> <value>` raises the reserved mark (never lowers it), for values handed out before the allocator
  existed: before the first real run, seed `code` and `pb` with the highest values any in-flight wave was given.

## 7. The closed-rows ratchet (`inventory_ratchet.py`)

`python scripts/orchestrator/inventory_ratchet.py [--base REF] [--head REF|WORKTREE]` compares the inventory at
`head` (default the working tree) with `base` (default the merge base of HEAD and `origin/main`, the same default
as `audit_witness_loss.py`, whose `rows_at` it reuses).

- The CLOSED set is the rows whose `state` is `OK`. A row closed at `base` must still be closed at `head`
  (present, state `OK`).
- GAP may not rise: when the GAP count at `head` exceeds the count at `base`, every row that is GAP at `head` and
  was not GAP at `base` (a reopened row or a newly catalogued one) is an offender.
- **The excuse marker**: an offender is excused when a line ADDED between `base` and `head` in `DEVLOG.md` or in a
  `kb/Work/*.md` note reads `reopens-rows: <id-or-glob>[, <id-or-glob>...] — <reason>` (an ASCII ` - ` or `: ` is
  accepted in place of the dash; the reason must be non-empty; globs use `fnmatch`, so a catalog harvest can write
  `reopens-rows: SR-10.7.3-* — 10.7.3 harvested`). Only lines added in the range count, so an old note cannot
  excuse a future regression.
- Exit 0 green, 1 with the offending row ids printed, 2 when the inventory does not exist at `base`.

**Wiring (described, not done here):** the lander runs it after merging a train and before `push-main.sh`
(a `lander-train-brief.md` step beside the witness-loss audit), and CI's `audits` job runs it with the `changes`
job's `BASE`, exactly as it runs `audit_witness_loss.py`. The orchestrator decides when to turn it on (open decision
D5), because it changes the landing contract.

## 8. The budget estimator (`budget.py`)

`python scripts/orchestrator/budget.py [--json] [--borrow-days N] [--now ISO]` and
`python scripts/orchestrator/budget.py --record --weekly W --session S --session-reset ISO` (appends a reading).

- **One account.** Every estimate is one account's (section 2.1): the account `account.py` resolves from
  `CLAUDE_CONFIG_DIR`, or `--account NAME`. Its week starts at ITS weekly reset (account 1 Sunday 03:00, account 2
  Saturday 10:00, America/Los_Angeles) and its quota is the shared `quota` with the row's overrides. `--record` stamps
  the reading with the account.
- **Anchor.** That account's newest reading in `readings.json` (owner-meter values read by the `meter` unit). Readings
  from before the account's current week's reset anchor nothing; the weekly base is then 0 from the reset. A reading
  with no `account` (written before 2026-10-07) is ambiguous: it anchors nothing, and the output counts it
  (`unstamped_readings`) so a fresh `--record` replaces it.
- **Spend since the anchor** comes from the local telemetry sink (`usage_report.py`'s `events()` over
  `~/.claude/telemetry/<day>.jsonl`, reused, not re-parsed): every `api_request` after the anchor whose
  `user.account_uuid` is the account's (an event without the attribute is counted: it cannot be attributed, and
  over-counting only holds early; an unknown account id counts every event and says so), converted to weekly points
  per model with `model_rules.json`'s calibration (`tokens_per_point`, counting the token kinds named in
  `counted_token_kinds`).
- **Weekly estimate** = anchor weekly % + points since the anchor.
- **Allowance** = day N × 14.3 % (day 1 starts at the account's weekly reset), plus `--borrow-days` days the owner
  allowed, capped at the weekly cap (`weekly_cap_pct`, 97 by the owner's 2026-10-07 instruction, kb/Work R69 §6).
- **Session estimate** = the anchor's session % (when its `session_reset` is still in the future) plus points since
  the anchor × `session_pct_per_weekly_pct`; after the reset it starts from 0 at the reset.
- **Decision**: `stop-week` when weekly ≥ the cap; `hold-day` when weekly ≥ the allowance; `hold-session` when the
  session estimate ≥ the soft stop (97 %); else `go`. The hard stop (99 %) is reported as `session_hard_stop_pct`
  (the supervisor does not yet act on it inside a unit; the soft stop before each unit is the guard). `resume_at`
  names when a hold ends.
- Output: `{weekly_est_pct, allowance_pct, headroom_pct, session_est_pct, decision, resume_at, week_start, day,
  account, telemetry_account, unstamped_readings, anchor, spend}`.

The estimate drifts from the meter between readings; the 3-hour `meter` unit re-anchors it. A reading refits nothing
automatically: the constants change only by an explicit edit of `model_rules.json` with its date and source (open
decision D6). **The calibration was refit to telemetry on 2026-10-07** (kb/Work PB2478): the 2026-10-04 constants
were fitted to the subagent tokens the Workflow tool reports, and against telemetry (input, output and cache-write
tokens; cache reads excluded) they overcounted about four-fold: account 2's own spend between its 0 % reading (13:34
PDT) and its 6 % week / 24 % session reading (14:54) estimated 25.4 points and a 127 % session, and held the loop.
The refit multiplies every rate by 4 (Opus 1.6 M counted tokens per point, ratios between families kept, Haiku priced
at its own rate instead of the Opus fallback) and measures `session_pct_per_weekly_pct` at 4.0; the planner's token
costs scale by the same 4, so a planned wave costs the same points. `test_budget.py` section 8 pins the fit to those
two readings. One interval on one account is a thin fit (the meter shows whole percents): refit whenever two readings
of one account bracket a measured spend (open decisions D6, D8).

## 9. The wave planner (`plan_wave.py`)

`python scripts/orchestrator/plan_wave.py (--budget-points P | --from-budget) [--wave N|next] [--scratch DIR]
[--reports DIR] [--clusters-json FILE] [--half-clusters-json FILE] [--no-branches] [--max-groups N] [--dry-run]`.
`--wave` defaults to one past the highest wave in the reports directory or the newest DEVLOG lines.

The plannable notes are exactly those `fix_clusters.py --json` lists (the submodule's view of `kb/Work`, which
applies `.agent-fleet.json`'s kind and skip flags), run twice: once over `status: open`, once over `status: half`.

1. **Awaiting landing.** A note whose newest report is `DONE` and names a branch that is not on `main` yet
   (`prune_worktrees.classify`, reused: `UNLANDED` or `CHECK`) is excluded and listed: re-planning it would dispatch
   work that already exists. The `land` or `resume` unit deals with it first.
2. **Finishers first.** (a) A note whose newest report (by wave, then time; header lines only) says `SPLIT` or
   `NOT STARTED`, whatever its branch's state (a `land` unit lands only `DONE` branches, so an unlanded `SPLIT` branch
   held for one was stranded; kb/Work PB2575): one finisher group per predecessor report. (b) The half
   clusters, one finisher group each. (c) An `UNLANDED` branch whose commit subjects name an open note no report
   covers. Each finisher's `pred` is a fixed paragraph naming the predecessor report, branch, worktree, head and the
   branch's class (cherry-pick if unlanded, orient from the report if landed). A finisher on a primary file absorbs
   the notes of the open clusters on that file up to the cap of five.
3. **Clusters**, minus every note above, ranked by inventory rows claimed (the sum of each note's
   `inventory_rows`, read with `work.py`'s one frontmatter reader), then by harm.
4. **One group per primary file**: a later group on the same file becomes a same-file successor (`after: <letter>`,
   letter suffixed `2`, `3`), the rule `wf_rolling_wave.js` and `check_practices.py` (O2) enforce.
5. **Model per group** from `model_rules.json`: Opus when the primary file or a code site matches a design-heavy
   pattern (`/Oo/`, `.g4`, `Frontend/Grammar/`), a note's area matches (`oo`, `grammar`), a note's body says a
   design question is open, or a note already appears in two reports while still open (a note that fails twice is a
   task-spec problem, PB1981); else Sonnet. Fable is never a value. The reason is printed per group.
6. **Cost**: tokens per group = `per_group + per_note × notes` for its model, converted to points with
   `tokens_per_point`; one lander per `train_size` groups at `lander_tokens` on Opus. Groups are taken in rank
   order while they fit; a group that does not fit is skipped and a smaller later one may still fit; a successor
   only with its predecessor; at most `wave.max_groups` (8) groups.
7. **Allocation** through `alloc.py`: three codes per group and one lead-id block of five per train. `devlog_n` is a
   peek, not a reservation: the lander re-reads the top of `DEVLOG.md` anyway, and a reserved number that no train
   used would leave a gap in the log. `--dry-run` peeks everything and writes nothing.
8. **Outputs** (not in `--dry-run`): `<scratch>\groups.json` in exactly `make_dispatch_specs.py`'s format (each
   group also carries its `model`), the rendered specs and `check_practices.py`'s verdict (by running
   `make_dispatch_specs.py`; exit 1 when red), and `<scratch>\wf-args-w<wave>.json` with the rolling wave's args
   (`scratch, wave, concurrency, train_size, min_final_train, devlog_n, previous_train, lead_id_blocks,
   implementer_model, authorization, groups`).
9. **Frontier models wait for the owner.** A note whose body says its work needs Fable or Mythos under the owner's
   approval (`routing.owner_approval_patterns`) is never planned, in either lane: that approval is per dispatch
   (MANDATORY-PRACTICES P1) and an unattended wave cannot ask, and routing it to Opus would pre-empt the owner's
   choice. It is printed as waiting; the attended session asks. First case: PB2118, R1's authorship (R69 section 4).

Measured on 2026-10-04 against the repository after train 1015 (budget 3 points): eight groups in two trains,
finishers first (PB244 from wave 1013's split, PB480 and PB1112 from wave 1013's not-started pair, PB1940 from
wave 1015's split), then the half clusters, with Opus on the OO and grammar files and on PB244 (attempted twice);
wave 1015's dropped group Y (PB1263 and four more) listed as awaiting landing. That is the shape waves 1012-1015
chose by hand (a finisher group first, same-file successors, Opus where the design is open), with more and smaller
groups because the open half notes now form their own clusters.

The constants in `model_rules.json` carry their date and source: one weekly point is about 1 M agent tokens on
Sonnet and 0.4 M on Opus (measured 2026-10-04 over waves 1011-1015). The per-group and per-note figures are seeded
from that measurement and are refit from `units.jsonl` and `usage_report.py` (PB1981 item 9).

### 9.1 The campaign lane (`--cluster`, kb/Work PB2120)

The fix lane plans from harm: `fix_clusters.py` reads `.agent-fleet.json`, which keeps `kind: defect`, skips
`blocked` and `process_only` notes, and resolves sites under `src/Cobol.Net.*` only. A CAMPAIGN (the legacy
retirement `PB2108`, the architecture review `PB1754`) is `process_only` work in `tests/`, `scripts/`, `.github/`,
`docs/` and the legacy tree, so before this lane it could be dispatched only by a hand-written `groups.json`. A
campaign is a named cluster in the register: every note whose `cluster:` list names the lead (a lead note may list
itself), ordered by `blocked_by:`.

- **Selection** is `work.py`'s `cluster_order(items, lead)`, the one reader of `cluster` and `blocked_by` (also behind
  `python scripts/spec/work.py next --cluster <lead> [--json]`): every non-terminal note naming the cluster, of any kind,
  whatever its harm flags, with its depth in the `blocked_by` topology and what it waits on (open blockers, in the
  cluster or not; `status: owner` or `blocked`; or `blocked: true` with no `blocked_by`). A note is READY when it waits
  on nothing. `work.py check` holds the two spellings together: `blocked_by` names notes, `blocked` is true exactly
  while a named blocker is open (a stale `true` hid PB1940 and PB1956 from every work list), and open notes form no
  cycle.
- **Planning** (`plan_wave.py --cluster <lead>`): a note is PLANNABLE when it is ready or when everything it waits on
  is plannable; the rest are printed as waiting with the reason. Plannable notes are clustered by the code sites their
  text names, with `fix_clusters.py`'s own `source_index`, `sites` and `cluster` (imported, never forked) over
  `model_rules.json` `campaign.src`, `ext` and `exclude_dir` (the whole tree less corpora and generated output; project
  files are not sites, because their dotted names read as `Type.Member` references), one topological depth at a time
  so a cluster never mixes a blocker with what it blocks. A note naming no site is a group of its own, ordered by its
  chain. The clusters holding one dependent's blockers MERGE when they share a depth and fit the cap, so the dependent
  can follow one chain (wave 1025 was planned so by hand: PB2108 + PB2109, then PB2110). Ranking puts depth first. Everything else is the fix lane's mechanism: awaiting landing, finishers, model
  routing, budget, trains, allocation and outputs.
- **Dependencies become successors.** A group whose notes wait on notes planned in this wave joins the TAIL of the
  blocker group's successor chain (`after:`), so it starts once that chain has returned, merges its branch and lands
  through it (the rolling wave's same-file mechanism, unchanged). A chain stays linear because the rolling wave holds a
  predecessor for one successor. In the fix lane the only link is a shared primary file, whose chain tail is that
  file's last group, so the rule there is the same-file rule exactly. A group whose blockers are planned in no group,
  or still sit in two chains (they did not merge, or the group's own file belongs to another chain), waits for a later
  wave. Its spec says it is a DEPENDENCY
  SUCCESSOR and to return BLOCKED if its blocker did not finish.
- **The supervisor** (`orchestrate.ps1 -Cluster <lead>[,<lead>...]`) checks each cluster exists at start, passes
  them to `next_unit.py`, whose campaign rule (section 3.2) alternates `campaign` and fix-lane `wave` units and gives
  the ready leads their campaign turns least recently run first, and ends a lead's lane when every note naming its
  cluster is landed or retired; once no lead is left the loop runs the fix lane alone. A `campaign` unit is a `wave`
  unit whose `plan_wave.py` call carries `--cluster <the lead whose turn it is>` (the `{CLUSTER_ARG}` placeholder).
  Several leads are for INDEPENDENT campaigns: a lead that is itself a note of another lead's cluster (PB2119, the
  Delete program, is both a lead and a note of the architecture review PB1754) would be planned twice, once as a
  note of the outer campaign and once as its own lane, so the operator passes the outer lead or the inner one, not
  both. Between independent lanes the file-set partition still holds: a campaign group whose files meet another
  lane's dispatched, still-open group (`inflight-groups.json`, `plan_wave.py#inflight_file_sets`) waits.
- **The handoff names a wave, the supervisor names its lane** (kb/Work PB2522). A unit's handoff is written by the
  model from what it sees, so it may name `wave` every time; the campaign rule therefore applies to a handoff's `wave`
  (or `campaign`) as it does to rule 7, and a ready lead that has waited through more than one wave-type unit is logged
  `campaign <lead> ready for N h` at every choice until its turn comes.
- **The fix lane is unchanged** without `--cluster`: `test_plan_wave.py` check 8 compares the whole fixture plan with a
  golden that the planner wrote before this lane existed, and `test_orchestrate_campaign.ps1` (8b) compares `next_unit.py`'s
  choice and the wave prompt.

Measured on 2026-10-07 (dry runs, budget 3 points, before wave 1025 landed): `--cluster PB2108` plans six groups in
two trains for 1.62 points: `A` = PB2108 + PB2109 (Opus, `build/ci`, merged because PB2110 waits on both), PB2120 on
its own, then the chain `A2` PB2110, `A3` PB2112, `A4` PB2113 (Opus, the lexer `.g4`), `A5` PB2114, and PB2111 waiting
(its primary file is PB2120's, another chain); `--cluster PB1754` plans PB2115, PB2116, PB2117 and PB2119 after PB2115
(`A2`) for 0.94 points, and holds PB2118 for the owner's Mythos approval. A live run also excludes the notes whose
branches await landing (wave 1025's), exactly as the fix lane does.

## 10. What is built and what is not

Built: everything in section 2, with tests. Not built: the separate-process agent pool (section 3.1), the
Task Scheduler entry (4.4), the ratchet's wiring into the lander brief and CI (7), the hook shapes for the WSL
lifecycle commands (4.5), automatic refit of the calibration (8), an in-unit action on the session hard stop (8),
and the terminals mode (13.2). The prototype has never run a real session; the owner's first run is `-DryRun`, then
`-MaxUnits 1 -Unit meter`, then `-MaxUnits 1`.

## 10a. Starting at logon

The reboot path is one Startup entry, `cobolnet-autoresume.cmd`, written by `scripts/orchestrator/install-autostart.ps1` (run once
by the owner; `-DryRun` shows the file, `-Uninstall` removes it). It opens a Windows Terminal window `cobol-supervisor` in the repo
and runs `scripts/orchestrator/autostart.ps1`, which lives in git: wait 25 s for the desktop, honour a `STOP` file and a waiting
`OWNER-QUESTIONS.md`, fast-forward the checkout when only the always-dirty settings file is modified (never stash or reset), then run
`orchestrate.ps1 -Watch` in that window so the owner can watch it, and print what the exit code means.

This replaces the old arrangement (a `%LOCALAPPDATA%\CobolNet` script that started an interactive `claude` session whose prompt had
to re-create a session-only cron job at `5 3 * * *`). The supervisor needs no cron: `budget.py` returns `hold-day` until 03:05 and
`hold-session` until the 5-hour reset, and every unit is a fresh session chained by `handoff.json`, so a reboot loses at most the unit
that was running (a Workflow in flight dies with its process; its worktrees and checkpoints survive and the next `resume` unit
picks them up). The installer moves the old script and the old `.cmd` to `%LOCALAPPDATA%\CobolNet\replaced\` as a rollback copy and
nothing calls them. `test_autostart.ps1` proves the install, the idempotent re-install, the uninstall, and that STOP and a waiting
owner question stop the start, all in temp directories.

## 10b. Starting an ATTENDED session (and the context-full restart)

The loop is headless and never needs an interactive session, but the owner works with one, and a session cannot start its own
successor: something outside it must launch the new one. `scripts/start-session.ps1` is that launcher, in git, the only one (a
copy in `%LOCALAPPDATA%` would be unversioned and would drift). It starts `claude -n COBOL` in the repository with a prompt that names
no state: the memory index's START HERE note is the single source, so the same script serves a boot start (until the owner installs
section 10a's entry, the Startup `.cmd` calls it) and a context-full restart. There is NO automatic context-full trigger. The attended
session performs the procedure itself, in this order: wind running work down (`stop.ps1`: agents checkpoint, the unit hands off),
save (a resume note marked START HERE, WIP checkpoint commits on the worktree branches), then ask the owner to run the script in a
new terminal. A STOP file left on purpose by that procedure is named in the prompt so the new session clears it deliberately.
`start-session.ps1 -DryRun` prints what would run; `-ConfigDir` starts another account's session (section 2.1).

## 11. Open decisions

- **D1** DECIDED (owner 2026-10-04): permission mode for unattended units is `bypassPermissions` for the COBOL work; the
  hooks are the guard.
- **D2** Register a Windows Task Scheduler task that starts the supervisor at logon.
- **D3** Build the separate-process agent pool (section 3.1) once measurement justifies it.
- **D9** Install the logon entry (`install-autostart.ps1`) once the supervisor has completed a supervised first run; until then
  the old Startup entry keeps running (owner runs the installer; it changes a Startup file, so I do not).
- **D4** DONE (2026-10-04): the WSL lifecycle commands are blocked by `forbidden_commands.py` (rule 6, with self-test cases);
  the same change stopped the verdict-chain rule from firing on a command that merely names a gate script.
- **D5** Wire the ratchet into the lander gate and CI's `audits` job.
- **D6** Refit the token-to-point calibration automatically from consecutive meter readings, or by hand only.
- **D7** The `session_pct_per_weekly_pct` constant (seeded at 5.0; measured 4.0 on 2026-10-07 from one interval of account 2, kb/Work PB2478)
  was a guess until those two readings inside one session window measured it; more intervals refine it.
- **D8** Which token kinds the calibration counts (section 8): the first meter readings decide whether
  `counted_token_kinds` and `tokens_per_point` match the meter.

## 12. Tests and how CI would run them

Python self-tests in the style of `scripts/hooks/test_dispatch_guard.py` (a script that prints a case count and
exits nonzero on a failure; no framework): `test_alloc.py` (including two real parallel processes allocating
concurrently with no duplicate), `test_inventory_ratchet.py` (a fabricated reopen, a GAP rise, the marker),
`test_budget.py` (with each account's reset, readings and telemetry), `scripts/hooks/test_dispatch_guard.py` (rule 0: a loop
unit's fleet launch refused after `STOP-UNIT`, an attended session's not), `test_ledger_state.py` (owed until the current stamp is
marked, owed again after an input-touching commit, not after an unrelated one; per account, the first mark needs `--url`),
`account.py --self-test`, `mailbox.py --self-test` (section 15), `test_checkpoint.py` (real linked worktrees: committed, dirty, clean; a synthesized handoff checked
against the schema's required and permitted keys), `test_plan_wave.py` (fixture notes, clusters and reports in a temp directory, rendered through the
real dispatch-spec template and `check_practices.py`'s same-file rule; the fix lane's whole plan against a golden the pre-campaign planner wrote; the campaign lane's selection, `blocked_by` order, `after:` derivation and waits, and `work.py check`'s topology rules, section 9.1), `test_watch_agent.py` (a transcript with a
partial last line), `landing_lease.py --self-test` (two landers, takeover, holder-only renew and release, a race;
section 4.7; `LandingLeaseDriftTests` also pins its arms by name), `train_measure.py --self-test` (the batched-gating
trial's per-train record and summary, kb/Work PB2515; `TrainMeasureDriftTests` also pins its arms), `landing_check.py
--self-test` (section 4.7's file-set guarantee). The supervisor's self-test is twelve parallel parts,
`scripts/orchestrator/test_orchestrate_*.ps1`, sharing the harness `testdata/orchestrate_test_lib.ps1`: they drive the
loop with a fake `-ClaudeExe` (`testdata/fake-claude.ps1`) and `open-watchers.ps1` with a fake `wt.exe`
(`testdata/fake-wt.ps1`), and also cover `start-session.ps1 -ConfigDir`, `stop.ps1`, `steer.ps1` (section 4.8: validation,
precedence over a synthesized handoff, a steer written while a unit runs, consumption, an orphan `handoff.json`,
`-Unit`; `test_orchestrate_steer.ps1`), the context policy (section 4.3: the hard cap stops the fleet, the soft cap
does not, and a loop unit's second launch is refused; `test_orchestrate_stop.ps1`) and `account-profile.ps1` (in a
temp HOME). They need PowerShell 7 and are Windows-only (Windows Terminal, `Win32_Process`, `USERPROFILE`, junctions), which
each part declares in its header (`# SELF-TEST-PLATFORM: windows — …`).

**Every one of them runs in every gate and in CI, found by discovery (kb/Work PB2563).** `scripts/self_tests.py` finds
each `test_*.py`/`test_*.ps1` and each script that handles `--self-test` under `scripts/` and runs them all in parallel,
each with a private `COBOL_COORD_DIR`, so none touches the live coordination directory. It is one of the gate driver's
audits (`run_gate_legs.py` `AUDITS`, before the gate slot), a leg of `linux-gate.sh`, and a step of CI's `audits` job
(Linux) and `windows-build-test` job (Windows, where the Windows-only parts run); a self-test for the other platform is
reported `NOT RUN on <platform>` with its declared reason, never skipped silently. `test_plan_wave.py` imports
`check_practices.py`, so the runner fetches the `tools/claude-skills` submodule when a checkout lacks it.
`test_inventory_ratchet.py` reads the inventory at `HEAD`, which CI's full-history checkout provides.

## 14. Publishing the ledger after every landing

Owner 2026-10-04: "Publish ledger each time." The page is the owner's live view (the artifact recorded in the memory
`conformance-ledger-artifact`), and a stale page is worse than none. Publishing is the one step a headless unit CANNOT do:
its session has no `Artifact` tool (probed 2026-10-04 on Haiku: asked whether the tool is available, it answers NO), and a
wrapper around the claude.ai API would be a second author of the page. So the work splits where the capability is:
- **A unit renders** (`units/wave.md` step 5, `units/land.md` step 3): `python scripts/spec/gen_ledger.py --out
  {COORD}\ledger.html`, refreshing `docs/rearchitecture/evidence/ledger-in-flight.md` when the lanes changed.
- **The supervisor announces.** After every unit it runs `ledger_state.py owed`, which compares the page's own stamp (the
  last commit touching an input of the page, `gen_ledger.STAMP_PATHS`, imported so the two cannot disagree) with
  `ledger-published.json`, and prints `LEDGER PUBLISH OWED` in its log when they differ. `stop.ps1 -Status` shows the same.
- **The attended session publishes** and records it: `Artifact` publish of the rendered page to the existing URL, then
  `python scripts/orchestrator/ledger_state.py mark-published`.
- **Per account** (kb/Work PB2481, section 2.1): an artifact belongs to the claude.ai account that published it, and
  another account cannot update it. `ledger-published.json` is therefore `{"accounts": {<name>: {url, stamp,
  published_at}}}`; `owed` compares the running account's stamp; `mark-published --url <url>` records the account's
  artifact the first time (later marks keep it); `ledger_state.py url` and the last line of `gen_ledger.py` name it, or
  say to publish a new one. `gen_ledger.py` writes `<coord>\ledger.html` by default. In an attended session a unit's end arrives as a
  notification, and the publish is the first action after the landing report.
The limit, stated plainly: in a fully unattended run the publish waits for the next attended session, and the log line and
`stop.ps1 -Status` are how it is seen. `next_unit.py` no longer starts a `land` unit for a stale page.

## 13. Watching agents

The owner may want to WATCH agents work, but only at no more cost than running them in the background. So agents
keep running as Workflow subagents, and watching is a separate, read-only view that spends no tokens and can be
started and stopped at will.

### 13.1 The view (built)

- `watch_agent.py <agent-*.jsonl>` follows one subagent transcript (Workflow transcripts live under
  `<config dir>/projects/<repo key>/<session>/subagents/workflows/wf_*/` (the loop's account's config dir, `account.py`
  `project_dir`), each with a sibling `agent-*.meta.json`
  carrying `description` such as `impl-X-PB1407` and `model`). Per line: local time, the running count of model
  calls (one per message id), the latest call's context size, and each tool use's name with a 120-character summary
  of its input, or the first 200 characters of assistant text. It reads only complete lines; a partial last line
  waits for its newline, and a missing file waits to appear.
- `watch-agent.ps1 <transcript>` is a tab's entry point: it titles the tab from the meta file, sets UTF-8 output and
  runs the renderer, restarting it if it ever exits abnormally.
- `open-watchers.ps1 -WorkflowDir <dir> | -SessionDir <dir>` opens one tab per agent in ONE named Windows Terminal
  window (`wt.exe -w cobol-agents new-tab --title "<letter> <lead>" --tabColor <Sonnet blue #1E66F5, Opus orange
  #FE640B> pwsh -NoProfile -File watch-agent.ps1 <transcript>`) and keeps scanning, so agents a rolling wave starts
  later get tabs too.
- The supervisor starts `open-watchers.ps1` for a `wave` unit when `-Watch` is passed (the headless session's
  transcripts live under its `--session-id`), stops the scanner when the unit ends (the tabs stay), and never fails a
  unit because a watcher failed.

### 13.2 Terminals mode (optional experiment, not built)

Each agent as its own `claude -p --agent <agent definition> --output-format stream-json --verbose` process in its own
tab (the CLI also has `--agents` and `--include-partial-messages`), so the tab shows the live stream rather than a
transcript view. It is the section 3.1 alternative with a terminal attached, and carries all of that section's costs
(the pool, the successor rule, train assembly, the stall ceiling and the dispatch checks move into the supervisor).
**The measurement that decides it:** run the same group both ways and compare tokens and calls per agent from
`scripts/telemetry/usage_report.py`; adopt it only if it is within 5 % of the background cost. The measured agent
context averages about 340k tokens per call, and startup is under 10 % of an agent's cost, so the difference should
be small, but it is unmeasured. The default stays background.

## 15. The session mailbox (`mailbox.py`, kb/Work PB2482)

Owner 2026-10-07 13:35 PDT: the two attended sessions (section 2.1) "pass work to each other without me in the loop unless
needed". `<coord>\mailbox\README.md` is the protocol (kinds, processing, the wake-up watcher, the doorbell); this tool
enforces its message shape, which hand-built JSON did not (13:47: a `\b` escape turned a path in `refs` into a
backspace; `operator-session.json` was invalid JSON with unescaped backslashes).
- `send` writes one whole message atomically into `to-<recipient>\` as `<yyyyMMdd-HHmmss>-<from>-<kind>-<slug>.json`;
  it refuses an unknown kind, a body over 900 characters, empty `refs`, a path ref that does not exist, any control
  character, and a `task` or a Mythos `dispatch` without the owner's approval quote or `--needs-owner`.
  `--unless-pending` sends nothing while the same kind and subject is open.
- `list`, `take` (prints the message; a malformed one is declined into `done\` with the reason, never acted on), `done`
  (adds `result` and MOVES the file, so an inbox is the open set), `watch` (returns when the inbox holds a message:
  the background watcher's body), `session` (reads or updates `operator-session.json` with the account from `account.py`).
- `from` is `mythos`, `operator` or `loop`: after every unit with the ledger publish owed, the supervisor posts one
  `publish` message (per stamp) to the operator, pointing at the unit's log (section 14), so the attended session is
  woken by its watcher rather than by reading the supervisor's log.
- It is a transport of pointers, never a work register (CLAUDE.md rule 8).
