---
name: session-start
description: Use at the start of every WiseOwl COBOL session, and whenever asked "where are we", "what is next", or "what is the current state" - reads the one live-state SSOT, runs the mechanical probe, and confirms the gate baseline before any code change.
---

# Session start

Live state is **computed, never remembered**. Memory and any status line written in a doc may be stale; these three
steps are not.

## 1. Ask the work register what to do — `kb/Work/`

⛔ **THE WORKLIST IS `kb/Work/`, NOT plan §0, AND NOT ANY OTHER FILE (CLAUDE.md rule 8).**

```
python scripts/spec/work.py next     # the ranked list; session-probe prints it too
python scripts/spec/work.py check    # the register is well-formed
```

`kb/Work.base` is the same data, sortable: **Fix next** · **Blocked** · **Open but nobody gets a wrong answer** ·
**Analyses (§11)** · **Everything**. `Fix next` = `not landed AND (wrong-answer OR crashes) AND not blocked` —
ranked by what a defect DOES to a user's program, never by its severity label. PB24 and PB39 are both `[MAJOR]`
and only one of them returns a wrong answer.

⛔ **KEEP IT CURRENT IN THE SAME CHANGE SET AS THE WORK.** A landed fix flips its note's `status` in the commit
that lands it. A newly found defect becomes a `kb/Work/` note *before* it becomes a DEVLOG paragraph. **Do not
start a list anywhere else** — five registers accumulated by 2026-08-04 and three each claimed to be canonical.

## 2. Read plan §0 — live state, gates and owner decisions

`docs/COBOLNET_REARCHITECTURE_PLAN.md` §0. It owns: where we are, the gates, the open GAPs, owner decisions and
the campaign narrative. ⛔ **It does NOT own a worklist — `kb/Work/` does, and §0 must never regrow one.** **Trust §0 over any status written anywhere else, including memory and this skill.**

## 3. Run the probe — never hand-read what a script computes

```
pwsh -NoProfile -File scripts/session-probe.ps1
```

Reports branch · dirty/unpushed · next-free diagnostic code · VCR todo count · corpus counts · inventory GAP.

Reading its output:
- **`⚠ UNEXPECTED BRANCH`** — expected is `phase-14` or `main`. Stop and ask before working on another.
- **`⚠ DIRTY TREE`** — commit or explain before proceeding. Never start new work on top of unexplained changes.
- **`diag`** — claim the next diagnostic code from `next free`, never by reading a list. `src-grep max >= catalog max`
  is the expected steady state; **catalog above src is the real anomaly** (an orphan descriptor) — reconcile it
  before allocating.
- **`invent`** — the traceability inventory GAP count is the P14 burn-down metric. "not built yet" means the
  denominator does not exist and no conformance percentage can be quoted.

## 3b. Act on the TOOLING block (kb/Work R49) — the same hook prints it after the probe

`scripts/hooks/tooling_check.py` checks every adopted capability instead of assuming it: the Workflow opt-in, the
role agents (`.claude/agents/`, selected with `agentType`), the guard hook, the telemetry sink, the C# LSP and the
weekly `/skill-doctor` review. Each line is `OK`, `REPAIRED` (fixed automatically, no permission needed), `N/A`, or
**`ASK-OWNER`** — ⛔ **ask the owner about every `ASK-OWNER` line NOW, one bare AskUserQuestion each; never skip one**
(owner 2026-09-25: permission-gated features must trigger a query, not silently go unused). Rerun it by hand with
`python scripts/hooks/tooling_check.py`.

## 4. Confirm the battery baseline before changing code

Plan §0 "Gates" carries the last known-green counts. If the last session ended mid-batch, re-confirm green before
building on it — see the `gate` skill.

## Before the session ends

Update the `kb/Work/` notes you touched, update §0 (live state only), and add a `DEVLOG.md` entry per commit. Commit and push
every checkpoint.

## What NOT to do here

- Do not summarize the plan back to the owner unless asked. Read it and start working.
- Do not ask "what would you like to work on?" — `python scripts/spec/work.py next` is the answer.
