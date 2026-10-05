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
| `scripts/orchestrator/units/*.md` | `common.md` (the rules every unit obeys) plus one short prompt per unit type; they reference files, never paste them |
| `scripts/orchestrator/handoff.schema.json` | the contract between a unit and the supervisor (section 5) |
| `scripts/orchestrator/alloc.py` | the one locked allocator for DEVLOG numbers, `PB` ids and `COBOLNET` codes (section 6) |
| `scripts/orchestrator/inventory_ratchet.py` | the closed-rows ratchet (section 7) |
| `scripts/orchestrator/budget.py` | the weekly and session quota estimator (section 8) |
| `scripts/orchestrator/plan_wave.py` + `model_rules.json` | the deterministic wave planner and its routing and cost constants (section 9) |
| `scripts/orchestrator/coord.py` | names the coordination directory, loads `model_rules.json`, writes JSON atomically |
| `scripts/orchestrator/watch_agent.py`, `watch-agent.ps1`, `open-watchers.ps1` | the token-free view of running agents in Windows Terminal tabs (section 13) |
| `scripts/orchestrator/test_*.py`, `test_orchestrate.ps1`, `testdata/` | the self-tests and their fakes (section 12) |

The coordination directory lives OUTSIDE every git worktree so that no worktree, branch switch or `git clean` can
lose it, and every session and agent sees the same copy. Default `E:\COBOL-coord`; override with the environment
variable `COBOL_COORD_DIR` (every Python tool) or `-CoordDir` (the supervisor, which exports `COBOL_COORD_DIR` to
the tools it runs). Contents:

