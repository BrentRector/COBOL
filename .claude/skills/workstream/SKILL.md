---
name: workstream
description: Use BEFORE dispatching any fleet, lander, implementer or adjudication workflow - the owner's standing instructions (2026-09-02) for running workstreams so a session-limit kill costs at most one step and a restart never repeats work - checkpoint to disk, fresh agents from checkpoints, a hard concurrency budget, finished work landed first, central id allocation. Carries the brief and workflow templates.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:agent-fleet` (Skill tool) before reading on. If the plugin is not loaded
> (a cloud session receives no project marketplace), Read `tools/claude-skills/skills/agent-fleet/SKILL.md` instead
> (`git submodule update --init tools/claude-skills` if the path is missing). THEN apply this overlay: it carries only
> what is specific to WiseOwl COBOL — commands, paths, kb/Work, push-main, the owner's dated decisions and
> measurements — and wins on conflict. Pinned: **brent-tools 1.14.1** (`tools/claude-skills`, kb/Work/PB1699). A
> practice improved here is written into the public base first, then consumed by moving the pin.

# Workstream — token-frugal, restart-safe orchestration

> ⛔ **THE MANDATORY PRACTICES ARE IN ONE FILE — `templates/MANDATORY-PRACTICES.md` — AND ARE ENFORCED.** Owner
> 2026-09-23: "All these best practices must be durably recorded for all future to must use." Every brief points at
> it; implementer/finisher dispatch specs are RENDERED by `make_dispatch_specs.py <groups.json>` from
> `templates/dispatch-spec-implementer.md` (never hand-written in a scratchpad); `check_practices.py` must print
> `=== PRACTICES CHECK: GREEN ===` over the briefs and over every rendered spec before a Workflow call. A new
> practice is added THERE, with its reason and measurement, and to `check_practices.py` — nowhere else.

> ⛔ **Owner standing instruction (2026-09-02).** Twenty-eight concurrent Opus agents burned ~20% of a session window
> in eleven minutes; ~fifty exhausted a window in ~2.5 h; two cutoffs in one day (the reset hour is NOT fixed — read it
> off the 429). A RESUMED long transcript re-reads its whole context on every turn, so resuming eight 300-turn
> implementers was the most expensive thing run all day, and un-checkpointed refuters lost every rule they had decided.
> "Run all this work so that we don't repeat effort when hitting a limit and resuming."

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

| job | checkpoint | resume unit |
|---|---|---|
| implementer (worktree) | `git commit -m "WIP checkpoint: …"` on the worktree branch **after every mechanism and every gate**, plus `<worktree>/STATUS.md` with `DONE` (per mechanism) · `NEXT` (the exact next step) · `BLOCKED` · `GATE` (last verdict lines + filter) · batch-file path · codes used | one mechanism |
| workflow stage (adjudicate / refute / write / validate) | one JSON line per rule (or draft) appended to `<out>/<stage>-<slug>.jsonl` the moment it is decided; read-and-skip on start; the stage's whole result also written to `<out>/out-<slug>.json` | one rule |
| lander | a commit in ITS worktree after each numbered step + `STATUS.md` | one step |
| orchestrator | reports are FILES under the scratchpad (`reports/<cluster>-report.md`), briefs are FILES referenced by path; the conversation carries only pointers | — |

⭐ **GRACEFUL STOP — the quota-suspend signal (owner 2026-09-22: "attempt to not lose work due to a quota kill").**
Workflow agents cannot be messaged, so every dispatch prompt carries this line: *"Before starting each new step,
check for the file `{SCRATCH}\STOP`; if it exists, checkpoint-commit, write STATUS.md NEXT, and return your
structured result with status SPLIT."* At ~80–85 % of the SESSION or ~90 % of the WEEKLY meter the orchestrator
creates `STOP`, waits for the returns, and only then `TaskStop`s stragglers and WIP-commits their worktrees — so a
quota kill never lands mid-step. Delete `STOP` before resuming.

⛔ **A WORKFLOW AGENT NEVER ENDS ITS TURN WHILE A BACKGROUND JOB RUNS.** Measured 2026-09-22 (wave 45): an agent told
"run the gate in the background and wait for the notification" ENDS its turn, is RETURNED by the harness, and its
background gate is KILLED — five implementers and the train-46 lander all came back "gate PENDING" with logs that stop
mid-leg. Every dispatch prompt says: start the job in the background to a log, then BLOCK in the foreground on
`timeout 580 bash -c 'until grep -qE "<verdict pattern>" <log>; do sleep 5; done'`, re-issued until the verdict prints (for
push-main, append `echo "PUSH-MAIN-EXIT=$?"` to its log and block on that).

