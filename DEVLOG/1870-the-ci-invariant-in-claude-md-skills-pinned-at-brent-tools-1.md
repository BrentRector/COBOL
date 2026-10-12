## Entry 1870 — 2026-10-04 15:25 PDT — The CI invariant in CLAUDE.md; skills pinned at brent-tools 1.17.0; the CI Guard analysis (PB1982); ledger after group Y (GAP 281)

The owner's rule, now in `CLAUDE.md` (Testing): GitHub CI must never fail when the same code was tested locally by the same
processes; a CI red is also a defect in the local gates, fixed in the same change (kb/Work PB1957 carries the survey and the planned
drift test mapping every CI job to a local leg). The public skills repo released v1.17.0 (the owner pushed it): the lander fixes a
confirmed review finding in the train, a pin-and-declare section in `cross-platform`, per-group model in the rolling wave, an
orchestrator-loop reference; this repo's submodule moved to it and the four "Pinned: brent-tools" mentions (gate, review,
spec-lookup, workstream) and PB1699 follow, which `check_practices.py` enforces. PB1982 records why every code-change CI run takes
9 to 13 minutes: the Guard job is the critical path (408 to 750 s over 13 runs, bimodal by runner host, 661 s of it the 376-program NIST
suite at JOBS=4 in the slow run), with options to profile first. An earlier answer in the same session had used one fast run and
said the opposite; the corrected data is in the note. The ledger was refreshed after group Y landed alone (entry 1869, 122 trend
points).

**Files:** `CLAUDE.md`, `kb/Work/PB1957.md`, `kb/Work/PB1982.md`, `kb/Work/PB1699.md`, `tools/claude-skills` (pin),
`.claude/skills/{gate,review,spec-lookup,workstream}/SKILL.md`, `docs/rearchitecture/evidence/ledger-trend.json`,
`docs/rearchitecture/evidence/ledger-in-flight.md`. No code changed, no diagnostic code used.

