---
name: review
description: Use when asked for a code review, architecture review, performance review, or duplication/efficiency analysis - the four review dimensions the owner requires, run as parallel agents with project-specific criteria and adversarial verification.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:review` (Skill tool) before reading on — its steps (target, free
> checks, triage, calibration, parallel dimension agents + specialist agents, adversarial verification, sibling sweep,
> report format) are the procedure. If the plugin is not loaded (a cloud session receives no project marketplace),
> Read `tools/claude-skills/skills/review/SKILL.md` instead (`git submodule update --init tools/claude-skills` if the
> path is missing). THEN apply this overlay: the project's review rules, which the base's "Project hooks" section
> says to honor as hard criteria. It wins on conflict. Pinned: **brent-tools 1.20.0** (`tools/claude-skills`,
> kb/Work/PB1699). Project context: scale=long-lived, consequence=high.

# Review

The owner requires **four review dimensions** because this is a commercial product with a decade-plus lifetime
(`PROMPT.md` §4, memory `project_required_reviews`). They are continuous criteria on every change AND a
comprehensive pass once the design settles.

Generic review tooling does not know this project's rules — that a `decimal` is banned, that a bound node carrying
C# text is a seam violation, that a fix without an ISO citation is incomplete. Review against THESE criteria.

## First: run the mechanical checks — they are free

```
python scripts/semgrep/verify.py
```

Six architectural invariants from `COBOLNET_DESIGN.md` §1.2 are already mechanized (banned numeric types, persisted
byte storage, silent TODOs, raw diagnostic literals, backend-neutrality). Anything it catches needs no human
judgment. Do not spend review effort on what a pattern already covers — and if the review finds a defect class a
rule COULD catch, add the rule (`scripts/semgrep/invariants.yml`) rather than only reporting the instance.

## The four dimensions

Run them as **parallel agents, one per dimension** — they are independent and each needs a different lens. Give each
the diff (or the subsystem) plus the criteria below.

### 1. Architecture
Folder/file layout and naming · single-responsibility, no god classes · clean phase boundaries (no semantics in the
parser, no codegen in the binder, no runtime logic in compile-time structures) · no cross-layer write-back, the
emitter never mutates the binder's model · one canonical mechanism per job, no parallel second · backend
neutrality: no C# text, Roslyn syntax, mangled identifier or format literal in a bound node or `Place`.

### 2. Full code review
Run `python scripts/spec/drift_rules.py <each changed file>` and check the change against every SPECIFIC rule it prints (a diff that breaks one is a finding even if its test was edited to pass) · Correctness against the CITED ISO rule — does the code implement the rule it names, or a convenient paraphrase? ·
sibling/paired functions agree · error handling: loud failure, never a silent no-op or swallowed exception ·
comment and doc accuracy (a comment that lies is worse than none) · idiomatic modern C# · every implemented rule
carries its exact §/GR · ⛔ **no fixed time limit in a test**: a test that compares a stopwatch reading
(`Elapsed`, `TotalSeconds`, `ElapsedMilliseconds`) against a ceiling is a finding — it measures the CI host, not the
code, and a loaded runner turns it red with no regression (`DeepNestingTests` failed twice that way; kb/Work PB1590).
Require the property instead: a work count through a test seam, an observed effect instead of a real sleep, completion,
or a growth RATIO of two readings in the same run. `NoWallClockAssertionDriftTests` enforces it mechanically.

### 3. Performance
Hot paths and allocation behavior · data-structure fit · compile throughput · `Span<T>`/`ReadOnlySpan<T>` where a
copy is being made · anything O(n²) over the corpus or the symbol table.

### 4. Duplication and efficiency
Repeated logic · two mechanisms doing one job · redundant recomputation of something the binder already resolved ·
anything a single canonical implementation should absorb. This is the dimension most often skipped and the one the
owner named explicitly.

## The branch form — one DONE branch, one reviewer, the findings file (kb/Work PB3027)

Every finished fix-lane branch is reviewed ON THE BRANCH before it may land (DESIGN-test-build-ci.md §3.17.2 step 2;
MANDATORY-PRACTICES I10): the rolling wave dispatches ONE read-only `cobol-reviewer` when an implementer returns DONE,
and a session dispatches one by hand for a branch it finished itself or one `python scripts/prune_worktrees.py --brief`
lists WAITING FOR REVIEW. The landing step never reviews and never fixes. The prompt gives the branch, its worktree, its
head, the implementer's report, the findings file (`<scratch>\review-w<wave>-<letter>.jsonl`; any path without a
space for a hand dispatch), the reviewer label and the round. Run every command against the branch's worktree
(`git -C <worktree> …`, `python <worktree>\scripts\…`) and name the tool `RF` below for
`python <worktree>\scripts\orchestrator\review_findings.py`:

1. `RF start <file> --round <k> --branch <branch> --head <head> --reviewer <label>` — first, before reading the diff.
2. **The pass**, by yourself (no sub-agents: one branch's diff is one reviewer's work): `git -C <worktree> diff
   origin/main...<head>` and the code around each hunk; the **full code review** dimension above in full —
   `python scripts/spec/drift_rules.py <each changed file>` and every SPECIFIC rule it prints, correctness against the
   CITED rule, the sibling arms, loud failure, comment accuracy, **no fixed time limit in a test**,
   `python scripts/spec/cite.py --check <clause> "<text>"` on every citation the diff adds; then the other three
   dimensions where the diff warrants them (a new type or file: architecture and duplication; a hot path: performance).
   The base skill's Step 6 holds for each candidate: try to REFUTE it, running its scenario when that is cheap; an
   uncertain candidate is dropped. A finding the implementer's report already answers with a reason you accept is not
   one.
3. Each surviving finding, the moment it survives: `RF finding <file> --round <k> '<one JSON object>'` with `file`,
   `line` (an integer), `category`, `summary` and `failure_scenario` (concrete inputs → the wrong result); `severity`
   and `fix` are welcome. The tool numbers it `R<k>-<n>`; the finisher answers it by that id.
4. `RF verdict <file> --round <k>` — CLEAN when the round recorded no finding, else FINDINGS n, computed from the file.
5. `RF stamp <report> --findings <file>` — writes `**Review:** CLEAN <label> <head>` (or `FINDINGS <n> <label> <head>
   <file>`) under the report's Status line. CLEAN at the branch's tip is what lists it WAITING TO LAND
   (`prune_worktrees.py`); FINDINGS sends it to a finisher: the wave's review finisher, or the next wave's planner
   (`plan_wave.py`). The stamp REFUSES a report that is not finished (`plan_wave.Report.finished`: a status line saying
   DONE, or DISCHARGED with a HEAD past its base; kb/Work PB3181): return verdict STOPPED with its refusal in the
   summary rather than edit the implementer's report. Return the verdict, the count, the head and the stamp line.

A RE-REVIEW (round k > 1) reviews the review finisher's branch: `RF show <file> --round <k-1>` lists the earlier
findings and the report's `## Review findings` section answers each by id (fixed in a commit, or `no_change_needed` with
the reason). Raise again only a finding neither fixed nor answered with a reason you accept, and any new defect the fix
introduced. `RF summary <scratch>` measures the step: findings per branch, the re-review rate, reviewer minutes.

## Verify, report, scale

The base's Step 6 (adversarial verification), Step 8 (report) and Scale apply as written. Here, additionally:

- **Findings become tracked work in `kb/Work/`** — one note per surviving defect (CLAUDE.md rule 8), never prose
  that evaporates and never a second list. (The branch form's findings file is not a list of open work: it is the
  branch's own review record, answered on the branch before it lands.) `docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md` is only a pointer now.
- Use `ReportFindings` when the host asks for it.
- The comprehensive whole-source pass is owner-scheduled for the conformance milestone.