**A killed agent is replaced by a FRESH agent that reads the checkpoint** (`STATUS.md` + `git log`, or the `.jsonl`).
⭐ **EVERY FLEET-OPTIMIZATION EXPERIMENT IS RECORDED (owner 2026-09-28, for a later research report).** When an
experiment, measurement or design attempt on the fleet ends, write `docs/rearchitecture/evidence/fleet-optimization/
YYYY-MM-DD-<slug>.{md,json}` in the format of that directory's README. The `.md` holds the question, hypothesis,
design, every attempt, result, limits and decision. The `.json` holds the raw per-agent data from
`scripts/telemetry/workflow_metrics.py`, because transcripts are pruned. Failures and null results are recorded too.
⭐ **The handoff is STAMPED (kb/Work/PB1698, 2026-09-28).** STATUS.md's first line is `STATUS-AT: <sha>`, which names
the commit it describes and is written after that commit. A resumer or same-file successor runs
`python tools/claude-skills/skills/agent-fleet/references/status_delta.py <worktree>`:
- CURRENT: the summary covers the whole branch.
- STALE: read only the commits it lists.
- UNSTAMPED or DIVERGED: read them all.
The stamp makes a stale summary detectable without re-reading the branch; the summary stays navigation, never evidence.
Resume via `SendMessage` only when the agent is within a step of finishing. A workflow resumes with `resumeFromRunId`,
but design its stages to read inputs from disk (`out-<slug>.json`) so a rewritten script never re-runs completed work.

## 2. The cost law, the turn caps, and the concurrency budget

⛔ **The reason is measured, not anecdotal.** An agent's token cost is QUADRATIC in its turn count:

> **`tokens ≈ 0.115·T + 0.00031·T²`**  — fitted over **n = 239 agents**, refitted independently to
> `0.1154·T + 0.000309·T²`. ⓜ **18 of those 239 ran ≥ 250 turns and burned 39 % of all 5.62 B tokens**; re-modelled
> at a 150-turn cap the same turns cost **45 % less**.
> (`docs/rearchitecture/evidence/PROCESS-REVIEW-2026-09-04.md`; the decisions on it are `kb/Work/PB468`.)

Everything below follows from that curve. A long transcript is not "a bit more expensive" — it is the single
largest line item in the burn-down.

**Hard turn caps (owner decision, 2026-09-04).**

| agent | cap | at the cap |
|---|---|---|
| any read-only agent — adjudicator, refuter, validator, probe, reviewer | **160 turns** | checkpoint, return what is decided, name what is not |
| implementer | **220 turns** | checkpoint, write `STATUS.md` `NEXT`, return a report headed `SPLIT` |

⛔ **A job that will not fit is SPLIT, never extended.** A fresh agent starting from the checkpoint is on the flat
part of the curve; the killed one's continuation is on the steep part.

**⭐ Four speed levers adopted by the owner on 2026-09-13** (asked "can we fix many known defects then run a comprehensive test?"): (1) the registrar CLUSTERS leads by root cause into one note per mechanism before any dispatch; (2) an implementer's gate is ITS OWN tests + `~Drift|~EditionGate` + the Unit assembly — the corpus and NIST legs run at the LANDER over the whole train (the train IS the "fix many, test once" step; CI and one battery per train stay); (3) ONE positive golden at the introducing edition + ONE negative below it, a copy per edition only where behaviour differs; (4) the report follows `templates/implementer-report-template.md`, ≤ 60 lines. Measured basis: a fix's gate was ~10 of its ~70 minutes; the transcript, not the tests, is the cost.

**One mechanism per implementer.** ⓜ A mechanism costs ~150 turns and an implementer's fixed cost is only ~70, so
the quadratic eats the amortization immediately: the **second mechanism in one transcript costs 44.6 M, more than
a whole fresh implementer at 40.3 M**. Two only when the notes share one code site *and* one root cause — in
which case the register's own clustering rule says they were one mechanism to begin with.

