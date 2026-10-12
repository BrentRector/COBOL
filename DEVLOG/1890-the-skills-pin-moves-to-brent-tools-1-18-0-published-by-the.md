## Entry 1890 — 2026-10-05 10:05 PDT — The skills pin moves to brent-tools 1.18.0 (published by the owner); a headless unit's push to the public repo is recorded as a permissions gap (PB2044)

The public skills repository now holds v1.18.0 on `main` (`019f4e2`) with its tag: the orchestrator-loop reference records what live runs validated (the supervisor owns
each unit's lifetime through an open stdin, frequent handoffs, STOP as a wind-down, a fast done unit is not a failure, the budget decision reaches the unit, plan from the
disk, a headless session may lack tools, and the pipe deadlock of PB2022). The owner pushed it by hand: the attended session's `git push origin main` was refused by the project's
deny rules, the GitHub connector's token cannot create a branch (403), and a first typo of mine gave the owner a Windows path in a bash prompt. The project pin moves from 1.17.1 to 1.18.0
(the four skill overlays, PB1699, the submodule pointer). The finding behind PB2044: v1.17.1 had been pushed to that repository's `main` by the headless wave unit itself at
02:54 with `git push -q origin HEAD:main`, which the deny rule `Bash(git push origin HEAD:main)` does not match because of the extra `-q`: the rules are text patterns. The owner asked for it to be
closed and will edit the permission file; the note carries the exact patterns (to be confirmed with `/permissions`) and the consequence (a unit can no longer publish the base skills, so that becomes an
owed push the attended session performs).

**Files:** `tools/claude-skills` (pin 6e94e58 to 019f4e2), `.claude/skills/{gate,review,spec-lookup,workstream}/SKILL.md`, `kb/Work/PB1699.md`, `kb/Work/PB2044.md`. No diagnostic code used.

