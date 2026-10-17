---
name: land-a-fix
description: Use when landing a conformance fix-queue item or any spec-derived fix - the complete loop from spec citation through golden, manifest registration, gate, DEVLOG, commit and push. Invoke it when starting a CA/V queue item or any "fix this bug" task.
---

> **Generic base:** [`engineering-standards`](https://github.com/BrentRector/claude-skills/blob/main/skills/engineering-standards/SKILL.md) + [`dotnet-engineering`](https://github.com/BrentRector/claude-skills/blob/main/skills/dotnet-engineering/SKILL.md) + [`variant-analysis`](https://github.com/BrentRector/claude-skills/blob/main/skills/variant-analysis/SKILL.md) in [BrentRector/claude-skills](https://github.com/BrentRector/claude-skills) — the reusable discipline, with the owner's bar in its sibling `engineering-standards`. THIS skill is the WiseOwl COBOL application of it and wins on conflict.

# Land a fix

> ⛔ **ONE WORK REGISTER: `kb/Work/`.** `python scripts/spec/work.py next` answers "what now"; `kb/Work.base` is the view. **Keep it current in the SAME change set as the work**, and **never start a list, table, tracker or "remaining work" section anywhere else** — five registers accumulated by 2026-08-04 and three each claimed to be canonical (CLAUDE.md rule 8).


The work register is **`kb/Work/`** — one note per item, `kb/Work.base` is the view, and
`python scripts/spec/work.py next` prints the ranked list (session-probe shows it every session).

⛔ **WORK THE `Fix next` VIEW TOP-DOWN, NOT SEVERITY TOP-DOWN.** It ranks on what a defect DOES to a user's
program — `not landed AND (wrong-answer OR crashes) AND not blocked` — because severity cannot separate the
cases that matter: PB24 (`FUNCTION LENGTH` silently wrong) and PB39 (rule-id numbering, zero wrong answers) are
BOTH `[MAJOR]`, and a session picked PB39. Each item's forensic prose is in its own note body.

## 1. Derive before you read

Use the `spec-lookup` skill. Produce the §/GR, the expected behavior, and the edition applicability BEFORE reading
the implementation. Work items carry a candidate fix — **verify it against the spec text; some are false
positives.**

## 2. Scope it completely

The spec plus the subsystem deep-dive (`docs/COBOLNET_DESIGN.md` §0.5 indexes them) define the scope. Tests
verify; they never scope. Implement every SR and GR of the rule, not the leg the failing test happens to hit.

**Deferral is debt.** "Document as a known non-conformance", "stage loud", "schedule for a dedicated pass", or
"reject the legal construct" are all debt, and only an explicit owner decision. If the correct fix is genuinely
large, surface the effort as a bare question — do not pre-decide the deferral.

## 3. Fix the root cause, then sweep

**Before you edit a file, ask which drift rules govern it:** `python scripts/spec/drift_rules.py <path> …` (derived from every `*DriftTests` summary; the full index is a rendered view, `python scripts/spec/drift_rules.py --out <path>`, never committed). Read the SPECIFIC rules it prints and honor them in the change — a drift test is a structural rule the gate will enforce anyway, and learning it from a red gate costs a cycle.

No workaround, no shim, no relabeling a defect a "quirk". Never edit valid COBOL to dodge a compiler bug — if the
source is valid, the compiler is broken.

**Every bug is a pattern.** Grep for the sibling instances and fix them in the same pass. Paste the fresh grep;
never write "swept" without one.

## 4. Cite it in the code

The exact section AND rule number (`§14.9.24 GR4d`), not "per the spec".

## 5. Write the test in the conformance suite

- Expected value **computed from the spec**, never copied from another compiler or from observed output.
- A test is `conformance-suite/cases/<ISO clause>-<Name>/<id>.cob` opening with its metadata header (`id`, `rules`,
  `accept`, `source-format`, `files`; `conformance-suite/README.md`, 'Adding a test') and its `<id>.out`. There is
  no manifest: the generator reads the header, and the suite runner (`ConformanceSuiteTests`) runs every test.
- A program WiseOwl must refuse names no `accept` edition and gets its entry in
  `conformance-suite/wiseowl/diagnostics.json` (`refuse-at`, `expect`: the code or a message substring).
- A test of WiseOwl's own behaviour (documented non-support, a WiseOwl option) goes in `wiseowl/cases/`.

`python conformance-suite/harness/generate.py --check` refuses a malformed header, an undeclared file and any file
under the removed `tests/conformance/`; it is a gate audit that runs before the build.

If the fix gates a construct's `introducedIn`, run the edition-gate sweep — see the `new-construct` skill.

## 6. Gate, then commit

Use the `gate` skill. Read the verdict line, then commit as a separate call.

## 7. DEVLOG, commit, push

- DEVLOG: one file per entry (kb/Work PB3025). Write `DEVLOG/pending-<slug>.md`: the first line `## <Title>`
  (no number, no stamp), then the narrative — what changed, why, what broke, what was learned; log the failures
  too. The landing numbers and stamps it with `python scripts/devlog.py stamp <branch>` (the number from
  `alloc.py devlog`, the header completed as `## Entry NNNN — YYYY-MM-DD HH:MM TZ — Title`, one commit). One entry
  per LANDING ON MAIN, not per commit (owner 2026-10-08, kb/Work PB2605): the landing checks
  (`scripts/orchestrator/enqueue.py checks`, run by `enqueue.py land` before the pull request, kb/Work PB3023)
  refuse a landing whose range adds no new, stamped entry numbered above the tree it was stamped on, or more than one,
  or deletes one (exit 6, `scripts/devlog.py check`; kb/Work PB3123: a merge group is built on the groups queued ahead
  of it, so the number is never held to the group's base). No landing records or commits an arch-oracle baseline
  (kb/Work PB3107): a restructuring wave compares against its own base commit with `scripts/arch/compare_oracle.py`.
- Commit message: write it to a scratchpad file and use `git commit -F <file>`. Do NOT inline a PowerShell
  here-string in the Bash tool — it is POSIX sh and the markers leak into the message.
- **Update the item's `kb/Work/` note in the SAME commit** — `status: landed`, and `closes_rows:` naming the
  traceability-inventory rows this fix re-verdicted (`closes_rows: []` plus a `closes_rows_reason:` when it
  closed none). `ClosesRowsBackLinkDriftTests` is red on a landed `kind: defect` note that says neither, and on
  a row named as closed that the inventory still counts as a GAP (owner decision 2026-09-19, `kb/Work/PB245`).
  ⚠ There is no fix-queue LANDED header any more: `CONFORMANCE-FIX-QUEUE.md` was retired on 2026-08-04 and is
  a pointer. Update plan §0 only for what §0 owns — live state, gates, owner decisions, narrative; never a worklist.
- **Push.** Every checkpoint.
