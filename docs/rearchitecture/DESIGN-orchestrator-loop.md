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
| `scripts/orchestrator/units/*.md` | one short prompt per unit type; each references files, never pastes them |
| `scripts/orchestrator/handoff.schema.json` | the contract between a unit and the supervisor (section 5) |
| `scripts/orchestrator/alloc.py` | the one locked allocator for DEVLOG numbers, `PB` ids and `COBOLNET` codes (section 6) |
| `scripts/orchestrator/inventory_ratchet.py` | the closed-rows ratchet (section 7) |
| `scripts/orchestrator/budget.py` | the weekly and session quota estimator (section 8) |
| `scripts/orchestrator/plan_wave.py` + `model_rules.json` | the deterministic wave planner and its routing and cost constants (section 9) |
| `scripts/orchestrator/test_*.py`, `scripts/orchestrator/test_orchestrate.ps1` | the self-tests (section 12) |

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
| `STOP` | the owner | ends the loop after the current unit |
| `STOP-UNIT` | the supervisor | asks the running unit to write its handoff and end (section 4.3) |
| `handoff.json` | the running unit | its handoff (section 5); the supervisor archives it per unit |
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

If the context cap fires (section 4.3) while a Workflow runs, the unit does NOT end the session: it creates the
fleet's graceful-stop file `{SCRATCH}\STOP`, so every implementer checkpoints and returns `SPLIT` and every lander
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

When the handoff names `next_unit`, the supervisor runs it. Otherwise (no handoff, or `next_unit` absent) it applies
these checks in order and takes the first that fires:

1. A handoff with `owner_question` → `owner-question` (stop).
2. The meter reading is older than 3 hours (or there is none) → `meter`.
3. The main checkout `E:\COBOL` is dirty (ignoring `.claude/settings.local.json`) or has unpushed commits → `resume`.
4. The last unit ended `split` or `failed`, or `prune_worktrees.py` reports an `UNLANDED` branch → `resume`.
5. The handoff lists `branches_pending` with status `DONE` → `land`.
6. The ledger (`gen_ledger.py` output in the coordination directory) is older than the last landing on `main` →
   `land` (whose prompt refreshes the ledger when it has nothing to land).
7. Otherwise → `wave`.

## 4. The supervisor loop (`orchestrate.ps1`)

Parameters: `-DryRun` (prints the decision and the exact `claude` command line, starts nothing), `-ClaudeExe` (the
executable; a test seam pointing at a fake that emits canned stream-json, not a wrapper), `-CoordDir`,
`-RepoDir` (default the repository containing the script), `-MaxContextTokens` (default 150000), `-MaxUnits`
(default unlimited), `-PermissionMode` (default `auto`), `-GraceMinutes` (default 30; a `wave` unit gets three
times this, because a lander train must be allowed to finish), `-BorrowDays` (passed to `budget.py`).

Each iteration, in this order:

1. **Single instance.** `orchestrate.lock` is created atomically holding the PID. An existing lock whose PID is a
   live process refuses the start (exit 3); a lock whose PID is gone is stale and is taken over.
2. **STOP.** `STOP` in the coordination directory ends the loop (exit 0). The owner creates it; the loop deletes
   nothing of the owner's.
3. **Circuit breaker.** Three consecutive units that end in under two minutes or with a nonzero exit or an invalid
   handoff stop the loop (exit 4) and write `logs\BREAKER-<time>.md` plus an `OWNER-QUESTIONS.md` entry naming the
   three units and their logs. Between failures the loop backs off 1, 2, 4 minutes (exponential, capped at 30).
4. **Budget.** `python scripts/orchestrator/budget.py --json` decides: `go` continues; `hold-session` sleeps until
   the session reset it reports; `hold-day` sleeps until 03:05 America/Los_Angeles the next day; `stop-week` ends the
   loop. Sleeps wake every minute to honour `STOP`.
5. **Next unit** (section 3.2).
6. **Run it**: `claude -p <prompt> --model <unit model> --permission-mode <mode> --permission-prompts none
   --output-format stream-json --verbose --session-id <fresh GUID>` from the repository root (plus `--chrome` for
   `meter`). The prompt is the unit file's text with three substitutions (`{COORD}`, `{HANDOFF}`, `{STOP_UNIT}`) and
   the previous handoff's path. The stream goes to `logs\<time>-<unit>.jsonl`.
   While it runs, the supervisor reads every `assistant` event's `usage` and keeps the running context estimate =
   `input_tokens + cache_read_input_tokens + cache_creation_input_tokens` of the LATEST call (the size of the
   context the model just read). When it passes `-MaxContextTokens` the supervisor creates `STOP-UNIT` and waits
   for the session to end; after the grace period it kills the process tree.
7. **Validate the handoff** against `handoff.schema.json` (`Test-Json -SchemaFile`). Missing or invalid counts as a
   failure for the breaker, and the next unit is `resume`.
8. **Record** one line in `units.jsonl`: `{unit, model, session_id, started_at, ended_at, duration_s, exit_code,
   calls, tokens: {input, output, cache_read, cache_creation}, peak_context, stop_unit_sent, killed,
   handoff_outcome, next_unit}`. This is the per-unit cost record PB1981 item 9 refits the cost law from.
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
a hang. The default mode is `auto`; open decision D1 asks whether the owner wants `bypassPermissions`.

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
| the owner wants it stopped | | `STOP` |
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
  session estimate ≥ the soft stop (70 %); else `go`. The hard stop (90 %) is reported as `session_hard_stop` for
  the supervisor's in-unit check. `resume_at` names when a hold ends.
