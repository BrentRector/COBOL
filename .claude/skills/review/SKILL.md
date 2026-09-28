---
name: review
description: Use when asked for a code review, architecture review, performance review, or duplication/efficiency analysis - the four review dimensions the owner requires, run as parallel agents with project-specific criteria and adversarial verification.
---

> ⛔ **BASE SKILL FIRST.** Invoke `brent-tools:review` (Skill tool) before reading on — its steps (target, free
> checks, triage, calibration, parallel dimension agents + specialist agents, adversarial verification, sibling sweep,
> report format) are the procedure. If the plugin is not loaded (a cloud session receives no project marketplace),
> Read `tools/claude-skills/skills/review/SKILL.md` instead (`git submodule update --init tools/claude-skills` if the
> path is missing). THEN apply this overlay: the project's review rules, which the base's "Project hooks" section
> says to honor as hard criteria. It wins on conflict. Pinned: **brent-tools 1.13.0** (`tools/claude-skills`,
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

## Verify, report, scale

The base's Step 6 (adversarial verification), Step 8 (report) and Scale apply as written. Here, additionally:

- **Findings become tracked work in `kb/Work/`** — one note per surviving defect (CLAUDE.md rule 8), never prose
  that evaporates and never a second list. `docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md` is only a pointer now.
- Use `ReportFindings` when the host asks for it.
- The comprehensive whole-source pass is owner-scheduled for the conformance milestone.
