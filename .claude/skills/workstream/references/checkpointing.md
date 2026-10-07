# Workstream checkpointing, graceful stop and restart (SKILL.md §1, full text)

## 1. Checkpoint to disk, never to a transcript

| job | checkpoint | resume unit |
|---|---|---|
| implementer (worktree) | `git commit -m "WIP checkpoint: …"` on the worktree branch **after every mechanism and every gate**, plus `<worktree>/STATUS.md` with `DONE` (per mechanism) · `NEXT` (the exact next step) · `BLOCKED` · `GATE` (last verdict lines + filter) · batch-file path · codes used | one mechanism |
| workflow stage (adjudicate / refute / write / validate) | one JSON line per rule (or draft) appended to `<out>/<stage>-<slug>.jsonl` the moment it is decided; read-and-skip on start; the stage's whole result also written to `<out>/out-<slug>.json` | one rule |
| lander | a commit in ITS worktree after each numbered step + `STATUS.md` | one step |
| orchestrator | reports are FILES under the scratchpad (`reports/<cluster>-report.md`), briefs are FILES referenced by path; the conversation carries only pointers | — |

⭐ **GRACEFUL STOP — the quota-suspend signal (owner 2026-09-22: "attempt to not lose work due to a quota kill").**
Workflow agents cannot be messaged, so every dispatch prompt carries this line: *"Before starting each new step,
check for the owner's global stop `<coord>\scratch\STOP` and this fleet's own `<scratch>\STOP-<scope>`; if either
exists, checkpoint-commit, write STATUS.md NEXT, and return your structured result with status SPLIT."* At the
session or weekly soft stop the orchestrator creates ITS FLEET's `STOP-<scope>` (never the global one: another
session's agents obey that, kb/Work PB2483), waits for the returns, and only then `TaskStop`s stragglers and
WIP-commits their worktrees — so a quota kill never lands mid-step. Delete that `STOP-<scope>` before resuming.

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