**⭐ Group related fixes — owner decision 2026-09-13 ("adopt it").** The FILL UNIT is a GROUP: the harm-ranked note plus every open note that shares its named source files or its rule family (the registrar's root-cause cluster, or a grep of the notes' code sites) — three or four notes, one implementer, one branch, one gate, one report with a section per note; the mechanism rule above still governs the WORK inside the group (each note fixed at its root, checkpoint per mechanism, SPLIT at a note boundary at the turn cap). Evidence: trains 37/38 re-merged PB416/PB391 (MOVE validity), PB419/PB420/PB425 (INITIALIZE and MOVE emitters) and PB443/PB877 (the subscript resolver) pairwise, two composition defects appeared that neither implementer could see, and half the reports folded a sibling note. A group touching a shared seam runs `~CorpusRunner` + `~Nist` at the implementer's gate (the PB425 drop).
⭐ **THE GROUPS ARE COMPUTED, NOT EYEBALLED — owner 2026-09-27 ("group fixes so all fixes in one source file, or one
small set of related code, all get fixed in one pass").** `python tools/claude-skills/skills/agent-fleet/references/fix_clusters.py [--json out.json]`
resolves every open defect's named code sites to real `src/Cobol.Net.*` files, gives each note a PRIMARY file, and
clusters notes by it (cap 5, split by harm, singletons absorbed into a cluster whose file they also name), ranked by
summed harm. **Fill each slot with the top-ranked cluster of a subsystem not already in flight**, never a hand-picked
lead + keyword siblings. Measured on its first run: 411 open actionable defects → 125 clusters (42 of five notes), so
a six-slot wave carries ~25–30 defects instead of ~10. Wave 65, grouped by hand, split `ConditionRenderer.cs`'s five
defects across two implementers (A and E) and left three of `ReferenceFormatProcessor.cs`'s five behind.

⭐ **ORIENTATION IS PAID ONCE, NOT ONCE PER WAVE — owner 2026-09-27 ("over many waves, more of the same orientation
… can we reduce the repeated work?").** Every dispatch spec tells the implementer to run `python tools/claude-skills/skills/agent-fleet/references/orient.py
<the files its notes name>` BEFORE reading any source (MANDATORY-PRACTICES P6; `check_practices.py` refuses a spec
without it). It prints each file's outline with line numbers, cited clauses, covering tests, what LANDED notes learned
about the file (their code sites and mechanisms, newest first), the open notes naming it, and recent commits. It is
derived on each run and never goes stale. Measured baseline (wave 65, 7 transcripts, 1,401 turns): 63 % of tool calls
were reads/searches, 15 % of turns and 43 % of tool-result bytes came before the first edit. Estimate: ~25 turns saved
per implementer (≈ 200 → 175), about **15 % of its tokens** on the cost law, about one weekly point per six-slot wave.
Verify on wave 67 with the same measurement (turns to first edit, read calls, pre-edit bytes).

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
- **Why.** Orientation was ~46 % of every implementer's tokens (waves 45–57): a successor pays the file's context
  once, not once per cluster. It is a FRESH agent, not a longer transcript, because of the quadratic cost law above:
  the cap exists because the sixth defect in one transcript costs more than a new agent, and the successor keeps the
  cheap part (the learned context) without the expensive part (the long transcript).
- **Args:** `{ scratch, wave, concurrency (6), train_size (5), min_final_train (3), devlog_n, previous_train,
  lead_id_blocks: ["PBa-PBb", …] (one block per train), groups: [{ letter, lead, notes, codes, after? }] }`.
- **Specs.** Render them with `make_dispatch_specs.py <groups.json>`, which takes the same groups plus `slug`,
  `group`, `root`, `files`, `body`, an optional `pred` text (predecessor or resume instructions) and the optional
  `after` (ignored by the renderer; the workflow uses it). Spec files are keyed on the full letter:
  `msg-w<wave>-<letter lower-cased>.txt`. `check_practices.py <groups.json>` refuses two groups whose `root` names
  the same primary `src/` file unless one declares `after:` the other.

⭐ **COMMAND CHAINING — MANDATORY-PRACTICES P14, enforced by the guard hook.** Never chain anything after a verdict
command (build, test, gate, push-main, battery); capture its status with `; echo "EXIT=$?"` if needed. Independent
commands go as PARALLEL tool calls in ONE turn. A blanket no-chaining ban (carlymr/carlys-claude-skills) was
considered and rejected in that form: split chains become extra turns, and turns are the quadratic cost.

**⭐ Lander throughput — owner decision 2026-09-22 ("do all that we can").** The lander is the serial bottleneck: train
47's lander took 84 min, and its whole-Conformance leg measured 9.6 min on a quiet host, 18.5 min at battery #84 and
30.6 min in train 48, because up to seventeen implementers were running the SAME whole assembly on the same 32 cores.
(1) Every implementer gate is `build-local.ps1 -Mode implementer`, the ORDERED whole population (owner, 2026-09-28,
kb/Work PB1708: "order, don't skip"), and it holds a GATE SLOT (`scripts/gate_slot.py`, kb/Work PB1720): at most N
implementer gates build or test at once, repository-wide, while the lander (`-Mode lander`) never takes one and never
waits. (2) Every implementer gate runs at `-Priority BelowNormal`, which Windows passes down to the build, the test
hosts and every compiled program, so the lander's Normal-priority gate wins the cores. (3) PIPELINED
LANDING: the next train's lander is dispatched while the previous one is still in CI; it merges and gates on the
current origin/main, and before `push-main.sh` it BLOCKS until the previous train's head is an ancestor of
origin/main, rebases, and re-gates only when the rebase touched code outside docs/kb/DEVLOG. CI remains the proof.
(4) Trains stay at 4–6 clusters. (5) The Conformance runner's compiled-program cache (kb/Work PB985) makes a
re-gate after a test-only fix skip recompilation.

**One landing per lander transcript.** ⓜ lander-3 carried 4 landings in 794 turns for **295.9 M**; the same turns
split into four fresh ~198-turn landers model at **140 M — 53 % less**. (What a lander may batch is *clusters
inside one landing* — §3.)

**The budget: 1 lander + ≤6 implementers + one 4-wide read-only chunk (≈11 agents).** ⭐ The implementer cap was
**raised from 3 to 6 by owner decision on 2026-09-05** (`kb/Work/PB468` Q4), on the measurement that the 3-slot lane ran
at ~100 % utilization all morning while the lander sat idle half the time (≈48 min per implementer, ≈33 min per
four-cluster landing); six implementers saturate one lander with five- or six-cluster trains, which is the band above.
⛔ **Fill the six slots ONE GROUP PER SUBSYSTEM** (a group per the paragraph above; formerly one item) (the top of `work.py next` for each of binding, codegen, frontend/grammar,
runtime, io …), never six items down the rank list: twelve consecutive file-I/O clusters on 2026-09-05 forced serial
branches, a six-conflict mid-flight merge (~70 turns) and measured-overlap manifests for every train. It is an
error-containment cap, and ⓜ it is TOKENS, not slots, that the weekly limit rations — so the budget is a default,
while the caps above are what actually control spend. Workflows take `concurrency`/`refuteConcurrency` args and run
chunks with `parallel()` inside a loop, never a 16-wide `pipeline`. A fleet over ~40 agents is the signal to SPLIT
the work, not to wait. Stage the earliest-stage, largest jobs behind the near-done ones.

## 3. Landing order and mechanics

- **Land finished work first**; read-only fleets are staged behind landings. **One lander on main at a time** (DEVLOG
  numbering and fast-forward ordering); **one LANDING per lander transcript** (§2) — split the queue into fresh agents.
- ⭐ **FIVE CLUSTERS PER LANDING (target 5; 4–6 is the band).** A landing is ~90 % fixed cost — bring the work in,
  build, gate, DEVLOG, commit, push — so ⓜ **10.4 M per cluster at k = 1 against 5.1 M at k = 5**, and 4.0 minutes
  of lander per cluster against 9.8. The corpus proves it directly: the golden lander landed 151 rows for 52.9 M =
  0.35 M/row; the PB383 lander landed 2 rows for 7.2 M = 3.6 M/row — **10×**. Mechanics: bring in each implementer's
  diff in turn, **one build**, the **WHOLE population of Conformance, Unit and Characterization in one leg** (`build-local.ps1 -Mode lander`; a union of the implementers' filter terms was never a landing gate — CI's `rest` shard is exactly the tests no term names, and it was red on the first push of trains 39, 40 and 41 in one day), **one commit per cluster inside the landing** so
  a red bisects by cluster, one DEVLOG entry naming every cluster, one push. Past six clusters the token curve is
  flat and the gate-attribution risk is not. Template: `templates/lander-train-brief.md`.
- ⛔ **Never spend a lander on a 1–2 cluster landing** unless nothing else is ready — that is the k = 1 corner of the
  table above, at twice the cost per cluster. If only one cluster is finished, hold it and dispatch the lander when
  the train is full; a blocking fix is the exception and is landed alone on purpose, said so in the report.
- ⭐ **A free implementer slot is filled within the TURN it frees** — mechanically, by the rolling wave (§2,
  `templates/wf_rolling_wave.js`), whose queue is filled from `fix_clusters.py` — never at the next convenient moment. ⓜ The fix lane ran at **under 15 % utilization** of the ≤3 cap for six days
  (~2.7 mechanisms/day delivered against ~24/day of capacity) purely because slots sat empty behind landings and
  behind the evidence lane. Keep a standing queue of apply-ready contracts so the dispatch is one turn.
- A worktree-isolated agent cannot run git against the shared checkout (the harness refuses `-C`, `cd`, EnterWorktree):
  landers gate in THEIR worktree, then `git fetch origin && git rebase origin/main` and land with
  **`bash scripts/push-main.sh`**; the orchestrator runs `git merge --ff-only origin/main` locally and removes dead
  worktrees itself (`git worktree remove --force`, `git branch -D`).
- ⛔ **THE ONLY WAY A COMMIT REACHES `main` IS `bash scripts/push-main.sh`, AND THE SERVER ENFORCES IT.** `main`
  carries a REQUIRED status check — `ci-gate`, the terminal job of the workflow — with `enforce_admins: true`, so a
  bare `git push origin HEAD:main` of an unverified commit is REFUSED. Admin exemption was never an option: every
  push here is made with the owner's credentials, so a rule that spares administrators spares everyone, and the
  orchestrator's own pushes obey this one (owner decision 2026-09-06, question 23). The script pushes HEAD to
  `ci/<short-sha>`, watches the run for that exact sha, and only then fast-forwards main and deletes the branch; on
  a red it prints the failing jobs and exits non-zero with main untouched.
  ⭐ It is IDEMPOTENT — a full-matrix run is ~25–30 min, longer than a command timeout, so a cut-off caller simply
  re-runs it (or runs it with `run_in_background`); a sha that already carries a green `ci-gate` skips straight to
  the main push. A red is still a BLOCKING finding, attributed by job / step / failing test in the report, and the
  orchestrator lands that fix alone before the next train. **The local battery runs on ONE host; the CI's Linux job
  is the only Linux gate — a test that depends on host ACL or file-lock semantics is not proven until CI is green.**
  ⓜ main was red for 29 h across 16 consecutive completed runs (2026-09-05/06) while every landing in that window
  — trains 10 through 20 — reported a green Windows-only gate and nobody looked (`kb/Work/PB796`).
- ⚠ **A docs-only landing still runs — and still has to be green.** `paths-ignore` is GONE from the workflow (it
  made the workflow decline to START, which a required per-commit check reads as "blocked forever"); the `changes`
  job now decides whether the MATRIX runs from the same path list, so a landing touching only `DEVLOG.md`,
  `docs/**`, `kb/**`, `PROMPT.md`, `CLAUDE.md` or `README.md` finishes in seconds. "No run for my sha" is no longer
  an answer — it is a failure, and `push-main.sh` treats it as one.
- Verdict batches are RE-APPLIED on the merged tree (`record_verdicts.py`), never merged as inventory JSON hunks.
- ⛔ A checkpoint file never enters a landing: every landing `git add` excludes it —
  `git add -A -- . ":!.claude/settings.local.json"` then `git reset -q -- STATUS.md`; ⛔ **not** `":!STATUS.md"`
  inside the pathspec, because STATUS.md is gitignored and `git add` EXITS 1 AND STAGES NOTHING when a pathspec
  item matches only an ignored path (measured 2026-09-06, git 2.55.0) — the form written here until now skipped
  the whole checkpoint for anyone who did not read its exit code. A lander that rebases onto a main that
  already carries a stray `STATUS.md` removes it (`git rm --cached STATUS.md`) in its commit. (2026-09-02: lander 3's
  checkpoint reached main inside a landing, and the next lander's rebase then had to choose between two agents'
  checkpoints for one path.)
- ⛔ **`git stash` IS FORBIDDEN IN THIS REPO — WIP GOES INTO A COMMIT.** The stash stack lives in the COMMON git
  directory (`git rev-parse --git-common-dir` → `E:\COBOL\.git`), so it is SHARED by every linked worktree:
  `git stash list` run from an isolated implementer worktree shows the other agents' entries and `stash pop` takes
  `stash@{0}` whoever pushed it. PB713's implementer ran `… && git stash pop -q` after a `git stash push` that had
  created nothing (its file was untracked), so the pop consumed the REGISTRAR's stash; it was restored with
  `git stash store`, and DEVLOG Entry 155 records the same shape ending worse ("Never `git stash` while background
  agents are writing files … Lost all 7 completed agents' work"). Commit WIP on your own worktree branch — that is
  what the checkpoint protocol is for — and to compare against a clean tree add a detached worktree
  (`git worktree add --detach <path> <sha>`), never stash/build/probe/pop.
- The GPL GnuCOBOL corpus (`tests/external/gnucobol`) is git-ignored and **per worktree**. The gate (`scripts/run_gate_legs.py`) fetches it
  when absent (train 23), so a fresh implementer or lander worktree measures the population from its first gate;
  `ExternalCorpusPopulationDriftTests` is RED BY DESIGN without it (`kb/Work/PB209`). ⛔ **The two population reds are NOT
  "the known worktree shape" any more** (`kb/Work/PB897`): the fetch was broken on every host whose PATH `tar` is GNU tar —
  it deleted the corpus and then failed to extract — so those reds were permanent and everyone was told to ignore them.
  The fetch now stages and swaps, tries each `tar` it can find, and prints one `FETCH FAILED: <cause>` line; a failed fetch
  makes the gate RED with `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED` on the verdict line. **Without that line
  the population reds are a real red** — attribute them, never wave them through.
- Read-only fleets probe a **pinned worktree with its own built compiler** (`git worktree add --detach <path> <sha>`,
  build there once) so no landing swaps a binary under them.
- One comprehensive battery per landing batch, run by the orchestrator when no fleet is live — ⭐ **in a WORKTREE cut
  at the batch head**, never on main. Its phase 2 (`guard-fast`) REBUILDS, so a battery on main freezes the lander
  for ⓜ ~45 min ≈ 1.5 landings ≈ 7 clusters; run it in a detached worktree at the batch's head commit and that cost
  is zero. The cadence does not change — ⓜ a bisect over N landings costs ⌈log₂N⌉ battery runs, and neither of the
  last two non-green batteries needed one (both reds were attributed by inspection).
- **The owner's ledger artifact is refreshed after EVERY landing that moves GAP / DOCUMENTED-NON-SUPPORT / the
  actionable count, and at every battery close** (owner reminder 2026-09-02): `python scripts/spec/gen_ledger.py
  --out <html> --in-flight <md>` then an `Artifact` publish to the existing URL (recorded in the orchestrator's memory
  `conformance-ledger-artifact`). Numbers are computed by the generator, never typed; only the in-flight narrative is
  hand-written.
- `python scripts/semgrep/verify.py` has a recorded red baseline (`kb/Work/PB175`): a landing must not INCREASE it.

## 4. Central allocation

The orchestrator allocates `kb/Work` ids and diagnostic-code ranges in the dispatch brief; agents never pick their own
(five collisions in one day each cost a renumbering pass). DEVLOG numbers are read from the file at landing time
(top entry + 1). `session-probe` reports the next free diagnostic code — it must end above ALL codes claimed by
in-flight worktrees.

## 5. On restart after a cutoff

The base's §12 "On restart after a cutoff" applies as written: its `status_delta.py` is
`tools/claude-skills/skills/agent-fleet/references/status_delta.py <worktree>`, and `.agent-fleet.json` already
leaves `.claude/settings.local.json` out of its uncommitted list.

## 6. Templates (substitute the session's paths for `{SCRATCH}`, `{PIN}`)

`templates/` beside this file: `implementer-brief.md` · `lander-brief.md` (ONE cluster) ·
**`lander-train-brief.md` (the five-cluster train — the default when more than two clusters are ready)** ·
`golden-lander-brief.md` · `registrar-brief.md` · `wf_lane3_adjudicate.js` · `wf_lane3_refute.js` ·
`wf_lane1_draft.js` · `wf_lane1_validate.js` · `merge_batch.py` · `build_lane1_inputs.py`. Each carries the
checkpoint protocol and its turn cap already; dispatch by pointing the agent at the file plus the substitutions,
never by pasting the brief into the prompt.
