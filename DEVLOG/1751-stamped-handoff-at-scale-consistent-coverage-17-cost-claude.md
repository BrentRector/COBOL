## Entry 1751 — 2026-09-28 10:42 PDT — Stamped handoff at scale: consistent coverage, −17 % cost; CLAUDE.md rule 4 forbids wrappers everywhere

**The scale A/B (PB1698).**
- Record: `docs/rearchitecture/evidence/fleet-optimization/2026-09-28-stamped-handoff-scale.md`, with its raw
  `.json` and its prep, run and analysis scripts.
- Design: 11 real finished branches, each in a CURRENT variant (the real STATUS.md) and a STALE variant (rewritten as
  it read `k` commits earlier), × stamped/unstamped × 2 replicates = 88 fresh Opus resumers. Statistics are paired by
  scenario.
- **Accuracy:** unstamped agents' coverage judgment disagreed with the truth in 11 of 44 cases (1 true miss, 10
  over-reports of loosely covered commits). Stamped agents disagreed in 0 of 44 (Fisher p = 0.0005).
- **Cost, overall:** turns ×0.83 (95 % CI 0.71–0.96, Wilcoxon p = 0.02) and tokens ×0.83 (0.68–1.00).
- **Cost when CURRENT:** tokens ×0.69 (0.57–0.84, p = 0.01) and wall time ×0.68.
- **Cost when STALE:** no significant saving (tokens ×0.94, CI 0.71–1.23). The pilot's 33 % on one branch did not
  replicate. The write-up says so, and public claims will use these numbers.
- **Incidental finding:** 3 of 14 FINISHED branches (21 %) ended with a STATUS.md that did not describe their own
  last commit, although nothing crashed. Candidate next practice: rewrite STATUS.md after every commit, enforced by a
  check.

**CLAUDE.md rule 4.** It now forbids wrappers, shims, forwarders, aliases and compatibility layers anywhere
(compilers, runtime, tests, scripts, tooling, skills). The owner, correcting the PB1699 plan's shim scripts: "The
'no wrappers' guidance also applies to all source code, including the compilers. We have no back compatibility
requirements. I've provided this guidance previously." The guidance lived only in orchestrator memory, where the
agents that write compiler code never saw it and the orchestrator itself overlooked it today. PB1699 was redirected:
the `scripts/spec/` copies are deleted, callers use the public scripts directly, and the project's settings live in
one auto-discovered `.agent-fleet.json`.

**Transparency.** Entry 1750's header time (10:45) was estimated rather than read from `date`; it was written
at about 10:40.

