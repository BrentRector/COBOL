---
name: workstream
description: Use BEFORE dispatching any fleet, lander, implementer or adjudication workflow, and before starting or supervising the orchestrator loop (scripts/orchestrator/orchestrate.ps1, the default way to run the fix lane) - the owner's standing instructions (2026-09-02) for running workstreams so a session-limit kill costs at most one step and a restart never repeats work - checkpoint to disk, fresh agents from checkpoints, a hard concurrency budget, finished work landed first, central id allocation. Carries the brief and workflow templates.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:agent-fleet` (Skill tool) before reading on. If the plugin is not loaded
> (a cloud session receives no project marketplace), Read `tools/claude-skills/skills/agent-fleet/SKILL.md` instead
> (`git submodule update --init tools/claude-skills` if the path is missing). THEN apply this overlay: it carries only
> what is specific to WiseOwl COBOL — commands, paths, kb/Work, push-main, the owner's dated decisions and
> measurements — and wins on conflict. Pinned: **brent-tools 1.19.1** (`tools/claude-skills`, kb/Work/PB1699). A
> practice improved here is written into the public base first, then consumed by moving the pin.

# Workstream — token-frugal, restart-safe orchestration

> ⛔ **THE MANDATORY PRACTICES ARE IN ONE FILE — `templates/MANDATORY-PRACTICES.md` — AND ARE ENFORCED.** Owner
> 2026-09-23: "All these best practices must be durably recorded for all future to must use." Every brief points at
> it; implementer/finisher dispatch specs are RENDERED by `make_dispatch_specs.py <groups.json>` from
> `templates/dispatch-spec-implementer.md` (never hand-written in a scratchpad); `check_practices.py` must print
> `=== PRACTICES CHECK: GREEN ===` over the briefs and over every rendered spec before a Workflow call. A new
> practice is added THERE, with its reason and measurement, and to `check_practices.py` — nowhere else.