| File | Writer | Meaning |
|---|---|---|
| `alloc.json` | `alloc.py` | the highest value reserved so far per kind |
| `alloc.lock` | `alloc.py` | the allocator's lock (atomic create; stale after 60 s) |
| `readings.json` | the `meter` unit (via `budget.py --record`) | owner-meter readings, a list of `{noted_at, weekly_pct, session_pct, session_reset}` |
| `orchestrate.lock` | the supervisor | one instance only: `{pid, started_at, host}` |
| `STOP` | the owner (`stop.ps1`) | closes work down as soon as possible without losing any: a running unit is wound down (below), then the loop ends; `stop.ps1 -Clear` removes it |
| `STOP-UNIT` | the supervisor | asks the running unit to write its handoff and end (section 4.3); created for the context cap and for `STOP` |
| `scratch\STOP` | the supervisor | the fleet's graceful-stop file, created with `STOP-UNIT`: every implementer and lander checkpoints, commits and returns `SPLIT`; removed at the start of the next unit |
| `handoff.json` | the running unit | its handoff (section 5); the supervisor archives it per unit |
| `handoff.last.json` | the supervisor | the newest VALID handoff, which the next unit reads and `next_unit.py` decides from |
| `scratch\` | the units and their fleets | the fleet scratch directory (`{SCRATCH}`): specs, `groups.json`, the Workflow args, and `reports\`, which `plan_wave.py` reads for finishers; it persists across units |
| `clusters-open.json`, `clusters-half.json` | `plan_wave.py` | the latest `fix_clusters.py --json` views it planned from |
| `units.jsonl` | the supervisor | one line per unit (section 4.5) |
| `OWNER-QUESTIONS.md` | the supervisor | questions only the owner can answer; the loop stops after writing one |
| `logs\` | the supervisor | the raw stream-json of every unit, its archived handoff, and the breaker's notes |

## 3. Unit types

A unit is one fresh `claude -p` session with one prompt, one model and one exit condition. Units never chain inside
one session.

| Unit | Model | Starts when | Ends when |
|---|---|---|---|
| `wave` | Opus (the orchestrator's judgment; the implementers are routed per group by `model_rules.json`) | nothing pending to land or resume and the budget says `go` | its Workflow returned, the last lander train landed through `push-main.sh`, the ledger refreshed (`python scripts/spec/gen_ledger.py`, `references/landing.md`) and the handoff written |
| `land` | Opus | finished implementer branches exist with no lander (a previous wave unit ended early), or a handoff says `next_unit: land` | the train landed (or was dropped with reasons) and the ledger refreshed |
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

If the context cap fires (section 4.3) or the owner creates `STOP` while a Workflow runs, the unit does NOT end the
session: the supervisor creates the fleet's graceful-stop file `{SCRATCH}\STOP` (with `STOP-UNIT`), so every implementer checkpoints and returns `SPLIT` and every lander
finishes or abandons its train at a cluster boundary, waits for the Workflow to return, writes a handoff with
`next_unit: resume` that names every branch, worktree and report, and ends. The next unit is a fresh `resume`.
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

`next_unit.py` reads the newest valid handoff (`handoff.last.json`) and applies these checks in order; the first
that fires wins (the supervisor's `-Unit` overrides the first iteration only):

1. The handoff carries `owner_question` → `owner-question` (stop).
2. The handoff names `next_unit` → that unit.
3. The newest meter reading is older than 3 hours (`quota.meter_max_age_hours`), or there is none → `meter`.
4. The repository is dirty (ignoring `.claude/settings.local.json`) or has unpushed commits → `resume`.
5. The last unit failed (as the breaker counts it) or ended `split`, or a branch that `prune_worktrees.classify`
   calls `UNLANDED` received a commit after the last unit started (an agent of a unit that died) → `resume`.
   Older unlanded branches do not trigger it, so an abandoned branch cannot cause a `resume` loop; `plan_wave.py`
   still sees them.
6. The handoff lists `branches_pending` with status `DONE` → `land`.
7. The ledger page (`docs/rearchitecture/evidence/conformance-ledger.html`, `gen_ledger.py`'s default output) is older
   than the newest inventory commit on `origin/main` → `land` (whose prompt refreshes the ledger when it has
   nothing to land).
8. Otherwise → `wave`.

## 4. The supervisor loop (`orchestrate.ps1`)

Parameters: `-DryRun` (prints the budget decision, the unit it would run and the exact `claude` command line, then
exits; it starts nothing and, past a hold, says what it would run after it), `-ClaudeExe` (the executable; a test
seam pointing at a fake that emits canned stream-json, not a wrapper; a `.ps1` or `.cmd` is launched through its
shell), `-CoordDir`, `-RepoDir` (default the repository containing the script), `-MaxContextTokens` (default
150000), `-MaxUnits` (default unlimited), `-PermissionMode` (default `bypassPermissions`), `-GraceMinutes` (default 30; a `wave`
unit gets three times this, because a lander train must be allowed to finish), `-BorrowDays` (passed to
`budget.py`), `-Unit` (the first unit, overriding `next_unit.py` once), `-Watch` (section 13), `-Python`, and the
test seams `-TelemetryDir` (passed to `budget.py`), `-IdleCloseSeconds` (default 20, section 4.6) and `-FastFailSeconds` (default 120; a unit under it fails only without a `done` handoff, because the `meter` unit legitimately takes about 40 s) and `-BackoffBaseSeconds`
(default 60). Exit codes: 0 stopped (`STOP`, `-MaxUnits`, `stop-week`, `-DryRun`), 3 another instance runs,
4 circuit breaker, 5 an owner question is waiting.

Each iteration, in this order:

1. **Single instance.** `orchestrate.lock` is created atomically holding the PID. An existing lock whose PID is a
   live process refuses the start (exit 3); a lock whose PID is gone is stale and is taken over.
2. **STOP.** `STOP` in the coordination directory ends the loop (exit 0), and it does so ASAP WITHOUT LOSING WORK
   (owner 2026-10-04: "close down work asap when needed without losing any"). Between units the loop ends at once;
   a hold or a backoff is interrupted within a minute; a RUNNING unit is wound down by the supervisor itself, not left
   to finish: it creates `STOP-UNIT` and the fleet's `scratch\STOP`, every agent checkpoint-commits its WIP and returns
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
5. **Next unit** (section 3.2).
6. **Run it**: `claude -p <prompt> --model <unit model> --permission-mode <mode> --permission-prompts none
   --output-format stream-json --verbose --session-id <fresh GUID>` from the repository root (plus `--chrome` for
   `meter`). The prompt is `units/common.md` followed by `units/<unit>.md`, with the substitutions `{COORD}`,
   `{HANDOFF}`, `{STOP_UNIT}`, `{PREV_HANDOFF}`, `{SCRATCH}`, `{TASKS_DIR}` and `{BORROW_DAYS}` (the supervisor's `-BorrowDays`, so the
   `wave` unit's `plan_wave.py --from-budget` sees the same allowance the supervisor's gate used). The stream goes to `logs\<time>-<unit>.jsonl`,
   stderr to `logs\<time>-<unit>.stderr.txt`.
   While it runs, the supervisor reads every `assistant` event's `usage` (counting each message id once: the
   stream repeats a call's usage on each content block) and keeps the running context estimate =
   `input_tokens + cache_read_input_tokens + cache_creation_input_tokens` of the LATEST call (the size of the
   context the model just read). When it passes `-MaxContextTokens`, or the owner creates `STOP`, the supervisor
   creates `STOP-UNIT` and the fleet's `scratch\STOP` and waits for the session to end; after the grace period it
   kills the process tree.
   **The supervisor, not the model, decides when the unit is over.** The prompt is the first stream-json message
   (`--input-format stream-json`) and stdin stays open, because a headless session exits the moment its model ends a
   turn and a Workflow or background gate dies with it (wave 1017, 2026-10-04: the unit wrote "I'm waiting for the
   Workflow" and ended its turn, and an eight-agent fleet died at minute 11). With stdin open a background task's
   completion notification wakes the model after `end_turn` (probed: ended at 16.8 s, woken at 41 s). The supervisor
   tracks `result` and `system/background_tasks_changed` events and closes stdin when the model has ended a turn, no
   background task runs and the stream has been quiet for `-IdleCloseSeconds` (20; a task's completion event arrives
   just after its list empties, so the close waits). `units/wave.md` also tells the model to wait with foreground
   calls on the Workflow's task-output file (`{TASKS_DIR}`), a second line of defence, never the only one.
7. **Validate the handoff** against `handoff.schema.json` (`Test-Json -SchemaFile`). Missing or invalid counts as a
   failure for the breaker, and the next unit is `resume`.
8. **Record** one line in `units.jsonl`: `{unit, reason, model, session_id, started_at, ended_at, duration_s,
   exit_code, handoff_outcome, next_unit, failed, log, calls, input, output, cache_read, cache_creation,
   peak_context, cost_usd, stop_unit_sent, killed}`. This is the per-unit cost record PB1981 item 9 refits the cost
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

### 4.3 The context cap (graceful stop)

The same protocol the workflows use for their agents: a file, checked before each new step, never a kill. The
cap exists because a unit's cost per call grows with its context; at 150k the next unit's fresh start is cheaper
than continuing.

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
| a unit's context grows without bound | each call costs more than a fresh start | `STOP-UNIT` at `-MaxContextTokens`, kill after the grace period |
| a unit ends with a Workflow in flight | the agents die with the process | the wave unit stays alive until its lander landed; on a cap it stops the fleet gracefully first (section 3.1) |
| a unit guesses an owner decision | a wrong irreversible landing | `owner_question` in the handoff stops the loop |
| the quota runs out mid-week | the owner's other work (the TENET project) is starved | `budget.py`: `hold-day` at the cumulative daily allowance, `stop-week` at the weekly cap |
| the 5-hour session window runs out mid-unit | a unit killed at the limit | `hold-session` at the soft stop (70 %) before a unit starts |
| two sessions allocate the same id or code | a renumbering pass (five collisions in one day, 2026-09) | `alloc.py`: one lock, reservations outside every worktree |
| a landing reopens a closed inventory row | silent conformance regression | `inventory_ratchet.py` (section 7) |
| the owner wants it stopped | losing agent work if the process were killed | `STOP` (`stop.ps1`): a graceful wind-down of the running unit and its fleet, a kill only after the grace period |
| a unit's model ends a turn with a fleet in flight | the headless process exits and the fleet dies (wave 1017) | the supervisor holds stdin open and closes it only when idle with no background task |
| a unit runs a WSL lifecycle command | every other session's Linux gate dies | the unit prompts forbid it; `forbidden_commands.py` is the hook-level guard (open decision D4: add these shapes there) |

## 5. The handoff (`handoff.schema.json`)

Required: `schema_version` (1), `unit`, `outcome` (`done` · `split` · `failed` · `owner-question`), `summary`
(at most 900 characters; the detail lives in files the handoff points at).
Optional: `next_unit` (one of the unit types, or null to let the deterministic checks choose) with
`next_unit_reason`; `branches_pending` (`{branch, worktree, report, status}`, status one of the rolling wave's
`DONE` · `SPLIT` · `DISCHARGED` · `BLOCKED`); `workflow` (`{run_id, state}` with state `landed` · `stopped` ·
`none`); `landed` (`{commits, gap_before, gap_after, devlog_entry}`); `meter_reading` (the reading appended, for
the `meter` unit); `owner_question` (`{question, context}`), required exactly when `outcome` is `owner-question`;
`notes_touched` (kb/Work ids). Additional properties are refused, so a misspelled key fails validation instead of
being ignored.

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
- **The lock** is an atomic create of `alloc.lock` (`os.open` with `O_CREAT | O_EXCL`) holding the PID and time.
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

- **Anchor.** The newest reading in `readings.json` (owner-meter values read by the `meter` unit). Readings from
  before the current week's reset (Sunday 03:00 America/Los_Angeles) anchor nothing; the weekly base is then 0 from
  the reset.
- **Spend since the anchor** comes from the local telemetry sink (`usage_report.py`'s `events()` over
  `~/.claude/telemetry/<day>.jsonl`, reused, not re-parsed): every `api_request` after the anchor, converted to
  weekly points per model with `model_rules.json`'s calibration (`tokens_per_point`, counting the token kinds named
  in `counted_token_kinds`).
- **Weekly estimate** = anchor weekly % + points since the anchor.
- **Allowance** = day N × 14.3 % (day 1 starts at the Sunday 03:00 reset), plus `--borrow-days` days the owner
  allowed, capped at the weekly cap (`weekly_cap_pct`, 98 by the owner's 2026-10-02 instruction).
- **Session estimate** = the anchor's session % (when its `session_reset` is still in the future) plus points since
  the anchor × `session_pct_per_weekly_pct`; after the reset it starts from 0 at the reset.
- **Decision**: `stop-week` when weekly ≥ the cap; `hold-day` when weekly ≥ the allowance; `hold-session` when the
  session estimate ≥ the soft stop (70 %); else `go`. The hard stop (90 %) is reported as `session_hard_stop_pct`
  (the supervisor does not yet act on it inside a unit; the soft stop before each unit is the guard). `resume_at`
  names when a hold ends.
- Output: `{weekly_est_pct, allowance_pct, headroom_pct, session_est_pct, decision, resume_at, week_start, day,
  anchor, spend}`.

The estimate drifts from the meter between readings; the 3-hour `meter` unit re-anchors it. A reading refits nothing
automatically: the constants change only by an explicit edit of `model_rules.json` with its date and source (open
decision D6). **The calibration is unverified against telemetry.** The 2026-10-04 constants were fitted to the
subagent tokens the Workflow tool reports; `budget.py` counts input, output and cache-write tokens from telemetry
(cache reads excluded). On 2026-10-04 at 14:50 PDT, with no anchor, it estimated 28.4 points spent since that
morning's reset (13.4 M Sonnet and 5.8 M Opus counted tokens). Until the first meter readings confirm or correct
that, treat the weekly estimate as a rough guide (open decision D8).

## 9. The wave planner (`plan_wave.py`)

`python scripts/orchestrator/plan_wave.py (--budget-points P | --from-budget) [--wave N|next] [--scratch DIR]
[--reports DIR] [--clusters-json FILE] [--half-clusters-json FILE] [--no-branches] [--max-groups N] [--dry-run]`.
`--wave` defaults to one past the highest wave in the reports directory or the newest DEVLOG lines.

The plannable notes are exactly those `fix_clusters.py --json` lists (the submodule's view of `kb/Work`, which
applies `.agent-fleet.json`'s kind and skip flags), run twice: once over `status: open`, once over `status: half`.

1. **Awaiting landing.** A note whose newest report names a branch that is not on `main` yet
   (`prune_worktrees.classify`, reused: `UNLANDED` or `CHECK`) is excluded and listed: re-planning it would dispatch
   work that already exists. The `land` or `resume` unit deals with it first.
2. **Finishers first.** (a) A note whose newest report (by wave, then time; header lines only) says `SPLIT` or
   `NOT STARTED`, with that report's branch landed or gone: one finisher group per predecessor report. (b) The half
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

Measured on 2026-10-04 against the repository after train 1015 (budget 3 points): eight groups in two trains,
finishers first (PB244 from wave 1013's split, PB480 and PB1112 from wave 1013's not-started pair, PB1940 from
wave 1015's split), then the half clusters, with Opus on the OO and grammar files and on PB244 (attempted twice);
wave 1015's dropped group Y (PB1263 and four more) listed as awaiting landing. That is the shape waves 1012-1015
chose by hand (a finisher group first, same-file successors, Opus where the design is open), with more and smaller
groups because the open half notes now form their own clusters.

The constants in `model_rules.json` carry their date and source: one weekly point is about 1 M agent tokens on
Sonnet and 0.4 M on Opus (measured 2026-10-04 over waves 1011-1015). The per-group and per-note figures are seeded
from that measurement and are refit from `units.jsonl` and `usage_report.py` (PB1981 item 9).

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
- **D7** The `session_pct_per_weekly_pct` constant (seeded at 5.0, i.e. one weekly point ≈ 5 % of a 5-hour window)
  is a guess until two readings inside one session window measure it.
- **D8** Which token kinds the calibration counts (section 8): the first meter readings decide whether
  `counted_token_kinds` and `tokens_per_point` match the meter.

## 12. Tests and how CI would run them

Python self-tests in the style of `scripts/hooks/test_dispatch_guard.py` (a script that prints a case count and
exits nonzero on a failure; no framework): `test_alloc.py` (including two real parallel processes allocating
concurrently with no duplicate), `test_inventory_ratchet.py` (a fabricated reopen, a GAP rise, the marker),
`test_budget.py`, `test_plan_wave.py` (fixture notes, clusters and reports in a temp directory, rendered through the
real dispatch-spec template and `check_practices.py`'s same-file rule), `test_watch_agent.py` (a transcript with a
partial last line). CI runs the hook self-tests in the `audits` job (`python3 scripts/hooks/test_forbidden_commands.py
&& ...`); the orchestrator tests would be one more step there
(`for t in scripts/orchestrator/test_*.py; do python3 "$t"; done`). They need no build; `test_plan_wave.py` imports
`check_practices.py`, which that job already has with its submodule. `test_inventory_ratchet.py` reads the
inventory at `HEAD`, which the job's full-history checkout provides. The supervisor's self-test,
`pwsh scripts/orchestrator/test_orchestrate.ps1`, drives the loop with a fake `-ClaudeExe`
(`testdata/fake-claude.ps1`) and `open-watchers.ps1` with a fake `wt.exe` (`testdata/fake-wt.ps1`); it needs
PowerShell 7 and git, so it would run in the `windows-build-test` job. The workflow file is not edited by this change.

## 13. Watching agents

The owner may want to WATCH agents work, but only at no more cost than running them in the background. So agents
keep running as Workflow subagents, and watching is a separate, read-only view that spends no tokens and can be
started and stopped at will.

### 13.1 The view (built)

- `watch_agent.py <agent-*.jsonl>` follows one subagent transcript (Workflow transcripts live under
  `~/.claude/projects/<repo key>/<session>/subagents/workflows/wf_*/`, each with a sibling `agent-*.meta.json`
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
