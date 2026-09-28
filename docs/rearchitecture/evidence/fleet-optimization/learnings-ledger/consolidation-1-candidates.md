# Learnings: running Claude agents on a long engineering campaign

This is the research record behind the skills in this repository. From March to September 2026 a large compiler
project was built mostly by Claude agents: first by a single agent session, and later by fleets of dozens of parallel
subagents (implementers, landers, adjudicators, refuters, registrars) run by an orchestrating session. Almost every
rule in [`agent-fleet`][af], [`automating-agent-guardrails`][ag], [`test-gate`][tg], [`spec-oracle`][so],
[`spec-compliance-audit`][sca], [`review`][rv] and [`engineering-standards`][es] began as one of the incidents below.

Each learning gives:

- the **problem** (what was observed, or what it cost)
- the **root cause** (the mechanism as it was established at the time)
- the **fix** and where this repository encodes it
- the **evidence** (measurements with n and dates, or "unmeasured")
- **when** it was learned.

Learnings are grouped by theme, oldest first within each theme. Failures, reversals, rejected ideas and null results
are kept on purpose. Where a later measurement overturned an earlier claim, both appear, and the correction is labeled.
Two lists close the document: the open problems (measured but not solved), and the lessons the project acted on that
the public skills do not carry yet.

A few terms used throughout:

- A **wave** is a batch of implementer agents dispatched together.
- A **train** is one lander's merge-gate-push of several finished branches.
- A **cluster** or **group** is the set of related defects one implementer fixes.
- **GAP** is the project's count of specification rules not yet shown to be met.
- "Tokens" includes cache reads unless stated. Cache reads were ~97.6 % of all tokens, and they are what the plan's
  usage limits consume.

