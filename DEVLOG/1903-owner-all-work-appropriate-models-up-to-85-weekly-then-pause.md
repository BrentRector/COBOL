## Entry 1903 — 2026-10-07 00:55 PDT — Owner: all work, appropriate models, up to 85 % weekly, then pause; wave 1026 dispatched (PB2115, PB2116, PB2117, PB2120) beside wave 1025

**The instruction.** "You can run all work using the appropriate models up to an 85% quota use. Then pause." (kb/Work R69 §6).
It lifts the 44 % cap on other work and supersedes this week's 90 % design allowance: every lane runs with the model its role
and `model_rules.json` assign until the weekly meter reads 85 %, then dispatching stops and in-flight agents land. The constant
the tooling reads, `model_rules.json` `quota.weekly_cap_pct`, is 85 with this source (it was 98 from 2026-10-02), so the
orchestrator loop and `budget.py` enforce it; Mythos and Fable keep the per-dispatch approval rule. Minutes later: "Do not,
however, exceed session limits as that would abort jobs and waste quota" — the 5-hour session window is a hard constraint: a
dispatch is made only when the session meter plus the wave's projected session cost stays under the 70 % soft stop, the STOP
file goes down at the soft stop so in-flight agents checkpoint and land, and both meters are read before every dispatch and
after every completion. At this dispatch the session meter read 9 % (window resets 01:00 PDT) and the weekly 57 %; two waves
of two and four Opus implementers plus two landers project to about 20 session points on the 5.0 ratio.

**Wave 1026, by hand, beside wave 1025.** The four items the machinery cannot plan (Entry 1901): C PB2115 (the Roslyn census;
findings go to a structured file, notes are filed later by a clerk with allocated ids), D PB2116 (the oracle capture, with one
design correction written into DESIGN-architecture-review §4 — the recorded oracle is a committed manifest of content hashes,
the emitted-C# blobs stay gitignored), E PB2117 (the performance baseline, A6's instrument, GnuCOBOL under WSL as the external
comparison, no wall-clock assertions), F PB2120 (a named cluster as a plannable unit; the public-skill paragraph comes back in
the report for the attended session to carry over, because a worktree agent never pushes the public submodule). Concurrency 4
so the two waves together stay inside the six-implementer budget; one train of four (`min_final_train` 2). Rendered by
`make_dispatch_specs.py` from `E:\COBOL-coord\scratch\groups-w1026.json`, `check_practices.py` GREEN, stall watchdog beside it.

**What follows.** When waves 1025 and 1026 have landed: the archive tag on main's last pre-delete commit; PB2111–PB2114 as the
next retirement wave; then `orchestrate.ps1` runs the fix lane and the external-repository slices PB2097–PB2104 until the cap.
The loop is not started while hand waves run, because its planner reads branches and worktrees and would try to land or resume
a running wave's branches.

