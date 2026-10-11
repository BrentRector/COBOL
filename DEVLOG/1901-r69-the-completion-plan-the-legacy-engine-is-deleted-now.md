## Entry 1901 — 2026-10-06 23:40 PDT — R69, the completion plan: the legacy engine is deleted NOW (decoupled from v1.0), the architecture review's start is SPLIT, dead-code removal is mandated, Mythos follows the Fable rule; fourteen register notes, the design, the plan and the skills updated

**The owner's question.** "I now have access to the Mythos 5.1 model. How can I or does it make sense to use it to build a
plan to complete the COBOL compiler", including re-architecture, code generation, layout, god classes and "similar solid
engineering cleanup for a decades-supported, production compiler", with the cheaper model preferred wherever it does the job.
The answer, after reading plan §0, `DESIGN-architecture-review.md`, PB1754 and the quota memories: the plan already exists in
three pieces (the fix lane in `kb/Work`, the review's R0–R5 design, the approved §8.13 external-repository design); what was
missing was two artifacts, one ordering decision and a rule for when a frontier model is worth 2.5× Opus.

**The legacy engine, measured (session on Mythos 5.1, main `e36c9c9da`).** The owner: "I'm not convinced the legacy engine
provides any real value anymore." Measured rather than asserted: its only build edge into the greenfield tree is the two
`ProjectReference`s in `tests/Cobol.Net.Tests.Conformance.csproj` (the `LegacyCompiler` oracle, used only under
`COBOLNET_DIFF_MODE=verify`) plus an `InternalsVisibleTo` in `Cobol.Net.Frontend.csproj`; no `src/Cobol.Net.*` source names a
legacy type. The one purpose it was kept for (Entry 997, 2026-07-22: the P14 guard-equivalence proof) dissolved on 2026-09-06
when PB750 pointed the guard at the greenfield compiler; `scripts/equivalence-proof.sh` and `EQUIVALENCE-PROOF.md` were never
created. The nightly bake-verify job (364 goldens against a live legacy run) was green on every recent night and can only report
that a spec-derived golden differs from the frozen engine. Its unit and integration suites (1,199 + 504 tests, 32 s) run in CI's
Guard job and every Linux gate and caught zero product defects since July; they cost a dropped train (PB655) and a CI red (the
legacy-divergent exemption on PB322's rows), and PB1861 exists only because they exist. It blocks D10 (the SUBSCRIPT lexer mode,
~250 lines of hand-rolled re-parsers), forces the shared grammar additive-only, and is named by 14 greenfield test and script
files, 6 guard scripts and 27 docs. The §11 gates P15 names as preconditions do not need it (A3 landed; A4 is the GnuCOBOL
corpus; A5 never mentioned it; A6's benchmark is better served by GnuCOBOL and a self-relative baseline).

**Owner decisions (kb/Work R69, never re-ask).** (1) "Yes, delete the legacy now decoupled from v1.0": P15's Cuts 1, 2 and
2.5 run now as cluster `PB2108` — PB2108 Cut 1a (the conformance project's two references, the oracle, the bake/verify modes,
the nightly CI job), PB2109 Cut 1b (the guard scripts' legacy legs, `GUARD_COMPILER=legacy`, the LEGACY_DIVERGENT exemption,
GreenfieldOnly, the three P15-slated scripts), PB2110 Cut 2 (the archive tag, the five project deletions, the solution
entries, `InternalsVisibleTo`, the gate arm, the drift-test references; PB1861 retires as moot), PB2111 the prose and register
sweep, PB2112 `CobolSharp.sln` → `Cobol.Net.sln` with every caller, PB2113 D10, PB2114 the grammar unification the additive-only
caution forced. v1.0 keeps Cut 3 and the §4.2.16 documentation; D14 and the P15 section are amended, Entry 997's "keep
engine" is superseded. (2) R64 is SPLIT: R0 (PB2115 Roslyn census · PB2116 oracle capture · PB2117 performance baseline, which
is A6's instrument) and R1 (PB2118, the target architecture) start now; the Delete program (PB2119) and R3 leaf waves run
between fix-lane trains in partitioned subsystems; R2/R3 over binding and code generation wait for GAP near zero. Why: the 173
remaining GAP rows land in the god classes (48 in §14.9, 22 in §13.18, 14 in §12.3), so every fix landed before the split
enlarges the extraction, and R1 lets the external-repository slices and the remaining fixes be written into the target layout.
(3) "Included in the plan must be removal and deletion of dead/obsolete code": Delete is the review's fifth wave kind,
census-measured and caller-complete; PB2119 is the program. (4) Models: Mythos 5.1 is Fable-tier and follows the Fable rule
(explicit approval per dispatch, MANDATORY-PRACTICES P1); its one planned use is R1's authorship, with an Opus refuter. Opus
takes the retirement cuts (the CI invariant), R0 tooling, refuters, landers, extract/unify; Sonnet takes census runs, sweeps,
renames, analyzer waves and measured Delete waves. `model_rules.json` routes `^build/(ci|legacy)` and `^architecture` to Opus
and prices Fable and Mythos at 160,000 tokens per weekly point (2.5× Opus, the published price ratio); `coord.family()` knows
`mythos`; `test_budget.py` checks the rate.

**Dispatch mechanics, measured.** `work.py next`'s `actionable()` selects harm-flagged defects; `.agent-fleet.json` skips
`process_only` and clusters by code sites under `src/Cobol.Net.*`; every R69 item is `process_only: true` with sites in
`tests/`, `scripts/`, `.github/`, `docs/` or `src/CobolSharp.*`. So the two clusters are dispatched with a hand-written
`groups.json` through the `workstream` skill until PB2120 (a named cluster as a plannable unit: `work.py next --cluster`,
`plan_wave.py --cluster`, `orchestrate.ps1 -Cluster`) lands. Ids PB2108–PB2120 and this entry number came from `alloc.py`.

**Written.** `kb/Work`: R69, PB2108–PB2120 (14 notes), PB1754 (`owner` → `open`, the split recorded), A4, A6, PB1861, PB1699
(pin 1.19.0). Plan: a new §0 live-state bullet; the P15 phase-table row, D14, the PHASE-15 GOAL, STATUS and §11-gates
paragraph, the shared-grammar caution. `DESIGN-architecture-review.md`: status, §1 scope, §2 the split start, §3 R0's
dead-artifact census and instruments, R1's author, the Delete wave kind, "Who does what", §6 the split-start risk, §7 the
decisions. Skills: `.claude/skills/architecture-review` rewritten over `brent-tools:architecture-audit` **1.19.0**
(`tools/claude-skills` main: the Delete wave kind, the dead-artifact census, the split start, model tiers, two anti-patterns,
README and plugin versions; `lint_skills.py` no longer reads `__pycache__` bytecode as reference text). `scripts/hooks/forbidden_commands.py`: a path registered in `.gitmodules` is another
repository, so a push of the skills submodule's `main` is no longer refused as a push to THIS repo's main (the folder-name
rule caught it because the path contains `COBOL`; three self-test cases); the 1.19.0 push itself went through the standalone
clone `E:/claude-skills`, which the rule already treated as another repository. MANDATORY-PRACTICES P1
carries the Mythos sentence. Memory: a new START HERE note.

**Not done, deliberately.** No compiler code changed and nothing was dispatched: this session ran on Mythos, the weekly meter
read 52 % on 2026-10-05 evening against a day-3 target of 42.9 %, and the 90 % allowance covers the external-repository design
and implementation only. The checks run before this commit were CI's audits job (citations, evidence supersession, witness
loss, drift rules, rule catalog, publishable, `cite.py --self-test`, `work.py check`, the hook tests, `check_practices.py`,
`test_dispatch_guard.py`) and the orchestrator's Python tests; the whole-population gate was not run because no test
population changed (CI's `changes` job decides the matrix from the paths). Whether the retirement may run under the 90 %
allowance this week is the one question left to the owner.