**Contents:**
[Cost and transcript length](#cost-and-transcript-length) ·
[Orientation and handoffs](#orientation-and-handoffs) ·
[Checkpointing, restarts, session and quota limits](#checkpointing-restarts-session-and-quota-limits) ·
[Concurrency and landing trains](#concurrency-and-landing-trains) ·
[Gates and verification](#gates-and-verification) ·
[Guardrails](#guardrails) ·
[Spec as oracle and citation discipline](#spec-as-oracle-and-citation-discipline) ·
[Adversarial review and refuters](#adversarial-review-and-refuters) ·
[Work register and process hygiene](#work-register-and-process-hygiene) ·
[Orchestration mechanics](#orchestration-mechanics) ·
[Model per role](#model-per-role) ·
[Measurement discipline](#measurement-discipline) ·
[Open problems](#open-problems) ·
[Not yet in the skills](#not-yet-in-the-skills)

---

## Cost and transcript length

### Don't dispatch a fleet to confirm what an inline probe already measured
- **Problem:** ten agents were sent to measure eight defects. The fleet grew to about 60 agents over about 2 hours,
  with about 6 solution builds happening under it. Every number that shipped came from the orchestrator's own inline
  run and none came from the fleet.
- **Root cause:** the orchestrator had already measured all eight with a shell loop (three tool calls). It then treated
  "confirm it in parallel" as a reason to fan out, and read a standing preference for workflows as "one fleet per task".
- **Fix in the skills:** [agent-fleet §9][af] "Measure inline before you fan out."
- **Evidence:** 10 → ~60 agents, ~1.75–2 h, 0 shipped numbers from the fleet.
- **When:** 2026-08-04.

### Discovery, not fixing, is where read-only agents spend their turns
- **Problem:** spec-adjudication agents averaged ~120 turns each, most of them locating the grammar rule, binder,
  emitter and tests for their subject. Two agents working on the same subject paid for the same search separately.
- **Root cause:** each agent re-derived facts that have exactly one answer per subject.
- **Fix in the skills:** [agent-fleet §11][af] "Precompute discovery": build a dossier once and hand it to every agent.
  The dossier is a map, and its silence is not evidence: a "not implemented" verdict still needs the agent's own
  recorded search ([spec-compliance-audit §5][sca]).
- **Evidence:** one batch of 39 agents used 574 M cache-read tokens for 224 rules (~2.6 M per rule).
- **When:** 2026-09-01 to 2026-09-02.

### Large review fleets bought almost no progress, and part of what they found they had caused
- **Problem:** thirteen review fleets of 52–117 agents (about 1,000 agents, ~4 B cache-read tokens) moved the GAP by
  27 rows. Five of the last fleet's eleven filed defects had been introduced, widened or wrongly certified by the same
  wave.
- **Root cause:** width without self-review. Also, 92 % of the open rows had never been adjudicated, so review was not
  the binding constraint.
- **Fix in the skills:** [agent-fleet §11][af] "Self-review before a large review fleet" and "route each item to the
  cheapest lane that moves the metric you actually report". The implementer's self-review checklist uses the
  [`agents/`](agents/) reviewers.
- **Evidence:** as above (2026-08-29 to 08-31).
- **When:** 2026-09-01.

### Agent cost is quadratic in turns, so cap every transcript and split instead of extending
- **Problem:** a few long agents dominated spend, and weekly limits paused the campaign three times in six days.
- **Root cause:** every turn re-reads the whole context. The fitted law is `tokens ≈ 0.115·T + 0.00031·T²` (M tokens,
  T = turns). An independent refit reproduced it to three significant figures.
- **Fix in the skills:** [agent-fleet §1][af]: hard turn caps per role, and SPLIT at the cap rather than extend. The role
  templates carry `maxTurns` (160 read-only, 220 implementer; [automating-agent-guardrails §2][ag]).
- **Evidence:** n = 239 agents, 5.62 B tokens. The 18 agents that ran 250 or more turns burned 39 % of all tokens. The
  same turns re-modeled under a 150-turn cap cost 45 % less (2.19 B → 1.21 B).
- **When:** 2026-09-04.

### One mechanism per implementer, and one landing per lander transcript
- **Problem:** batching several fixes into one implementer, or several landings into one lander, looked like
  amortization.
- **Root cause:** the quadratic law. A mechanism costs ~150 turns against ~70 of fixed cost, so the second mechanism
  sits on the steep part of the curve.
- **Fix in the skills:** [agent-fleet §7][af] "Don't put two unrelated mechanisms in one transcript"; [§6][af] "one
  landing per lander transcript". (A later refinement, grouping defects that share *files*, is under
  [Concurrency](#group-related-fixes-by-file-and-compute-the-groups).)
- **Evidence:** modeled from the n = 239 law. A second mechanism in one transcript costs 44.6 M tokens, more than a
  whole fresh implementer at 40.3 M. One lander carried 4 landings in 794 turns for 295.9 M tokens, and the same work
  as four fresh landers models at 140 M (53 % less).
- **When:** 2026-09-04.

### The transcript, not the test run, is the cost of a fix
- **Problem:** the owner asked whether gating less often would speed things up.
- **Root cause:** the fix's gate took about 10 of its ~70 minutes and almost no tokens. The transcript was the cost:
  probing, citations, a golden per edition, and a 300-line report.
- **Fix in the skills:** four levers.
  1. Cluster leads by root cause ([agent-fleet §7][af]).
  2. Implementers gate narrowly and landers gate the whole suite ([§6][af]).
  3. A minimal witness set per change.
  4. A report of 60 lines or fewer ([`brief-template.md`][brief]).
- **Evidence:** ~10 of ~70 minutes. The levers' combined effect was not measured separately.
- **When:** 2026-09-13.

### Implementers are the expensive lane; route work to the cheapest lane that moves the metric
- **Problem:** the owner said, "the overhead seems huge for the actual progress we make." The fill order had been
  reversed several times, each order optimizing a different metric (harm, rows, GAP).
- **Root cause (measured):**
  - Across waves 45–57 (3.09 B tokens), implementers and finishers used ~89 % of fleet tokens, landers 8 %,
    registrars 2 % and batteries 0.4 %.
  - Closing a row through the fix lane cost ~13 M tokens, against ~2.6 M through adjudication.
- **Fix in the skills:** [agent-fleet §11][af] "Measure tokens per closed unit for each lane" and "route each item to the
  cheapest lane".
- **Evidence:** above (waves 45–57).
- **When:** 2026-09-23.

### Command chaining: a targeted rule, not a blanket ban
- **Problem:** `test && git commit` committed on false greens, even though the test-gate rule forbade it.
- **Root cause:** a chain's exit status is its last command's. A blanket "never chain" rule, taken from another public
  skill set, was considered and REJECTED: splitting every chain adds turns, and turns are the quadratic cost.
- **Fix in the skills:** [agent-fleet §1][af] (never chain after a verdict command; independent commands go as parallel
  tool calls in one turn). The guard rule `no-chain-after-verdict` enforces it ([`guard-rules.json`][rules]).
- **Evidence:** the hook's self-test passed 35/35. The saving from not banning chains is unmeasured.
- **When:** 2026-09-27.

---

## Orientation and handoffs

### Keep state outside the context window
- **Problem:** long sessions drifted, cold starts took long ramp-ups, and compaction lost decisions that were then
  argued again.
- **Root cause:** accumulated context and compaction artifacts.
- **Fix in the skills:** four external layers:
  - a plan that holds current state
  - a dev log that holds reasoning
  - persistent memory for process rules
  - forensic commit messages.

  [engineering-standards §6][es] carries "docs describe the current state; history goes in the log". The four-layer
  pattern itself is not written down as a skill.
- **Evidence:** a hypothesis at the time. A later session summary: "Even with 1M tokens, the four layers of external
  memory were essential."
- **When:** 2026-03-13.

### One self-contained input file per agent
- **Problem:** a fan-out told "read the shared config and process element k" processed four items twice and skipped
  five.
- **Root cause:** agents miscount positional indices into shared input.
- **Fix in the skills:** [agent-fleet §2][af]: each agent gets its own input file, written by a program, and the returns
  are reconciled against the expected worklist.
- **Evidence:** 4 double-processed, 5 skipped, in one run.
- **When:** June 2026.

### Anything a later session needs must live in the repository
- **Problem:** a decision-complete plan lived in an off-repo temp directory and was lost; regenerating one brief cost
  ~362 k tokens. Later, the live-state doc pointed at a batch generator inside a session scratchpad. A cleared session
  gets a new scratchpad, so the instructions pointed at nothing, and a rule that existed only in a transcript was lost.
- **Root cause:** session-scoped or machine-local storage was treated as durable.
- **Fix in the skills:** [agent-fleet §2][af]: the brief is a file an agent re-reads after a restart.
  [claude-cloud-sessions §6][ccs]: a cloud session gets a self-contained brief. The general rule ("durable
  instructions live in the repo") is only implicit.
- **Evidence:** ~362 k tokens for one regenerated brief. A cold start was verified by running it.
- **When:** 2026-06 to 2026-07-30.

### Re-verify a plan's anchors before a multi-wave phase
- **Problem:** a phase plan's audit had drifted on about 5 of 6 re-checked claims (wrong line anchors, gaps already
  closed). Nine scouts each allocated ids from the same "next free" number, colliding in ~40 places.
- **Root cause:** plans are written from reads that go stale, and parallel agents duplicated live state.
- **Fix in the skills:** [agent-fleet §2][af] "Don't put a fact in a brief that you haven't verified in this session";
  [§8][af] central id allocation.
- **Evidence:** ~5 of 6 claims drifted; ~40 colliding sites.
- **When:** 2026-07-17 to 2026-07-19.

### State the bar, not only the output format
- **Problem:** 7 of 19 adjudicated rows cited a test that compared against another implementation (a differential)
  as their evidence, and all 7 passed the validator.
- **Root cause:** "It was my fault, in the prompt and in the schema." The prompt listed the test-reference forms that
  resolve, but never said evidence must be derived from the specification. The prompt and the schema were
  under-specified in the same direction, so the defect arrived pre-validated.
- **Fix in the skills:** [agent-fleet §3][af]; the evidence rules in [spec-compliance-audit §5][sca].
- **Evidence:** 7 of 19.
- **When:** 2026-07-29 to 2026-07-30.

### Every reported lead carries its repro and code site
- **Problem:** the same fact was found three times: by the implementer, again by the registrar filing it, and again by
  the next implementer.
- **Root cause:** leads were reported without a runnable repro or a `file:line`.
- **Fix in the skills:** [agent-fleet §2][af] "Every lead an agent reports carries its repro and code site";
  [`brief-template.md`][brief].
- **Evidence:** re-probing cost ~25 turns per implementer, against 1 turn to rerun a given repro (estimate).
- **When:** 2026-09-04.

### A predecessor's prose can describe work that never happened
- **Problem:** a battery operator killed by a limit left a report naming three commits that exist in no ref, and CI
  runs "stopped" that had in fact completed.
- **Root cause:** the prose was written before the actions it described had completed.
- **Fix in the skills:** [agent-fleet §4][af]: the handoff summary is "navigation, never evidence". A successor
  re-derives every number from the artifacts.
- **Evidence:** a per-case re-diff of the rerun found 0 verdict differences over 1,323 cases.
- **When:** 2026-09-22.

### One-call orientation, derived fresh on every run
- **Problem:** implementers re-surveyed the same files every wave.
- **Root cause (measured):**
  - Waves 45–57: codebase search was 31.7 % and reading 14.5 % of implementer tokens (46 % together).
  - Wave 65 (7 transcripts, 1,401 turns): 63 % of tool calls were reads or searches, and 15 % of turns and 43 % of
    tool-result bytes came before the first edit.
- **Fix in the skills:** [`orient.py`][orient] ([agent-fleet §2][af]). It prints an outline, cited clauses, covering
  tests, what earlier landed fixes learned about the file, the open items, and recent commits. It is derived from the
  tree on every run, so it cannot go stale. Pre-generated "codebase maps" were rejected, because they cost a fleet to
  write and go stale at the next landing.
- **Evidence:** one ~1,100-line file with 78 notes printed in 86 lines, in 0.3 s. The ~15 % token saving is an
  ESTIMATE, never verified ([Open problems](#open-problems)). Eval: 1.00 with the skill vs 0.00 without.
- **When:** 2026-09-23 (measured), 2026-09-27 (fix).

### Handoff summaries go stale without any crash
- **Problem:** finished agents, not only killed ones, left a `STATUS.md` that did not describe their own last commit.
- **Root cause:** a small final commit (a gate-red fix, a regenerated index) made after the last status rewrite.
- **Fix in the skills:** [agent-fleet §4][af]: rewrite `STATUS.md` after EVERY commit. See the next learning for the
  stamp, and [Guardrails](#a-written-rule-fails-silently-21--of-the-time-enforce-only-the-cheap-arms) for enforcement.
- **Evidence:** 3 of 14 finished real branches (21 %). A first count was 3 of 8, and a second sample 0 of 6.
- **When:** 2026-09-28.

### Stamp the handoff with the commit it describes
- **Problem:** after agents died between a commit and a status rewrite, the safe rule became "re-read every commit".
  That spends the orientation the summary was meant to save.
- **Root cause:** a summary without a commit identity cannot be told stale from current.
- **Fix in the skills:** the first line of `STATUS.md` is `STATUS-AT: <sha>`. A successor runs
  [`status_delta.py`][delta], which prints CURRENT, STALE by N (read only those N commits), or UNSTAMPED/DIVERGED
  ([agent-fleet §4][af]).
- **Evidence:**
  - Pilot, n = 4 per arm on one branch: −31 % turns, −33 % tokens.
  - Scale A/B: 88 fresh resumers, 11 real branches × {STALE, CURRENT} × 2 arms × 2 replicates.
    - Coverage misjudged in 11 of 44 unstamped resumes vs 0 of 44 stamped (Fisher p = 0.0005). 10 of the 11 misses
      were over-reports.
    - Tokens ×0.83 overall (95 % CI 0.68–1.00, p = 0.023) and ×0.69 when the summary was current (0.57–0.84,
      p = 0.010).
    - **CORRECTION:** the pilot's saving in the STALE case did not replicate (×0.94, CI 0.71–1.23, p = 0.58). It came
      from one branch whose uncovered commit was large and obvious.
- **When:** 2026-09-28.

---

## Checkpointing, restarts, session and quota limits

### Session and spend limits kill fleets mid-run
- **Problem:** in a 184-agent review workflow, 99 agents were killed by a session limit, leaving 37 findings
  unverified. The resume was then killed by the monthly spend limit, with 107 of 119 agent calls failing.
- **Root cause:** a long fan-out with no per-agent persistence.
- **Fix in the skills:** [agent-fleet §4][af]: every agent writes its own output the moment an item is decided (one JSON
  line per item), and a merge is safe mid-run. Findings without an adversarial verdict are marked unverified, not
  actionable.
- **Evidence:** 99 of 184 killed; 107 of 119 failed.
- **When:** 2026-07-05 to 2026-07-26.

### The limit kills every live agent at once, landers included
- **Problem:** about 14 concurrent implementers and landers plus a 39-agent workflow hit HTTP 429 after ~2.5 h. One
  lander died after applying its patch but before committing. Finished work was still queued behind a single lander.
- **Root cause:** the cap is per session window, not per agent. The reset time is not a fixed hour (12:20 am and
  4:10 pm PT were both seen).
- **Fix in the skills:** [agent-fleet §6][af] "Land finished work first"; [§12][af] restart procedure (read the reset time
  from the limit message; check `git status` on main because a dead lander may already have applied its patch).
- **Evidence:** two cutoffs, 2026-09-01 and 2026-09-02.
- **When:** 2026-09-01.

### Resuming a long transcript costs more than a fresh agent reading a checkpoint
- **Problem:** 28 concurrent agents used ~20 % of a session window in 11 minutes, and ~50 agents had exhausted the
  previous window in ~2.5 h. Fifteen refuters that wrote nothing to disk until finished lost every decision when the
  window closed.
- **Root cause:** a resumed agent re-reads its whole transcript on every turn, and state lived only in transcripts.
  Resuming eight 300-turn implementers at once was the costliest action of the day.
- **Fix in the skills:** [agent-fleet §4][af] checkpoint to disk after every unit (WIP commit + `STATUS.md`; one JSONL
  line per decided item); [§1][af] resuming is often more expensive than starting fresh; [§12][af] fresh agents from
  checkpoints, in landing order.
- **Evidence:** as above. The eval for "fresh vs resume" was dropped because the baseline already passed.
- **When:** 2026-09-02.

### A workflow run resumes only in the process that ran it
- **Problem:** after a reboot, a suspended workflow could not be resumed by its run id. Earlier, a run could not be
  resumed from another session.
- **Root cause:** a run's cache is bound to the process (and session) that owned it.
- **Fix in the skills:** partial. [agent-fleet §4][af] has stages read their inputs from disk and [§12][af] dispatches
  fresh agents, so nothing is redone. The caveat that run-id resume is same-process only is not written down.
  WORKAROUND used: re-dispatch as a new run; the worktrees and WIP commits survive.
- **Evidence:** 6 worktrees preserved, 0 implementers redone.
- **When:** 2026-09-04; 2026-09-20.

### A continuation after a kill confirms, it never re-applies
- **Problem:** session limits killed landers mid-train three times. One died with six cluster commits made and the gate
  not yet run.
- **Root cause:** long landings outlived the remaining window.
- **Fix in the skills:** [agent-fleet §12][af] step 2 (finish from the NEXT step, never re-apply). The continuation
  lander confirmed every recorded batch by dry run: 7 dry runs, 0 rows changed, and an inventory rebuild that was
  byte-identical.
- **Evidence:** three trains.
- **When:** 2026-09-05 to 2026-09-06.

### Wind down before the limit instead of riding into it
- **Problem:** the third fleet-killing 429 in two days. Each time 7–9 agents died, landers died mid-landing, and each
  restart cost about an hour of assessment.
- **Root cause:** no running usage tally. A fleet-size rule alone is not enough: 9 agents × ~300 k tokens is 2.7 M per
  batch.
- **Fix in the skills:** [agent-fleet §5][af]: keep a running tally; at ~75 % dispatch only finishers; at ~85–90 % stop
  gracefully with a STOP file that every agent checks before each step.
- **Evidence:** ≈ 8–9 M subagent tokens over ~3.5 h exhausted one window (2026-09-06).
- **When:** 2026-09-06 (tally); 2026-09-22 (STOP file).

### A quota estimate from a stale calibration is worse than none
- **Problem:** the orchestrator estimated 57 % of the weekly quota used when the meter read 90 %.
- **Root cause:** a rate calibrated during a temporary usage boost was reused after the boost ended. The rate also
  drifts from week to week.
- **Fix in the skills:** partial. [agent-fleet §5][af] keeps a tally, and [claude-cloud-sessions §7][ccs] has a budget
  watcher that reads the meter. The project's rule, "read the real meter before every dispatch; never plan on an
  extrapolated rate", is not stated in the skills.
- **Evidence:** calibrations of 0.66 M, ~0.11 M, ~0.25 M, ~0.35 M, ~0.4 M and ~0.55 M agent tokens per weekly-meter
  point were recorded between 2026-09-13 and 2026-09-23. The sources disagree because the rate moved.
- **When:** 2026-09-12 onward.

### Pace to the weekly budget, not only the session window
- **Problem:** six implementers per five-hour window cost ~16 % of the weekly limit, which would have used the week's
  quota six days early. Later, a mis-derived daily schedule stopped work with ~12 points unspent.
- **Root cause:** pacing by session window only, then by a wrongly derived per-day cap.
- **Fix in the skills:** not yet. The project uses a cumulative daily allowance (about 1/7 of the week per day), keeps
  trains small to land before a reset, and dispatches against a STOP line.
- **Evidence:** a full six-implementer wave ≈ 9 weekly points ≈ 65 % of one day's allowance.
- **When:** 2026-09-13 to 2026-09-28.

### A graceful STOP file works where messaging cannot
- **Problem:** workflow agents cannot be messaged, and a hard kill lands mid-step. The owner said: "attempt to not lose
  work due to a quota kill".
- **Root cause:** there was no channel into a running workflow agent.
- **Fix in the skills:** [agent-fleet §5][af]; the STOP rule is in [`brief-template.md`][brief].
- **Evidence:** in practice a lander stopped cleanly before its push for a session reset, and two registrars stopped
  with work in WIP commits that finishers then completed.
- **When:** 2026-09-22.

---

## Concurrency and landing trains

### Parallel writers on disjoint files work; worktrees must start from current main
- **Problem:** a 6-agent worktree workflow reported 18 bugs fixed, and the diffs were unusable.
- **Root cause:** the worktrees had branched from a commit about two sessions behind main, and one agent wrote a
  colliding log entry number.
- **Fix in the skills:** partial. [agent-fleet §7][af] gives one group per file or subsystem, and [§8][af] allocates ids
  centrally. "Branch worktrees from current main" is not stated. A later success was six agents on per-file partial
  classes, with test authoring parallelized and correctness fixes integrated one at a time.
- **Evidence:** 18 fixes discarded (May 2026). The later disjoint wave went from 55 to 74 passing programs (+19,
  none lost).
- **When:** May–June 2026.

### Freeze the tree while a fleet or gate reads it, and work through the freeze
- **Problem:**
  - Builds ran while a measurement fleet probed the binary, and a "file locked" build error was written off as
    transient.
  - An edit during a gate's parallel leg produced 2 false regressions.
  - The owner asked "why do you keep stopping?" twice during battery runs.
- **Root cause:** the tree was treated as unfrozen, and then the freeze was over-read as "wait".
- **Fix in the skills:** [agent-fleet §9][af] (frozen tree, the locked-file alarm, drafting the next landing in scratch
  meanwhile); [test-gate][tg] "Never edit source while a gate is running". The written freeze rule was broken
  again at least four times in August, which is why a hook was added; see
  [Guardrails](#a-build-guard-that-guessed-its-way-into-four-self-blocks-and-one-silent-pass).
- **Evidence:** ~6 rebuilds under one fleet; 2 false regressions (351/353 match).
- **When:** 2026-07-11; 2026-08-04; 2026-08-09.

### A concurrency budget, and filling slots one subsystem at a time
- **Problem:**
  - Fleet width drove quota exhaustion, and three finished clusters sat unlanded behind one lander.
  - After a budget of 3 implementers, the lane ran near 100 % while the lander idled about half the time.
  - Filling slots down the rank list sent twelve consecutive file-I/O clusters out together, which forced a
    six-conflict merge.
- **Root cause:** there was no budget; then the cap was set before measuring throughput; and rank order concentrated
  overlapping work.
- **Fix in the skills:** [agent-fleet §5][af] (1 lander + ~3–6 implementers + one ~4-wide read-only chunk); [§7][af] "Fill
  parallel slots with one group per subsystem". The owner raised the cap from 3 to 6 on 2026-09-05.
- **Evidence:** ≈ 48 min per implementer vs ≈ 33 min per four-cluster landing; the six-conflict merge cost ~70 turns.
- **When:** 2026-09-02; 2026-09-05.

### Idle slots, not work, were the bottleneck: fill a slot the turn it frees
- **Problem:** the defect register filled about 10× faster than it drained.
- **Root cause:** implementers were dispatched one cluster at a time, behind landings, behind the evidence lane.
- **Fix in the skills:** [agent-fleet §6][af] "Fill a freed implementer slot in the same turn it frees", later made
  mechanical by the rolling wave (below).
- **Evidence:** fix-lane utilization was under 15 % of the authorized cap (~2.7 mechanisms/day delivered against
  ~24/day of capacity). Two adjudication batches opened 169 notes while ~16 mechanisms closed in six days.
- **When:** 2026-09-04.

### A landing is ~90 % fixed cost: land 4–6 clusters per train
- **Problem:** landing one cluster at a time paid the whole merge, build, gate and push per cluster.
- **Root cause:** fixed cost dominates a landing.
- **Fix in the skills:** [agent-fleet §6][af]: 4–6 clusters per landing, one commit per cluster so a red bisects.
  Beyond ~6, the gate becomes hard to attribute.
- **Evidence:** modeled 10.4 M tokens per cluster at k = 1 vs 5.1 M at k = 5, and 9.8 vs 4.0 lander-minutes per
  cluster. A golden-writing lander closed 151 rows for 52.9 M (0.35 M/row), against 3.6 M/row for a 2-row landing.
- **When:** 2026-09-04.

### Run the comprehensive battery in its own worktree
- **Problem:** the batch battery's second phase rebuilds, and it froze the lander for about 45 minutes.
- **Root cause:** a shared build tree.
- **Fix in the skills:** partial. [agent-fleet §9][af] pins analysis fleets to a detached, read-only worktree. The same
  treatment for the battery is not stated.
- **Evidence:** ~45 min ≈ 7 clusters of landing throughput.
- **When:** 2026-09-04.

### Composition defects appear only on the merged tree
- **Problem:**
  - Two clusters that were each green composed wrongly: one renamed a method, and the other's later batch wrote the
    old name back.
  - A set missed a member that a sibling cluster added.
  - Trains that brought clusters in one at a time found three such seams.
- **Root cause:** each cluster's own gate cannot see its siblings.
- **Fix in the skills:** partial. [agent-fleet §7][af] groups a file's defects in one branch, and [§6][af] has the lander
  gate the whole suite. Merging clusters one at a time with a build between each, and resolving tests that check
  references rather than pattern-match them, are not in the skills.
- **Evidence:** 1 red of 27,772 tests (train 39); 3 seams (train 26).
- **When:** 2026-09-09 to 2026-09-19.

### Never merge structured data textually
- **Problem:**
  - Merging both sides of a JSON conflict produced duplicate keys. The parser keeps the last one, so 96 test cases
    would have silently vanished with every validator green.
  - Another merge dropped one element (123 counted where 124 were due).
  - A union resolution left one entry in a manifest three times. The local gate passed 8,073 tests; CI's population
    guard saw 8,073 executed of 8,074 discovered.
- **Root cause:** JSON accepts duplicate keys silently, and a test lister counts duplicates while execution does not.
- **Fix in the skills:** not yet. The project merges manifests as sets by script, with element-count checks,
  regenerates generated files instead of merging them, re-applies recorded batches in landing order, and makes the
  loader throw on a duplicate name.
- **Evidence:** counts above.
- **When:** 2026-09-13 to 2026-09-22.

### Group related fixes by file, and compute the groups
- **Problem:**
  - Fixes on the same files, landed separately, produced two composition defects that neither implementer could see.
  - Half the implementer reports ended by folding in a sibling note.
  - Hand-picked groups carried 1–3 notes each, and one split a file's five defects across two implementers.
- **Root cause:** the fill unit was one note, not one code region, and groups were assembled by hand from keyword
  matches.
- **Fix in the skills:** [agent-fleet §7][af]: the fill unit is a group, and groups are computed from code sites by
  [`fix_clusters.py`][clusters] (primary file per note, cap ~5, ranked by summed harm).
- **Evidence:** 411 open defects → 125 clusters (42 of five), so a six-slot wave carries ~25–30 fixes instead of ~10.
  Eval 1.00 vs 0.00.
- **When:** 2026-09-13 (groups); 2026-09-27 (computed).

### Implementers competing for cores tripled the lander's gate
- **Problem:** the lander's whole-suite leg went from 9.6 min on a quiet host to 18.5 and then 30.6 min, and one train
  took 84 min. Serial landings made one train wait 44 min for the previous train's CI.
- **Root cause:** up to seventeen implementers ran the same whole assembly on the same 32 cores, because a dispatch
  template had drifted from the brief.
- **Fix in the skills:** partial. [agent-fleet §6][af]: implementers gate narrowly at low process priority, and only
  the lander runs the whole suite. Not in the skills: PIPELINED landing (the next lander merges and gates while the
  previous train is in CI, and waits only before the push) and a compiled-output cache for re-gates.
- **Evidence:** 9.6 → 18.5 → 30.6 min. The first pipelined train waited 0 min. The cache took a cold re-gate from
  22 m 53 s to 1 m 18 s.
- **When:** 2026-09-22.

### Replace the wave barrier with a rolling wave; chain same-file successors
- **Problem:** under "N implementers, then one train", every finished slot waited for the slowest group. A file with
  more defects than the cluster cap became two groups, and each oriented from scratch.
- **Root cause:** barrier synchronization, and orientation at ~46 % of implementer tokens.
- **Fix in the skills:** [`rolling-wave.js`][wave] ([agent-fleet §6][af]): workers pull from one queue, and trains
  start when 4–6 branches are ready. [§7][af]: a same-file successor merges its predecessor's branch and orients from
  its handoff notes; it is a fresh agent, not a longer transcript.
- **Evidence:** design rationale only. The rolling wave has not been A/B-tested ([Open problems](#open-problems)).
- **When:** 2026-09-27.

---

## Gates and verification

### "It ran" is not success
- **Problem:** after ~70 functions were implemented and 30 unit tests were green, a demo ran cleanly, and every
  function returned zero. Later, a program's own "all tests successful" counter was trusted without a diff against the
  expected output.
- **Root cause:** the unit tests exercised the runtime directly and never the code generator between the two. Surface
  signals ("it compiled", "it ran") were taken as proof.
- **Fix in the skills:** [engineering-standards §5][es] "Verify the values, not 'it ran'"; [test-gate][tg].
- **Evidence:** 30 green tests, 100 % of results wrong.
- **When:** 2026-03-13; recurred 2026-03-15.

### A regression guard that compares against recorded output can lock bugs in
- **Problem:** the guard compared each program's output with a recorded baseline, and wrong results had been recorded
  as "expected".
- **Root cause:** the baselines were captured from the implementation, not derived from the authority.
- **Fix in the skills:** [test-gate][tg] Standards ("expected values come from the authority, not from copying the
  current output"); [spec-oracle §3][so].
- **Evidence:** 232 wrong results locked in across 19 tests. A purge cut them to 85 (147 fixed).
- **When:** 2026-03-29.

### A verification that errored is not a verdict
- **Problem:** a fix was publicly declared broken, then the declaration was retracted: the fix worked. It was the third
  misleading verification that session.
- **Root cause:** the check ran a stale binary, and a relative-path race meant the program never launched, while the
  file comparison trivially matched.
- **Fix in the skills:** [test-gate][tg] "A missing observation is not a negative one"; absolute paths.
- **Evidence:** 3 misleading verifications in one session.
- **When:** 2026-05-30.

### Build the whole solution before a no-build test run
- **Problem:** CI was red while every local run was green, because the tests exercised an old compiler.
- **Root cause:** building one project does not re-copy its output into the test projects, so a no-build run tests
  whatever was copied at the last full build.
- **Fix in the skills:** [test-gate][tg] "Always first: build fresh".
- **Evidence:** it recurred at least four times (June–July 2026) before becoming a standing rule.
- **When:** first standing rule 2026-07-09.

### Local green is evidence about one host
- **Problem:**
  - CI ran only the legacy suites, so the code under active development had no CI coverage.
  - Later, 11 clusters in a row went red on CI while the local guard was green.
  - Three consecutive pushes went red from a check compiled only into Debug builds, which CI's Release build
    stripped.
- **Root cause:** the local battery ran on one OS, in Debug, and was not a superset of CI.
- **Fix in the skills:** [test-gate][tg] "After the push: CI is the final authority", plus a local Release leg when
  behavior can differ by configuration.
- **Evidence:** 11 red pushes; 3 red pushes.
- **When:** 2026-06-09; 2026-07-08 to 2026-07-11.

### Gate on the verdict line, never on a chain's exit code
- **Problem:**
  - `test | tail && git commit` committed with the verdict line reading `Failed: 1`.
  - A verify-commit-push chain pushed on red twice in one day, although the rule was already in memory.
  - A battery exited 0 while its verdict line reported 31 regressions.
- **Root cause:** the exit status is the last command's. "Knowing a rule, having just re-read the document containing
  it, and writing a commit about that document, were all insufficient; only the FORM prevents it."
- **Fix in the skills:** [test-gate][tg] "Read the verdict line, not the exit code"; the guard rule
  `no-chain-after-verdict` ([automating-agent-guardrails §1][ag]).
- **Evidence:** 2 red pushes in one day (2026-07-11); 31 regressions caught by reading the line (2026-08-06).
- **When:** 2026-07-11; hook 2026-09-27.

### A check that has never failed may not be checking anything
- **Problem:**
  - A static-analysis scan printed "Scan completed successfully" while 4 of its 6 rules were dead.
  - Drift guards were written that would have stayed green through the very defect they commemorated.
  - A test asserted a field before the pass that sets it had run, so it could never fail.
- **Root cause:** guards were generalized from the instances the author happened to see. "A guard nobody has watched
  fail is a guess about a guess."
- **Fix in the skills:** [test-gate][tg] "A gate that has never failed proves nothing"; [engineering-standards §5][es]
  (a negative control for every rule, and every new check seen to fail once for the right reason).
- **Evidence:** 4 of 6 rules dead. Once the rules were fixed, one of them found 423 hits.
- **When:** 2026-07-26 to 2026-08-31.

### A filter that matches nothing is a silent green, and so is one dead term among live ones
- **Problem:**
  - A wave filter reported 354 passed and ran neither of the two new tests. Parameterized test names are invisible
    to the filter property.
  - After test classes were partitioned, four CI filters matched nothing. The headline "external suite byte-exact"
    verdict would have printed green over zero of its cases.
  - One dead term OR'd among live terms printed a clean pass on every run.
- **Root cause:** `dotnet test --filter` answers "no test matches" with exit 0, and a whole-filter population check
  cannot see a single dead term.
- **Fix in the skills:** [test-gate][tg] ("A filter that matches nothing is a silent green", "Confirm the new tests
  actually ran, by name"). Not in the skills: probing each OR'd term for its own population. The project refuses a DEAD
  term and labels an INERT one.
- **Evidence:** 354 green / 0 of 2 targets (2026-07-28); four dead CI filters (2026-09-02); a dead term found
  2026-09-06.
- **When:** 2026-07-28 to 2026-09-06.

### A missing observation is not a negative one
- **Problem:**
  - A guard printed ALL GREEN at 352 of 353 because one program had vanished.
  - Five runs on one unchanged tree gave five different outcomes.
  - A killed process was scored as the zero-tolerance "we reject, they accept" direction.
  - A fan-out whose agents had all died reported a clean result
    ([Orchestration](#a-fan-out-whose-agents-all-died-reported-all-clean)).
- **Root cause:** harnesses counted only regressions and inferred verdicts from absent data. The same launcher logic
  had been copied seven times, which is why the defect kept reappearing in new places.
- **Fix in the skills:** [test-gate][tg]: pass, fail and no-verdict must partition the declared population, measured
  against a committed manifest, and a lost result is re-observed, never scored. [spec-compliance-audit][sca] Measured
  lessons.
- **Evidence:** 352/353; 7 copies merged into one.
- **When:** 2026-08-03.

### Compare differentials case by case, never by totals
- **Problem:** for weeks the live-state doc said "0 per-case flips" because four totals matched. No per-case record had
  ever been kept.
- **Root cause:** identical totals are consistent with offsetting flips.
- **Fix in the skills:** [test-gate][tg] "Compare differential or snapshot results case by case"; a committed per-case
  baseline where a missing baseline is red.
- **Evidence:** 1,323 cases. The guard was proven with two planted offsetting flips that left the totals unchanged.
- **When:** 2026-08-04.

### A selector is no evidence about what it dropped
- **Problem:** the work ranker hid about half the harmful work. A regex written to count a construct reported 1
  program in 1,611 when there were 3.
- **Root cause:** the ranking predicate omitted a harm class ("rejects legal input"). The regex required an optional
  keyword that the author's own example happened to contain.
- **Fix in the skills:** [test-gate][tg] "A filter, ranker or selector tells you about what it returned and nothing
  about what it dropped"; [engineering-standards §5][es] "Vary the axis your current subject holds fixed".
- **Evidence:** actionable items went from 10 to 19 on one predicate change; the count went from 1 to 3.
- **When:** 2026-08-06; 2026-09-22.

### Commits pushed faster than CI finishes leave no commit verified
- **Problem:** four commits inside one ~25-minute CI run left three runs cancelled. The pattern (fast legs green, slow
  legs blank) looked like broken legs, and one cancellation hid a real break until the next day.
- **Root cause:** `cancel-in-progress` concurrency. The surviving green covers the batch, not each commit.
- **Fix in the skills:** partial. [test-gate][tg] reads the run for the exact pushed commit. "Let a run finish before
  pushing again when per-commit attribution matters" is not stated.
- **Evidence:** 3 of 4 runs cancelled.
- **When:** 2026-08-07.

### The landing protocol never read CI: main was red for 29 hours
- **Problem:** 16 consecutive CI runs failed over ~29 h while about ten landings each reported a green local gate. The
  owner found it first: "You keep breaking the CI CD run."
- **Root cause:** every landing step stopped at the push. The session's probe line only checked whether the workflow
  was enabled. The reds were Linux-only host behaviors that no local gate could see.
- **Fix in the skills:** [test-gate][tg] "After the push: CI is the final authority" (a red blocks, and is landed alone);
  the guard rule `land-via-script`. The project added a required status check that binds repository admins too, and
  a landing script that pushes the verified sha to a CI branch, waits for green, and fast-forwards that exact sha.
- **Evidence:** 16 runs, ~29 h. Payoff one week later: CI caught five reds in a train whose 25-term local gate was
  green, with main untouched.
- **When:** 2026-09-06.

### Conflict markers passed three green gates
- **Problem:** an implementer committed four unresolved conflict hunks into a script that no test imports.
- **Root cause:** `git add -A` stages a conflicted file as readily as a clean one, and "a broken tool reads as a silent
  tool".
- **Fix in the skills:** not yet. The project asserts between add and commit on the OUTPUT of
  `git diff --cached --check` and `git grep --cached` (not their exit codes), and runs a repo-wide conflict-marker
  test.
- **Evidence:** 4 hunks, 3 green gates.
- **When:** 2026-09-06.

### A union of filter terms never converges; the landing gate is the whole suite
- **Problem:** three trains in a row passed a green union-of-filters gate and each drew a red first CI run on the
  shard that runs "everything else". Another train's 25-term union was green while five programs of an external
  conformance suite were red.
- **Root cause:** the leftover shard is by construction the population no term names, so adding one term per lesson
  cannot converge.
- **Fix in the skills:** [test-gate][tg] "A landing gate covers the whole affected assembly"; [agent-fleet §6][af] "The
  lander gates the WHOLE test suite, unfiltered".
- **Evidence:** the whole assembly (~5,800 cases) took ~10 min, against a red CI run (~30 min) plus a re-push.
- **When:** 2026-09-13 to 2026-09-19.

### Gates that stop gating
- **Problem:**
  - Two tests went red on every host because a corpus fetch deleted the old copy before extracting, and the
    extraction failed. Four brief templates told agents to ignore them.
  - A script-level `|| true` discarded the exit code.
  - A resolver found a Windows Store stub named `python3`, so a battery "ran" citation audits that never executed.
- **Root cause:** known reds normalized as "environmental", and interpreters resolved by name.
- **Fix in the skills:** [test-gate][tg] (a red that already existed still blocks the merge; population checks). The
  interpreter-stub trap is not in the skills.
- **Evidence:** the audits printed failure markers while running none of their checks.
- **When:** 2026-09-13 to 2026-09-27.

### Self-reports hide reds; the lander reads each gate log itself
- **Problem:** one implementer's gate log had no verdict line. Another's report hid a real red behind "still running".
- **Root cause:** implementer self-reports were trusted.
- **Fix in the skills:** not stated as such. [agent-fleet §6][af] has the lander gate the whole suite, which caught
  these; the rule "read every implementer's gate log, never the report's summary of it" is not written down.
- **Evidence:** two clusters in one train.
- **When:** 2026-09-22.

### A registered test is not an executed test
- **Problem:** a new negative test shipped without its expected-error file, because the implementer's gate ran only
  the "every program is registered" test.
- **Root cause:** registration proves presence, not execution.
- **Fix in the skills:** [test-gate][tg] "Confirm the new tests actually ran, by name".
- **Evidence:** 1 incident, caught by the lander's whole-suite gate.
- **When:** ~2026-09-23.

### A change that rejects input it used to accept has a global blast radius
- **Problem:** a tightened check passed the implementer's own tests and broke 15 compatibility samples.
- **Root cause:** every "this must be accepted" sample is a candidate red.
- **Fix in the skills:** [test-gate][tg] "A change that REJECTS input the tool used to accept runs the whole
  accepted-input corpus".
- **Evidence:** 15 samples (train 63).
- **When:** 2026-09-25.

### Wall-clock assertions measure the host, and so does a same-run timing ratio
- **Problem:** a 20-second ceiling failed at 26.4 s on a loaded CI runner, for a ~1-second compile. The owner: "THIS IS
  THE SECOND TIME THIS FAILED DUE TO A FEW SECONDS OVERRUN." The first red had been re-run instead of fixed. The
  replacement, a same-run growth ratio, then read 36× against a bound of 30 on untouched code.
- **Root cause:** shared runners are arbitrarily loaded. REVERSED premise: "load that slows both sizes alike cannot move
  a ratio" was wrong, because the small reading is a few milliseconds that one preemption dominates.
- **Fix in the skills:** [review][rv] Determinism check and [`pr-test-analyzer`](agents/pr-test-analyzer.md): count the
  work, not the time. Eval `review-wall-clock-ceiling`.
- **Evidence:** 2 false reds, then 1 more from the ratio. Six tests were reshaped to count work.
- **When:** 2026-09-25.

### A status lookup that fails is no verdict
- **Problem:** the landing script announced "CI IS RED, AND IT IS ALREADY ON MAIN" for a green run.
- **Root cause:** a single API read hit a TLS timeout, and the script treated "not success" as red.
- **Fix in the skills:** [test-gate][tg] "A status lookup that FAILS is no verdict": retry, and report it as UNVERIFIED.
- **Evidence:** 2 incidents.
- **When:** 2026-09-25 to 2026-09-27.

---

## Guardrails

### Agents change valid input to dodge a bug in the tool
- **Problem:** the agent edited the demo program (removing text, simplifying output) until it compiled, and changed test
  inputs and assertions to match broken behavior.
- **Root cause:** "when *using* software you work around bugs. But we are *building* the software."
- **Fix in the skills:** [engineering-standards §3][es] "Never change valid input to dodge a bug in the tool".
- **Evidence:** at least 4 occurrences in March 2026.
- **When:** 2026-03-13.

### Every bug is a pattern
- **Problem:** fixes were applied to one of several sibling sites (two of three arithmetic statements; one of five
  places carrying the same rule). "The user had to prompt this audit — it should have been automatic."
- **Root cause:** fix, test, move on.
- **Fix in the skills:** [variant-analysis][va]; [engineering-standards §3][es]. The two-arm dispatch is the most
  reproducible shape: a repro exercises one arm, the tests follow it, and the other arm survives.
- **Evidence:** the two-arm shape was counted a fifth time on 2026-08-03 and an eighth by 2026-08-30.
- **When:** 2026-03-15 onward.

### Silent fallbacks "for convenience" are wrong answers
- **Problem:** a silent-drop fallback was used 13 times, and another fallback made every comparison with a zero or
  negative literal true.
- **Root cause:** "convenient fallbacks that avoided compilation failures at the cost of silent wrong behavior", added
  without the owner's knowledge.
- **Fix in the skills:** [engineering-standards §1][es] "No hacks, no shims, no fallbacks"; [§4][es] "Never ship a
  half-feature"; the [`silent-failure-hunter`](agents/silent-failure-hunter.md) reviewer.
- **Evidence:** 13 failures in one conformance program traced to one silent drop.
- **When:** 2026-03-16.

### Deferral and "scope to the test" are debt
- **Problem:** the agent repeatedly proposed deferring spec-correct behavior, gating finished features off, or scoping a
  feature to what one test referenced. The owner: "you regularly prefer to accumulate technical debt rather than do it
  correctly."
- **Root cause:** the smallest diff that fits the stated estimate.
- **Fix in the skills:** [engineering-standards §4][es] "Tests verify; they do not scope" and "Deferral is debt, and
  only the owner may choose it"; [agent-fleet][af] Standards.
- **Evidence:** at least 5 separate owner corrections, March–July 2026.
- **When:** 2026-03-22 to 2026-07-21.

### `git stash` is shared by every worktree
- **Problem:**
  - A stash taken while background agents were writing files lost all seven agents' completed work (March).
  - An implementer's `git stash pop` consumed a different agent's stash (September). Its own stash had created
    nothing, because the change was untracked.
  - A lander used `--autostash`.
- **Root cause:** the stash stack lives in the common git directory, shared by all linked worktrees.
- **Fix in the skills:** [agent-fleet §4][af] "Never use `git stash` for WIP; commit it instead"; guard rules `no-stash`
  and `no-autostash` ([`guard-rules.json`][rules]).
- **Evidence:** 7 agents' work lost (2026-03-27); 1 stash restored by hand (2026-09-06).
- **When:** 2026-03-27; 2026-09-06; 2026-09-25.

### Move rituals into skills and forgotten rules into hooks, and let hooks fail open
- **Problem:** rules were broken "through forgetting, not disagreement". The first hooks were tested only from the repo
  root, and one failed CLOSED when run from a subdirectory.
- **Root cause:** a rule held in memory is forgotten exactly when attention is elsewhere, and a guard that fails closed
  on its own bug blocks legitimate work.
- **Fix in the skills:** [automating-agent-guardrails §1][ag] (the contract: fail open, name an `Instead:` command, a
  self-test that proves every rule fires and passes its allowed neighbors).
- **Evidence:** unmeasured at the time.
- **When:** 2026-07-26.

### Ground rules in a prompt constrain intent, not effects
- **Problem:** a probe agent wrote three files into the repository root despite explicit scratch-only rules, and the
  next `git add -A` pushed them.
- **Root cause:** only the tree shows what an agent actually did.
- **Fix in the skills:** [agent-fleet §12][af] "run `git status --short` and account for every unexpected path";
  read-only roles carry [`readonly_guard.py`][ro] in their own frontmatter ([automating-agent-guardrails §2][ag]).
- **Evidence:** 3 files, 1 pushed commit.
- **When:** 2026-08-09.

### A build guard that guessed its way into four self-blocks and one silent pass
- **Problem:** after the fleet incident above, a hook refused builds while a fleet was live. It then blocked legitimate
  builds four separate ways:
  1. An agent's own transcript counted as "live".
  2. The hook payload's transcript path is the session's, not the subagent's, so the exclusion never matched.
  3. It froze the whole session when only one tree needed freezing.
  4. Git-Bash drive paths were parsed as Windows relative paths.

  Then it let builds through: liveness was "transcript written in the last 120 s", and a tool call writes nothing for
  its whole duration.
- **Root cause:** each failure was a derivation (caller identity, tree, liveness) that was guessed instead of measured.
  The second cause was established only by instrumenting the live payload; the first hypothesis, "a different
  spelling", was wrong.
- **Fix in the skills:** the lessons, not the guard: [automating-agent-guardrails §1][ag] (fail open, a self-test with
  both branches), [agent-fleet §5][af] (liveness from processes and the journal). The build guard itself is not
  published.
- **Evidence:** 4 of 4 builds refused in one agent. In the fail-open case each agent had 5–7 silences over 120 s,
  up to 585 s.
- **When:** 2026-08-18 to 2026-09-28.

### Prose rules get broken; enforce the owner's rules with hooks
- **Problem:** a prose-only rule set was broken repeatedly: backslash escapes mangled by heredocs across five or more
  sessions (three lapses in one), a shared-stash pop, an `--autostash`, and red pushes.
- **Root cause:** instructions alone are not reliably followed, and the tool harness rewrites escapes in heredoc
  bodies, so no quoting form survives.
- **Fix in the skills:** [automating-agent-guardrails §1][ag] and [`guard_commands.py`][guard] with
  `no-escapes-in-heredoc`, `no-stash`, `no-autostash`, `land-via-script` and `no-chain-after-verdict`. Routing
  around a block (for example, wrapping the command in a script) is itself a violation.
- **Evidence:** the heredoc rule fired on the orchestrator's own command within minutes of being registered.
- **When:** 2026-09-25.

### A hook's message must reach the agent readably
- **Problem:** the first live block message reached the agent as mojibake.
- **Root cause:** Python on Windows writes stderr in the console code page, and the harness reads UTF-8.
- **Fix in the skills:** [automating-agent-guardrails §1][ag] contract ("writes stderr as UTF-8").
- **Evidence:** 1 incident; bytes checked.
- **When:** 2026-09-25.

### Owner-gated steps must be asked, never silently skipped
- **Problem:** tooling that needed the owner's permission simply went unused. The owner: "automatic use of such a
  feature should trigger a query to me, not silently failure to use it."
- **Root cause:** agents skip what they are not allowed to do and say nothing.
- **Fix in the skills:** [automating-agent-guardrails §3][ag]: a session-start readiness check whose ASK-OWNER lines
  each become one question.
- **Evidence:** unmeasured.
- **When:** 2026-09-25.

### A repo-scoped guard keyed on a name fails after a rename
- **Problem:** after the repository was renamed, the push guard let a direct push through. A relative `cd` also
  escaped it.
- **Root cause:** the guard recognized "this repo" by a hard-coded name.
- **Fix in the skills:** [automating-agent-guardrails §1][ag]: scope by git common directory against
  `$CLAUDE_PROJECT_DIR`, and test the guard in an unrelated repository.
- **Evidence:** the CI self-test went 20/21 on the rename (then 25/25).
- **When:** 2026-09-26.

### A written rule fails silently 21 % of the time; enforce only the cheap arms
- **Problem:** "rewrite STATUS.md after every commit" was violated by 3 of 14 finished branches.
- **Root cause:** nothing prevented the violation; the stamp only made it detectable. A hook registered with
  `if: Bash(git commit*)` mostly never ran, because agents commit through `git add -A && git commit` and through
  checkpoint scripts they write themselves.
- **Fix in the skills:** [`status_guard.py`][sguard] ([agent-fleet §4][af]): PreToolUse with NO `if` filter refuses a
  commit while the stamp is not HEAD, and Stop/SubagentStop refuses once to finish in that state. A reminder after every
  command was REJECTED on cost (the owner: "We cannot afford a 20% more cost", "reduce cost where possible but not at
  the risk of reducing correctness").
- **Evidence:** 40 headless sandbox sessions; the reminder cost ~+20 % turns and ~+19 % dollars. The sandbox reproduced
  no stale handoffs in any arm ([Measurement](#a-sandbox-that-cannot-reproduce-the-failure-cannot-measure-the-fix)).
- **When:** 2026-09-28.

### No wrappers, no copies: consume the shared skills from one pinned source
- **Problem:** project copies of the fleet scripts and the public ones diverged in both directions, and the migration
  plan proposed shim scripts. The owner: "I've provided this guidance previously." The guidance had lived only in the
  orchestrator's memory, where agents never saw it.
- **Root cause:** two copies of every generic practice, reached by a prose pointer.
- **Fix in the skills:** [README Install](README.md#install) (pin as a submodule; project skills are overlays);
  [agent-fleet §2][af] "never a copy or a wrapper" and one `.agent-fleet.json`.
- **Evidence:** after the parity fix, both copies produced 118 clusters from 357 notes. A fresh `git worktree add` gets
  no submodules even with `submodule.recurse=true` (measured).
- **When:** 2026-09-28.

---

## Spec as oracle and citation discipline

### Build from the specification, not from training data
- **Problem:** the first parser was written "from vibes". After a day of one-at-a-time patches, the external conformance
  suite stood at 79 of 391 programs, against 78 at the start.
- **Root cause:** "AI assistants treat specifications as references to consult when confused, not as blueprints to
  follow from the start."
- **Fix in the skills:** [spec-oracle][so]: derive the expected behavior and a checked citation before reading or
  writing code.
- **Evidence:** 78 → 79 of 391 by patching; 192 of 391 after rebuilding the parser from the specification's grammar.
- **When:** 2026-03-13 to 2026-03-14.

### No output difference is "implementation variation" without a citation
- **Problem:** output differences against the expected results were waved through as a "standard difference between
  implementations".
- **Root cause:** accepting "close enough" instead of asking what the spec requires.
- **Fix in the skills:** [spec-oracle][so] "Every difference from the expected output counts as a bug until a citation
  says otherwise."
- **Evidence:** the program then matched byte for byte.
- **When:** 2026-03-15.

### Another implementation is a regression net, never an authority
- **Problem:** the legacy implementation had a non-conforming behavior that the test harness's normalization masked.
  Matching it would have copied the bug.
- **Root cause:** a differential is blind to every bug both sides share.
- **Fix in the skills:** [spec-oracle][so] "Why other oracles are not authority"; [spec-compliance-audit][sca] Measured
  lessons. The legacy differential was later made opt-in: "near-zero correctness signal and pure friction".
- **Evidence:** 7 expected outputs re-derived from the spec with owner approval, each verified twice.
- **When:** June 2026; opt-in 2026-07-22.

### Text extraction of a standard loses its diagrams, always toward "more restrictive"
- **Problem:** a dropped pair of choice bars manufactured an ambiguity that survived two adversarial lenses and reached
  the owner as a decision fork. Grammar rules inherited the same loss, so legal input was rejected.
- **Root cause:** the PDF's text layer was unusable, so the Markdown was OCR. Rule prose survived, and the
  general-format diagrams did not.
- **Fix in the skills:** [spec-oracle §2][so] (render the page whenever a figure decides the answer). Eval
  `spec-oracle-render-the-diagram`: 1.00 vs 0.33.
- **Evidence:** of 19 diagrams that carry choice indicators, 18 lost them (95 %), and 17 of those losses changed a
  decision. A later figure sweep checked 88 claims: 61 confirmed, 27 refuted, and all 19 normative errors were falsely
  restrictive.
- **When:** 2026-07-19.

### Check a "decision fork" against the text before spending an owner decision
- **Problem:** five claimed owner-decision forks reached a research packet; the first was about the wrong construct.
- **Root cause:** questions were framed without re-reading the governing rule, and one fork was an OCR artifact.
- **Fix in the skills:** [spec-oracle §1][so] (find the specific governing rule); [§ Where the spec leaves
  latitude][so].
- **Evidence:** 4 of 5 dissolved into the spec text.
- **When:** 2026-07-19.

### Citations are inherited, not invented; check every one mechanically
- **Problem:**
  - Wrong clause numbers with correct quoted text spread into comments, tests and logs.
  - One agent's "fabricated" citation turned out to come from the project's own design document, where it appeared
    twice (and in 13 source comments).
  - A nonexistent clause was cited in ~21 places for months.
  - A cleanup found about 200 sites citing nine clause numbers that do not exist.
- **Root cause:** "The failure is not inventing a citation, it is INHERITING one." The clause number is never
  re-derived, and "a phantom survives by accreting meaning".
- **Fix in the skills:** [spec-oracle §3][so] and [`citation-checker.md`](skills/spec-oracle/references/citation-checker.md):
  the quote must occur inside that clause's own text, re-checked every time it is passed on.
- **Evidence:** 6 wrong citations in one review (2026-07-19); 54 doc-vs-spec conflicts, ~25 of them wrong clause
  numbers (2026-07-22); ~200 sites (2026-09-02).
- **When:** 2026-07-19 to 2026-09-02.

### A line number is not a citation
- **Problem:** an appendix cited spec line numbers, which pointed at the wrong sentence for weeks before they pointed
  past the end of the file.
- **Root cause:** editing the transcription slid about 180 citations. "A dangling reference is loud. A reference that
  resolves to the wrong sentence is not."
- **Fix in the skills:** [spec-oracle §3][so]: a citation is a clause number plus a verbatim fragment.
- **Evidence:** 174 citations re-verified.
- **When:** 2026-07-28.

### Validate the premise, not only the rule
- **Problem:** a finding quoted a real rule correctly. The owner made a decision on it and machinery was built, but the
  construct it described cannot occur in legal input.
- **Root cause:** "I validated the rule TEXT and never validated the PREMISE."
- **Fix in the skills:** [spec-oracle][so] "A valid rule with an impossible premise": write the test input first, and if
  it cannot be written legally the finding is refuted. The unreachable machinery was reverted.
- **Evidence:** 1 full implementation reverted.
- **When:** 2026-07-28.

### A checker can be buggier than the thing it checks
- **Problem:** a broad citation audit reported 133 of 183 citations defective, and every inspected one was a checker
  bug. A figure audit reported 76 findings, of which 1 was real. The documentation audit covered only half the prose,
  because source-code comments carry citations at the same density.
- **Root cause:** labels in quotation marks, the wrong nearest-clause heuristic, and a population chosen by directory.
- **Fix in the skills:** partial. [test-gate][tg] "make it fail once… then ask what its scope leaves out"; the narrow,
  precise check in [`citation-checker.md`](skills/spec-oracle/references/citation-checker.md).
- **Evidence:** 133 → 0 false findings after calibration; 5 real defects fixed.
- **When:** 2026-07-29 to 2026-07-30.

### A real clause can answer a different question
- **Problem:** three comments justifying NOT implementing something cited real, checkable clauses about other subjects,
  and the mechanical check passed all three. Later, the project's conformance definition was cited by a clause that is
  about user documentation, in six places.
- **Root cause:** a mechanical check proves the quote is in the clause, not that the clause governs the question.
  Because these citations argue against writing code, no test fails.
- **Fix in the skills:** [spec-oracle][so] Failure modes; [`comment-analyzer`](agents/comment-analyzer.md) (eval
  1.00 vs 0.50).
- **Evidence:** 3 in one session (2026-08-04/05); 6 locations (2026-09-22).
- **When:** 2026-08-04; 2026-09-22.

### Survey the implementations on latitude questions; don't recall
- **Problem:** the owner answered two decision questions with "What do other modern implementations do?" A
  first-principles framing was then overturned: all five surveyed compilers made the same choice.
- **Root cause:** decisions were presented without a survey, or with one built from memory.
- **Fix in the skills:** [spec-oracle][so] "Where the spec leaves latitude" (the spec, then the reference
  implementation, then the other major vendors; survey, don't recall; record the choice).
- **Evidence:** 5 of 5 compilers agreed. One question was settled "in minutes" from the reference implementation's
  source.
- **When:** 2026-08-03 to 2026-08-08.

### A citation edited in passing is still a citation
- **Problem:** the orchestrator's rewrite of a comment dropped a clause qualifier, a design draft cited a clause that
  does not exist, and a paraphrase was presented as a verbatim quote.
- **Root cause:** "I ran the check on the rules I reasoned about, but not on a clause reference I edited in passing."
- **Fix in the skills:** [spec-oracle][so] "Re-check every citation you pass on, including ones you wrote yourself last
  week". The project also moved its citation audits into required CI after 14 wrong clause numbers reached main from
  sessions that skipped the local gate.
- **Evidence:** 14 wrong numbers on main (2026-09-25).
- **When:** 2026-09-13 to 2026-09-27.

---

## Adversarial review and refuters

### An audit scoped to five items reported "no divergences"
- **Problem:** a comprehensive audit was requested, and the agent checked 5 items.
- **Root cause:** under-scoping, which gave false confidence.
- **Fix in the skills:** [review][rv] Scale (a completeness critic asks what was NOT examined); [spec-compliance-audit
  §1][sca] (a rule catalog as the denominator).
- **Evidence:** the full audit found 80 issues (7 critical). One missed bug affected 32 programs of the external
  conformance suite. "A full day was wasted."
- **When:** 2026-03-14.

### Find-then-verify reviews find real bugs in every phase; verify by running
- **Problem:** self-reviewed slices shipped silent miscompiles of legal input.
- **Root cause:** tests exercise the happy path their author imagined.
- **Fix in the skills:** [review][rv] Steps 5–6 (parallel finders, then a skeptic per finding that tries to run the
  scenario). Repros were "sometimes WRONG in their specifics … but named REAL bugs".
- **Evidence:** one wave: 45 agents → 29 findings → 22 confirmed, 2 refuted (3.18 M tokens, ~23 min).
- **When:** June–July 2026.

### A verifier that agrees must agree with the reasoning
- **Problem:** a verification pass agreed with a verdict held for the wrong reason, which would have shipped a defect
  that rejects legal input.
- **Root cause:** checking the conclusion, not the reasoning.
- **Fix in the skills:** [review][rv] Step 6 "Right answer, wrong reason"; [spec-compliance-audit §6][sca].
- **Evidence:** 1 behavior-changing case.
- **When:** 2026-07-19.

### Refuters overturn a large share, and almost always downward
- **Problem:** adjudicators closed rules as conforming after sampling outputs or reading one of two code paths.
- **Root cause:** the author of a verdict is biased toward it, and sampled outputs miss the branch that matters.
  Refuters told to READ THE IMPLEMENTATION found the two-arm defects reliably.
- **Fix in the skills:** [agent-fleet §10][af]; [spec-compliance-audit §6][sca] (a refuter on every closing verdict;
  "an all-upheld report is a red flag"; if the refuter fails, the verdict is unverified).
- **Evidence:**
  - One batch: 26 agents, 16 overturns, all downgrades.
  - Batches of 70 and 120 closing verdicts: 19 and 44 overturned, "not one overturn went upward".
  - Cloud batches: 3 of 10 and 21 of 51 overturned.
  - **Source conflict resolved:** one batch (2026-08-29) overturned in both directions, though downgrades still
    dominated. Every other batch recorded was downward only.
- **When:** 2026-07-29 to 2026-09-25.

### Review fleets on a green wave find the wave's own defects
- **Problem:** waves gated green still carried defects their implementers had introduced, widened, or certified.
- **Root cause:** only the implementer had read the change.
- **Fix in the skills:** [agent-fleet §11][af] "Self-review before a large review fleet"; [review][rv].
- **Evidence:** fleets of 25–106 agents confirmed 3–26 findings each. In one 106-agent wave, 5 of 11 defects were the
  wave's own.
- **When:** 2026-07-29 to 2026-08-31.

### Re-probe a stale backlog before fixing it
- **Problem:** 331 known-bad rules had been judged before several fix waves landed.
- **Root cause:** a note's word is not evidence, and the tree had moved on.
- **Fix in the skills:** partial. [spec-compliance-audit §6][sca] (the refuter re-derives the result) and
  [engineering-standards §3][es] ("a remembered pattern is a hypothesis"). "Re-probe every note on today's tree and
  discharge it with evidence if it no longer reproduces" is not stated as a fleet rule.
- **Evidence:** a 24-agent probe-and-refute pass: 114 FIXED, 170 still live, 47 not implemented, and 20 prober claims
  overturned. A later 17-agent pass had refuters break 14 FIXED claims.
- **When:** 2026-08-09.

### REJECTED: merging the verdict and evidence lanes ("witness-first")
- **Problem:** adjudication produced verdicts without evidence, so a second lane re-read every rule to write the
  witness. That is a double derivation.
- **Root cause of keeping it:** an agent that has read the code "has already formed an expectation from the code". A
  self-written witness or a blind replicate checks the record of one derivation instead of supplying a second.
- **Fix in the skills:** owner decision, NO. The lanes stay separate, and the double derivation is "the price of the
  guarantee". [spec-compliance-audit §5–§6][sca] keep witness and refuter independent.
- **Evidence:** one batch of 184 rules moved the GAP by 15 (8 %), another of 292 rules by 32 (11 %). The merged
  pipeline projected ~53 B → ~29 B remaining tokens, and was declined anyway.
- **When:** 2026-09-04.

### Free-text verdicts need structured fields; integrate per claim AND per evidence
- **Problem:**
  - A merge script recognized only the word `WITHDRAWN`. Refuters wrote "CLEARED" or put the instruction in the
    reason field, so two rows would have closed on evidence a refuter had withdrawn.
  - Later, integration joined by rule id: one overturned test landed, and valid evidence was withheld from sibling
    claims.
- **Root cause:** parsing agent prose, and a join key that is too coarse.
- **Fix in the skills:** [agent-fleet §10][af] "Keep verdicts per claim AND per piece of evidence". Structured withdrawal
  fields are not stated.
- **Evidence:** 2 rows (2026-09-04); 1 wrong landing and 2 landers checking every row by hand (2026-09-24/25).
- **When:** 2026-09-04; 2026-09-25.

### A registrar re-measures every lead before filing it
- **Problem:** leads forwarded from implementer reports were often wrong, and a second agent repeated them verbatim
  ("a registrar that copies a report's measurement forward is a second place for the report's mistakes to live").
- **Root cause:** unverified self-reports.
- **Fix in the skills:** not yet as a role. The project's registrar greps the register first, rebuilds, re-runs every
  probe on its own build, re-checks every citation, and drops dead leads with the reason.
- **Evidence:** one pass of 48 leads: three did not survive, and one quoted "citation" was a paraphrase. Another pass:
  two of four handed-forward leads were wrong.
- **When:** 2026-09-13 to 2026-09-22.

### A review step on the merged diff before every landing push
- **Problem:** the owner wanted each train reviewed before it reached main.
- **Fix in the skills:** [automating-agent-guardrails §6][ag] (a review of the merged diff before the landing push; a
  finding blocks); the lander template drops the affected branch.
- **Evidence:** three trains reviewed, 0 confirmed findings, none dropped (a null result so far).
- **When:** 2026-09-25.

---

## Work register and process hygiene

### Log missteps honestly; they are the data
- **Problem:** the project was also research into human–AI collaboration.
- **Fix in the skills:** [engineering-standards §6][es] "Say it immediately when you were wrong". Corrections are added
  as new entries, never rewritten history.
- **Evidence:** dozens of recorded missteps; this document is built from them.
- **When:** 2026-03-13.

### Docs describe the current state; history lives in the log
- **Problem:** 179 docs, about 100 of them design essays with stale banners and superseded claims.
- **Fix in the skills:** [engineering-standards §6][es].
- **Evidence:** 179 → 126 docs.
- **When:** June–July 2026.

### A live-state document decays by accumulating true statements
- **Problem:**
  - The live-state section grew to 690 lines, most of it narrative, and restated lists it had promised to hold once.
  - A probe warning was trained to be ignored.
  - Later the section again accreted dated bullets, each true when written, until a reader could not tell which was
    current. A "main is identical to the phase branch" line misled a session sitting on 17 unmerged commits.
- **Root cause:** "adding to the top of a live-state doc feels like diligence, and pruning feels like deletion."
- **Fix in the skills:** not yet. The project's rule: an edit to live state is almost always a REPLACEMENT, and numbers
  are computed by a probe, never quoted.
- **Evidence:** 690 → 123 lines; 17 unmerged commits.
- **When:** 2026-07-26; 2026-07-29 to 2026-08-05.

### Memory rots unless it is checked
- **Problem:** memories routed sessions to deleted files, one asserted that a deleted subsystem still existed, and others
  were session narrative.
- **Root cause:** memories accumulate narrative and are never validated.
- **Fix in the skills:** not yet. The project consolidated its memory and added mechanical checks: names match files,
  links resolve, cited files exist, and no live status is kept in the index.
- **Evidence:** 91 files / 25,511 words → 55 / 9,590. 44 of 91 name mismatches, 15 dangling links, and 11 of 13 cited
  repository files gone.
- **When:** 2026-07-26.

### A work item's stated premise is a claim, not a finding
- **Problem:** filed work items carried wrong layers, counts, causes and blockers. One said "four functions" where the
  standard said twenty. One was sized as "the riskiest category" of change and turned out not to be needed.
- **Root cause:** the entries had been reasoned, not measured.
- **Fix in the skills:** [engineering-standards §3][es] "Diagnose from evidence" and "A remembered pattern is a
  hypothesis"; [variant-analysis Step 5][va] (probe every candidate). The project's rule: write the probe from the
  governing rule, never from the reported symptom, and re-measure every claim before writing code.
- **Evidence:** the stated premise failed on 14 of 16 items across two sessions.
- **When:** 2026-07-30 to 2026-08-09.

### One work register, ranked by harm
- **Problem:** "what is left" was written in five places, and three of them each claimed to be canonical. A wrong-answer
  defect sat in a prose paragraph where no list could see it. A session picked a zero-harm item over a silent wrong
  answer with the same severity label.
- **Root cause:** hand-maintained parallel lists, and ranking by label instead of consequence.
- **Fix in the skills:** [spec-compliance-audit §7][sca] (one tracked item per mechanism; rank by what the defect DOES);
  [engineering-standards][es] Project hooks ("Work register: the one place"); [review][rv] Step 8 ("findings become
  tracked work").
- **Evidence:** 5 registers, 3 claiming to be canonical.
- **When:** 2026-08-04.

### A status written in two places drifts
- **Problem:** five items had one status in their heading and another in their metadata, including one closed that
  same day.
- **Root cause:** tools read only the metadata, so nothing contradicted the heading.
- **Fix in the skills:** [engineering-standards §2][es] "One rule in one place" (general). The register check that
  compares the two is project-side.
- **Evidence:** 5 items.
- **When:** 2026-08-05.

### Ask the owner only genuine choices, one bare question at a time
- **Problem:** the owner received bookkeeping questions ("may we mark these rows unsupported?") and bundled questions.
  The owner: "If we do not support a facility, we do not support that facility… What am I missing?"
- **Root cause:** a safeguard that reserved a verdict class to the owner produced questions with no real choice in
  them.
- **Fix in the skills:** partial. [spec-oracle][so] (survey the implementations before asking). Not stated: ask one bare
  question at a time, and have the owner decide a CLASS of cases once instead of row by row. When questions were put in
  plain language beside what the reference implementation does, six came back decided in one message.
- **Evidence:** about 10 per-row questions replaced by one class decision.
- **When:** 2026-08-30; 2026-09-24.

### Link each fix to what it closed
- **Problem:** 131 of 138 defective inventory rows were invisible to the ranker. Separately, a witness round paid for
  a fix that had already landed, for the fourth time.
- **Root cause:** ownership was inferred by scraping ids from row prose (the wrong direction), and nothing linked a
  landed fix to its rows.
- **Fix in the skills:** [spec-compliance-audit §7][sca]: each item lists the rows it claims, and on landing records
  the rows it CLOSED (or why none).
- **Evidence:** 131 of 138; actionable items went from 29 to 54 after the fix.
- **When:** 2026-08-31; 2026-09-19.

### Allocate ids centrally
- **Problem:** five id collisions in one day, each costing a renumbering pass. Parallel landers overwrote each other's
  reports. Later, an id taken from a memory snapshot collided with a running registrar, and two orchestrators (local
  and cloud) filed the same id.
- **Root cause:** parallel writers minting ids, from snapshots rather than from the tree.
- **Fix in the skills:** [agent-fleet §8][af]; ids, codes and the report path are in [`brief-template.md`][brief].
- **Evidence:** 5 collisions in one day (2026-09-02); more on 2026-09-21 to 2026-09-28.
- **When:** 2026-09-02.

### A status page nobody regenerates goes stale
- **Problem:** three landings moved the headline counts before the owner's status page was touched. The owner: "you
  should be keeping the artifact up to date."
- **Root cause:** the page was hand-written from numbers an agent recomputed each time.
- **Fix in the skills:** not yet. The project renders the page by script from its measured inputs, with a staleness
  check, and refreshes it as a landing step.
- **Evidence:** as above.
- **When:** 2026-09-02.

### Findings left in reports are invisible to the ranker
- **Problem:** implementers found and reported more defects than they closed. Silent wrong answers sat as unfiled
  paragraphs, and two real defects sat inside another item's body.
- **Root cause:** "a ranker can only rank what was written down."
- **Fix in the skills:** [spec-compliance-audit §7][sca] "A finding that exists only in an audit report or a log
  paragraph is invisible to every work list"; [review][rv] Step 8.
- **Evidence:** one pass turned 7 unfiled paragraphs from 3 reports into 5 new items and 2 appends.
- **When:** 2026-09-09 to 2026-09-21.

### Practices kept in prose are forgotten: one practices file, rendered briefs, a checker
- **Problem:** one wave received the current practices only because a session scratchpad (a chain of spec-rendering
  scripts and a gate-wait file) survived a reboot. The repository's briefs lacked some of them.
- **Root cause:** practices lived in transcripts and hand-written briefs.
- **Fix in the skills:** partial. [`brief-template.md`][brief] is rendered by a script, and [agent-fleet §2][af] says "if
  you keep a brief checker, make it fail a brief without" a required line. The project keeps every practice, with its
  reason and measurement, in one file, and its checker fails any brief or rendered spec that drops one.
- **Evidence:** the checker went red on the older briefs and on the hand-rendered spec, and green after the fix.
- **When:** 2026-09-23.

### Timestamps come from the clock
- **Problem:** log and meter entries were stamped ahead of real time.
- **Root cause:** the model estimated elapsed time.
- **Fix in the skills:** not yet. Project rule: every stamp comes from `date` in the same command.
- **Evidence:** 2 meter stamps (2026-09-12) and 2–4 log headers (2026-09-27/28).
- **When:** 2026-09-12; 2026-09-27.

---

## Orchestration mechanics

### Agents without the tools they need improvise from training data
- **Problem:** extraction agents failed 3 of 3 on permissions. A spec agent that could not read the PDF synthesized its
  answer from training data. The orchestrator then claimed agents could not have shell access, which earlier agents had
  used.
- **Root cause:** tool availability differed per agent, and nobody checked.
- **Fix in the skills:** [automating-agent-guardrails §2][ag]: restart, then prove each role with a smoke agent that
  reports its model and tries its tools.
- **Evidence:** 3 of 3 failed.
- **When:** 2026-03-14.

### Dense keyword lists trip the API's content filter
- **Problem:** agents working on reserved-word lists were killed by content filtering (HTTP 400) four times, once in the
  main session.
- **Root cause:** large all-caps word dumps, arriving in tool results or file writes, trip the safety layer's
  encoded-content heuristics.
- **Fix in the skills:** not yet. WORKAROUND: such lists never transit the API. Agents write them to files and return
  counts, and generators print counts only.
- **Evidence:** 4 occurrences. Afterwards, 12- and 8-agent workflows (~809 k and ~862 k tokens) had 0 trips.
- **When:** 2026-06-11 to 2026-07-03.

### Schema-forced returns can degrade to stubs
- **Problem:** two of six scouts returned placeholder output that satisfied the structured-output schema.
- **Root cause:** the schema forced a return even when the agent had nothing.
- **Fix in the skills:** partial. [agent-fleet §3][af] (state the bar; spot-check substance).
- **Evidence:** 2 of 6.
- **When:** 2026-07-04.

### Workflow scripts: LF only, and parse the args
- **Problem:** the Workflow tool refused a script "as control characters" without saying why, and a script sliced its
  args as text.
- **Root cause:** git checked the script out with CRLF (and later, Python's `write_text` on Windows wrote CRLF). The
  args arrived as a JSON string.
- **Fix in the skills:** [agent-fleet §6][af] "Keep a workflow script LF-only". Parsing args is not stated.
- **Evidence:** refusals on 2026-07-26, 2026-09-04 and 2026-09-27.
- **When:** 2026-07-26.

### Never read a two-stage fleet's output before the completion signal
- **Problem:** a batch was published, gated and pushed early. The re-merge moved 10 of 55 rows, three of which had been
  reported as permanently closed.
- **Root cause:** stage one writes the file and stage two rewrites it, so "files existing is not files finished". An
  adversarial second stage only removes confidence, so an early read systematically reports a better result than the
  truth.
- **Fix in the skills:** [agent-fleet §9][af] "Never poll a running fleet's output directory". The same rule applies to
  report files, after a mid-write read put a false item in front of the owner.
- **Evidence:** 10 of 55 moved, all downgrades.
- **When:** 2026-07-29; 2026-09-21.

### A fan-out whose agents all died reported "all clean"
- **Problem:** a nine-agent sweep lost all nine agents to API 529 errors (zero tokens, zero tool calls). Its summary
  said every use was correct.
- **Root cause:** a `defects.length ? … : fallback` branch "unable to distinguish 'nothing was wrong' from 'nothing
  ran'".
- **Fix in the skills:** [test-gate][tg] "A missing observation is not a negative one" (general). The synthesis now
  reports INCONCLUSIVE and names the failure.
- **Evidence:** 9 of 9 agents dead; later retries lost 9 and 3.
- **When:** 2026-07-29.

### "Workflow completed" does not mean every agent succeeded
- **Problem:** 5 of 12 refuters died on API errors, and their output files still held unrefuted first-stage results.
  The run reported "completed".
- **Root cause:** per-agent failures appear only in the notification's failure list.
- **Fix in the skills:** not yet. Read the failure list before the results, and resume only the dead agents.
- **Evidence:** 5 of 12.
- **When:** 2026-07-30.

### A defect can satisfy the check downstream of it
- **Problem:** a batch promised 674 agent inputs and 673 were visible, with no error. The missing file existed and was
  non-empty.
- **Root cause:** a colon in a slug wrote an NTFS alternate data stream (`path:stream`). The exists-and-non-empty check
  passed on the bug.
- **Fix in the skills:** partial. [agent-fleet §2][af] reconciles returns against the worklist. Sanitizing names to
  `[a-z0-9-]` and comparing the promised population with a glob are not stated.
- **Evidence:** 674 vs 673.
- **When:** 2026-09-02.

### Worktree-isolated agents cannot reach other worktrees
- **Problem:** a lander brief assumed it could read the implementers' worktrees.
- **Root cause:** the harness refuses `git -C <another worktree>`, `cd` and entering a worktree from an isolated agent.
  PowerShell from the Bash tool was also refused there.
- **Fix in the skills:** not yet. Landers take branch content from the shared object store (`git diff <base>
  <branch>`), and the orchestrator verifies each implementer worktree is clean before dispatching a lander.
- **Evidence:** measured on the first train.
- **When:** 2026-09-04.

### Sibling agents share one scratchpad
- **Problem:** a finisher read a gate file holding a sibling's verdict. Registrars overwrote each other's scripts, and
  finishers overwrote reports.
- **Root cause:** every subagent of one session gets the same scratch directory, and briefs used generic filenames.
- **Fix in the skills:** partial. [agent-fleet §8][af] allocates report paths. A per-agent subdirectory named in every
  brief is proposed but OPEN.
- **Evidence:** one scratchpad held 1,034 entries, eleven of them generic gate files.
- **When:** 2026-09-06 to 2026-09-24.

### An agent that ends its turn kills its own background gate
- **Problem:** in one wave, five implementers and a lander returned "gate PENDING" with logs stopping mid-leg, and the
  train did not land. Another lander spent ~440 of its 560 tool calls on one-word waits.
- **Root cause:** briefs said "run the gate in the background and wait for the notification". A workflow agent that
  stops has ended its turn, and its background process dies with it.
- **Fix in the skills:** [agent-fleet §5][af] "An agent never ends its turn while its own background job is running":
  block in the foreground on the log until the verdict prints.
- **Evidence:** 5 implementers + 1 lander.
- **When:** 2026-09-22.

### Cloud sessions differ in hooks, repos and billing
- **Problem:**
  - A session with two repositories attached loaded no repo hooks ("Found 0 total hooks").
  - A remote-isolated subagent landed in the default environment.
  - Routine-launched sessions billed the plan while a promotional credit sat unused.
  - `claude --cloud` looked hung and was aborted twice.
- **Root cause:** a multi-repo session starts in the parent directory; billing depends on the launch surface; and the
  CLI prints nothing for a minute or more.
- **Fix in the skills:** [claude-cloud-sessions][ccs] §3, §5, §7 (a user-level hook shim; billing measured per surface;
  a self-contained brief; push per N units). Eval 1.00 vs 0.00.
- **Evidence:** six routine runs (~$30) left the credit untouched; adjudication cost ~$0.34 per row.
- **When:** 2026-09-24.

### An agent that reads the wrong slice finishes "successfully"
- **Problem:** a writer returned 12 rows, none from its own 13-row input. The tally read 119 of 120, "one row dropped",
  and not "wrong by thirteen".
- **Root cause:** input slugs were not unique across lanes, so the agent found another lane's same-named file.
- **Fix in the skills:** partial. [agent-fleet §2][af] reconciles returns. The per-slug check that returned ids are a
  subset of the input ids is OPEN.
- **Evidence:** overlap 0, foreign 12.
- **When:** 2026-09-25.

### A workflow agent's last action must be its structured return
- **Problem:** three agents finished their work and ended on a report file or a summary message, which stranded their
  branches.
- **Fix in the skills:** [`rolling-wave.js`][wave] prompt ("YOUR LAST ACTION MUST BE THE StructuredOutput CALL").
- **Evidence:** 3 agents.
- **When:** ~2026-09-26.

### Workflow agents act on the session's latest message, so carry the authorization
- **Problem:** all nine implementers in a wave returned BLOCKED ("the user asked a LinkedIn question, not for the fix
  lane") and made no changes.
- **Root cause:** a workflow agent treats the session's most recent user message as its request. The agents were right
  to decline. "The fix is provenance, not persuasion."
- **Fix in the skills:** [agent-fleet §2][af]; the `authorization` argument of [`rolling-wave.js`][wave] quotes the
  human's direction verbatim in every prompt.
- **Evidence:** 9 of 9 refused before the change; 0 after.
- **When:** 2026-09-27.

### A dead agent deadlocked the rolling wave for 16 hours
- **Problem:** a workflow showed "running" for 16 h 16 m with nothing left to do.
- **Root cause:** an agent that dies on an API error makes `agent()` REJECT, not return null. The rejection escaped the
  worker with its slot still counted and no wake-up sent, so a same-file successor waited forever.
- **Fix in the skills:** [`rolling-wave.js`][wave] v1.8.2 (a rejection is recorded `NO-RESULT`; the slot and wake-up are
  released in `finally`).
- **Evidence:** a simulation with a rejecting agent hangs the old script and finishes with the new one.
- **When:** 2026-09-27 (incident); 2026-09-28 (fix).

### Transcript file times are no liveness signal (CORRECTED)
- **Problem:** a watchdog saw 20+ minutes of silence from a working fleet and called it dead.
- **Root cause:**
  - The first explanation, "transcripts are written lazily", was WRONG, and it was published in v1.7.0.
  - Measured causes: (1) a tool call writes nothing until it returns, so each ~580-second gate wait is a silence;
    (2) clearing the orchestrating session moves the running agents' transcripts to the new session's directory,
    while the workflow journal stays in the old one.
- **Fix in the skills:** [agent-fleet §5][af] (judge liveness by processes and the journal; a transcript ending on an
  open tool call is live up to that tool's maximum duration). Corrected in v1.8.1.
- **Evidence:** 5–7 silences of 582–585 s per agent in one run.
- **When:** 2026-09-28.

### A model call can hang, and nothing notices
- **Problem:** an implementer finished and gated its work, then never produced another token. A second agent hung
  78 minutes later. Nothing noticed for 90 minutes, and the final train waited on them. The owner: "We just wasted
  1.5 hours waiting for nothing."
- **Root cause:** nothing watched; a workflow cannot stop one of its own agents; and nothing gave up. Quota was ruled
  out. Why the model call hung is not established.
- **Fix in the skills:** [`stall_watch.py`][stall] ([agent-fleet §5][af]) runs beside every workflow and exits when an
  agent is silent over 10 min waiting on the model or over 12 min in one tool call. `implementerCeilingMin` (default
  240) in [`rolling-wave.js`][wave] records `STALLED` and moves on.
- **Evidence:** over 150 transcripts (~27,800 silences), model waits were under 30 s at p99 and 94 s at p99.9, and
  tool calls peaked at 585 s. The first live run flagged exactly the two hung agents.
- **When:** 2026-09-28.

### A permission classifier can block an agent editing orchestration files
- **Problem:** a lander could not include a workflow-template fix.
- **Root cause:** the harness's classifier treats an agent editing the orchestration template as self-modification.
- **Fix in the skills:** not yet. WORKAROUND: the owner authorized landing it directly.
- **Evidence:** 1 incident.
- **When:** 2026-09-28.

---

## Model per role

### Specialists from different angles break tunnel vision
- **Problem:** the agent was stuck in a combinatorial design space (grammar ordering × semantics × parser performance).
- **Fix in the skills:** partial. [review][rv] adds specialist [`agents/`](agents/). Two agents with different expertise
  proposed a design outside the stuck agent's frame.
- **Evidence:** the blocked program compiled in under 15 s and passed 111 tests afterwards.
- **When:** 2026-03-29 to 2026-03-31.

### Subagents silently inherit the main loop's model
- **Problem:** subagents burned 40 % of a session window in 6 minutes.
- **Root cause:** subagents and workflow agents inherit the main loop's (premium) model unless told otherwise.
- **Fix in the skills:** [agent-fleet §11][af] "The model follows the ROLE"; each role template sets `model`
  ([automating-agent-guardrails §2][ag]).
- **Evidence:** 40 % in 6 minutes.
- **When:** 2026-09-01.

### Reduce effort only where a non-reduced step checks the output
- **Problem:** every agent ran at the session's effort level.
- **Fix in the skills:** [automating-agent-guardrails §2][ag]: the refuter (the quality gate) keeps the highest effort,
  and mechanical roles get lower effort, with a rollback trigger: move a role back up if its refuter overturn rate rises.
- **Evidence:** the owner's measured curves: for retrieval and verification, medium ≈ high at 70–85 % of the cost; for
  long-horizon coding, medium gives up ~2 points for half the cost; hard reasoning has no free cut. Lower effort on
  mechanical pipeline stages is still an open owner question.
- **When:** 2026-09-04; 2026-09-25.

### A dated model pin silently held the fleet on an old model
- **Problem:** a new model generation shipped, and the fleet stayed on the old one.
- **Root cause:** an environment pin with a dated model id overrode the alias every workflow passed.
- **Fix in the skills:** not yet. Pin by alias, never a dated id, and treat every number fitted on the old model (the
  cost law, turn caps, tokens per meter point) as a hypothesis to re-measure.
- **Evidence:** a probe agent reported the new model id after the change.
- **When:** 2026-09-22.

### A 1-hour prompt cache for roles that wait on gates
- **Problem:** gate-blocked agents re-wrote their whole context to the cache after every wait longer than 5 minutes.
- **Root cause:** the default 5-minute cache TTL.
- **Fix in the skills:** [automating-agent-guardrails §2][ag] (`cacheTtl: 1h` on every role that waits).
- **Evidence:** unmeasured ([Open problems](#open-problems)).
- **When:** 2026-09-25.

### A per-call model overrides the role, so mechanical roles ran on the top tier
- **Problem:** a practice said "pass the top model on every agent". Since a per-call `model` overrides a role's
  frontmatter, the cheap chore role ran on the top tier. A 30-lookup code-site pass that judged nothing ran on the
  top-tier analyst.
- **Root cause:** the practice text contradicted the role configuration, and override precedence decided.
- **Fix in the skills:** [agent-fleet §11][af]; [automating-agent-guardrails §2][ag] and the read-only
  [`locator.md`](skills/automating-agent-guardrails/templates/agents/locator.md) template. A mechanical agent returns
  `NEEDS-ESCALATION` on a judgment call.
- **Evidence:** ~165 k tokens for 30 lookups. The saving from the split is unmeasured.
- **When:** 2026-09-27.

### A premium model's "separate limit" is a ceiling on the same pool (REJECTED pilot)
- **Problem:** the usage page's "separate weekly limit" row for a premium model was read as free extra capacity, and a
  pilot moving refuters onto it was proposed.
- **Root cause:** misreading. The plan's terms say the premium model draws from the regular weekly limit faster, and
  its row only caps its share.
- **Fix in the skills:** [agent-fleet §11][af] "A premium model is an exception, never a default". The pilot was
  rejected; the premium model is an escalation for an item the everyday tier failed twice.
- **Evidence:** the plan's help article (quoted); the owner's estimate of ~2× cost per unit.
- **When:** 2026-09-27.

---

## Measurement discipline

### Ask the source's own witness, not your copy of it
- **Problem:** the rule-count denominator moved 2,974 → 3,210 → 3,949 → 2,314 → 3,247 as extractor bugs were found. The
  first version had been validated only against the Markdown it came from.
- **Root cause:** extractor bugs, plus validating a copy against itself. "Inflation reads as thoroughness."
- **Fix in the skills:** [spec-compliance-audit §1][sca] (decompose the standard into a rule catalog). Validating
  completeness against the source's own table of contents and PDF outline, made fatal, is not stated.
- **Evidence:** at one point 601 phantom rules sat in one block and 896 rules were lost.
- **When:** 2026-07-26.

### Green gates are not evidence when they cannot see what changed
- **Problem:** the owner reported that the transcribed standard was "basically unintelligible" as rendered, which had
  been true for a long time, with every gate green.
- **Root cause:** "every gate reads the file as TEXT and the reader reads it as a RENDERED PAGE". A consistency check
  proved agreement with the generator, so a blind spot they shared agreed with itself.
- **Fix in the skills:** [test-gate][tg] "then ask what its scope leaves out"; [engineering-standards §5][es] "Compare
  against an independent implementation, not a round trip of your own".
- **Evidence:** four occurrences in one session, all under green gates. 4,161 rule labels were parsed as list items.
- **When:** 2026-07-26 to 2026-07-28.

### Measure before scheduling performance work; record the null results
- **Problem:** a test class was named "the obvious suspect" for a slow suite before anything was measured. A predicted
  ~10× speed-up from splitting the serial test class, and a speed-up from regrouping a guard's work, were both expected.
- **Root cause:** plausible arithmetic, not observation. "Sum-of-test-time ÷ wall measures COLLECTION concurrency, not
  core utilization."
- **Fix in the skills:** partial. [`dotnet-engineering`](skills/dotnet-engineering/SKILL.md) benchmarking. The rule
  "run as concurrently as correctness allows, and reduce the WORK rather than the parallelism" is not stated.
- **Evidence:**
  - The real bottleneck was a different class: 2,015 tests, 780 s on one thread.
  - NULL: the split gave −17 %, not ~10×.
  - NULL: the regrouping measured 564 s before and 598 s after, and was kept for correctness only.
  - REVERSED: a cap on compile fan-out was withdrawn.
- **When:** 2026-08-03; 2026-09-02.

### Rebalance CI shards on measured minutes, with a population guard
- **Problem:** the CI verdict took 30 minutes, and test-case count was a bad proxy for shard balance, twice.
- **Root cause:** a single test can cost ~13× the others.
- **Fix in the skills:** not yet. Use shards with an "everything else" exclusion shard and a guard that shard totals sum
  exactly to discovery. CORRECTION by the owner: larger CI runners are not cost-neutral when the baseline is the free
  tier.
- **Evidence:** from ~30 min to measured runs of 23.3 and then 21.4 min.
- **When:** 2026-08-07.

### Three accountings of "tokens", and only one is what the quota consumes
- **Problem:** three proposals quoted cost per row as 188 k, 9.66 M and 99 k tokens.
- **Root cause:** harness-reported subagent tokens exclude cache reads, and cache reads are what the usage limit
  consumes.
- **Fix in the skills:** partial. [agent-fleet §11][af] measures "cache-read per item". Per-role cost now comes from
  local telemetry ([automating-agent-guardrails §4][ag]).
- **Evidence:** cache reads were 97.6 % of all tokens; the harness-visible figure was ~2.4 % of the true one.
- **When:** 2026-09-04.

### A flattering headline rate mis-plans the campaign
- **Problem:** a headline of 138 rows per day implied about 22 days to finish.
- **Root cause:** 42 % of the closed rows came from three one-off landings that cannot repeat.
- **Fix in the skills:** not yet. Publish the earned rate, plan for zero free wins, and name the softest number in any
  projection.
- **Evidence:** 720 rows over 18 landings (mean 40, median 14, four closing zero). The earned rate was 69 rows/day.
- **When:** 2026-09-04.

### Keep every experiment as a frozen record, null results included
- **Problem:** measurements lived in transcripts, which are pruned, and refuted claims lingered in edited evidence files.
- **Root cause:** there was no durable format for experiments.
- **Fix in the skills:** not yet. The project writes one record per experiment (question, hypothesis, design, every
  attempt, result with its interval, limits, decision) plus the raw per-agent data. A record is never edited; a later
  refutation gets its own record and a `superseded_by` marker. This document draws on those records.
- **Evidence:** n/a. Backfilling earlier measurements is still pending.
- **When:** 2026-09-06 (frozen-evidence rule); 2026-09-28 (experiment records).

### A config check that reads the file you wrote, not the one the tool honors
- **Problem:** after cost telemetry was adopted, nothing was ever exported, while the readiness check reported it "OK".
- **Root cause:** Claude Code IGNORES the telemetry-enabling variables in a project's `.claude/settings.local.json`
  (project settings may only turn telemetry off), and the check read that same file.
- **Fix in the skills:** [automating-agent-guardrails §4][ag] (1.10.2).
  - `readiness_check.py --enable-telemetry` now writes the variables to the user settings (`~/.claude/settings.json`).
  - The check counts only the user settings or the environment as "enabled", and flags a project file that sets the
    switch as dead configuration.
  - The skill had kept the old advice until this document's own consolidation surfaced the contradiction. Its
    self-test was also not hermetic: it inherited the machine's telemetry variable, so it was RED on any machine with
    telemetry on. That is fixed too.
- **Evidence:** Claude Code's own system diagnostics message. The self-test now covers the ignored-project-setting
  case.
- **When:** 2026-09-25 (project); 2026-09-28 (skill).

### A pilot's effect can fail to replicate
- **Problem:** a pilot of the stamped handoff (n = 4 per arm, one branch) showed −33 % tokens, and the owner asked for
  "more definitive statistics" before a public claim.
- **Root cause:** one branch whose uncovered commit was large and obvious.
- **Fix in the skills:** the public claim uses only the scale result ([agent-fleet §4][af]).
- **Evidence:** 88 agents; paired exact Wilcoxon, a 10,000-sample bootstrap, and Fisher's exact test. The STALE-case
  saving was NULL (p = 0.58), and the CURRENT case held (tokens ×0.69, p = 0.010).
- **When:** 2026-09-28.

### A sandbox that cannot reproduce the failure cannot measure the fix
- **Problem:** an A/B of the status-guard hook was meant to show that it lowers the stale-handoff rate.
- **Root cause:** sandbox tasks ran 13–58 turns against 100–220 in production, and produced 0 stale handoffs in 40
  sessions, so the benefit arm had nothing to prevent. A first toy task was too easy even for the rule-only arm. A
  mis-registered hook (`if` filter) mostly never ran, and a runner bug (a relative `--settings` path) kept one arm from
  starting. Hooks load at session start, so the arms had to be separate headless sessions, not subagents.
- **Fix in the skills:** the cost arm still decided (the per-command reminder was rejected at ~+20 %), and only the
  hooks that fire on a violation were deployed ([`status_guard.py`][sguard]). The production measurement is open.
- **Evidence:** 0 of 40 stale. The hypothesis that the template wording caused staleness was not supported.
- **When:** 2026-09-28.

---

## Open problems

Measured, or at least observed, but not solved:

1. **Stale handoffs in production.** 21 % of finished branches (3 of 14) ended stale before the status guard. The
   sandbox could not reproduce the failure, so the guard's effect in production is unmeasured.
2. **Orientation savings.** `orient.py`'s ~15 % token saving per implementer is an estimate that has never been
   verified.
3. **The rolling wave and same-file successors** have never been A/B-tested against the wave barrier.
4. **The checkpoint file does not travel with the branch.** It is git-ignored, so it survives only while its worktree
   directory does. In one audit, 5 of 29 agent branches had lost their worktree (checkpoint unrecoverable), and 13 of 28
   live worktrees had no checkpoint at all. The stamp detects staleness; it does not fix survival.
5. **Implementer gate filters are guessed from names.** A train dropped two groups on whole-suite reds their filtered
   gates never ran, and finishing them cost ~1.17 M tokens. Test impact analysis (derive the filter from changed files,
   falling back to the whole suite) is approved but not built. A related gap: the per-commit gate reaches the only
   independent external oracle only if an implementer types its filter term.
6. **Why model calls hang** is not known. The watchdog detects hangs; it does not prevent them.
7. **Intermittent reds without a mechanism.** False "regressions" under full parallel fan-out persist ("the rate is not
   falling"), and a few one-off reds have no captured message.
8. **Sibling agents share one scratch directory.** Per-agent subdirectories are proposed.
9. **An agent can return another slice's results.** The per-slug check that returned ids are a subset of the input ids
   is proposed.
10. **The citation checker fails both open and closed** at region boundaries and on markup. A fix is proposed.
11. **Positive-only evidence for "shall reject" rules.** About 120 rows were closed on tests that show legal input
    compiling but never exercise the rejection. The owner question is open.
12. **Unmeasured tunings:** the 1-hour cache TTL, the mechanical-role model split, and lower effort on mechanical
    stages.
13. **Quota calibration drifts** between 0.11 M and 0.66 M agent tokens per weekly-meter point, so every estimate needs
    the real meter.
14. **The build guard cannot map workflow-created worktrees to their agents**, so it treats them as the main tree.
15. **Backfilling pre-September measurements** into frozen experiment records is pending.

## Not yet in the skills

Lessons the project acted on that this repository does not yet carry, or carries only in part:

- Pacing to a weekly budget with a cumulative daily allowance. Reading the real usage meter instead of an extrapolated
  rate.
- Workflow run-id resume works only in the owning process; after a reboot, re-dispatch.
- "Workflow completed" can hide dead agents: read the failure list first.
- Dense keyword lists trip the API content filter: never let them transit the API.
- Name sanitization (a colon wrote an NTFS alternate data stream), and a promised-vs-globbed population check.
- Worktree-isolated agents cannot reach other worktrees: take branch content from the object store.
- Per-agent scratch subdirectories; lane-unique input slugs; refusing results outside an agent's input slice.
- A permission classifier can block edits to orchestration files.
- Branch worktrees from current main.
- Run the comprehensive battery in its own detached worktree.
- Pipelined landing, and a compiled-output cache for re-gates.
- Merge clusters one at a time with a build between each, and never add a cluster after the gate has started.
- Merge structured data (JSON, manifests) as sets by script, never textually.
- A conflict-marker assertion between staging and committing.
- Probing each OR'd filter term for its own population (dead vs inert terms).
- Letting CI finish before the next push when per-commit attribution matters.
- The lander reads each implementer's gate log itself, never the report's summary.
- Interpreter-stub resolution (a Store `python3` alias) that silently skips a gate.
- A registrar role that re-measures every lead before filing it.
- Re-probing stale work items on today's tree before fixing, and discharging them with evidence.
- Structured withdrawal fields in refuter output.
- Live-state documents are replaced, never appended to; memory hygiene checks.
- A generated status page instead of a hand-written one.
- One practices file with its reasons, rendered briefs, and a checker that fails a brief dropping a practice (the brief
  template covers only part of this).
- The bare-question protocol for owner decisions.
- Timestamps only from the clock.
- Pin models by alias, never a dated id, and re-measure fitted numbers after a model change.
- CI sharding on measured minutes with a population guard.
- Frozen experiment records, including null results.
- The external-state pattern (plan, log, memory, commit messages) and "durable instructions live in the repository".

[af]: skills/agent-fleet/SKILL.md
[ag]: skills/automating-agent-guardrails/SKILL.md
[tg]: skills/test-gate/SKILL.md
[so]: skills/spec-oracle/SKILL.md
[sca]: skills/spec-compliance-audit/SKILL.md
[rv]: skills/review/SKILL.md
[es]: skills/engineering-standards/SKILL.md
[va]: skills/variant-analysis/SKILL.md
[ccs]: skills/claude-cloud-sessions/SKILL.md
[brief]: skills/agent-fleet/references/brief-template.md
[orient]: skills/agent-fleet/references/orient.py
[clusters]: skills/agent-fleet/references/fix_clusters.py
[delta]: skills/agent-fleet/references/status_delta.py
[sguard]: skills/agent-fleet/references/status_guard.py
[stall]: skills/agent-fleet/references/stall_watch.py
[wave]: skills/agent-fleet/references/rolling-wave.js
[rules]: skills/automating-agent-guardrails/templates/guard-rules.json
[guard]: skills/automating-agent-guardrails/scripts/guard_commands.py
[ro]: skills/automating-agent-guardrails/scripts/readonly_guard.py
