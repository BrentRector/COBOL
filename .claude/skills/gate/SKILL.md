---
name: gate
description: Use before every commit and before every merge to choose and run the correct test gate - the ordered whole-population gate per commit (build-local -Mode implementer - two legs, fail-fast, a gate slot; leg 1 only while the owner's batched-gating trial is set), the lander's one-leg whole population per train with per-cluster attribution of a red, and the comprehensive battery per accumulated batch - and to read the verdict without producing a false green.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:test-gate` (Skill tool) before reading on. If the plugin is not loaded
> (a cloud session receives no project marketplace), Read `tools/claude-skills/skills/test-gate/SKILL.md` instead
> (`git submodule update --init tools/claude-skills` if the path is missing). THEN apply this overlay: it carries only
> what is specific to WiseOwl COBOL — commands, paths, CI and push-main, the owner's dated decisions — and wins on
> conflict. Pinned: **brent-tools 1.19.1** (`tools/claude-skills`, kb/Work/PB1699).

# Gate

**Self-check first: which gate is this — an implementer's commit, a lander's train, or the batch's pre-merge?** Each
has one command below. No gate FILTERS any more: every gate runs the whole population, ORDERED so a likely red comes
first (owner, 2026-09-28, kb/Work PB1708: "order, don't skip"). What the owner corrected repeatedly was over-gating per
fix with the SERIAL suites (the battery, `guard.sh`); the ordered gate is not that — its fail-fast leg 1 returns a red
in minutes, and a gate slot caps how many run at once.

## Always first

```
dotnet build Cobol.Net.sln -c Debug
```

Build the **solution**, not one project. Building only the compiler project and then running `dotnet test
--no-build` tests whatever compiler was copied into the test bin at its LAST full build — a stale compiler. Local
goes green while CI fails.

## Per commit — the ordered gate (an implementer)

```
pwsh scripts/build-local.ps1 -Mode implementer -Priority BelowNormal *> <log>
```

The driver, `scripts/run_gate_legs.py` (DESIGN-test-build-ci.md §3.14; kb/Work PB1721), holds the worktree's GATE
LOCK (a second gate in the same worktree is refused), runs the audits (FAIL-FAST, before the slot and the build: a red
audit ends an implementer gate in seconds with `NO LEG RAN`, kb/Work PB2523 — run `python scripts/spec/drift_rules.py`
and the citation audits yourself before gating to avoid the round trip; one audit, `SELF-TESTS`, is every script
self-test under `scripts/`, discovered and run in parallel by `scripts/self_tests.py`, kb/Work PB2563 — run
`python scripts/self_tests.py` yourself after editing a script) and fetches the per-worktree GnuCOBOL corpus
when absent — both BEFORE it queues, because they read only the tree (kb/Work PB2524) — then takes a GATE SLOT
(`scripts/gate_slot.py`: at most N implementer gates build or test at once, repository-wide — `gate-slot: waiting, k
ahead` is the cap working), builds the solution and lists every discovered case of Conformance, Unit and
Characterization. The ORDER PLAN (`scripts/gate_plan.py`) puts in leg 1 the tests the change ADDS, the previous gate's
reds and the cheapest cases the change can reach — the tiers `impacted_tests.py` derives from an impact map, when one
exists — and everything else in leg 2, its collections LONGEST FIRST, timed from the shared timings store every gate
publishes to after its legs, so a fresh worktree is timed too (kb/Work PB2527). The test hosts run Server GC (kb/Work
PB2526). Both legs run the three assemblies concurrently. It is
FAIL-FAST: a red in leg 1 stops the gate `RED/INCOMPLETE` and names the remainder
(`TestResults/build-local/<run>/not-run-*.txt`). It is GREEN only when every leg ran, every assembly's population
equals its `--list-tests` (`scripts/test_population.py`) and every leg host ran this plan on these binaries.

- **The cap is ONE shared setting** (kb/Work PB2514): `python scripts/gate_slot.py set-cap N [--until ISO] [--why
  TEXT]` writes it into the slot directory every worktree shares; every gate re-reads it while it waits, so a raise
  reaches the queue at once, and an expired raise is the default (1) again with no one acting. `gate_slot.py status`
  prints the cap, the implementer scope, every held slot and the queue. No environment variable sets it.
- ⭐ **THE BATCHED-GATING TRIAL** (owner 2026-10-07 16:25 PDT, kb/Work PB2515, until Sat 2026-10-10 10:00 PDT): while
  the shared implementer scope is `leg1` (`gate_slot.py set-implementer-scope leg1 --until <ISO> --why <text>`, which
  must carry an expiry), `-Mode implementer` runs LEG 1 ONLY, names every leg-2 case NOT RUN and prints
  `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched-gating trial, PB2515): GREEN — … ===` (or `…: RED`). That is the
  implementer's done-state, and the Linux gate moves to the lander with the rest of the population. The lander's
  whole-population train gate is the population check for every cluster; a red there is ATTRIBUTED per cluster by
  re-running only the failing cases on each cluster alone, then fixed in the train or ejected to a finisher
  (`lander-train-brief.md` step 3, MANDATORY-PRACTICES L12), and every train is recorded with
  `scripts/orchestrator/train_measure.py record`. The CI invariant holds: the lander runs the whole population, the
  Linux gate and the oracle before every push. With the scope `whole` — the default, and after the expiry — the
  implementer gate is the whole population, as described above.
- **The impact map only ORDERS.** It is recorded ON DEMAND (`python scripts/spec/record_impact_map.py`, a detached
  worktree, ~25 min at BelowNormal, inside a gate slot), never per commit (kb/Work PB1709). With no map, or a stale
  one, the gate still runs everything — the order is plainer, never the population smaller.
- **An implementer is done only on GREEN**, and then runs CI's Linux legs under WSL (MANDATORY-PRACTICES I8,
  `bash scripts/linux-gate.sh --nice`).

## Per train — the lander

```
pwsh scripts/build-local.ps1 -Mode lander *> <log>
```

The implementer scope never applies to it. No plan, ONE leg — every red of every cluster in one run — no fail-fast and no slot, at Normal priority: the lander
never waits. Then `bash scripts/linux-gate.sh` (MANDATORY-PRACTICES L10) and `push-main.sh`.

**Do NOT run per commit:** the battery or the serial `scripts/guard.sh`.

## Per accumulated batch / pre-merge — comprehensive

- Full greenfield Conformance + full characterization
- The GnuCOBOL external differential, before AND after, diffing PER-CASE verdicts
- `scripts/guard-fast.sh` (parallel, the CLI-level NIST leg through `cobol`) — never the serial `guard.sh`

- **CI's own Linux leg — comprehensive is not comprehensive without it.** Everything above runs on ONE host
  (this Windows machine, Debug). The workflow's `ubuntu-26.04` jobs — `guard-fast.sh`'s NIST loop, the Linux
  conformance shards, Linux greenfield unit + characterization — and the Release build are verified by CI and by
  nothing else, so a test keyed on host ACL or file-lock semantics can be green here and red there. Read the
  verdict for the commit you pushed:
  `gh run list --branch main --commit <sha> --json databaseId,status,conclusion,displayTitle`, then
  `gh run view <id> --log-failed` on a red. `scripts/session-probe.ps1` prints the latest run's conclusion at
  session start, so a red main is never inferred from silence (`kb/Work/PB796`).
- ⛔ **AND IT IS NO LONGER OPTIONAL TO READ IT: `ci-gate` IS A REQUIRED STATUS CHECK ON `main`.** `ci-gate` is the
  workflow's terminal job — `if: always()`, `needs:` every other job, PASS only when each is `success` or
  `skipped` — and it is the ONE stable check name the protection can require (every matrix job's check name
  carries its shard AND its filter text, and every one of them is skipped on a docs-only push). Protection is
  `strict: false`, `enforce_admins: true`, force-pushes and deletions off. So the landing gate is a SCRIPT, not a
  habit: **`bash scripts/push-main.sh`** pushes HEAD to `ci/<short-sha>`, waits for `ci-gate` on that sha, and only
  then fast-forwards main — a bare `git push origin HEAD:main` of an unverified commit is refused by the server
  (owner decision 2026-09-06, question 23; `kb/Work/PB796`). It is idempotent: re-run it after a timeout.
- ⚠ **A docs-only push runs `changes` + `ci-gate` and nothing else** — seconds, not a matrix. `paths-ignore` was
  removed because a workflow that declines to START leaves a required per-commit check unsatisfiable; the same
  path list now lives in the `changes` job, one layer down, where it decides whether the MATRIX runs.

`guard-fast.sh` is **not** CI-complete: CI builds Release, local builds Debug. Any change whose semantics differ by
build configuration gets a local `-c Release` leg before push. Better: do not write configuration-divergent
compiler behavior.

SERIAL does not mean comprehensive-per-change. The grammar batch is serial (one shared parser, so implement
constructs one after another) but still gets ONE comprehensive gate for the whole batch.

## Reading the verdict — where false greens come from

The base's "Read the verdict line", "Flakes and attribution" and "A filter that matches nothing" sections apply as
written (full output to a file, no source edits while a gate runs, no flake without the test's name and an isolated
re-run). Here, additionally:

- **Never chain ANYTHING after a verdict command** (build, test, `build-local`, `push-main.sh`, battery) with `&&`,
  `||` or `;` — MANDATORY-PRACTICES P14, and the guard hook (`scripts/hooks/forbidden_commands.py` rule 5) BLOCKS
  it. To capture the status in the same call, append `; echo "EXIT=$?"`; read-only commands may follow that.
- `scripts/build-local.{ps1,sh}` prints ONE `=== BUILD-LOCAL GATE: ` verdict line — `GREEN`, `RED`,
  `RED/INCOMPLETE` (stopped after leg 1), `LEG 1 ONLY (batched-gating trial, PB2515): GREEN|RED` (the trial's leg 1
  ran, leg 2 is the lander's — never a whole-population GREEN), `BUILD FAILED` or `NOT RUN` (the lock was held, the
  shared gate settings are malformed, the population could not be listed) — block on that line, never on the exit code. The run directory it names holds
  every leg's full log, trx and identity record, the plan and `verdict.json` (timings, first red, slot).

## Read the failure before diagnosing it

Stack trace = emitter or runtime bug. `error CS####` = generated-code problem. A COBOLNET diagnostic = front-end
reject. Timeout = infinite loop. These are different problems; check which one you have first.