> ⭐ **THE DEFAULT WAY TO RUN THE FIX LANE IS THE ORCHESTRATOR LOOP** (owner 2026-10-04: it "appears to better handle
> autonomous progress towards the compiler"; wave 1018 ran 2.5 hours unattended and landed 8 of 8 groups, GAP 281 to
> 255). `pwsh scripts/orchestrator/orchestrate.ps1 [-BorrowDays N] [-MaxUnits N] [-Unit meter|resume|land|wave|campaign] [-Cluster <lead>] [-DryRun]
> [-Watch]` runs one bounded unit per fresh `claude -p` session, chosen by `next_unit.py` from the last handoff and the
> disk (meter, resume, land, wave); each unit loads THIS skill and follows `scripts/orchestrator/units/*.md`. Design:
> `docs/rearchitecture/DESIGN-orchestrator-loop.md` (kb/Work PB1981); the generalized form is the base skill's
> `references/orchestrator-loop.md`. What it already does, so no session hand-rolls it:
> - **The supervisor owns each unit's lifetime**: the prompt is the first stream-json message and stdin stays open, because
>   a one-shot `claude -p` terminates background tasks 600 s after its model ends a turn (wave 1017's fleet died that way).
> - **Frequent handoffs** (PB2015): `checkpoint.json` every 5 minutes and on background-task changes, the model's
>   `milestones.jsonl`, and a synthesized `split`/`resume` handoff when a unit dies or the supervisor does.
> - **STOP** (`pwsh scripts/orchestrator/stop.ps1`, `-Status`, `-Clear`) winds a running unit and its fleet down without
>   losing work: agents checkpoint-commit and return `SPLIT`; a kill only after the grace period.
> - **One allocator** (`alloc.py`), the quota estimate (`budget.py`, with the owner's `-BorrowDays`), the closed-rows ratchet,
>   the planner (`plan_wave.py`, which plans from branches and worktrees, never from remembered reports).
>
> **The attended session's duties while the loop runs**: publish the ledger whenever the supervisor logs `LEDGER PUBLISH
> OWED` (a headless unit has no Artifact tool): render `{COORD}\ledger.html`, publish it to the owner's artifact, then
> `python scripts/orchestrator/ledger_state.py mark-published` (PB2016, owner 2026-10-04: "Publish ledger each time");
> read each unit's handoff; prune with `scripts/prune_worktrees.py`; run the battery on its cadence. Hand-dispatching a
> Workflow stays legal for a supervised one-off, and everything below still governs it. Do not edit the main checkout
> while a unit runs: work in a worktree.

The orchestrator (the session model) dispatches, reconciles, gates and commits; every other job — probe, implement,
validate, adversarial review, land — is a subagent, and ⭐ **THE MODEL FOLLOWS THE ROLE** (MANDATORY-PRACTICES P1,
owner 2026-09-27): JUDGMENT roles (implementer, lander, adjudicator, refuter, registrar, reviewer) run the latest
Opus by the ALIAS `opus` (Opus 5.5 since 2026-09-22 — never pin a dated id; `~/.claude/settings.json` sets
`CLAUDE_CODE_SUBAGENT_MODEL=opus` as the default), and the workflow passes `model: 'opus'` for them. MECHANICAL
roles run Sonnet from their own frontmatter: `cobol-clerk` for chores that write (filing notes from a structured
report, leak scans, doc prose, link and format sweeps) and `cobol-locator` for read-only lookups (code-site
location, orient.py / fix_clusters.py / where.py summaries, transcript and log measurements). ⛔ Never pass
`model` on a mechanical role's `agent()` call: it overrides the frontmatter. A mechanical agent that hits a
judgment call returns `NEEDS-OPUS: <why>`, and that item is re-dispatched to a judgment role. ⭐ **Fleets run through the Workflow tool on a STANDING owner opt-in (2026-09-25)**, and
every `agent()` names its role's `agentType` from `.claude/agents/` (`cobol-implementer` · `cobol-lander` ·
`cobol-refuter` · `cobol-adjudicator` · `cobol-clerk` · `cobol-locator`), which fixes that role's effort, turn cap, 1-hour prompt cache
and — for the read-only roles — a hook that refuses writes inside any git tree (MANDATORY-PRACTICES O4, P12).

## 1. Checkpoint to disk, never to a transcript

- Commit WIP on the worktree branch after every mechanism and every gate, and keep `<worktree>/STATUS.md` (first line `STATUS-AT: <sha>`; DONE · NEXT · BLOCKED · GATE).
- A workflow stage appends one JSON line per decided rule to `<out>/<stage>-<slug>.jsonl` and skips what is already there on start.
- Reports and briefs are files under the scratchpad; the conversation carries pointers only.
- Every dispatch prompt carries the graceful-stop line: before each new step check for `{SCRATCH}\STOP`; if it exists, checkpoint-commit, write STATUS.md NEXT, return status SPLIT.
- A workflow agent never ends its turn while a background job runs: start the job in the background to a log, then block in the foreground on `timeout 580 bash -c 'until grep -qE "<verdict pattern>" <log>; do sleep 5; done'`.
- A killed agent is replaced by a FRESH agent that reads the checkpoint; resume via `SendMessage` only within a step of finishing.
- Every fleet-optimization experiment is recorded under `docs/rearchitecture/evidence/fleet-optimization/`.

Read `references/checkpointing.md` before writing a dispatch prompt, briefing a resume or replacement, or recording a fleet experiment.

## 2. The cost law, the turn caps, and the concurrency budget

- Turn caps: 160 for any read-only agent, 400 for an implementer; at the cap, checkpoint and return `SPLIT`. A job that will not fit is SPLIT, never extended.
- One mechanism per implementer. The fill unit is a GROUP computed by `fix_clusters.py` (never hand-picked), one group per subsystem, orientation through `orient.py` before any source is read.
- Budget: 1 lander + at most 6 implementers + one 4-wide read-only chunk; workflows take `concurrency` args and run chunks with `parallel()` in a loop, never a 16-wide `pipeline`; a fleet over ~40 agents is split.
- Implementer gate: `build-local.ps1 -Mode implementer -Priority BelowNormal` (holds a gate slot); lander gate: `-Mode lander`. Trains of 4-6 clusters, one landing per lander transcript, the next lander dispatched while the previous train is in CI.

Read `references/cost-law-and-budget.md` before sizing a wave, setting caps, grouping fixes or dispatching a lander; it carries the cost law and its measurements, the four speed levers, and the lander-throughput decisions.

⭐ **THE FIX LANE IS A ROLLING WAVE — owner 2026-09-27 ("fix related work together … minimize repetitive context
gathering"; "use this new rolling functionality as it reduces context and token use").** Every fix-lane dispatch is
`Workflow({scriptPath: ".claude/skills/workstream/templates/wf_rolling_wave.js", args})`. There is no wave barrier:
- **Rolling pool.** `concurrency` workers pull groups from one queue, so a freed slot is refilled the moment an
  implementer returns, never after the slowest of the wave.
- **Trains as branches finish.** A lander train starts when `train_size` branches are ready. Trains are serialized,
  and each lander re-reads the DEVLOG top and rebases onto whatever the previous train landed. A final remainder of at
  least `min_final_train` branches gets a train; a smaller one is held for the next queue's first train.
- **Same-file successors.** A file with more open notes than the cluster cap (`fix_clusters.py` caps at 5) becomes
  two or more groups, and each later one declares `after: '<predecessor letter>'`. The successor waits for its
  predecessor, merges its branch, and orients from that report's "For the next implementer" section and STATUS.md
  instead of re-surveying the file. The predecessor's branch is HELD from the trains and lands THROUGH the successor,
  which contains it; if the successor produces nothing landable, the predecessor lands alone. A successor may also
  follow a group in the same technological area (a runtime file, then its emitter).
- **A campaign is a named cluster, and it is planned, not hand-picked** (kb/Work PB2120, design section 9.1). Process
  work the fix lane cannot see (`process_only`, sites outside `src/Cobol.Net.*`: the legacy retirement `PB2108`, the
  architecture review `PB1754`) runs from its notes' `cluster:` and `blocked_by:`:
  `python scripts/spec/work.py next --cluster <lead>` lists it in dependency order,
  `python scripts/orchestrator/plan_wave.py --cluster <lead> --budget-points P --dry-run` plans a wave of it (its
  dependent notes become successors, `after:`, of the groups holding their blockers), and
  `pwsh scripts/orchestrator/orchestrate.ps1 -Cluster <lead>` alternates its waves with fix-lane waves until every
  note naming it has landed. Never hand-write a `groups.json` for a campaign; a note that needs Fable or Mythos is
  held for the owner's per-dispatch approval and dispatched by the attended session.
- **Why.** Orientation was ~46 % of every implementer's tokens (waves 45–57): a successor pays the file's context
  once, not once per cluster. It is a FRESH agent, not a longer transcript, because of the quadratic cost law above:
  the cap exists because the sixth defect in one transcript costs more than a new agent, and the successor keeps the
  cheap part (the learned context) without the expensive part (the long transcript).
- **Args:** `{ scratch, wave, concurrency (6), train_size (5), min_final_train (3), implementer_model ('opus'; 'sonnet' when the owner allows it; a group's own `model` overrides it, so one wave can mix Sonnet and Opus groups), devlog_n, previous_train,
  lead_id_blocks: ["PBa-PBb", …] (one block per train), groups: [{ letter, lead, notes, codes, after? }] }`.
- **Return payloads are capped** (kb/Work PB1912, owner 2026-10-02): an implementer's `summary` is at most 900 characters,
  `leads` at most 6 of at most 500, and the lander's final text at most 25 lines, because everything an agent returns is
  re-read by the orchestrator on every later turn. The detail lives in the report file. Experiment pending: compare the
  workflow output size with waves 1001 (31,864 bytes) and 1002 (27,210).
- **Specs.** Render them with `make_dispatch_specs.py <groups.json>`, which takes the same groups plus `slug`,
  `group`, `root`, `files`, `body`, an optional `pred` text (predecessor or resume instructions) and the optional
  `after` (ignored by the renderer; the workflow uses it). Spec files are keyed on the full letter:
  `msg-w<wave>-<letter lower-cased>.txt`. `check_practices.py <groups.json>` refuses two groups whose `root` names
  the same primary `src/` file unless one declares `after:` the other.

⭐ **COMMAND CHAINING — MANDATORY-PRACTICES P14, enforced by the guard hook.** Never chain anything after a verdict
command (build, test, gate, push-main, battery); capture its status with `; echo "EXIT=$?"` if needed. Independent
commands go as PARALLEL tool calls in ONE turn. A blanket no-chaining ban (carlymr/carlys-claude-skills) was
considered and rejected in that form: split chains become extra turns, and turns are the quadratic cost.

## 3. Landing order and mechanics

- Land finished work first; one lander on main at a time; five clusters per landing (4-6 is the band) and never a lander for a 1-2 cluster landing unless it is a blocking fix; one build, the WHOLE population in one leg, one commit per cluster, one DEVLOG entry, one push.
- The only way a commit reaches `main` is `bash scripts/push-main.sh` (the `ci-gate` check is required; the script is idempotent; a red is a blocking finding). A docs-only landing still runs and still has to be green.
- Verdict batches are re-applied on the merged tree with `record_verdicts.py`, never merged as JSON hunks.
- A checkpoint file never enters a landing: `git add -A -- . ":!.claude/settings.local.json"` then `git reset -q -- STATUS.md`.
- `git stash` is forbidden in this repo; WIP goes into a commit.
- The GnuCOBOL corpus is per worktree; the two population reds are real unless the verdict line says `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED`.
- Read-only fleets probe a pinned worktree with its own built compiler; the comprehensive battery runs in a worktree cut at the batch head, never on main.
- The ledger artifact is refreshed after every landing that moves GAP / DOCUMENTED-NON-SUPPORT / the actionable count and at every battery close; `scripts/semgrep/verify.py` must not increase its red baseline.

Read `references/landing.md` before dispatching a lander, landing, pushing, running a battery or refreshing the ledger.

## 4. Central allocation

The orchestrator allocates `kb/Work` ids and diagnostic-code ranges in the dispatch brief; agents never pick their own
(five collisions in one day each cost a renumbering pass). Every allocation goes through ONE locked allocator,
`python scripts/orchestrator/alloc.py pb N | code N | devlog`, which keeps the reservations of in-flight worktrees
outside every worktree, so two sessions never hand out the same value (`peek <kind>` reads without reserving;
`scripts/orchestrator/plan_wave.py` allocates a wave's codes and lead-id blocks through it). DEVLOG numbers are read
from the file at landing time (top entry + 1): a lander takes top + 1 whatever `alloc.py devlog` reserved, so re-read
the top before committing an entry that waited for a rebase (entry 1880 was renumbered after train 1018b took 1879). `session-probe` prints `alloc.py peek code --probe`: the next free
diagnostic code above every code in `src/`, the catalog and the reservations.

## 5. On restart after a cutoff

The base's §12 "On restart after a cutoff" applies as written: its `status_delta.py` is
`tools/claude-skills/skills/agent-fleet/references/status_delta.py <worktree>`, and `.agent-fleet.json` already
leaves `.claude/settings.local.json` out of its uncommitted list. Under the orchestrator loop a restart is automatic:
the supervisor turns a surviving `checkpoint.json` into the dead unit's handoff, and the first unit is a `resume` that
reads it (the first real `resume` unit recovered six interrupted worktrees in 141 s). Read a dead unit's `.stderr.txt`
before its stream: it names the real cause (the 600 s background ceiling).

## 6. Templates (substitute the session's paths for `{SCRATCH}`, `{PIN}`)

`templates/` beside this file: `implementer-brief.md` · `lander-brief.md` (ONE cluster) ·
**`lander-train-brief.md` (the five-cluster train — the default when more than two clusters are ready)** ·
`golden-lander-brief.md` · `registrar-brief.md` · `wf_lane3_adjudicate.js` · `wf_lane3_refute.js` ·
`wf_lane1_draft.js` · `wf_lane1_validate.js` · `merge_batch.py` · `build_lane1_inputs.py`. Each carries the
checkpoint protocol and its turn cap already; dispatch by pointing the agent at the file plus the substitutions,
never by pasting the brief into the prompt.