- Output: `{weekly_est_pct, allowance_pct, headroom_pct, session_est_pct, decision, resume_at, anchor, spend}`.

The estimate drifts from the meter between readings; the 3-hour `meter` unit re-anchors it. Every reading also
refits nothing automatically: the constants change only by an explicit edit of `model_rules.json` with its date and
source (open decision D6 asks whether readings should refit them).

## 9. The wave planner (`plan_wave.py`)

`python scripts/orchestrator/plan_wave.py --wave N --budget-points P [--scratch DIR] [--reports DIR]
[--clusters-json FILE] [--dry-run]` (or `--from-budget` to take `headroom_pct` from `budget.py`).

1. **Finishers first.** Open or half notes become finisher groups when (a) the newest report under the reports
   directory that names the note in its header says `SPLIT` or `NOT STARTED` (one finisher group per predecessor
   report, carrying that report's branch, worktree and head), (b) the note is `status: half`, or (c) an `UNLANDED`
   branch (`prune_worktrees.classify`, reused) carries the note's id in its commit subjects. A finisher group's
   `pred` is a fixed paragraph naming the predecessor report and branch.
2. **Clusters** from `fix_clusters.py --json` (the submodule's view of the open notes), minus the finishers' notes,
   ranked by inventory rows claimed (the sum of each note's `inventory_rows`), then by harm. The cluster cap of five
   notes comes from `fix_clusters.py --max 5`.
3. **One group per primary file**: a second cluster on the same file becomes a same-file successor
   (`after: <letter>`, letter suffixed `2`), the rule `wf_rolling_wave.js` and `check_practices.py` (O2) enforce.
4. **Model per group** from `model_rules.json`: Opus when the primary file or the area matches a design-heavy
   pattern (`/Oo/`, `.g4`, `frontend/grammar`, `oo`), when a note's body says a design question is open, or when a
   note already has two reports without landing (a note that fails twice is a task-spec problem, PB1981); else
   Sonnet. Fable is never a value.
5. **Cost**: tokens per group = `per_group + per_note × notes` for its model, converted to points with
   `tokens_per_point`; one lander per `train_size` groups at `lander_tokens` on Opus. Groups are taken in rank order
   while the running total fits the budget; a successor is taken only with its predecessor.
6. **Allocation** through `alloc.py`: three codes per group, one lead-id block of five per train, one DEVLOG
   number per train (`--dry-run` peeks and reserves nothing).
7. **Outputs** (not in `--dry-run`): `<scratch>\groups.json` in exactly `make_dispatch_specs.py`'s format, the
   rendered specs and `check_practices.py`'s verdict (by running `make_dispatch_specs.py`), and
   `<scratch>\wf-args-w<wave>.json` with the rolling wave's args (`scratch, wave, concurrency, train_size,
   min_final_train, devlog_n, previous_train, lead_id_blocks, implementer_model, authorization, groups`).

The constants in `model_rules.json` carry their date and source: one weekly point is about 1 M agent tokens on
Sonnet and 0.4 M on Opus (measured 2026-10-04 over waves 1011-1015). The per-group and per-note figures are seeded
from that measurement and are refit from `units.jsonl` and `usage_report.py` (PB1981 item 9).

## 10. What is built and what is not

Built: everything in section 2, with tests. Not built: the separate-process agent pool (section 3.1), the
Task Scheduler entry (4.4), the ratchet's wiring into the lander brief and CI (7), the hook shapes for the WSL
lifecycle commands (4.5), automatic refit of the calibration (8). The prototype has never run a real session;
the owner's first run is `-DryRun`, then `-MaxUnits 1`.

## 11. Open decisions

- **D1** Permission mode for unattended units: `auto` (default here) or `bypassPermissions`.
- **D2** Register a Windows Task Scheduler task that starts the supervisor at logon.
- **D3** Build the separate-process agent pool (section 3.1) once measurement justifies it.
- **D4** Add the WSL lifecycle commands to `forbidden_commands.py` (today only the prompts forbid them).
- **D5** Wire the ratchet into the lander gate and CI's `audits` job.
- **D6** Refit the token-to-point calibration automatically from consecutive meter readings, or by hand only.
- **D7** The `session_pct_per_weekly_pct` constant (seeded at 5.0, i.e. one weekly point ≈ 5 % of a 5-hour window)
  is a guess until two readings inside one session window measure it.

## 12. Tests and how CI would run them

Python self-tests in the style of `scripts/hooks/test_dispatch_guard.py` (a script that prints a case count and
exits nonzero on a failure; no framework): `test_alloc.py` (including two real parallel processes allocating
concurrently with no duplicate), `test_inventory_ratchet.py` (a fabricated reopen, a GAP rise, the marker),
`test_budget.py`, `test_plan_wave.py` (fixture notes, clusters and reports in a temp directory). CI runs the hook
self-tests in the `audits` job (`python3 scripts/hooks/test_forbidden_commands.py && ...`); the orchestrator tests
would be one more step there (`for t in scripts/orchestrator/test_*.py; do python3 "$t"; done`). They need no
submodule and no build. The supervisor's self-test, `pwsh scripts/orchestrator/test_orchestrate.ps1`, drives the
loop with a fake `-ClaudeExe` and needs PowerShell 7, so it would run in the `windows-build-test` job or a `pwsh`
step on Linux. The workflow file is not edited by this change.
