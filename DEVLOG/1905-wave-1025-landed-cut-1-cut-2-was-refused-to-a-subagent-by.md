## Entry 1905 — 2026-10-07 02:40 PDT — Wave 1025 landed Cut 1; Cut 2 was refused to a subagent by the permission classifier, so the attended session removed the trees on a branch and dispatched a finisher (wave 1027); PB2142 filed

**Train 1025** (Entry 1904, `86f33fd7d`): PB2108 and PB2109 landed as one implementer's work — no greenfield test, script or
CI job runs the legacy engine (95 files, +819/−1,570); the NIST leg still reads 364 programs through `cobol`; PB2121 filed
(orphaned per-case goldens are undetectable once the bake re-sweep is gone). Wave 1025 cost 742,667 subagent tokens.

**Group B (PB2110, Cut 2) returned BLOCKED, correctly.** The Claude Code auto-mode permission classifier refused the
implementer's `git rm -r` of the five legacy trees as "Irreversible Local Destruction" and the agent stopped rather than route
around it. The attended session performed that one step itself on branch `pb2110-cut2` (`36441a24c`: 231 files, 74,493
deletions, the five solution entries removed, a stamped STATUS.md) and dispatched wave 1027, one Opus finisher that merges
the branch and finishes from the blocked agent's measured site list (`E:\COBOL-coord\scratch\reports\w1025b-PB2110-report.md`).
The archive tag `legacy-byte-engine-final` goes on main's last pre-delete commit after that train lands.

**PB2142 (owner decision, `status: owner`).** Every Delete wave (PB2119) and large move will meet the same classifier: either a
scoped permission rule for `git rm -r` under `.claude/worktrees/`, or the attended-session step becomes the Delete wave's
written rule and the loop never runs one unattended. Option 2 is what happens until decided.

**Meter at the wave-1027 dispatch** (read on the usage page, recorded with `budget.py --record`): session 5 % (window resets
06:00), weekly 60 %, Fable 29 %; the cap is 85 % (R69 §6). Wave 1026's four implementers (census, oracle, performance
baseline, cluster planning) are in flight with real progress on their branches.

